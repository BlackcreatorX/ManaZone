
using UnityEngine;
using TMPro;

public class HPSystem : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("UI")]
    [SerializeField] private UnityEngine.UI.Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    private float currentHealth;
    private bool isInvulnerable;

    // Getters y setters
    public float MaxHealth
    {
        get { return maxHealth; }
    }

    public float CurrentHealth
    {
        get { return currentHealth; }
        private set { currentHealth = Mathf.Clamp(value, 0f, maxHealth); }
    }

    public void SetInvulnerable(bool value)
    {
        isInvulnerable = value;
    }

    private void Awake()
    {
        CurrentHealth = maxHealth;
        RefreshHealthUI();
    }

    public void TakeDamage(float damage)
    {
        if (isInvulnerable || damage <= 0f || currentHealth <= 0f)
            return;

        ShieldEffect shield = GetComponentInChildren<ShieldEffect>();
        if (shield != null && shield.IsActive)
            return;

        CurrentHealth -= damage;

        TPSController playerController = GetComponent<TPSController>();
        if (playerController != null)
            playerController.PlayDamageAnimation();

        Debug.Log($"{name} recibió {damage} de daño. Vida: {CurrentHealth}");

        if (CurrentHealth <= 0f)
        {
            bool isPlayer = GetComponentInParent<TPSController>() != null;
            EnemyCombatAI enemyAI = GetComponentInParent<EnemyCombatAI>();
            SpellableTarget spellableTarget = GetComponentInParent<SpellableTarget>();
            GameObject enemyRoot = enemyAI != null
                ? enemyAI.gameObject
                : spellableTarget != null ? spellableTarget.gameObject : gameObject;

            // In a fight, a non-player HPSystem belongs to an opponent. Pass the
            // opponent GameObject ID, which is the same ID used by the tracker.
            FightResultsUI.ReportDeath(isPlayer, !isPlayer, enemyRoot.GetInstanceID());
            Destroy(gameObject);
        }
        RefreshHealthUI();

    }

    public void Heal(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f)
            return;

        CurrentHealth += amount;

        Debug.Log($"{name} recuperó {amount} de vida. Vida: {CurrentHealth}");
        RefreshHealthUI();
    }

    private void RefreshHealthUI()
    {
        if (healthText != null)
            healthText.text = $"{CurrentHealth}/{MaxHealth}";
        if (healthSlider != null && maxHealth > 0f)
            healthSlider.value = CurrentHealth / MaxHealth;
    }
}
