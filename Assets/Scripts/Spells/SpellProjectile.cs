
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpellProjectile : MonoBehaviour, ILaunchableSpell, IOwnedSpellProjectile
{
    [Header("Projectile")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 4f;

    [Header("Effects")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private bool appliesSlow = false;
    [SerializeField] private float slowDuration = 2f;

    private Rigidbody rb;
    private Transform ignoredOwner;
    public Transform OwnerRoot => ignoredOwner;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Launch(Vector3 direction)
    {
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.linearVelocity = direction.normalized * speed;

        Destroy(gameObject, lifetime);
    }

    public void SetOwner(Transform owner)
    {
        ignoredOwner = owner != null ? owner.root : null;
    }

    public bool IsOwnedBy(Transform owner)
    {
        return owner != null && ignoredOwner == owner.root;
    }

    public void SetDamage(float value)
    {
        damage = Mathf.Max(0f, value);
    }

    public void Reflect(Transform newOwner)
    {
        ignoredOwner = newOwner != null ? newOwner.root : null;
        Vector3 reflectedVelocity = -rb.linearVelocity;
        if (reflectedVelocity.sqrMagnitude < 0.01f)
            reflectedVelocity = -transform.forward * speed;

        rb.linearVelocity = reflectedVelocity;
        transform.forward = reflectedVelocity.normalized;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ignoredOwner != null && other.transform.root == ignoredOwner)
            return;

        ParryEffect parry = other.transform.root.GetComponentInChildren<ParryEffect>();
        if (parry != null && parry.TryParry(this))
            return;

        ShieldEffect shield = other.transform.root.GetComponentInChildren<ShieldEffect>();
        if (shield != null && shield.IsActive)
        {
            Destroy(gameObject);
            return;
        }

        // Offensive projectiles pass through other spells. Shield and parry are
        // resolved above as explicit defensive effects.
        if (other.GetComponentInParent<SpellProjectile>() != null
            || other.GetComponentInParent<CCProjectile>() != null)
            return;

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage);

            if (appliesSlow)
            {
                ISlowable slowable = other.GetComponentInParent<ISlowable>();
                slowable?.ApplySlow(slowDuration);
            }
        }

        // El proyectil desaparece al impactar.
        Destroy(gameObject);
    }
}
