using System;
using System.Collections.Generic;
using UnityEngine;
using Alchemy.Inspector;
using Game.Buildings;
using Game.Data;
using Game.Upgrades;
using Game.WaveSystem;
using Game.Weapons;

namespace Game.Core
{
    [DefaultExecutionOrder(-50)]
    public class RunProgressionManager : MonoBehaviour
    {
        public static RunProgressionManager Instance { get; private set; }

        [Title("References")]
        [SerializeField] private Castle _castle;
        [SerializeField] private UpgradeDatabase _upgradeDatabase;
        [SerializeField] private CastleWeapon _castleWeapon;

        private Castle CastleInstance => _castle != null ? _castle : (_castle = Castle.Instance ?? FindAnyObjectByType<Castle>());
        private CastleWeapon WeaponInstance => _castleWeapon != null ? _castleWeapon : (_castleWeapon = FindAnyObjectByType<CastleWeapon>());

        [Title("Progression State")]
        [ShowInInspector, ReadOnly] private int _currentLevel = 1;
        [ShowInInspector, ReadOnly] private int _currentXP = 0;
        [ShowInInspector, ReadOnly] private int _xpToNextLevel = 75; // 50 + 1 * 25
        [ShowInInspector, ReadOnly] private int _lifetimeGoldEarned = 0;
        [ShowInInspector, ReadOnly] private int _pendingUpgradeSelections = 0;

        [Title("Modifiers Stack")]
        [ShowInInspector, ReadOnly] public int BonusCoinsPerKill { get; private set; } = 0;
        [ShowInInspector, ReadOnly] public float ExtraDiamondDropChance { get; private set; } = 0f;
        [ShowInInspector, ReadOnly] public float RangedDamageMultiplier { get; private set; } = 1f;
        [ShowInInspector, ReadOnly] public float MeleeDamageMultiplier { get; private set; } = 1f;
        [ShowInInspector, ReadOnly] public float AttackSpeedMultiplier { get; private set; } = 1f;
        [ShowInInspector, ReadOnly] public float WeaponDamageMultiplier { get; private set; } = 1f;
        [ShowInInspector, ReadOnly] public float WeaponCooldownMultiplier { get; private set; } = 1f;
        [ShowInInspector, ReadOnly] public float UnitHealthMultiplier { get; private set; } = 1f;
        [ShowInInspector, ReadOnly] public float UnitRespawnMultiplier { get; private set; } = 1f;
        [ShowInInspector, ReadOnly] public float BuildingHealthMultiplier { get; private set; } = 1f;
        [ShowInInspector, ReadOnly] public float SlowOnHitPercent { get; private set; } = 0f;
        [ShowInInspector, ReadOnly] public float TrapDamageMultiplier { get; private set; } = 1f;
        [ShowInInspector, ReadOnly] public float BombDamageMultiplier { get; private set; } = 1f;
        [ShowInInspector, ReadOnly] public float EconomicSpeedMultiplier { get; private set; } = 1f;
        [ShowInInspector, ReadOnly] public float SpellDamageMultiplier { get; private set; } = 1f;
        [ShowInInspector, ReadOnly] public float BuildingCostDiscountPercent { get; private set; } = 0f;
        [ShowInInspector, ReadOnly] public float SupportEffectivenessMultiplier { get; private set; } = 1f;
        [ShowInInspector, ReadOnly] public int BonusAggroLimit { get; private set; } = 0;
        [ShowInInspector, ReadOnly] public int ExtraBuildSlots { get; private set; } = 0;

        private float _metaCombatDamageBonus;
        private float _metaEconomySpeedBonus;
        private float _metaWeaponSpellBonus;
        private float _metaDiamondChanceBonus;

        private readonly List<UpgradeData> _appliedUpgrades = new();
        private readonly Dictionary<string, int> _takenUpgradeCounts = new();

        public int CurrentLevel => _currentLevel;
        public int CurrentXP => _currentXP;
        public int XPToNextLevel => _xpToNextLevel;
        public int LifetimeGoldEarned => _lifetimeGoldEarned;
        public int PendingUpgradeSelections => _pendingUpgradeSelections;
        public int CurrentHonor => _currentXP;
        public int HonorToNextLevel => _xpToNextLevel;
        public IReadOnlyList<UpgradeData> AppliedUpgrades => _appliedUpgrades;
        public UpgradeDatabase Database => _upgradeDatabase;
        public KnightData ActiveKnight => RunConfig.Instance != null ? RunConfig.Instance.SelectedKnight : null;

