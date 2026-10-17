
using UnityEngine;

public class CCProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifeTime = 5f;

    private Vector3 direction;
    private bool hasHit;
    private bool isLaunched;
    private Transform ignoredOwner;

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

        MonoBehaviour[] components =
            other.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour component in components)
        {
            if (component == null)
                continue;

            if (component is IStunnable target)
            {
                hasHit = true;
                target.Stun();

                Debug.Log(
                    $"¡{other.name} recibió el hechizo de control!"
                );

                Destroy(gameObject);
                return;
            }
        }
    }
}

public interface IStunnable
{
    void Stun();
}
