using UnityEngine;

public enum EnemyPatternKind { Harassment, Charged, Counter, SpaceControl }

[CreateAssetMenu(menuName = "Mana Zone/Enemy AI/Attack Pattern")]
public sealed class EnemyAIPattern : ScriptableObject
{
    [SerializeField] private string patternId = "pattern";
    [SerializeField] private EnemyPatternKind kind;
    [SerializeField] private SpellDefinition[] actions;
    [SerializeField, Min(0f)] private float minDistance;
    [SerializeField, Min(0f)] private float maxDistance = 20f;
    [SerializeField, Min(0f)] private float preparationTime = 0.5f;
    [SerializeField, Min(0f)] private float pauseBetweenActions = 0.2f;
    [SerializeField, Min(0f)] private float recoveryTime = 1f;
    [SerializeField, Min(0f)] private float selectionWeight = 1f;
    [SerializeField, Range(0f, 1f)] private float lowPlayerHealthBonus;

    public string PatternId => patternId;
    public EnemyPatternKind Kind => kind;
    public SpellDefinition[] Actions => actions;
    public float MinDistance => minDistance;
    public float MaxDistance => maxDistance;
    public float PreparationTime => preparationTime;
    public float PauseBetweenActions => pauseBetweenActions;
    public float RecoveryTime => recoveryTime;
    public float SelectionWeight => selectionWeight;
    public float LowPlayerHealthBonus => lowPlayerHealthBonus;
}