        public void BindWeapon(CastleWeapon weapon)
        {
            _castleWeapon = weapon;
        }

        public event Action<int, int> HonorChanged; // current, target
        public event Action<int, int> XPChanged;    // current, target
        public event Action<int> LevelUp;
        public event Action<int> BuildSlotsCountChanged;
        public event Action<UpgradeData> UpgradeApplied;
        public event Action<List<UpgradeData>> UpgradeSelectionRequested;
        public event Action<List<UpgradeData>> ChestSelectionRequested;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (_castle == null)
            {
                _castle = Castle.Instance ?? FindAnyObjectByType<Castle>();
            }
            RecalculateXPThreshold();
            LoadMetaBonuses();
        }

        private void LoadMetaBonuses()
        {
            if (RunConfig.Instance == null) return;

            _metaCombatDamageBonus = MetaBonusCatalog.GetTotalBonus(MetaBonusType.CombatDamagePercent);
            _metaEconomySpeedBonus = MetaBonusCatalog.GetTotalBonus(MetaBonusType.EconomySpeedPercent);
            _metaWeaponSpellBonus = MetaBonusCatalog.GetTotalBonus(MetaBonusType.WeaponAndSpellPowerPercent);
            _metaDiamondChanceBonus = MetaBonusCatalog.GetTotalBonus(MetaBonusType.DiamondDropChancePercent);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Lifetime Gold XP (GIG-17, D-5): only enemy coin drops and gold mine income grant castle XP.
        /// Sell refunds never grant XP.
        /// </summary>
        public void AddCombatGoldXP(int goldAmount)
        {
            if (goldAmount <= 0) return;

            _lifetimeGoldEarned += goldAmount;
            _currentXP += goldAmount;
            NotifyXPChanged();

            bool leveledUp = false;
            while (_currentXP >= _xpToNextLevel)
            {
                _currentXP -= _xpToNextLevel;
                _currentLevel++;
                leveledUp = true;
                RecalculateXPThreshold();

                // +1 building slot every 5 castle levels (GIG-31)
                if (_currentLevel % 5 == 0)
                {
                    ExtraBuildSlots++;
                    BuildSlotsCountChanged?.Invoke(ExtraBuildSlots);
                }

                // Auto-upgrade CastleWeapon every 4 castle levels up to Lv 4 (GIG-85, GIG-93)
                UpgradeWeaponForCastleLevel(_currentLevel);

                // Increase Castle Max HP using selected Knight's HPPerLevel (GIG-17, GIG-97)
                Castle targetCastle = CastleInstance;
                if (targetCastle != null)
                {
                    int hpGain = ActiveKnight != null && ActiveKnight.HPPerLevel > 0 ? ActiveKnight.HPPerLevel : 100;
                    targetCastle.IncreaseMaxHealth(hpGain, healAmount: true);
                }

                _pendingUpgradeSelections++;
                LevelUp?.Invoke(_currentLevel);
                NotifyXPChanged();
            }

            if (leveledUp && CanOpenModalRightNow())
            {
                TryPresentNextPendingUpgrade(viaChest: true);
            }
        }

        public void AddHonor(int amount)
        {
            AddCombatGoldXP(amount);
        }

        private bool CanOpenModalRightNow()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return false;
            }

            if (WaveController.Instance != null)
            {
                WaveState state = WaveController.Instance.CurrentState;
                if (state == WaveState.WaveActive || state == WaveState.WaveClearing)
                {
                    return false;
                }
            }

