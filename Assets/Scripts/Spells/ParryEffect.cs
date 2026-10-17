
using UnityEngine;

public class ParryEffect : MonoBehaviour
{
    [Header("Parry Settings")]
    [SerializeField] private float defaultWindow = 0.2f;

    private float remainingTime;
    private bool isActive;
    private bool hasResolved;
    private ManaSystem manaSystem;
    private float manaRecovery;
    private HPSystem protectedPlayer;

    private IParryResponse response;

    private void Awake()
    {
        response = GetComponent<IParryResponse>();
    }

    public void Activate(float duration, ManaSystem manaSystem, float manaRecovery)
    {
        remainingTime = duration;
        this.manaSystem = manaSystem;
        this.manaRecovery = manaRecovery;
        protectedPlayer = transform.root.GetComponent<HPSystem>();
        if (protectedPlayer != null)
            protectedPlayer.SetInvulnerable(true);
        isActive = true;
        hasResolved = false;
    }

    public bool TryParry(SpellProjectile projectile)
    {
        if (!CanParry())
            return false;

        projectile.Reflect(transform.root);
        CompleteSuccessfulParry();
        return true;
    }

    public bool TryParry(CCProjectile projectile)
    {
        if (!CanParry())
            return false;

        projectile.Reflect(transform.root);
        CompleteSuccessfulParry();
        return true;
    }

    private void Update()
    {
        if (!isActive || hasResolved)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            ResolveParry(ParryResult.NotParried);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || hasResolved)
            return;

        SpellProjectile spellProjectile =
            other.GetComponentInParent<SpellProjectile>();
        CCProjectile ccProjectile =
            other.GetComponentInParent<CCProjectile>();

        if (spellProjectile == null && ccProjectile == null)
            return;

        if (spellProjectile != null)
            TryParry(spellProjectile);
        else if (ccProjectile != null)
            TryParry(ccProjectile);
    }

    private bool CanParry()
    {
        return isActive && !hasResolved;
    }

    private void CompleteSuccessfulParry()
    {
        if (manaSystem != null)
            manaSystem.RestoreMana(manaRecovery);

        Debug.Log($"Parried. Se recuperaron {manaRecovery:F1} de maná y el proyectil fue devuelto.");
        ResolveParry(ParryResult.Parried);
    }

    private void ResolveParry(ParryResult result)
    {
        if (hasResolved)
            return;

        hasResolved = true;
        isActive = false;

        if (protectedPlayer != null)
            protectedPlayer.SetInvulnerable(false);

        Debug.Log("Resultado del parry: " + result);

        response?.OnParryResult(result);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (protectedPlayer != null)
            protectedPlayer.SetInvulnerable(false);
    }
}
