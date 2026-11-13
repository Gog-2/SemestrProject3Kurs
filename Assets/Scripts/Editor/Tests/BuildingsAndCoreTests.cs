using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Game.Buildings;
using Game.Core;
using Game.Data;
using Game.Upgrades;

namespace Game.Editor.Tests
{
    [TestFixture]
    [Category("BuildingsAndCore")]
    public class BuildingsAndCoreTests
    {
        private BuildingData _smallBuilding;
        private BuildingData _largeBuilding;
        private GameObject _walletGo;

        [SetUp]
        public void SetUp()
        {
            BuildingLevelStats[] levels =
            {
                new() { cost = 100, maxHP = 200 },
                new() { cost = 150, maxHP = 300 },
                new() { cost = 200, maxHP = 400 },
                new() { cost = 300, maxHP = 600 }
            };

            _smallBuilding = ScriptableObject.CreateInstance<BuildingData>();
            _smallBuilding.InitializeRuntime("small_turret", "Turret", "1x1", BuildingCategory.Archers, new Vector2Int(1, 1), BuildingUpgradeType.Local, levels);

            _largeBuilding = ScriptableObject.CreateInstance<BuildingData>();
            _largeBuilding.InitializeRuntime("large_barracks", "Barracks", "2x2", BuildingCategory.Melee, new Vector2Int(2, 2), BuildingUpgradeType.Local, levels);
        }

        [TearDown]
        public void TearDown()
        {
            if (_smallBuilding != null) UnityEngine.Object.DestroyImmediate(_smallBuilding);
            if (_largeBuilding != null) UnityEngine.Object.DestroyImmediate(_largeBuilding);
            if (_walletGo != null) UnityEngine.Object.DestroyImmediate(_walletGo);
        }

        [Test]
        public void BuildingSlotInventory_EnforcesStackCapsAndSlotLimits()
        {
            var slotsView = new List<BuildingData>();
            int notifyCount = 0;
            var inventory = new BuildingSlotInventory(slotsView, defaultMaxSlots: 2, getModifiers: null, notifySlotsChanged: () => notifyCount++);

            Assert.AreEqual(9, BuildingSlotInventory.GetStackCap(_smallBuilding));
            Assert.AreEqual(4, BuildingSlotInventory.GetStackCap(_largeBuilding));

            Assert.IsTrue(inventory.TryAddBlueprint(_smallBuilding, 10));
            Assert.AreEqual(2, inventory.Count);
            Assert.AreEqual(9, inventory.GetSlotCount(0));
            Assert.AreEqual(1, inventory.GetSlotCount(1));
            Assert.AreEqual(1, notifyCount);

            // Both slots belong to _smallBuilding; _largeBuilding cannot open a 3rd slot when limit is 2
            Assert.IsFalse(inventory.CanAddBlueprint(_largeBuilding));
            Assert.IsTrue(inventory.CanAddBlueprint(_smallBuilding));

            // Consuming one reduces slot 0 from 9 -> 8 and keeps blueprints remaining
            Assert.IsTrue(inventory.ConsumeOneBlueprint(_smallBuilding));
            Assert.AreEqual(8, inventory.GetSlotCount(0));

            Assert.IsTrue(inventory.TryConsumeSlotForSale(1, out BuildingData sold, out bool emptied));
            Assert.AreSame(_smallBuilding, sold);
            Assert.IsTrue(emptied);
            Assert.AreEqual(1, inventory.Count);
        }

        [Test]
        public void BuildingSlotInventory_ExtraSlotsModifier_ExpandsSlotLimit()
        {
            var mods = new StubRunModifiers { ExtraBuildSlots = 3 };
            var inventory = new BuildingSlotInventory(new List<BuildingData>(), defaultMaxSlots: 4, getModifiers: () => mods, notifySlotsChanged: null);

            Assert.AreEqual(7, inventory.GetMaxSlotsLimit());
        }

