using UnityEngine;

[CreateAssetMenu(menuName = "Mana Zone/Spells/Stun Only Spell")]
public sealed class StunOnlySpellDefinition : SpellDefinition
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField, Min(0f)] private float stunDuration = 1.5f;

    public override bool Cast(SpellCastContext context)
    {
        if (projectilePrefab == null || context.CastPoint == null)
            return false;

        GameObject projectile = Instantiate(projectilePrefab, context.CastPoint.position, context.CastPoint.rotation);
        CCProjectile controlProjectile = projectile.GetComponent<CCProjectile>();
        if (controlProjectile == null)
        {
            Debug.LogError($"Control projectile prefab '{projectilePrefab.name}' must have a CCProjectile component.", projectile);
            Destroy(projectile);
            return false;
        }

        controlProjectile.SetStunEffect(stunDuration, false);
        controlProjectile.SetOwner(context.Owner);
        controlProjectile.Launch(context.CastPoint.forward);
        return true;
    }
}
