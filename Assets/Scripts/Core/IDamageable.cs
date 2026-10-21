using System;

namespace Game.Core
{
    public interface IDamageable
    {
        int CurrentHP { get; }
        int MaxHP { get; }
        bool IsDead { get; }

        void TakeDamage(int amount);

        event Action<IDamageable> HealthChanged;
        event Action<IDamageable> Destroyed;
    }
}
