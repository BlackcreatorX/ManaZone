using UnityEngine;

public enum EnemyAIState { Idle, Approach, Reposition, EvaluateCombat, PrepareAttack, Attack, Defend, Recover, Stunned, Dead }

/// <summary>Shared arena-combat brain. Spell execution and resource accounting stay in SpellCaster/ManaSystem.</summary>
[RequireComponent(typeof(CharacterController))]
public sealed class EnemyCombatAI : MonoBehaviour, ITimedStunnable
{
    [Header("Setup")]
    [SerializeField] private Transform player;
    [SerializeField] private EnemyAIPersonality personality;
    [SerializeField] private EnemyAIPattern[] patterns;
    [SerializeField] private SpellCaster spellCaster;
    [SerializeField] private ManaSystem mana;
    [SerializeField] private HPSystem health;
    [Header("Ground navigation")]
    [SerializeField] private LayerMask groundLayers = 1 << 3;
    [SerializeField, Min(0.1f)] private float groundProbeDistance = 1.25f;
    [SerializeField] private LayerMask obstacleLayers = ~(1 << 3);
    [SerializeField, Min(0f)] private float wallClearance = 1.2f;
    [Header("Fairness and movement")]
    [SerializeField, Min(0f)] private float stunDuration = 1.2f;
    [SerializeField, Range(0f, 1f)] private float defensiveResponseChance = 0.45f;
    [SerializeField] private DefensiveSpellDefinition defensiveResponse;
    [SerializeField, Min(0.1f)] private float counterObservationWindow = 1.75f;
    [SerializeField, Min(0f)] private float attackAccuracy = 0.9f;
    [SerializeField] private bool debugState;

    private CharacterController controller;
    private EnemyAIState state;
    private EnemyAIPattern selectedPattern;
    private int actionIndex;
    private float stateTimer, decisionTimer, stunTimer;
    private float dodgeRemaining, strafeDirectionTimer;
    private int completedPatternCount;
    private int strafeDirection = 1;
    private bool observedPlayerAttack;
    private float counterObservationRemaining;
    private bool continuePatternAfterPause;
    private string selectionReason = "Waiting";
    private StunFlipEffect stunFlipEffect;

