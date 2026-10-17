
using UnityEngine;

public class ShieldEffect : MonoBehaviour
{
    private bool isActive;
    private ManaSystem manaSystem;
    private float manaRefundOnBlock;

    public bool IsActive => isActive;

    public void Activate(ManaSystem manaSystem, float manaRefundOnBlock)
    {
        this.manaSystem = manaSystem;
        this.manaRefundOnBlock = manaRefundOnBlock;
        isActive = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive)
            return;

        SpellProjectile spellProjectile =
            other.GetComponentInParent<SpellProjectile>();
        CCProjectile ccProjectile =
            other.GetComponentInParent<CCProjectile>();

        if (spellProjectile == null && ccProjectile == null)
            return;

        GameObject projectile = spellProjectile != null
            ? spellProjectile.gameObject
            : ccProjectile.gameObject;

        Destroy(projectile);
        if (manaSystem != null)
            manaSystem.RestoreMana(manaRefundOnBlock);

        Debug.Log($"¡El escudo bloqueó un proyectil y recuperó {manaRefundOnBlock:F1} de maná!");
    }

    public void Deactivate()
    {
        isActive = false;
        Destroy(gameObject);
    }
}
