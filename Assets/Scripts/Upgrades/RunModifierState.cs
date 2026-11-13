using System;
using Game.Buildings;
using UnityEngine;

namespace Game.Upgrades
{
    /// <summary>
    /// Mutable run modifier state operated on by upgrade effect commands.
    /// </summary>
    public sealed class RunModifierState
    {
        private readonly Func<Castle> _getCastle;
        private readonly Action<int> _onBuildSlotsChanged;

        public RunModifierState(Func<Castle> getCastle = null, Action<int> onBuildSlotsChanged = null)
        {
            _getCastle = getCastle;
            _onBuildSlotsChanged = onBuildSlotsChanged;
        }

        public int BonusCoinsPerKill { get; set; }
        public float ExtraDiamondDropChance { get; set; }
        public float RangedDamageMultiplier { get; set; } = 1f;
        public float MeleeDamageMultiplier { get; set; } = 1f;
        public float AttackSpeedMultiplier { get; set; } = 1f;
        public float WeaponDamageMultiplier { get; set; } = 1f;
        public float WeaponCooldownMultiplier { get; set; } = 1f;
        public float UnitHealthMultiplier { get; set; } = 1f;
        public float UnitRespawnMultiplier { get; set; } = 1f;
        public float BuildingHealthMultiplier { get; set; } = 1f;
        public float SlowOnHitPercent { get; set; }
        public float TrapDamageMultiplier { get; set; } = 1f;
        public float BombDamageMultiplier { get; set; } = 1f;
        public float EconomicSpeedMultiplier { get; set; } = 1f;
        public float SpellDamageMultiplier { get; set; } = 1f;
        public float BuildingCostDiscountPercent { get; set; }
        public float SupportEffectivenessMultiplier { get; set; } = 1f;
        public int BonusAggroLimit { get; set; }
        public int ExtraBuildSlots { get; private set; }

        public Castle CastleInstance => _getCastle?.Invoke();

        public void AddExtraBuildSlots(int delta)
        {
            ExtraBuildSlots += delta;
            _onBuildSlotsChanged?.Invoke(ExtraBuildSlots);
        }
    }
}