    public EnemyAIState State => state;
    public string CurrentPattern => selectedPattern != null ? selectedPattern.name : "None";
    public string CurrentAction => selectedPattern != null && selectedPattern.Actions != null && actionIndex < selectedPattern.Actions.Length && selectedPattern.Actions[actionIndex] != null ? selectedPattern.Actions[actionIndex].DisplayName : "None";
    public string SelectionReason => selectionReason;
    public float RecoveryRemaining => state == EnemyAIState.Recover ? stateTimer : 0f;
    private float EffectiveReactionTime => Mathf.Max(0.15f, personality.ReactionTime / Mathf.Max(0.5f, personality.Aggression));
    private float EffectiveSelectionInterval => Mathf.Max(0.1f, personality.SelectionInterval / Mathf.Max(0.5f, personality.Aggression));

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (spellCaster == null) spellCaster = GetComponent<SpellCaster>();
        if (mana == null) mana = GetComponent<ManaSystem>();
        if (health == null) health = GetComponent<HPSystem>();
        if (groundLayers.value == 0)
        {
            int floorLayer = LayerMask.NameToLayer("Suelo");
            if (floorLayer >= 0) groundLayers = 1 << floorLayer;
        }
        if (groundLayers.value == 0)
            Debug.LogWarning("EnemyCombatAI no tiene Ground Layers configurado. Asigna la layer 'Suelo'.", this);
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }
        state = EnemyAIState.Idle;
        // Avoid every enemy opening its combat movement with the same rightward strafe.
        strafeDirection = Random.value < 0.5f ? -1 : 1;
        strafeDirectionTimer = personality != null
            ? personality.StrafeDirectionInterval * Random.Range(0.85f, 1.25f)
            : 3.5f;
        if (spellCaster == null || personality == null || patterns == null || patterns.Length == 0)
            Debug.LogWarning("EnemyCombatAI requiere SpellCaster, personalidad y al menos un patrón asignados.", this);
    }

    private void Update()
    {
        if (health != null && health.CurrentHealth <= 0f) { SetState(EnemyAIState.Dead); return; }
        if (state == EnemyAIState.Dead) return;
        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            SetState(EnemyAIState.Stunned);
            if (stunTimer <= 0f) { selectedPattern = null; SetState(EnemyAIState.EvaluateCombat); }
            return;
        }
        if (player == null || personality == null || spellCaster == null) { SetState(EnemyAIState.Idle); return; }

        decisionTimer -= Time.deltaTime;
        if (observedPlayerAttack)
        {
            counterObservationRemaining -= Time.deltaTime;
            if (counterObservationRemaining <= 0f)
                observedPlayerAttack = false;
        }
        if (state == EnemyAIState.Idle || state == EnemyAIState.Approach || state == EnemyAIState.Reposition)
            FacePlayer();

        switch (state)
        {
            case EnemyAIState.Idle:
                if (decisionTimer <= 0f) { decisionTimer = EffectiveSelectionInterval; SetState(EnemyAIState.EvaluateCombat); }
                break;
            case EnemyAIState.Approach:
            case EnemyAIState.Reposition:
                MoveToAttackRange();
                if (decisionTimer <= 0f) { decisionTimer = EffectiveSelectionInterval; SetState(EnemyAIState.EvaluateCombat); }
                break;
            case EnemyAIState.EvaluateCombat:
                if (decisionTimer <= 0f) Evaluate();
                break;
            case EnemyAIState.PrepareAttack:
                FacePlayer();
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f) { actionIndex = 0; SetState(EnemyAIState.Attack); }
                break;
            case EnemyAIState.Attack:
                ExecuteAction();
                break;
            case EnemyAIState.Recover:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    if (continuePatternAfterPause && selectedPattern != null)
                    {
                        continuePatternAfterPause = false;
                        SetState(EnemyAIState.Attack);
                    }
                    else
                    {
                        selectedPattern = null;
                        decisionTimer = EffectiveReactionTime;
                        SetState(EnemyAIState.Idle);
                    }
                }
                break;
            case EnemyAIState.Defend:
                FacePlayer();
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f) ExecuteAction();
                break;
        }

        if (state != EnemyAIState.Stunned && state != EnemyAIState.Dead
            && state != EnemyAIState.Approach && state != EnemyAIState.Reposition)
            MoveDuringCombat();
    }

    public static void NotifyPlayerSpellCast(Transform attacker)
    {
        if (attacker == null)
            return;

        EnemyCombatAI[] enemies = FindObjectsOfType<EnemyCombatAI>();
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyCombatAI enemy = enemies[i];
            if (enemy.player != null && enemy.player.root == attacker.root)
                enemy.ObservePlayerAttack();
        }
    }

    public void ObservePlayerAttack()
    {
        if (state == EnemyAIState.Stunned || state == EnemyAIState.Dead)
            return;

        if (defensiveResponseChance <= 0f
            || (defensiveResponseChance < 1f && Random.value >= defensiveResponseChance))
            return;

        if (defensiveResponse != null && spellCaster != null
            && (spellCaster.IsPersistentActive(defensiveResponse)
                || (spellCaster.CanCastSpell(defensiveResponse) && spellCaster.TryCastSpell(defensiveResponse))))
        {
            FacePlayer();
            return;
        }

        observedPlayerAttack = true;
        counterObservationRemaining = Mathf.Max(0.1f, counterObservationWindow);
        if (state == EnemyAIState.Idle || state == EnemyAIState.Recover
            || state == EnemyAIState.EvaluateCombat || state == EnemyAIState.Approach
            || state == EnemyAIState.Reposition || state == EnemyAIState.PrepareAttack)
        {
            selectedPattern = null;
            decisionTimer = 0f;
            SetState(EnemyAIState.EvaluateCombat);
        }
    }

    public void Stun()
    {
        Stun(stunDuration);
    }

    public void Stun(float duration)
    {
        Stun(duration, true);
    }

    public void Stun(float duration, bool flipVisual)
    {
        if (state == EnemyAIState.Dead) return;
        stunTimer = Mathf.Max(stunTimer, duration);
        if (flipVisual)
        {
            if (stunFlipEffect == null)
                stunFlipEffect = GetComponent<StunFlipEffect>() ?? gameObject.AddComponent<StunFlipEffect>();
            stunFlipEffect.Apply(stunTimer);
        }
        selectedPattern = null;
        SetState(EnemyAIState.Stunned);
    }

    private void Evaluate()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (patterns == null) { SetState(EnemyAIState.Idle); return; }
        EnemyAIPattern best = null;
        EnemyAIPattern bestCounter = null;
        float total = 0f;
        float counterTotal = 0f;
        for (int i = 0; i < patterns.Length; i++)
        {
            EnemyAIPattern p = patterns[i];
            if (p == null || distance < p.MinDistance || distance > p.MaxDistance) continue;
            if (p.Kind == EnemyPatternKind.Counter && !observedPlayerAttack) continue;
            bool canExecute = p.Actions != null && p.Actions.Length > 0;
            float cost = 0f;
            for (int a = 0; canExecute && a < p.Actions.Length; a++)
            {
                SpellDefinition spell = p.Actions[a];
                if (spell == null || !spellCaster.CanCastSpell(spell)) { canExecute = false; break; }
                cost += spell.IsPersistent ? 0f : spell.ManaCost;
            }
            if (!canExecute || (mana != null && mana.CurrentMana < cost)) continue;
            float weight = p.SelectionWeight * personality.Preference(p.Kind);
            if (p.Kind == EnemyPatternKind.Harassment) weight *= personality.Aggression;
            else if (p.Kind == EnemyPatternKind.Counter) weight *= 1f / Mathf.Max(0.5f, personality.Aggression);
            if (mana != null && mana.MaxMana > 0f)
                weight *= Mathf.Clamp01(1f / (1f + cost / mana.MaxMana / personality.ManaSpending));
            HPSystem playerHealth = player.GetComponentInParent<HPSystem>();
            if (playerHealth != null && playerHealth.MaxHealth > 0f)
                weight *= 1f + p.LowPlayerHealthBonus * (1f - playerHealth.CurrentHealth / playerHealth.MaxHealth);
            if (weight <= 0f) continue;
            if (p.Kind == EnemyPatternKind.Counter)
            {
                counterTotal += weight;
                if (Random.value * counterTotal < weight) bestCounter = p;
            }
            else
            {
                total += weight;
                if (Random.value * total < weight) best = p;
            }
        }
        if (observedPlayerAttack && bestCounter != null)
        {
            best = bestCounter;
            total += counterTotal;
        }
        if (total <= 0f)
        {
            selectedPattern = null;
            int rangeCorrection = GetAttackRangeCorrection(distance);
            if (rangeCorrection > 0) SetState(EnemyAIState.Approach);
            else if (rangeCorrection < 0) SetState(EnemyAIState.Reposition);
            else SetState(EnemyAIState.Idle);
            decisionTimer = EffectiveSelectionInterval;
            selectionReason = rangeCorrection == 0
                ? "No available pattern in current attack range"
                : "Moving into an attack pattern's range";
            return;
        }
        if (best == null)
        {
            selectedPattern = null;
            SetState(EnemyAIState.Idle);
            return;
        }

        selectedPattern = best;
        actionIndex = 0;
        if (best.Kind == EnemyPatternKind.Counter)
        {
            observedPlayerAttack = false;
            counterObservationRemaining = 0f;
        }
        selectionReason = best.Kind == EnemyPatternKind.Counter
            ? "Reactive counter to the player's spell"
            : "Weighted by valid pattern, personality preference, and aggression";
        bool isParry = best.Actions[0] is DefensiveSpellDefinition defensiveSpell
            && defensiveSpell.Mode == DefensiveMode.Parry;
        stateTimer = best.PreparationTime + (isParry ? 0f : EffectiveReactionTime);
        SetState(best.Actions[0] is DefensiveSpellDefinition ? EnemyAIState.Defend : EnemyAIState.PrepareAttack);
    }

    private void ExecuteAction()
    {
        if (selectedPattern == null || selectedPattern.Actions == null || actionIndex >= selectedPattern.Actions.Length)
        { SetState(EnemyAIState.Recover); stateTimer = selectedPattern != null ? selectedPattern.RecoveryTime : 0.5f; return; }
        FacePlayer();
        SpellDefinition action = selectedPattern.Actions[actionIndex];
        if (!(action is DefensiveSpellDefinition) && Random.value > attackAccuracy)
        {
            actionIndex++;
            continuePatternAfterPause = actionIndex < selectedPattern.Actions.Length;
            if (!continuePatternAfterPause) RegisterCompletedPattern();
            stateTimer = continuePatternAfterPause ? selectedPattern.PauseBetweenActions : selectedPattern.RecoveryTime;
            SetState(EnemyAIState.Recover);
            return;
        }
        // Enemy patterns always execute charged spells at full charge.
        if (!spellCaster.TryCastSpell(action, 1f))
        {
            selectedPattern = null;
            SetState(EnemyAIState.Recover);
            stateTimer = 0.5f;
            return;
        }
        actionIndex++;
        if (actionIndex >= selectedPattern.Actions.Length)
        {
            RegisterCompletedPattern();
            SetState(EnemyAIState.Recover);
            stateTimer = selectedPattern.RecoveryTime;
        }
        else
        {
            // The inter-action pause is handled as a short recovery before the next action.
            stateTimer = selectedPattern.PauseBetweenActions;
            continuePatternAfterPause = true;
            SetState(EnemyAIState.Recover);
        }
    }

    // Returns 1 when too far, -1 when too close, and 0 when inside any pattern range.
    private int GetAttackRangeCorrection(float distance)
    {
        if (patterns == null)
            return 0;

        float nearestGap = float.PositiveInfinity;
        int correction = 0;
        for (int i = 0; i < patterns.Length; i++)
        {
            EnemyAIPattern pattern = patterns[i];
            if (pattern == null)
                continue;
            if (distance >= pattern.MinDistance && distance <= pattern.MaxDistance)
                return 0;

            float gap = distance < pattern.MinDistance
                ? pattern.MinDistance - distance
                : distance - pattern.MaxDistance;
            if (gap >= nearestGap)
                continue;

            nearestGap = gap;
            correction = distance < pattern.MinDistance ? -1 : 1;
        }
        return correction;
    }

    private void MoveToAttackRange()
    {
        if (player == null || personality == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);
        int correction = GetAttackRangeCorrection(distance);
        if (correction == 0)
        {
            SetState(EnemyAIState.EvaluateCombat);
            return;
        }

        Vector3 direction = (player.position - transform.position).normalized * correction;
        MoveWithGroundCheck(direction, personality.MovementSpeed);
    }

    private void RegisterCompletedPattern()
    {
        completedPatternCount++;
        int dodgeEvery = Mathf.Max(1, personality.DodgeEveryPatterns);
        if (completedPatternCount % dodgeEvery == 0)
        {
            dodgeRemaining = personality.DodgeDuration;
        }
    }

    private void MoveDuringCombat()
    {
        if (player == null || personality == null || controller == null) return;

        Vector3 towardPlayer = player.position - transform.position;
        towardPlayer.y = 0f;
        if (towardPlayer.sqrMagnitude < 0.001f) return;
        towardPlayer.Normalize();
        Vector3 lateral = Vector3.Cross(Vector3.up, towardPlayer) * strafeDirection;
        Vector3 movement;
        float speed;
        if (dodgeRemaining > 0f)
        {
            dodgeRemaining = Mathf.Max(0f, dodgeRemaining - Time.deltaTime);
            movement = lateral - towardPlayer * 0.45f;
            speed = personality.CombatStrafeSpeed * personality.DodgeSpeedMultiplier;
        }
        else
        {
            strafeDirectionTimer -= Time.deltaTime;
            if (strafeDirectionTimer <= 0f)
            {
                strafeDirection = -strafeDirection;
                strafeDirectionTimer = personality.StrafeDirectionInterval * Random.Range(0.85f, 1.25f);
            }
            movement = lateral;
            speed = personality.CombatStrafeSpeed;
        }

        if (movement.sqrMagnitude > 0.001f)
            MoveWithGroundCheck(movement, speed);
    }

    private void MoveWithGroundCheck(Vector3 direction, float speed)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f || groundLayers.value == 0) return;
        direction.Normalize();

        Vector3 chosenDirection = direction;
        if (!CanMoveDirection(chosenDirection, speed))
        {
            bool found = false;
            float bestAlignment = float.NegativeInfinity;
            // If the direct route leaves the floor, try nearby directions and slide along its edge.
            for (int i = 0; i < 7; i++)
            {
                float angle;
                switch (i)
                {
                    case 0: angle = 45f; break;
                    case 1: angle = -45f; break;
                    case 2: angle = 90f; break;
                    case 3: angle = -90f; break;
                    case 4: angle = 135f; break;
                    case 5: angle = -135f; break;
                    default: angle = 180f; break;
                }
                Vector3 candidate = Quaternion.Euler(0f, angle, 0f) * direction;
                if (!CanMoveDirection(candidate, speed)) continue;
                float alignment = Vector3.Dot(direction, candidate);
                if (alignment > bestAlignment)
                {
                    bestAlignment = alignment;
                    chosenDirection = candidate;
                    found = true;
                }
            }

            if (!found) return;
        }

        controller.Move(chosenDirection * speed * Time.deltaTime);
    }

    private bool CanMoveDirection(Vector3 direction, float speed)
    {
        return HasGroundAtStep(direction, speed) && HasWallClearance(direction, speed);
    }

    private bool HasWallClearance(Vector3 direction, float speed)
    {
        if (obstacleLayers.value == 0 || wallClearance <= 0f) return true;

        Vector3 origin = controller.bounds.center;
        float radius = Mathf.Max(0.1f, controller.radius * 0.8f);
        float distance = wallClearance + speed * Time.deltaTime;
        if (!Physics.SphereCast(origin, radius, direction.normalized, out RaycastHit hit, distance,
                obstacleLayers, QueryTriggerInteraction.Ignore))
            return true;

        // The player is a combat target, not a wall. Let the CharacterController handle contact range.
        return player != null && hit.transform.root == player.root;
    }

    private bool HasGroundAtStep(Vector3 direction, float speed)
    {
        Vector3 step = direction.normalized * speed * Time.deltaTime;
        Vector3 feet = transform.position + step + controller.center - Vector3.up * (controller.height * 0.5f);
        Vector3 rayOrigin = feet + Vector3.up * 0.25f;
        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundProbeDistance,
                groundLayers, QueryTriggerInteraction.Ignore))
            return false;

        return Vector3.Angle(hit.normal, Vector3.up) <= controller.slopeLimit;
    }

    private void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 8f * Time.deltaTime);
    }

    private void SetState(EnemyAIState newState)
    {
        if (state == newState) return;
        state = newState;
        if (debugState) Debug.Log($"{name} AI: {state} | {CurrentPattern} | {CurrentAction} | mana {spellCaster?.CurrentMana:F0} | {selectionReason}", this);
    }

}
