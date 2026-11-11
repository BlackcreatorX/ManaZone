using UnityEngine;

public enum RoguelikeUpgradeStat
{
    // Mejoras de jugador (categoría Utilidad)
    ManaRegeneration,
    MaxMana,
    MaxHealth,
    MoveSpeed,
    // Mejoras de ataque
    ManaCost,
    Damage,
    Cooldown,
    ControlDuration
}

public enum RoguelikeUpgradeCategory
{
    Utilidad,
    Proyectil,
    ControlCC,
    Defensiva,
    Cargada
}

[CreateAssetMenu(menuName = "Mana Zone/Roguelike/Upgrade")]
public sealed class RoguelikeUpgradeDefinition : ScriptableObject
{
    [SerializeField] private string displayName = "Mejora";
    [TextArea, SerializeField] private string description;
    [SerializeField] private RoguelikeUpgradeStat stat;
    [SerializeField] private RoguelikeUpgradeCategory category;
    [Tooltip("Multiplicador aplicado por cada elección. Ejemplo: 1.2 = +20%, 0.9 = -10%.")]
    [SerializeField, Min(0.1f)] private float multiplier = 1.2f;

    public string DisplayName => displayName;
    public string Description => description;
    public RoguelikeUpgradeStat Stat => stat;
    public RoguelikeUpgradeCategory Category => category;
    public float ManaCostMultiplier => stat == RoguelikeUpgradeStat.ManaCost ? multiplier : 1f;
    public float DamageMultiplier => stat == RoguelikeUpgradeStat.Damage ? multiplier : 1f;
    public float CooldownMultiplier => stat == RoguelikeUpgradeStat.Cooldown ? multiplier : 1f;
    public float ValueMultiplier => multiplier;

    public bool IsCompatibleWith(SpellDefinition spell)
    {
        if (category == RoguelikeUpgradeCategory.Utilidad)
            return true;
        if (spell == null) return false;

        switch (category)
        {
            case RoguelikeUpgradeCategory.Proyectil:
                if (spell is ProjectileSpellDefinition projectileSpell)
                    return !projectileSpell.IsStunAttack;
                return spell is ChargedProjectileSpellDefinition || spell is ChargedBurstSpellDefinition;
            case RoguelikeUpgradeCategory.Cargada:
                return spell is ChargedSpellDefinition;
            case RoguelikeUpgradeCategory.Defensiva:
                return spell is DefensiveSpellDefinition;
            case RoguelikeUpgradeCategory.ControlCC:
                return spell is StunOnlySpellDefinition
                    || (spell is ProjectileSpellDefinition stunProjectile && stunProjectile.IsStunAttack);
            default:
                return false;
        }
    }

    public bool IsStatValidForCategory()
    {
        switch (category)
        {
            case RoguelikeUpgradeCategory.Utilidad:
                return stat == RoguelikeUpgradeStat.ManaRegeneration
                    || stat == RoguelikeUpgradeStat.MaxMana
                    || stat == RoguelikeUpgradeStat.MaxHealth
                    || stat == RoguelikeUpgradeStat.MoveSpeed;
            case RoguelikeUpgradeCategory.Proyectil:
                return stat == RoguelikeUpgradeStat.ManaCost
                    || stat == RoguelikeUpgradeStat.Damage
                    || stat == RoguelikeUpgradeStat.Cooldown;
            case RoguelikeUpgradeCategory.ControlCC:
                return stat == RoguelikeUpgradeStat.ControlDuration
                    || stat == RoguelikeUpgradeStat.ManaCost
                    || stat == RoguelikeUpgradeStat.Cooldown;
            case RoguelikeUpgradeCategory.Defensiva:
            case RoguelikeUpgradeCategory.Cargada:
                return stat == RoguelikeUpgradeStat.ManaCost
                    || stat == RoguelikeUpgradeStat.Damage
                    || stat == RoguelikeUpgradeStat.Cooldown;
            default:
                return false;
        }
    }
}