        [Test]
        public void PlayerWallet_AddAndSpendCoinsAndDiamonds_ValidatesBalances()
        {
            _walletGo = new GameObject("Test_Wallet") { hideFlags = HideFlags.HideAndDontSave };
            PlayerWallet wallet = _walletGo.AddComponent<PlayerWallet>();

            MethodInfo awake = typeof(PlayerWallet).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            awake?.Invoke(wallet, null);

            int startCoins = wallet.Coins;
            int coinsEvents = 0;
            wallet.CoinsChanged += _ => coinsEvents++;

            wallet.AddCoins(50);
            Assert.AreEqual(startCoins + 50, wallet.Coins);
            Assert.IsFalse(wallet.TrySpendCoins(startCoins + 500));
            Assert.AreEqual(startCoins + 50, wallet.Coins);
            Assert.IsTrue(wallet.TrySpendCoins(50));
            Assert.AreEqual(startCoins, wallet.Coins);
            Assert.AreEqual(2, coinsEvents);

            wallet.AddDiamonds(5);
            Assert.IsFalse(wallet.TrySpendDiamonds(6));
            Assert.IsTrue(wallet.TrySpendDiamonds(3));
            Assert.AreEqual(2, wallet.Diamonds);
        }

        [Test]
        public void DifficultyModifiers_AndGameConstants_HaveExpectedValues()
        {
            Assert.AreEqual(1f, DifficultyModifiers.GetEnemyDamageMultiplier(DifficultyMode.Base), 0.0001f);
            Assert.AreEqual(1.25f, DifficultyModifiers.GetEnemyDamageMultiplier(DifficultyMode.Hard), 0.0001f);
            Assert.AreEqual(1.35f, DifficultyModifiers.GetEnemyDamageMultiplier(DifficultyMode.Legendary), 0.0001f);
            Assert.AreEqual(0.75f, DifficultyModifiers.GetCoinDropMultiplier(DifficultyMode.Legendary), 0.0001f);

            Assert.IsFalse(string.IsNullOrEmpty(GameConstants.DefaultKnightId));
            Assert.IsFalse(string.IsNullOrEmpty(GameConstants.DefaultArenaId));
            Assert.IsFalse(string.IsNullOrEmpty(GameConstants.DefaultWeaponId));
        }

        private sealed class StubRunModifiers : IRunModifiers
        {
            public KnightData ActiveKnight => null;
            public int ExtraBuildSlots { get; set; }
            public event Action<int> BuildSlotsCountChanged { add { } remove { } }
            public event Action<UpgradeData> UpgradeApplied { add { } remove { } }

            public float GetBuildingMaxHpMultiplier(BuildingData data) => 1f;
            public int GetBuildingMaxHpFlatBonus(BuildingData data) => 0;
            public float GetEffectiveUpgradeCostMultiplier() => 1f;
            public float GetEffectiveEconomySpeedMultiplier() => 1f;
            public float GetEffectiveUnitDamageMultiplier(bool isRanged, bool isArcherUnit = false) => 1f;
            public float GetEffectiveBuildingAttackDamageMultiplier(BuildingCategory category) => 1f;
            public float GetEffectiveUnitMaxHpMultiplier() => 1f;
            public int GetEffectiveUnitAggroBonus() => 0;
            public float GetEffectiveAttackSpeedMultiplier() => 1f;
            public float GetEffectiveUnitRespawnMultiplier() => 1f;
            public float GetEffectiveMageCooldownMultiplier() => 1f;
            public float GetEffectiveSupportMultiplier() => 1f;
            public float GetEffectiveExplosiveDamageMultiplier() => 1f;
            public float GetExplosiveRadiusBonus() => 0f;
            public float GetEffectivePiercingCooldownMultiplier() => 1f;
            public float GetPiercingRangeBonus() => 0f;
            public float GetEffectiveWeaponDamageMultiplier() => 1f;
            public float GetEffectiveWeaponCooldownMultiplier() => 1f;
            public float GetEffectiveSpellDamageMultiplier() => 1f;
            public float GetEffectiveTrapDamageMultiplier() => 1f;
            public float GetEffectiveTrapUsesMultiplier() => 1f;
            public void GetControlDamageSlow(out float slowPercent, out float slowDuration) { slowPercent = 0f; slowDuration = 0f; }
            public float GetHunterStealthBonusSeconds() => 0f;
            public float GetBuilderRepairSpeedMultiplier() => 1f;
            public float GetBuilderRangeBonus() => 0f;
            public int GetEffectiveCoinPerKillBonus() => 0;
            public float GetEffectiveDiamondDropChanceBonus() => 0f;
        }
    }
}
