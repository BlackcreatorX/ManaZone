
using UnityEngine;

public enum DefensiveMode
{
    Shield,
    Parry
}

[System.Serializable]
public class SpellSlot
{
    [Header("Spell")]
    public string spellName;
    public GameObject prefab;

    [Header("Cooldown")]
    public float cooldown = 1f;

    [Header("Mana Cost (defensive shield: per second)")]
    public float manaCost = 15f;
}

[System.Serializable]
public class DefensiveSpellSlot : SpellSlot
{
    [Header("Defensive Mode")]
    public DefensiveMode defensiveMode = DefensiveMode.Shield;

    [Header("Parry")]
    public float parryWindow = 0.2f;

    [Header("Mana Recovery")]
    public float manaRecovery = 10f;
}
