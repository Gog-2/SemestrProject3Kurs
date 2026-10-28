using System;
using System.Collections.Generic;
using UnityEngine;
using Alchemy.Inspector;
using Game.Buildings;
using Game.Upgrades;

namespace Game.Core
{
    public class RunProgressionManager : MonoBehaviour
    {
        public static RunProgressionManager Instance { get; private set; }

        [Title("References")]
        [SerializeField] private Castle _castle;
        [SerializeField] private UpgradeDatabase _upgradeDatabase;

        [Title("Progression State")]
        [ShowInInspector, ReadOnly] private int _currentLevel = 1;
        [ShowInInspector, ReadOnly] private int _currentHonor = 0;
        [ShowInInspector, ReadOnly] private int _honorToNextLevel = 75; // 50 + 1 * 25

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
        [ShowInInspector, ReadOnly] public int ExtraBuildSlots { get; private set; } = 0;

        private readonly List<UpgradeData> _appliedUpgrades = new();

        public int CurrentLevel => _currentLevel;
        public int CurrentHonor => _currentHonor;
        public int HonorToNextLevel => _honorToNextLevel;
        public IReadOnlyList<UpgradeData> AppliedUpgrades => _appliedUpgrades;
        public UpgradeDatabase Database => _upgradeDatabase;

        public event Action<int, int> HonorChanged; // current, target
        public event Action<int> LevelUp;
        public event Action<UpgradeData> UpgradeApplied;
        public event Action<List<UpgradeData>> UpgradeSelectionRequested;

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
                _castle = FindAnyObjectByType<Castle>();
            }

            if (_upgradeDatabase == null)
            {
                _upgradeDatabase = ScriptableObject.CreateInstance<UpgradeDatabase>();
                _upgradeDatabase.PopulateWithDefaultUpgrades();
            }

            RecalculateHonorThreshold();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void AddHonor(int amount)
        {
            if (amount <= 0) return;

            _currentHonor += amount;
            HonorChanged?.Invoke(_currentHonor, _honorToNextLevel);

            while (_currentHonor >= _honorToNextLevel)
            {
                _currentHonor -= _honorToNextLevel;
                _currentLevel++;
                RecalculateHonorThreshold();

                // Bonus build slot every 5 levels per GDD
                if (_currentLevel % 5 == 0)
                {
                    ExtraBuildSlots++;
                }

                // Increase castle health per level
                if (_castle != null)
                {
                    _castle.IncreaseMaxHealth(100, healAmount: true);
                }

                LevelUp?.Invoke(_currentLevel);
                HonorChanged?.Invoke(_currentHonor, _honorToNextLevel);
            }
        }

        private void RecalculateHonorThreshold()
        {
            _honorToNextLevel = 50 + _currentLevel * 25;
        }

        public void RequestUpgradeSelection()
        {
            if (_upgradeDatabase == null) return;

            List<UpgradeData> choices = _upgradeDatabase.GetRandomUpgrades(3, _appliedUpgrades);
            UpgradeSelectionRequested?.Invoke(choices);
        }

        public void ApplyUpgrade(UpgradeData upgrade)
        {
            if (upgrade == null) return;

            _appliedUpgrades.Add(upgrade);

            switch (upgrade.EffectType)
            {
                case UpgradeEffectType.CastleMaxHealthPercent:
                    if (_castle != null)
                    {
                        int hpBonus = Mathf.RoundToInt(_castle.MaxHP * upgrade.Value);
                        _castle.IncreaseMaxHealth(hpBonus, healAmount: true);
                    }
                    break;

                case UpgradeEffectType.CastleMaxHealthFlat:
                    if (_castle != null)
                    {
                        _castle.IncreaseMaxHealth(Mathf.RoundToInt(upgrade.Value), healAmount: true);
                    }
                    break;

                case UpgradeEffectType.CastleHealPercent:
                    if (_castle != null)
                    {
                        _castle.HealPercent(upgrade.Value);
                    }
                    break;

                case UpgradeEffectType.BuildSlotAdd:
                    ExtraBuildSlots += Mathf.RoundToInt(upgrade.Value);
                    break;

                case UpgradeEffectType.RangedDamagePercent:
                    RangedDamageMultiplier += upgrade.Value;
                    break;

                case UpgradeEffectType.MeleeDamagePercent:
                    MeleeDamageMultiplier += upgrade.Value;
                    break;

                case UpgradeEffectType.AttackSpeedPercent:
                    AttackSpeedMultiplier += upgrade.Value;
                    break;

                case UpgradeEffectType.WeaponDamagePercent:
                    WeaponDamageMultiplier += upgrade.Value;
                    break;

                case UpgradeEffectType.WeaponCooldownPercent:
                    WeaponCooldownMultiplier = Mathf.Max(0.2f, WeaponCooldownMultiplier - upgrade.Value);
                    break;

                case UpgradeEffectType.UnitHealthPercent:
                    UnitHealthMultiplier += upgrade.Value;
                    break;

                case UpgradeEffectType.UnitRespawnPercent:
                    UnitRespawnMultiplier = Mathf.Max(0.2f, UnitRespawnMultiplier - upgrade.Value);
                    break;

                case UpgradeEffectType.BuildingHealthPercent:
                    BuildingHealthMultiplier += upgrade.Value;
                    break;

                case UpgradeEffectType.CoinPerKill:
                    BonusCoinsPerKill += Mathf.RoundToInt(upgrade.Value);
                    break;

                case UpgradeEffectType.DiamondDropChanceAdd:
                    ExtraDiamondDropChance += upgrade.Value;
                    break;

                case UpgradeEffectType.SlowOnHitPercent:
                    SlowOnHitPercent = Mathf.Max(SlowOnHitPercent, upgrade.Value);
                    break;

                case UpgradeEffectType.TrapDamagePercent:
                    TrapDamageMultiplier += upgrade.Value;
                    break;

                case UpgradeEffectType.BombDamagePercent:
                    BombDamageMultiplier += upgrade.Value;
                    break;
            }

            UpgradeApplied?.Invoke(upgrade);
        }

        [Button]
        public void DebugAdd100Honor()
        {
            AddHonor(100);
        }
    }
}
