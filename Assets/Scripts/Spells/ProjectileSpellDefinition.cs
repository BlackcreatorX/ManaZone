using UnityEngine;

[CreateAssetMenu(menuName = "Mana Zone/Spells/Projectile Spell")]
public sealed class ProjectileSpellDefinition : SpellDefinition
{
    [SerializeField] private GameObject projectilePrefab;

    public override bool Cast(SpellCastContext context)
    {
        if (projectilePrefab == null || context.CastPoint == null)
            return false;

        GameObject projectile = Instantiate(
            projectilePrefab,
            context.CastPoint.position,
            context.CastPoint.rotation);

        ILaunchableSpell launchable = projectile.GetComponent<ILaunchableSpell>();
        if (launchable == null)
        {
            Debug.LogError($"Projectile prefab '{projectilePrefab.name}' must implement ILaunchableSpell.", projectile);
            Destroy(projectile);
            return false;
        }

        launchable.Launch(context.CastPoint.forward);
        return true;
    }
}
