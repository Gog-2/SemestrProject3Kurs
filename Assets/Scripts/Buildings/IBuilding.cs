using System;
using UnityEngine;

namespace Game.Buildings
{
    public interface IBuilding
    {
        int CurrentHP { get; }
        int MaxHP { get; }
        float BreakCost { get; }
        Vector2Int Origin { get; }
        Vector2Int Size { get; }

        void TakeDamage(int amount);

        event Action<IBuilding> HealthChanged;
        event Action<IBuilding> Destroyed;
    }
}
