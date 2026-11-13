using System;
using System.Collections.Generic;
using Game.Buildings;
using UnityEngine;

namespace Game.Upgrades
{
    public sealed class DelegateUpgradeEffect : IUpgradeEffect
    {
        private readonly Action<float, RunModifierState> _apply;

        public UpgradeEffectType EffectType { get; }

        public DelegateUpgradeEffect(UpgradeEffectType effectType, Action<float, RunModifierState> apply)
        {
            EffectType = effectType;
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        }

        public void Apply(float value, RunModifierState state)
        {
            _apply(value, state);
        }
    }

    public sealed class CastleMaxHealthPercentEffect : IUpgradeEffect
    {
        public UpgradeEffectType EffectType => UpgradeEffectType.CastleMaxHealthPercent;

        public void Apply(float value, RunModifierState state)
        {
            Castle castle = state.CastleInstance;
            if (castle != null)
            {
                int hpBonus = Mathf.RoundToInt(castle.MaxHP * value);
                castle.IncreaseMaxHealth(hpBonus, healAmount: true);
            }
        }
    }

    public sealed class CastleMaxHealthFlatEffect : IUpgradeEffect
    {
        public UpgradeEffectType EffectType => UpgradeEffectType.CastleMaxHealthFlat;

        public void Apply(float value, RunModifierState state)
        {
            Castle castle = state.CastleInstance;
            if (castle != null)
            {
                castle.IncreaseMaxHealth(Mathf.RoundToInt(value), healAmount: true);
            }
        }
    }

    public sealed class CastleHealPercentEffect : IUpgradeEffect
    {
        public UpgradeEffectType EffectType => UpgradeEffectType.CastleHealPercent;

        public void Apply(float value, RunModifierState state)
        {
            Castle castle = state.CastleInstance;
            if (castle != null)
            {
                castle.HealPercent(value);
            }
        }
    }

    public sealed class BuildSlotAddEffect : IUpgradeEffect
    {
        public UpgradeEffectType EffectType => UpgradeEffectType.BuildSlotAdd;

        public void Apply(float value, RunModifierState state)
        {
            state.AddExtraBuildSlots(Mathf.RoundToInt(value));
        }
    }

    public sealed class RepairAllBuildingsPercentEffect : IUpgradeEffect
    {
        public UpgradeEffectType EffectType => UpgradeEffectType.RepairAllBuildingsPercent;

        public void Apply(float value, RunModifierState state)
        {
            var active = Building.ActiveBuildings;
            for (int i = 0; i < active.Count; i++)
            {
                Building b = active[i];
                if (b != null && !b.IsDead)
                {
                    b.Repair(Mathf.RoundToInt(b.MaxHP * value));
                }
            }
        }
    }

    /// <summary>
    /// Maps each UpgradeEffectType to its IUpgradeEffect command without a monolithic switch.
    /// </summary>
    public sealed class UpgradeEffectRegistry
    {
        private readonly Dictionary<UpgradeEffectType, IUpgradeEffect> _effects = new();

        public UpgradeEffectRegistry()
        {
            Register(new CastleMaxHealthPercentEffect());
            Register(new CastleMaxHealthFlatEffect());
            Register(new CastleHealPercentEffect());
            Register(new BuildSlotAddEffect());
            Register(new RepairAllBuildingsPercentEffect());

            RegisterStat(UpgradeEffectType.RangedDamagePercent, (v, s) => s.RangedDamageMultiplier += v);
            RegisterStat(UpgradeEffectType.MeleeDamagePercent, (v, s) => s.MeleeDamageMultiplier += v);
            RegisterStat(UpgradeEffectType.AttackSpeedPercent, (v, s) => s.AttackSpeedMultiplier += v);
            RegisterStat(UpgradeEffectType.WeaponDamagePercent, (v, s) => s.WeaponDamageMultiplier += v);
            RegisterStat(UpgradeEffectType.WeaponCooldownPercent, (v, s) => s.WeaponCooldownMultiplier = Mathf.Max(0.2f, s.WeaponCooldownMultiplier - v));
            RegisterStat(UpgradeEffectType.UnitHealthPercent, (v, s) => s.UnitHealthMultiplier += v);
            RegisterStat(UpgradeEffectType.UnitRespawnPercent, (v, s) => s.UnitRespawnMultiplier = Mathf.Max(0.2f, s.UnitRespawnMultiplier - v));
            RegisterStat(UpgradeEffectType.BuildingHealthPercent, (v, s) => s.BuildingHealthMultiplier += v);
            RegisterStat(UpgradeEffectType.CoinPerKill, (v, s) => s.BonusCoinsPerKill += Mathf.RoundToInt(v));
            RegisterStat(UpgradeEffectType.DiamondDropChanceAdd, (v, s) => s.ExtraDiamondDropChance += v);
            RegisterStat(UpgradeEffectType.SlowOnHitPercent, (v, s) => s.SlowOnHitPercent = Mathf.Max(s.SlowOnHitPercent, v));
            RegisterStat(UpgradeEffectType.TrapDamagePercent, (v, s) => s.TrapDamageMultiplier += v);
            RegisterStat(UpgradeEffectType.BombDamagePercent, (v, s) => s.BombDamageMultiplier += v);
            RegisterStat(UpgradeEffectType.EconomicSpeedPercent, (v, s) => s.EconomicSpeedMultiplier += v);
            RegisterStat(UpgradeEffectType.SpellDamagePercent, (v, s) => s.SpellDamageMultiplier += v);
            RegisterStat(UpgradeEffectType.BuildingCostDiscountPercent, (v, s) => s.BuildingCostDiscountPercent = Mathf.Min(0.5f, s.BuildingCostDiscountPercent + v));
            RegisterStat(UpgradeEffectType.SupportEffectivenessPercent, (v, s) => s.SupportEffectivenessMultiplier += v);
            RegisterStat(UpgradeEffectType.AggroLimitAdd, (v, s) => s.BonusAggroLimit += Mathf.RoundToInt(v));
        }

        public void Register(IUpgradeEffect effect)
        {
            if (effect == null) throw new ArgumentNullException(nameof(effect));
            _effects[effect.EffectType] = effect;
        }

        public bool TryApply(UpgradeEffectType effectType, float value, RunModifierState state)
        {
            if (_effects.TryGetValue(effectType, out IUpgradeEffect effect))
            {
                effect.Apply(value, state);
                return true;
            }

            Debug.LogWarning($"[UpgradeEffectRegistry] No effect registered for {effectType}.");
            return false;
        }

        private void RegisterStat(UpgradeEffectType type, Action<float, RunModifierState> mutator)
        {
            Register(new DelegateUpgradeEffect(type, mutator));
        }
    }
}
