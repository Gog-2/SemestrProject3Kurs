using System;
using UnityEngine;
using Game.Core;

namespace Game.Buildings
{
    public interface IBuilding : IDamageable
    {
        float BreakCost { get; }
        Vector2Int Origin { get; }
        Vector2Int Size { get; }

        event Action<IBuilding> BuildingHealthChanged;
        event Action<IBuilding> BuildingDestroyed;
    }
}
