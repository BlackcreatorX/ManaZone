using UnityEngine;

public abstract class ChargedSpellDefinition : SpellDefinition
{
    [SerializeField, Min(0.1f)] private float chargeDuration = 1.5f;

    public float ChargeDuration => Mathf.Max(0.1f, chargeDuration);
    public float ManaDrainPerSecond => ManaCost / ChargeDuration;
}
