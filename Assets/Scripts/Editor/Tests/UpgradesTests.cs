using System;
using NUnit.Framework;
using UnityEngine;
using Game.Upgrades;

namespace Game.Editor.Tests
{
    [TestFixture]
    [Category("Upgrades")]
    public class UpgradesTests
    {
        [Test]
        public void UpgradeEffectRegistry_HasCommandRegisteredForEveryEffectType()
        {
            var registry = new UpgradeEffectRegistry();
            var state = new RunModifierState();

            foreach (UpgradeEffectType effectType in Enum.GetValues(typeof(UpgradeEffectType)))
            {
                bool applied = registry.TryApply(effectType, 0.1f, state);
                Assert.IsTrue(applied, $"UpgradeEffectType.{effectType} is missing from UpgradeEffectRegistry.");
            }
        }

        [Test]
        public void UpgradeEffectRegistry_AppliesMultipliersCapsAndBuildSlotCallbacks()
        {
            int reportedExtraSlots = 0;
            var state = new RunModifierState(getCastle: null, onBuildSlotsChanged: slots => reportedExtraSlots = slots);
            var registry = new UpgradeEffectRegistry();

            Assert.IsTrue(registry.TryApply(UpgradeEffectType.WeaponCooldownPercent, 2.0f, state));
            Assert.AreEqual(0.2f, state.WeaponCooldownMultiplier, 0.0001f);

            Assert.IsTrue(registry.TryApply(UpgradeEffectType.UnitRespawnPercent, 5.0f, state));
            Assert.AreEqual(0.2f, state.UnitRespawnMultiplier, 0.0001f);

            Assert.IsTrue(registry.TryApply(UpgradeEffectType.BuildingCostDiscountPercent, 0.9f, state));
            Assert.AreEqual(0.5f, state.BuildingCostDiscountPercent, 0.0001f);

            Assert.IsTrue(registry.TryApply(UpgradeEffectType.BuildSlotAdd, 2f, state));
            Assert.AreEqual(2, state.ExtraBuildSlots);
            Assert.AreEqual(2, reportedExtraSlots);

            Assert.IsTrue(registry.TryApply(UpgradeEffectType.CoinPerKill, 3f, state));
            Assert.AreEqual(3, state.BonusCoinsPerKill);

            Assert.IsTrue(registry.TryApply(UpgradeEffectType.SlowOnHitPercent, 0.35f, state));
            Assert.IsTrue(registry.TryApply(UpgradeEffectType.SlowOnHitPercent, 0.15f, state));
            Assert.AreEqual(0.35f, state.SlowOnHitPercent, 0.0001f);
        }

        [Test]
        public void UpgradeData_AffinityTag_MapsRepresentativeEffectTypes()
        {
            UpgradeData data = ScriptableObject.CreateInstance<UpgradeData>();
            try
            {
                data.InitializeRuntime("u1", "T", "D", UpgradeEffectType.CastleMaxHealthFlat, 50f);
                Assert.AreEqual(UpgradeAffinityTag.Castle, data.AffinityTag);

                data.InitializeRuntime("u2", "T", "D", UpgradeEffectType.RangedDamagePercent, 0.2f);
                Assert.AreEqual(UpgradeAffinityTag.Ranged, data.AffinityTag);

                data.InitializeRuntime("u3", "T", "D", UpgradeEffectType.CoinPerKill, 1f);
                Assert.AreEqual(UpgradeAffinityTag.Economy, data.AffinityTag);

                data.InitializeRuntime("u4", "T", "D", UpgradeEffectType.SpellDamagePercent, 0.2f);
                Assert.AreEqual(UpgradeAffinityTag.Magic, data.AffinityTag);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(data);
            }
        }
    }
}
