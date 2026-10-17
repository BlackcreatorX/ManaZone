
using UnityEngine;
using System.Collections;

public class SpellableTarget : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Movement")]
    [SerializeField] private float slowMultiplier = 0.4f;

    private float currentHealth;
    private Coroutine slowRoutine;

    public float CurrentSpeedMultiplier { get; private set; } = 1f;
    [Header("UI")]
    [SerializeField] private UnityEngine.UI.Slider healthBar;
    [SerializeField] private TMPro.TMP_Text NameText;


    private void Awake()
    {
        currentHealth = maxHealth;
        NameText.text = name;
        if (healthBar != null)
            healthBar.value = currentHealth / maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"{name} recibió {damage} de daño. Vida: {currentHealth}");

        if (currentHealth <= 0f)
        {
            Destroy(gameObject);
        }
        healthBar.value = currentHealth / maxHealth;
    }

    public void ApplySlow(float duration)
    {
        if (slowRoutine != null)
            StopCoroutine(slowRoutine);

        slowRoutine = StartCoroutine(SlowRoutine(duration));
    }

    private IEnumerator SlowRoutine(float duration)
    {
        CurrentSpeedMultiplier = slowMultiplier;

        yield return new WaitForSeconds(duration);

        CurrentSpeedMultiplier = 1f;
        slowRoutine = null;
    }
}
