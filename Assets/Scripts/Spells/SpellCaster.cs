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

    private readonly List<InputAction> selectActions = new List<InputAction>();
    private readonly Dictionary<SpellDefinition, float> cooldowns = new Dictionary<SpellDefinition, float>();
    private InputAction castAction;
    private TPSController playerMovement;
    private int selectedSpellIndex;
    private SpellDefinition activePersistentDefinition;
    private GameObject activePersistentEffect;

    private void Awake()
    {
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
            return;

        int availableSlots = Mathf.Min(selectActions.Count, equippedSpells.Length);
        for (int i = 0; i < availableSlots; i++)
        {
            if (selectActions[i].WasPressedThisFrame())
            {
                SelectSpell(i);
                break;
            }
        }

        if (castAction != null && castAction.WasPressedThisFrame())
            CastSelectedSpell();
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

    /// <summary>Shared casting entry point for players and AI. Input remains owned by this component.</summary>
    public bool TryCastSpell(SpellDefinition spell)
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

        float manaCost = spell.IsPersistent ? 0f : spell.ManaCost;
        if (manaSystem == null || !manaSystem.TrySpendMana(manaCost))
            return false;

        SpellCastContext context = new SpellCastContext(this, transform, castPoint, manaSystem);
        if (!spell.Cast(context))
        {
            manaSystem.RestoreMana(manaCost);
            return false;
        }

        cooldowns[spell] = spell.Cooldown;
        return true;
    }

    public bool CanCastSpell(SpellDefinition spell)
    {
        if (spell == null || manaSystem == null || !IsEquipped(spell))
            return false;
        if (spell.IsPersistent && activePersistentDefinition == spell && activePersistentEffect != null)
            return true;
        return (!cooldowns.TryGetValue(spell, out float remaining) || remaining <= 0f)
            && manaSystem.CurrentMana >= (spell.IsPersistent ? 0f : spell.ManaCost);
    }

    public float CurrentMana => manaSystem != null ? manaSystem.CurrentMana : 0f;

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
        if (activePersistentEffect != null)
            Destroy(activePersistentEffect);
    }
}
