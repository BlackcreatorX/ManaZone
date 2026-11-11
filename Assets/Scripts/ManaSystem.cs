
using System;
using UnityEngine;
using TMPro;


public class ManaSystem : MonoBehaviour
{
    [Header("Mana")]
    [SerializeField] private float maxMana = 100f;
    [SerializeField] private float regenerationPerSecond = 12f;

    [Header("UI")]
    [SerializeField] private UnityEngine.UI.Slider manaBar;
    [SerializeField] private TMP_Text manaText;

    public float CurrentMana { get; private set; }
    public float MaxMana => maxMana;
    public bool RegenerationEnabled { get; set; } = true;

    // Notifica cuando cambia el maná.
    public event Action<float, float> OnManaChanged;

    private void Awake()
    {
        CurrentMana = maxMana;
        NotifyManaChanged();
    }

    private void Update()
    {
        if (RegenerationEnabled)
            RegenerateMana(regenerationPerSecond * Time.deltaTime);
    }

    public bool TrySpendMana(float amount)
    {
        if (amount < 0f || CurrentMana < amount)
            return false;

        CurrentMana -= amount;
        NotifyManaChanged();

        return true;
    }

    public void RegenerateMana(float amount)
    {
        float previousMana = CurrentMana;

        CurrentMana = Mathf.Clamp(
            CurrentMana + amount,
            0f,
            maxMana
        );

        if (!Mathf.Approximately(previousMana, CurrentMana))
            NotifyManaChanged();
    }

    public void RestoreMana(float amount)
    {
        RegenerateMana(amount);
    }

    private void NotifyManaChanged()
    {
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
        if (manaBar != null && MaxMana > 0f)
            manaBar.value = CurrentMana / MaxMana;
        if (manaText != null)
            manaText.text = CurrentMana.ToString("F0");

    }
}
