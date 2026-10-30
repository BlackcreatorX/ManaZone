using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpellCaster : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string selectActionPrefix = "SelectSpell";
    [SerializeField] private string castActionName = "CastSpell";

    [Header("References")]
    [SerializeField] private Transform castPoint;
    [SerializeField] private ManaSystem manaSystem;

    [Header("Loadout")]
    [SerializeField] private SpellDefinition[] equippedSpells;
    [SerializeField] private TMP_Text spellSelectionText;

    [Header("Spell Visuals")]
    [Tooltip("Visuales asociados por índice a los hechizos equipados. Déjalos apagados en la escena.")]
    [SerializeField] private GameObject[] spellVisuals = new GameObject[3];
    [SerializeField, Min(0.01f)] private float visualStartScale = 0.1f;
    [SerializeField, Min(0f)] private float visualGrowDuration = 0.3f;
    [SerializeField, Min(0f)] private float visualVisibleDuration = 2f;

    private readonly List<InputAction> selectActions = new List<InputAction>();
    private readonly Dictionary<SpellDefinition, float> cooldowns = new Dictionary<SpellDefinition, float>();
    private InputAction castAction;
    private TPSController playerMovement;
    private int selectedSpellIndex;
    private SpellDefinition activePersistentDefinition;
    private GameObject activePersistentEffect;
    private ChargedSpellDefinition chargingSpell;
    private float chargeStartedAt;
    private bool restoreManaRegeneration;
    private GameObject activeSpellVisual;
    private Coroutine visualGrowCoroutine;

    private void Awake()
    {
        HideSpellVisuals();
        if (manaSystem == null)
            manaSystem = GetComponent<ManaSystem>();
        playerMovement = GetComponent<TPSController>();

        BindInputActions();
        UpdateSelectionLabel();

        if (manaSystem == null)
            Debug.LogError("SpellCaster necesita un ManaSystem asignado o en el mismo objeto.", this);
        if (castPoint == null)
            Debug.LogError("SpellCaster necesita un Cast Point para generar proyectiles.", this);
    }

    private void BindInputActions()
    {
        if (inputActions == null)
        {
            // AI-controlled casters use the shared TryCastSpell API without owning player input.
            return;
        }

        InputActionMap map = inputActions.FindActionMap(actionMapName);
        if (map == null)
        {
            Debug.LogError($"No existe el Action Map '{actionMapName}'.", this);
            return;
        }

        foreach (InputAction action in map.actions)
        {
            if (action.name.StartsWith(selectActionPrefix, System.StringComparison.Ordinal))
                selectActions.Add(action);
        }
        selectActions.Sort((left, right) => GetActionIndex(left).CompareTo(GetActionIndex(right)));

        castAction = map.FindAction(castActionName);
    }

    private int GetActionIndex(InputAction action)
    {
        string suffix = action.name.Substring(selectActionPrefix.Length);
        return int.TryParse(suffix, out int index) ? index : int.MaxValue;
    }

    private void OnEnable()
    {
        foreach (InputAction action in selectActions)
            action.Enable();
        castAction?.Enable();
    }

    private void OnDisable()
    {
        CancelCharge();
        foreach (InputAction action in selectActions)
            action.Disable();
        castAction?.Disable();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (equippedSpells == null)
            return;

        foreach (SpellDefinition spell in equippedSpells)
        {
            if (spell != null && cooldowns.TryGetValue(spell, out float remaining) && remaining > 0f)
                cooldowns[spell] = Mathf.Max(0f, remaining - deltaTime);
        }

        // A stun blocks player input for casting while cooldowns continue to elapse.
        if (playerMovement != null && playerMovement.IsStunned)
        {
            CancelCharge();
            return;
        }

        int availableSlots = Mathf.Min(selectActions.Count, equippedSpells.Length);
        for (int i = 0; i < availableSlots; i++)
        {
            if (selectActions[i].WasPressedThisFrame())
            {
                SelectSpell(i);
                break;
            }
        }

        if (castAction != null)
        {
            if (castAction.WasPressedThisFrame())
                BeginOrCastSelectedSpell();
            if (chargingSpell != null && castAction.WasReleasedThisFrame())
                ReleaseChargedSpell();
        }

        DrainChargeMana(deltaTime);
    }

    private void SelectSpell(int index)
    {
        if (index < 0 || index >= equippedSpells.Length || equippedSpells[index] == null)
            return;

        selectedSpellIndex = index;
        UpdateSelectionLabel();
    }

    private void UpdateSelectionLabel()
    {
        if (spellSelectionText == null)
            return;

        SpellDefinition selected = equippedSpells != null && selectedSpellIndex < equippedSpells.Length
            ? equippedSpells[selectedSpellIndex]
            : null;
        spellSelectionText.text = selected != null
            ? $"Hechizo: {selected.DisplayName}"
            : "Sin hechizo equipado";
    }

    private void CastSelectedSpell()
    {
        if (equippedSpells == null || selectedSpellIndex < 0 || selectedSpellIndex >= equippedSpells.Length)
            return;

        SpellDefinition spell = equippedSpells[selectedSpellIndex];
        TryCastSpell(spell);
    }

    private void BeginOrCastSelectedSpell()
    {
        if (equippedSpells == null || selectedSpellIndex < 0 || selectedSpellIndex >= equippedSpells.Length)
            return;

        SpellDefinition spell = equippedSpells[selectedSpellIndex];
        if (spell is ChargedSpellDefinition chargedSpell)
        {
            if (CanCastSpell(chargedSpell))
            {
                chargingSpell = chargedSpell;
                chargeStartedAt = Time.time;
                restoreManaRegeneration = manaSystem.RegenerationEnabled;
                manaSystem.RegenerationEnabled = false;
            }
            return;
        }

        CastSelectedSpell();
    }

    private void ReleaseChargedSpell()
    {
        ChargedSpellDefinition spell = chargingSpell;
        chargingSpell = null;
        manaSystem.RegenerationEnabled = restoreManaRegeneration;
        float charge01 = Mathf.Clamp01((Time.time - chargeStartedAt) / spell.ChargeDuration);
        TryCastSpellInternal(spell, charge01, true);
    }

    private void DrainChargeMana(float deltaTime)
    {
        if (chargingSpell == null || deltaTime <= 0f)
            return;

        float elapsed = Mathf.Min(deltaTime, Mathf.Max(0f, chargingSpell.ChargeDuration - (Time.time - chargeStartedAt)));
        float drain = chargingSpell.ManaDrainPerSecond * elapsed;
        if (elapsed > 0f)
        {
            float amountToSpend = Mathf.Min(drain, manaSystem.CurrentMana);
            if (amountToSpend > 0f)
                manaSystem.TrySpendMana(amountToSpend);

            if (amountToSpend + 0.0001f < drain)
            {
                ReleaseChargedSpell();
                return;
            }
        }

        if (Time.time - chargeStartedAt >= chargingSpell.ChargeDuration)
            ReleaseChargedSpell();
    }

    private void CancelCharge()
    {
        if (chargingSpell == null)
            return;

        chargingSpell = null;
        if (manaSystem != null)
            manaSystem.RegenerationEnabled = restoreManaRegeneration;
    }

    /// <summary>Shared casting entry point for players and AI. Input remains owned by this component.</summary>
    public bool TryCastSpell(SpellDefinition spell, float charge01 = 1f)
    {
        return TryCastSpellInternal(spell, charge01, false);
    }

    private bool TryCastSpellInternal(SpellDefinition spell, float charge01, bool chargeManaAlreadyPaid)
    {
        if (spell == null || !IsEquipped(spell))
            return false;

        if (spell.IsPersistent && activePersistentDefinition == spell && activePersistentEffect != null)
        {
            Destroy(activePersistentEffect);
            activePersistentEffect = null;
            activePersistentDefinition = null;
            return true;
        }

        if (cooldowns.TryGetValue(spell, out float remaining) && remaining > 0f)
            return false;

        float manaCost = spell.IsPersistent || (chargeManaAlreadyPaid && spell is ChargedSpellDefinition) ? 0f : spell.ManaCost;
        if (manaSystem == null || !manaSystem.TrySpendMana(manaCost))
            return false;

        SpellCastContext context = new SpellCastContext(this, transform, castPoint, manaSystem, charge01);
        if (!spell.Cast(context))
        {
            manaSystem.RestoreMana(manaCost);
            return false;
        }

        float cooldown = spell.Cooldown;
        if (playerMovement != null && spell is ProjectileSpellDefinition projectileSpell && projectileSpell.IsBasicAttack)
            cooldown = playerMovement.PlayBasicAttackAnimation(spell.Cooldown);
        else if (playerMovement != null && spell is ProjectileSpellDefinition stunSpell && stunSpell.IsStunAttack)
            playerMovement.PlayStunAttackAnimation();

        if (playerMovement != null && !(spell is DefensiveSpellDefinition))
            EnemyCombatAI.NotifyPlayerSpellCast(transform);

        cooldowns[spell] = cooldown;
        ShowSpellVisual(spell);
        return true;
    }

    private void HideSpellVisuals()
    {
        if (spellVisuals == null)
            return;

        foreach (GameObject visual in spellVisuals)
        {
            if (visual != null)
                visual.SetActive(false);
        }
    }

    private void ShowSpellVisual(SpellDefinition spell)
    {
        if (spellVisuals == null || spell == null)
            return;

        int spellIndex = System.Array.IndexOf(equippedSpells, spell);
        if (spellIndex < 0 || spellIndex >= spellVisuals.Length)
            return;

        GameObject nextVisual = spellVisuals[spellIndex];
        if (visualGrowCoroutine != null)
        {
            StopCoroutine(visualGrowCoroutine);
            visualGrowCoroutine = null;
        }

        // Apaga cualquier visual asignado que haya quedado activo antes de encender el nuevo.
        foreach (GameObject visual in spellVisuals)
        {
            if (visual != null && visual != nextVisual)
                visual.SetActive(false);
        }

        if (activeSpellVisual == nextVisual)
        {
            if (activeSpellVisual != null)
            {
                activeSpellVisual.SetActive(true);
                visualGrowCoroutine = StartCoroutine(HideSpellVisualAfterDelay(activeSpellVisual, visualVisibleDuration));
            }
            return;
        }

        activeSpellVisual = nextVisual;
        if (activeSpellVisual == null)
            return;

        activeSpellVisual.transform.localScale = Vector3.one * visualStartScale;
        activeSpellVisual.SetActive(true);
        visualGrowCoroutine = StartCoroutine(GrowSpellVisual(activeSpellVisual));
    }

    private System.Collections.IEnumerator GrowSpellVisual(GameObject visual)
    {
        Vector3 startScale = Vector3.one * visualStartScale;
        Vector3 targetScale = Vector3.one;
        float elapsed = 0f;

        while (elapsed < visualGrowDuration)
        {
            elapsed += Time.deltaTime;
            float progress = visualGrowDuration > 0f ? Mathf.Clamp01(elapsed / visualGrowDuration) : 1f;
            visual.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            yield return null;
        }

        visual.transform.localScale = targetScale;
        yield return HideSpellVisualAfterDelay(visual, visualVisibleDuration);
    }

    private System.Collections.IEnumerator HideSpellVisualAfterDelay(GameObject visual, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (activeSpellVisual == visual)
        {
            visual.SetActive(false);
            activeSpellVisual = null;
        }
        visualGrowCoroutine = null;
    }

    public bool CanCastSpell(SpellDefinition spell)
    {
        if (spell == null || manaSystem == null || !IsEquipped(spell))
            return false;
        if (spell.IsPersistent && activePersistentDefinition == spell && activePersistentEffect != null)
            return true;
        bool ready = !cooldowns.TryGetValue(spell, out float remaining) || remaining <= 0f;
        if (spell is ChargedSpellDefinition)
            return ready && manaSystem.CurrentMana > 0f;
        return ready && manaSystem.CurrentMana >= (spell.IsPersistent ? 0f : spell.ManaCost);
    }

    public float CurrentMana => manaSystem != null ? manaSystem.CurrentMana : 0f;

    public bool IsPersistentActive(SpellDefinition spell)
    {
        return spell != null && activePersistentDefinition == spell && activePersistentEffect != null;
    }

    private bool IsEquipped(SpellDefinition spell)
    {
        if (equippedSpells == null) return false;
        for (int i = 0; i < equippedSpells.Length; i++)
            if (equippedSpells[i] == spell) return true;
        return false;
    }

    public GameObject CreatePersistentEffect(SpellDefinition definition, GameObject prefab)
    {
        if (definition == null || prefab == null)
            return null;

        if (activePersistentEffect != null)
            Destroy(activePersistentEffect);

        activePersistentDefinition = definition;
        activePersistentEffect = InstantiateAttached(prefab);
        return activePersistentEffect;
    }

    public GameObject CreateTransientAttachedEffect(GameObject prefab)
    {
        return prefab == null ? null : InstantiateAttached(prefab);
    }

    private GameObject InstantiateAttached(GameObject prefab)
    {
        GameObject instance = Instantiate(prefab, transform.position, transform.rotation, transform);
        instance.transform.localPosition = Vector3.up;
        instance.transform.localRotation = Quaternion.identity;
        return instance;
    }

    private void OnDestroy()
    {
        CancelCharge();
        if (activePersistentEffect != null)
            Destroy(activePersistentEffect);
    }

    public bool StartChargedBurst(GameObject projectilePrefab, int count, float interval)
    {
        if (projectilePrefab == null || castPoint == null || count <= 0)
            return false;
        StartCoroutine(FireChargedBurst(projectilePrefab, count, interval));
        return true;
    }

    private System.Collections.IEnumerator FireChargedBurst(GameObject projectilePrefab, int count, float interval)
    {
        for (int i = 0; i < count; i++)
        {
            if (castPoint == null)
                yield break;

            GameObject projectile = Instantiate(projectilePrefab, castPoint.position, castPoint.rotation);
            ILaunchableSpell launchable = projectile.GetComponent<ILaunchableSpell>();
            if (launchable != null)
            {
                if (launchable is IOwnedSpellProjectile ownedProjectile)
                    ownedProjectile.SetOwner(transform);
                launchable.Launch(castPoint.forward);
            }
            else
                Destroy(projectile);

            if (i + 1 < count)
                yield return new WaitForSeconds(interval);
        }
    }
}
