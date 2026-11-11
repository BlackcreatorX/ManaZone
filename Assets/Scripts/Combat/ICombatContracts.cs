using UnityEngine;

/// <summary>Common contract for anything that can receive spell damage.</summary>
public interface IDamageable
{
    void TakeDamage(float amount);
}

/// <summary>Optional combat capability for targets affected by control spells.</summary>
public interface IStunnable
{
    void Stun();
}

/// <summary>Stun capability that lets a spell choose its duration.</summary>
public interface ITimedStunnable : IStunnable
{
    void Stun(float duration);
    void Stun(float duration, bool flipVisual);
}

/// <summary>Optional capability for a spell that applies a movement slow.</summary>
public interface ISlowable
{
    void ApplySlow(float duration);
}

/// <summary>Common launch contract used by projectile spell prefabs.</summary>
public interface ILaunchableSpell
{
    void Launch(Vector3 direction);
}

/// <summary>Optional ownership tracking for projectiles that can interact with defenses.</summary>
public interface IOwnedSpellProjectile
{
    Transform OwnerRoot { get; }
    void SetOwner(Transform owner);
}