            return true;
        }

        private void UpgradeWeaponForCastleLevel(int castleLevel)
        {
            CastleWeapon weapon = WeaponInstance;
            if (weapon == null) return;

            // Level 1 (Lv 1-3), Level 2 (Lv 4-7), Level 3 (Lv 8-11), Level 4 (Lv 12+)
            int targetWeaponLevel = Mathf.Clamp(1 + castleLevel / 4, 1, 4);
            if (weapon.CurrentLevel < targetWeaponLevel)
            {
                weapon.SetLevel(targetWeaponLevel);
            }
        }

        private void NotifyXPChanged()
        {
            HonorChanged?.Invoke(_currentXP, _xpToNextLevel);
            XPChanged?.Invoke(_currentXP, _xpToNextLevel);
        }

        private void RecalculateXPThreshold()
        {
            _xpToNextLevel = 50 + _currentLevel * 25;
        }

        public List<UpgradeData> GenerateWeightedUpgradeChoices(int count = 3)
        {
            if (_upgradeDatabase == null) return new List<UpgradeData>();

            return _upgradeDatabase.GetRandomUpgrades(
                count,
                null,
                _takenUpgradeCounts,
                _currentLevel,
                Building.ActiveBuildings,
                ActiveKnight);
        }

        public bool TryPresentNextPendingUpgrade(bool viaChest = true)
        {
            if (_pendingUpgradeSelections <= 0 || _upgradeDatabase == null) return false;

            List<UpgradeData> choices = GenerateWeightedUpgradeChoices(3);
            if (choices == null || choices.Count == 0)
            {
                _pendingUpgradeSelections = 0;
                return false;
            }

            _pendingUpgradeSelections--;

            if (viaChest && ChestSelectionRequested != null)
            {
                ChestSelectionRequested.Invoke(choices);
            }
            else
            {
                UpgradeSelectionRequested?.Invoke(choices);
            }

            return true;
        }

        public void RequestUpgradeSelection()
        {
            List<UpgradeData> choices = GenerateWeightedUpgradeChoices(3);
            UpgradeSelectionRequested?.Invoke(choices);
        }

        public int GetTakenUpgradeCount(string upgradeId)
        {
            if (string.IsNullOrEmpty(upgradeId)) return 0;
            return _takenUpgradeCounts.TryGetValue(upgradeId, out int count) ? count : 0;
        }

        public void ApplyUpgrade(UpgradeData upgrade)
        {
            if (upgrade == null) return;

            _appliedUpgrades.Add(upgrade);
            if (!string.IsNullOrEmpty(upgrade.Id))
            {
                _takenUpgradeCounts.TryGetValue(upgrade.Id, out int count);
                _takenUpgradeCounts[upgrade.Id] = count + 1;
            }

            ApplyUpgradeEffect(upgrade.EffectType, upgrade.Value);
            UpgradeApplied?.Invoke(upgrade);
        }

        public void ApplyShopBonusEffect(UpgradeEffectType effectType, float value, string label = null)
        {
            ApplyUpgradeEffect(effectType, value);
            UpgradeApplied?.Invoke(null);
        }

        private void ApplyUpgradeEffect(UpgradeEffectType effectType, float value)
        {
            switch (effectType)
            {
                case UpgradeEffectType.CastleMaxHealthPercent:
                    if (CastleInstance != null)
                    {
                        int hpBonus = Mathf.RoundToInt(CastleInstance.MaxHP * value);
                        CastleInstance.IncreaseMaxHealth(hpBonus, healAmount: true);
                    }
                    break;

                case UpgradeEffectType.CastleMaxHealthFlat:
                    if (CastleInstance != null)
                    {
                        CastleInstance.IncreaseMaxHealth(Mathf.RoundToInt(value), healAmount: true);
                    }
                    break;

                case UpgradeEffectType.CastleHealPercent:
                    if (CastleInstance != null)
                    {
                        CastleInstance.HealPercent(value);
                    }
                    break;

                case UpgradeEffectType.BuildSlotAdd:
                    ExtraBuildSlots += Mathf.RoundToInt(value);
                    BuildSlotsCountChanged?.Invoke(ExtraBuildSlots);
                    break;

                case UpgradeEffectType.RangedDamagePercent:
                    RangedDamageMultiplier += value;
                    break;

                case UpgradeEffectType.MeleeDamagePercent:
                    MeleeDamageMultiplier += value;
                    break;

                case UpgradeEffectType.AttackSpeedPercent:
                    AttackSpeedMultiplier += value;
                    break;

                case UpgradeEffectType.WeaponDamagePercent:
                    WeaponDamageMultiplier += value;
                    break;

                case UpgradeEffectType.WeaponCooldownPercent:
                    WeaponCooldownMultiplier = Mathf.Max(0.2f, WeaponCooldownMultiplier - value);
                    break;

                case UpgradeEffectType.UnitHealthPercent:
                    UnitHealthMultiplier += value;
                    break;

                case UpgradeEffectType.UnitRespawnPercent:
                    UnitRespawnMultiplier = Mathf.Max(0.2f, UnitRespawnMultiplier - value);
                    break;

                case UpgradeEffectType.BuildingHealthPercent:
                    BuildingHealthMultiplier += value;
                    break;

                case UpgradeEffectType.CoinPerKill:
                    BonusCoinsPerKill += Mathf.RoundToInt(value);
                    break;

                case UpgradeEffectType.DiamondDropChanceAdd:
                    ExtraDiamondDropChance += value;
                    break;

                case UpgradeEffectType.SlowOnHitPercent:
                    SlowOnHitPercent = Mathf.Max(SlowOnHitPercent, value);
                    break;

                case UpgradeEffectType.TrapDamagePercent:
                    TrapDamageMultiplier += value;
                    break;

                case UpgradeEffectType.BombDamagePercent:
                    BombDamageMultiplier += value;
                    break;

                case UpgradeEffectType.EconomicSpeedPercent:
                    EconomicSpeedMultiplier += value;
                    break;

                case UpgradeEffectType.SpellDamagePercent:
                    SpellDamageMultiplier += value;
                    break;

                case UpgradeEffectType.BuildingCostDiscountPercent:
                    BuildingCostDiscountPercent = Mathf.Min(0.5f, BuildingCostDiscountPercent + value);
                    break;

                case UpgradeEffectType.SupportEffectivenessPercent:
                    SupportEffectivenessMultiplier += value;
                    break;

                case UpgradeEffectType.AggroLimitAdd:
                    BonusAggroLimit += Mathf.RoundToInt(value);
                    break;

                case UpgradeEffectType.RepairAllBuildingsPercent:
                    var active = Building.ActiveBuildings;
                    for (int i = 0; i < active.Count; i++)
                    {
                        Building b = active[i];
                        if (b != null && !b.IsDead)
                        {
                            b.Repair(Mathf.RoundToInt(b.MaxHP * value));
                        }
                    }
                    break;
            }
        }

        // --- Unified Modifier Helpers (Upgrades + Meta + Selected Knight Passives) ---

        public float GetBuildingMaxHpMultiplier(BuildingData data)
        {
            float mult = BuildingHealthMultiplier;
            KnightData knight = ActiveKnight;
            if (knight == null || data == null) return mult;

            if (data.IsDestructible)
            {
                mult += knight.DestructibleBuildingHpBonus;
            }

            if (data.Category != BuildingCategory.Economy)
            {
                mult += knight.NonEconomyBuildingHpBonus;
            }

            return Mathf.Max(0.1f, mult);
        }

        public int GetBuildingMaxHpFlatBonus(BuildingData data)
        {
            KnightData knight = ActiveKnight;
            if (knight == null || data == null) return 0;
            return data.Category == BuildingCategory.Economy ? knight.EconomyBuildingHpFlatBonus : 0;
        }

        public float GetEffectiveEconomySpeedMultiplier()
        {
            float knightBonus = ActiveKnight != null ? ActiveKnight.EconomySpeedBonus : 0f;
            return Mathf.Max(0.1f, EconomicSpeedMultiplier + _metaEconomySpeedBonus + knightBonus);
        }

        public float GetEffectiveUnitDamageMultiplier(bool isRanged, bool isArcherUnit = false)
        {
            float mult = (isRanged ? RangedDamageMultiplier : MeleeDamageMultiplier) + _metaCombatDamageBonus;
            if (isArcherUnit && ActiveKnight != null)
            {
                mult += ActiveKnight.RangedUnitDamageBonus;
            }
            return Mathf.Max(0.1f, mult);
        }

        public float GetEffectiveBuildingAttackDamageMultiplier(BuildingCategory category)
        {
            float baseMult = (category == BuildingCategory.Archers || category == BuildingCategory.Siege
                ? RangedDamageMultiplier
                : MeleeDamageMultiplier) + _metaCombatDamageBonus;
            return Mathf.Max(0.1f, baseMult);
        }

        public float GetEffectiveUnitMaxHpMultiplier()
        {
            float knightBonus = ActiveKnight != null ? ActiveKnight.UnitMaxHpBonus : 0f;
            return Mathf.Max(0.1f, UnitHealthMultiplier + knightBonus);
        }

        public int GetEffectiveUnitAggroBonus()
        {
            int knightBonus = ActiveKnight != null ? ActiveKnight.UnitAggroBonus : 0;
            return BonusAggroLimit + knightBonus;
        }

        public float GetEffectiveAttackSpeedMultiplier() => Mathf.Max(0.1f, AttackSpeedMultiplier);

        public float GetEffectiveUnitRespawnMultiplier() => Mathf.Max(0.2f, UnitRespawnMultiplier);

        public float GetEffectiveMageCooldownMultiplier()
        {
            float knightReduction = ActiveKnight != null ? ActiveKnight.MageCooldownReduction : 0f;
            return Mathf.Clamp(1f - knightReduction, 0.25f, 1.5f);
        }

        public float GetEffectiveSupportMultiplier()
        {
            float knightBonus = ActiveKnight != null ? ActiveKnight.SupportEffectivenessBonus : 0f;
            return Mathf.Max(0.1f, SupportEffectivenessMultiplier + knightBonus);
        }

        public float GetEffectiveExplosiveDamageMultiplier()
        {
            float knightBonus = ActiveKnight != null ? ActiveKnight.ExplosiveDamageBonus : 0f;
            return Mathf.Max(0.1f, BombDamageMultiplier + _metaCombatDamageBonus + knightBonus);
        }

        public float GetExplosiveRadiusBonus()
        {
            float knightRadius = ActiveKnight != null ? ActiveKnight.ExplosiveRadiusBonus : 0f;
            return knightRadius;
        }

        public float GetEffectivePiercingCooldownMultiplier()
        {
            float knightSpeed = ActiveKnight != null ? ActiveKnight.PiercingCooldownSpeedBonus : 0f;
            return 1f / Mathf.Max(0.1f, 1f + knightSpeed);
        }

        public float GetPiercingRangeBonus()
        {
            return ActiveKnight != null ? ActiveKnight.PiercingRangeBonus : 0f;
        }

        public float GetEffectiveWeaponDamageMultiplier()
        {
            return Mathf.Max(0.1f, WeaponDamageMultiplier + _metaWeaponSpellBonus);
        }

        public float GetEffectiveWeaponCooldownMultiplier()
        {
            return Mathf.Clamp(WeaponCooldownMultiplier, 0.2f, 2f);
        }

        public float GetEffectiveSpellDamageMultiplier()
        {
            return Mathf.Max(0.1f, SpellDamageMultiplier + _metaWeaponSpellBonus);
        }

        public float GetEffectiveTrapDamageMultiplier()
        {
            float knightBonus = ActiveKnight != null ? ActiveKnight.TrapDamageBonus : 0f;
            return Mathf.Max(0.1f, TrapDamageMultiplier + _metaCombatDamageBonus + knightBonus);
        }

        public float GetEffectiveTrapUsesMultiplier()
        {
            float knightBonus = ActiveKnight != null ? ActiveKnight.TrapUsesBonus : 0f;
            return Mathf.Max(1f, 1f + knightBonus);
        }

        public void GetControlDamageSlow(out float slowPercent, out float slowDuration)
        {
            slowPercent = ActiveKnight != null ? ActiveKnight.ControlSlowPercent : 0f;
            slowDuration = ActiveKnight != null ? ActiveKnight.ControlSlowDuration : 0f;
        }

        public float GetHunterStealthBonusSeconds()
        {
            return ActiveKnight != null ? ActiveKnight.HunterStealthBonusSeconds : 0f;
        }

        public float GetBuilderRepairSpeedMultiplier()
        {
            float knightBonus = ActiveKnight != null ? ActiveKnight.BuilderRepairSpeedBonus : 0f;
            return Mathf.Max(0.1f, GetEffectiveSupportMultiplier() + knightBonus);
        }

        public float GetBuilderRangeBonus()
        {
            return ActiveKnight != null ? ActiveKnight.BuilderRangeBonus : 0f;
        }

        public int GetEffectiveCoinPerKillBonus()
        {
            int knightBonus = ActiveKnight != null ? ActiveKnight.BonusCoinPerKill : 0;
            return BonusCoinsPerKill + knightBonus;
        }

        public float GetEffectiveDiamondDropChanceBonus()
        {
            return ExtraDiamondDropChance + _metaDiamondChanceBonus;
        }

        public float GetEffectiveUpgradeCostMultiplier()
        {
            return Mathf.Clamp(1f - BuildingCostDiscountPercent, 0.5f, 1f);
        }

        [Button]
        public void DebugAdd100Honor()
        {
            AddCombatGoldXP(100);
        }
    }
}
