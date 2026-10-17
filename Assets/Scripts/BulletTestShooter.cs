
using UnityEngine;

public class BulletTestShooter : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float shootInterval = 0.5f;
    [SerializeField] private float bulletSpeed = 15f;

    [Header("Test Settings")]
    [SerializeField] private bool shootContinuously = true;

    private float shootTimer;

    private void Update()
    {
        if (!shootContinuously)
            return;

        shootTimer += Time.deltaTime;

        if (shootTimer >= shootInterval)
        {
            Shoot();
            shootTimer = 0f;
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null || shootPoint == null)
        {
            Debug.LogWarning("Asigna el prefab de bala y el Shoot Point.");
            return;
        }

        GameObject bullet = Instantiate(
            bulletPrefab,
            shootPoint.position,
            shootPoint.rotation
        );

        Rigidbody rb = bullet.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = -shootPoint.forward * bulletSpeed;
        }
        else
        {
            Debug.LogWarning(
                "La bala no tiene Rigidbody; no se le aplicó velocidad.",
                bullet
            );
        }

        Debug.Log("Bala disparada.");
    }
}
