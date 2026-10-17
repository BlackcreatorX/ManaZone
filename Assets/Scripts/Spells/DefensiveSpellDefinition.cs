using UnityEngine;

public enum DefensiveMode
{
    Shield,
    Parry
}

[CreateAssetMenu(menuName = "Mana Zone/Spells/Defensive Spell")]
public sealed class DefensiveSpellDefinition : SpellDefinition
{
    [SerializeField] private DefensiveMode mode = DefensiveMode.Shield;
    [SerializeField] private GameObject shieldPrefab;
    [SerializeField] private GameObject parryPrefab;
    [SerializeField] private string parryDisplayName = "Parry";
    [SerializeField, Min(0f)] private float parryWindow = 0.2f;
    [SerializeField, Min(0f)] private float manaDrainPerSecond = 15f;
    [SerializeField, Min(0f)] private float manaRecovery = 10f;

    public override bool IsPersistent => mode == DefensiveMode.Shield;

    public override string DisplayName => mode == DefensiveMode.Shield ? BaseDisplayName : parryDisplayName;

    public override float ManaCost => mode == DefensiveMode.Parry ? BaseManaCost : 0f;

    public override bool Cast(SpellCastContext context)
    {
        if (mode == DefensiveMode.Shield)
        {
            if (shieldPrefab == null || context.Mana == null || context.Mana.CurrentMana <= 0f)
                return false;

            GameObject shieldObject = context.Caster.CreatePersistentEffect(this, shieldPrefab);
            if (shieldObject == null)
                return false;

            ShieldEffect shield = shieldObject.GetComponent<ShieldEffect>();
            if (shield == null)
            {
                Debug.LogError("Shield prefab must contain ShieldEffect.", shieldObject);
                Destroy(shieldObject);
                return false;
            }

            shield.Activate(context.Mana, manaDrainPerSecond, manaRecovery);
            return true;
        }

        if (parryPrefab == null)
            return false;

        GameObject parryObject = context.Caster.CreateTransientAttachedEffect(parryPrefab);
        if (parryObject == null)
            return false;

        ParryEffect parry = parryObject.GetComponent<ParryEffect>();
        if (parry == null)
        {
            Debug.LogError("Parry prefab must contain ParryEffect.", parryObject);
            Destroy(parryObject);
            return false;
        }

        parry.Activate(parryWindow, context.Mana, manaRecovery);
        return true;
    }
}
