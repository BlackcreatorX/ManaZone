using UnityEngine;

[CreateAssetMenu(menuName = "Mana Zone/Spells/Charged Projectile Spell")]
public sealed class ChargedProjectileSpellDefinition : ChargedSpellDefinition
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField, Min(0f)] private float minimumDamage = 25f;
    [SerializeField, Min(0f)] private float maximumDamage = 100f;

    public override bool Cast(SpellCastContext context)
    {
        if (projectilePrefab == null || context.CastPoint == null)
            return false;

        GameObject projectile = Instantiate(projectilePrefab, context.CastPoint.position, context.CastPoint.rotation);
        SpellProjectile spellProjectile = projectile.GetComponent<SpellProjectile>();
        if (spellProjectile == null)
        {
            Debug.LogError($"Projectile prefab '{projectilePrefab.name}' must have a SpellProjectile component.", projectile);
            Destroy(projectile);
            return false;
        }

        float damage = Mathf.Lerp(minimumDamage, Mathf.Max(minimumDamage, maximumDamage), context.Charge01);
        spellProjectile.SetDamage(damage);
        spellProjectile.Launch(context.CastPoint.forward);
        return true;
    }
}
