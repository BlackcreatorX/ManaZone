using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Offers three distinct upgrades compatible with the player's current loadout.</summary>
public sealed class RoguelikeManager : MonoBehaviour
{
    [SerializeField] private SpellCaster playerSpellCaster;
    [SerializeField] private ManaSystem playerMana;
    [SerializeField] private RoguelikeUpgradeDefinition[] upgradePool;
    [SerializeField, Min(1)] private int optionsPerReward = 3;

    private readonly List<RoguelikeUpgradeDefinition> currentOptions = new List<RoguelikeUpgradeDefinition>();
    public IReadOnlyList<RoguelikeUpgradeDefinition> CurrentOptions => currentOptions;
    public event Action<IReadOnlyList<RoguelikeUpgradeDefinition>> OnOptionsGenerated;
    public event Action<RoguelikeUpgradeDefinition> OnUpgradeChosen;

    private void Awake()
    {
        if (playerSpellCaster == null) playerSpellCaster = GetComponent<SpellCaster>();
        if (playerMana == null) playerMana = GetComponent<ManaSystem>();
    }

    public IReadOnlyList<RoguelikeUpgradeDefinition> GenerateOptions()
    {
        currentOptions.Clear();
        List<RoguelikeUpgradeDefinition> candidates = new List<RoguelikeUpgradeDefinition>();
        if (upgradePool != null)
        {
            foreach (RoguelikeUpgradeDefinition upgrade in upgradePool)
            {
                if (upgrade != null && upgrade.IsStatValidForCategory() && IsCompatible(upgrade)) candidates.Add(upgrade);
            }
        }

        int count = Mathf.Min(Mathf.Max(1, optionsPerReward), candidates.Count);
        HashSet<RoguelikeUpgradeCategory> offeredCategories = new HashSet<RoguelikeUpgradeCategory>();
        for (int i = 0; i < count; i++)
        {
            List<int> distinctStatIndices = new List<int>();
            for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
                if (!offeredCategories.Contains(candidates[candidateIndex].Category)) distinctStatIndices.Add(candidateIndex);
            if (distinctStatIndices.Count == 0) break;

            int pick = distinctStatIndices[UnityEngine.Random.Range(0, distinctStatIndices.Count)];
            RoguelikeUpgradeDefinition selected = candidates[pick];
            currentOptions.Add(selected);
            offeredCategories.Add(selected.Category);
            candidates.RemoveAt(pick);
        }
        OnOptionsGenerated?.Invoke(currentOptions);
        return currentOptions;
    }

    public bool ChooseUpgrade(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= currentOptions.Count) return false;
        RoguelikeUpgradeDefinition chosen = currentOptions[optionIndex];
        if (chosen == null || !chosen.IsStatValidForCategory() || !IsCompatible(chosen)) return false;

        if (chosen.Stat == RoguelikeUpgradeStat.ManaRegeneration)
        {
            if (playerMana == null) return false;
            playerMana.MultiplyRegeneration(chosen.ValueMultiplier);
        }
        else if (chosen.Stat == RoguelikeUpgradeStat.MaxMana)
        {
            if (playerMana == null) return false;
            playerMana.MultiplyMaxMana(chosen.ValueMultiplier);
        }
        else if (chosen.Stat == RoguelikeUpgradeStat.MaxHealth)
        {
            HPSystem health = GetPlayerComponent<HPSystem>();
            if (health == null) return false;
            health.MultiplyMaxHealth(chosen.ValueMultiplier);
        }
        else if (chosen.Stat == RoguelikeUpgradeStat.MoveSpeed)
        {
            TPSController movement = GetPlayerComponent<TPSController>();
            if (movement == null) return false;
            movement.MultiplyMoveSpeed(chosen.ValueMultiplier);
        }
        else
        {
            if (playerSpellCaster == null) return false;
            playerSpellCaster.AddUpgrade(chosen);
        }

        currentOptions.Clear();
        OnUpgradeChosen?.Invoke(chosen);
        return true;
    }

    private T GetPlayerComponent<T>() where T : Component
    {
        if (playerSpellCaster != null) return playerSpellCaster.GetComponent<T>();
        return playerMana != null ? playerMana.GetComponent<T>() : GetComponent<T>();
    }

    private bool IsCompatible(RoguelikeUpgradeDefinition upgrade)
    {
        if (upgrade.Category == RoguelikeUpgradeCategory.Utilidad)
            return true;
        if (playerSpellCaster == null || playerSpellCaster.EquippedSpells == null) return false;
        foreach (SpellDefinition spell in playerSpellCaster.EquippedSpells)
            if (upgrade.IsCompatibleWith(spell)) return true;
        return false;
    }
}
