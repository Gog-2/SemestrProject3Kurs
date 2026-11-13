using System;
using Game.Buildings;
using Game.Data;
using Game.Upgrades;

namespace Game.Core
{
    /// <summary>
    /// Abstraction over run-level progression modifiers for buildings, units, weapons, and spells.
    /// </summary>
    public interface IRunModifiers
    {
        KnightData ActiveKnight { get; }
        int ExtraBuildSlots { get; }

        event Action<int> BuildSlotsCountChanged;
        event Action<UpgradeData> UpgradeApplied;

        float GetBuildingMaxHpMultiplier(BuildingData data);
        int GetBuildingMaxHpFlatBonus(BuildingData data);
        float GetEffectiveUpgradeCostMultiplier();

        float GetEffectiveEconomySpeedMultiplier();
        float GetEffectiveUnitDamageMultiplier(bool isRanged, bool isArcherUnit = false);
        float GetEffectiveBuildingAttackDamageMultiplier(BuildingCategory category);
        float GetEffectiveUnitMaxHpMultiplier();
        int GetEffectiveUnitAggroBonus();
        float GetEffectiveAttackSpeedMultiplier();
        float GetEffectiveUnitRespawnMultiplier();

        float GetEffectiveMageCooldownMultiplier();
        float GetEffectiveSupportMultiplier();
        float GetEffectiveExplosiveDamageMultiplier();
        float GetExplosiveRadiusBonus();
        float GetEffectivePiercingCooldownMultiplier();
        float GetPiercingRangeBonus();

        float GetEffectiveWeaponDamageMultiplier();
        float GetEffectiveWeaponCooldownMultiplier();
        float GetEffectiveSpellDamageMultiplier();

        float GetEffectiveTrapDamageMultiplier();
        float GetEffectiveTrapUsesMultiplier();
        void GetControlDamageSlow(out float slowPercent, out float slowDuration);
        float GetHunterStealthBonusSeconds();
        float GetBuilderRepairSpeedMultiplier();
        float GetBuilderRangeBonus();

        int GetEffectiveCoinPerKillBonus();
        float GetEffectiveDiamondDropChanceBonus();
    }
}
