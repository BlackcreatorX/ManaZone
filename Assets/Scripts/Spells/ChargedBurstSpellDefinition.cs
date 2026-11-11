using UnityEngine;

[CreateAssetMenu(menuName = "Mana Zone/Spells/Charged Burst Spell")]
public sealed class ChargedBurstSpellDefinition : ChargedSpellDefinition
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField, Min(1)] private int minimumProjectiles = 3;
    [SerializeField, Min(1)] private int maximumProjectiles = 8;
    [SerializeField, Min(0.01f)] private float intervalBetweenProjectiles = 0.12f;

    public override bool Cast(SpellCastContext context)
    {
        if (projectilePrefab == null || context.CastPoint == null)
            return false;

        int minCount = Mathf.Max(1, minimumProjectiles);
        int maxCount = Mathf.Max(minCount, maximumProjectiles);
        int count = Mathf.RoundToInt(Mathf.Lerp(minCount, maxCount, context.Charge01));
        if (projectilePrefab.GetComponent<ILaunchableSpell>() == null)
        {
            Debug.LogError($"Projectile prefab '{projectilePrefab.name}' must implement ILaunchableSpell.", projectilePrefab);
            return false;
        }

        return context.Caster.StartChargedBurst(projectilePrefab, count, intervalBetweenProjectiles);
    }
}
