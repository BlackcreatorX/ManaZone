using UnityEngine;

[CreateAssetMenu(menuName = "Mana Zone/Enemy AI/Personality")]
public sealed class EnemyAIPersonality : ScriptableObject
{
    [SerializeField, Min(0.1f)] private float aggression = 1f;
    [SerializeField, Min(0.1f)] private float manaSpending = 1f;
    [SerializeField, Min(0f)] private float preferredDistance = 8f;
    [SerializeField, Min(0f)] private float reactionTime = 0.8f;
    [SerializeField, Min(0f)] private float selectionInterval = 0.5f;
    [SerializeField, Min(0f)] private float movementSpeed = 3f;
    [SerializeField, Min(0.1f)] private float combatStrafeSpeed = 1.6f;
    [SerializeField, Min(1f)] private float dodgeSpeedMultiplier = 1.8f;
    [SerializeField, Min(0.1f)] private float dodgeDuration = 0.45f;
    [SerializeField, Min(1)] private int dodgeEveryPatterns = 2;
    [SerializeField, Min(0.1f)] private float strafeDirectionInterval = 3.5f;
    [SerializeField, Range(0f, 1f)] private float harassmentPreference = 0.5f;
    [SerializeField, Range(0f, 1f)] private float chargedPreference = 0.5f;
    [SerializeField, Range(0f, 1f)] private float counterPreference = 0.5f;
    [SerializeField, Range(0f, 1f)] private float controlPreference = 0.5f;

    public float Aggression => aggression;
    public float ManaSpending => manaSpending;
    public float PreferredDistance => preferredDistance;
    public float ReactionTime => reactionTime;
    public float SelectionInterval => selectionInterval;
    public float MovementSpeed => movementSpeed;
    public float CombatStrafeSpeed => combatStrafeSpeed;
    public float DodgeSpeedMultiplier => dodgeSpeedMultiplier;
    public float DodgeDuration => dodgeDuration;
    public int DodgeEveryPatterns => dodgeEveryPatterns;
    public float StrafeDirectionInterval => strafeDirectionInterval;
    public float Preference(EnemyPatternKind kind)
    {
        switch (kind)
        {
            case EnemyPatternKind.Harassment: return harassmentPreference;
            case EnemyPatternKind.Charged: return chargedPreference;
            case EnemyPatternKind.Counter: return counterPreference;
            default: return controlPreference;
        }
    }
}
