
using UnityEngine;

public class ParryEffect : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Transform heart;
    [SerializeField, Min(0f)] private float heartStartScale = 0.1f;
    [SerializeField, Min(0f)] private float heartGrowDuration = 0.2f;
    [SerializeField, Min(0f)] private float heartAbsorbScale = 0.12f;
    [SerializeField, Min(0.01f)] private float heartAbsorbDuration = 0.22f;

    private float remainingTime;
    private bool isActive;
    private bool hasResolved;
    private bool hasParried;
    private ManaSystem manaSystem;
    private float manaRecovery;
    private HPSystem protectedPlayer;
    private Vector3 heartTargetScale;
    private float heartGrowElapsed;
    private float heartAbsorbElapsed;

    public void Activate(float duration, ManaSystem manaSystem, float manaRecovery)
    {
        if (heart == null)
            heart = transform.Find("Heart");

        if (heart != null)
        {
            heartTargetScale = heart.localScale;
            heart.localScale = heartTargetScale * heartStartScale;
            heartGrowElapsed = 0f;
            heartAbsorbElapsed = heartAbsorbDuration;
        }

        remainingTime = duration;
        this.manaSystem = manaSystem;
        this.manaRecovery = manaRecovery;
        protectedPlayer = transform.root.GetComponent<HPSystem>();
        if (protectedPlayer != null)
            protectedPlayer.SetInvulnerable(true);
        isActive = true;
        hasResolved = false;
        hasParried = false;
    }

    public bool TryParry(SpellProjectile projectile)
    {
        if (!CanParry() || IsOwnedByParrier(projectile))
            return false;

        projectile.Reflect(transform.root);
        CompleteSuccessfulParry();
        return true;
    }

    public bool TryParry(CCProjectile projectile)
    {
        if (!CanParry() || IsOwnedByParrier(projectile))
            return false;

        projectile.Reflect(transform.root);
        CompleteSuccessfulParry();
        return true;
    }

    private void Update()
    {
        AnimateHeartEntry();

        if (!isActive || hasResolved)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            ResolveParry(hasParried ? ParryResult.Parried : ParryResult.NotParried);
        }
    }

    private void AnimateHeartEntry()
    {
        if (heart == null)
            return;

        if (heartGrowElapsed < heartGrowDuration)
            heartGrowElapsed += Time.deltaTime;

        float entryProgress = heartGrowDuration > 0f
            ? Mathf.Clamp01(heartGrowElapsed / heartGrowDuration)
            : 1f;
        Vector3 entryScale = Vector3.Lerp(heartTargetScale * heartStartScale, heartTargetScale, entryProgress);

        if (heartAbsorbElapsed < heartAbsorbDuration)
            heartAbsorbElapsed = Mathf.Min(heartAbsorbElapsed + Time.deltaTime, heartAbsorbDuration);

        float absorbProgress = heartAbsorbDuration > 0f
            ? Mathf.Clamp01(heartAbsorbElapsed / heartAbsorbDuration)
            : 1f;
        float pulse = Mathf.Sin(absorbProgress * Mathf.PI);
        heart.localScale = entryScale * (1f + heartAbsorbScale * pulse);
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

    private bool IsOwnedByParrier(IOwnedSpellProjectile projectile)
    {
        return projectile != null && projectile.OwnerRoot == transform.root;
    }

    private void CompleteSuccessfulParry()
    {
        hasParried = true;
        heartAbsorbElapsed = 0f;
        if (manaSystem != null)
            manaSystem.RestoreMana(manaRecovery);

        Debug.Log($"Parried. Se recuperaron {manaRecovery:F1} de maná y el proyectil fue devuelto.");
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

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (protectedPlayer != null)
            protectedPlayer.SetInvulnerable(false);
    }
}
