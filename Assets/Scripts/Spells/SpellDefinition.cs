using UnityEngine;

public abstract class SpellDefinition : ScriptableObject
{
    [SerializeField] private string displayName = "Spell";
    [SerializeField, Min(0f)] private float cooldown = 1f;
    [SerializeField, Min(0f)] private float manaCost = 10f;

    public virtual string DisplayName => displayName;
    public float Cooldown => cooldown;
    public virtual float ManaCost => manaCost;
    protected float BaseManaCost => manaCost;
    protected string BaseDisplayName => displayName;
    public virtual bool IsPersistent => false;

    public abstract bool Cast(SpellCastContext context);
}

public sealed class SpellCastContext
{
    public SpellCastContext(SpellCaster caster, Transform owner, Transform castPoint, ManaSystem mana, float charge01 = 1f)
    {
        Caster = caster;
        Owner = owner;
        CastPoint = castPoint;
        Mana = mana;
        Charge01 = Mathf.Clamp01(charge01);
    }

    public SpellCaster Caster { get; }
    public Transform Owner { get; }
    public Transform CastPoint { get; }
    public ManaSystem Mana { get; }
    public float Charge01 { get; }
}
