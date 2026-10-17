
using UnityEngine;

public class ShieldEffect : MonoBehaviour
{
    private bool isActive;
    private ManaSystem manaSystem;
    private float manaDrainPerSecond;
    private float manaRefundOnBlock;

    public bool IsActive => isActive;

    public void Activate(ManaSystem manaSystem, float manaDrainPerSecond, float manaRefundOnBlock)
    {
        this.manaSystem = manaSystem;
        this.manaDrainPerSecond = manaDrainPerSecond;
        this.manaRefundOnBlock = manaRefundOnBlock;
        if (this.manaSystem != null)
            this.manaSystem.RegenerationEnabled = false;
        isActive = true;
    }

    private void Update()
    {
        if (!isActive || manaDrainPerSecond <= 0f)
            return;

        if (manaSystem == null || !manaSystem.TrySpendMana(manaDrainPerSecond * Time.deltaTime))
            Deactivate();
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
        if (manaSystem != null)
            manaSystem.RegenerationEnabled = true;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (manaSystem != null)
            manaSystem.RegenerationEnabled = true;
    }
}
