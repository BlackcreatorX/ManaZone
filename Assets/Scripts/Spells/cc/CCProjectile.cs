
using UnityEngine;

public class CCProjectile : MonoBehaviour, ILaunchableSpell, IOwnedSpellProjectile
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField, Min(0f)] private float stunDuration = 1.2f;
    [SerializeField] private bool flipTargetDuringStun = true;

    private Vector3 direction;
    private bool hasHit;
    private bool isLaunched;
    private Transform ignoredOwner;
    public Transform OwnerRoot => ignoredOwner;

    public void SetStunDuration(float duration)
    {
        stunDuration = Mathf.Max(0f, duration);
    }

    public void SetStunEffect(float duration, bool flipVisual)
    {
        SetStunDuration(duration);
        flipTargetDuringStun = flipVisual;
    }

    public void SetOwner(Transform owner)
    {
        ignoredOwner = owner != null ? owner.root : null;
    }

    public bool IsOwnedBy(Transform owner)
    {
        return owner != null && ignoredOwner == owner.root;
    }

    public void Launch(Vector3 launchDirection)
    {
        direction = launchDirection.normalized;
        isLaunched = true;

        Debug.Log("CCProjectile lanzado. Dirección: " + direction);
    }

    public void Reflect(Transform newOwner)
    {
        ignoredOwner = newOwner != null ? newOwner.root : null;
        direction = -direction;
        if (direction.sqrMagnitude < 0.01f)
            direction = -transform.forward;

        hasHit = false;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (!isLaunched || hasHit)
            return;

        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit)
            return;

        if (ignoredOwner != null && other.transform.root == ignoredOwner)
            return;

        ParryEffect parry = other.transform.root.GetComponentInChildren<ParryEffect>();
        if (parry != null && parry.TryParry(this))
            return;

        ShieldEffect shield = other.transform.root.GetComponentInChildren<ShieldEffect>();
        if (shield != null && shield.IsActive)
        {
            hasHit = true;
            Destroy(gameObject);
            return;
        }

        // Offensive spells do not cancel one another on contact.
        if (other.GetComponentInParent<SpellProjectile>() != null
            || other.GetComponentInParent<CCProjectile>() != null)
            return;

        IStunnable target = other.GetComponentInParent<IStunnable>();
        if (target != null)
        {
            hasHit = true;
            if (target is ITimedStunnable timedTarget)
                timedTarget.Stun(stunDuration, flipTargetDuringStun);
            else
                target.Stun();
            Destroy(gameObject);
        }
    }
}
