using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Game.Spells;

namespace Game.Editor.Tests
{
    [TestFixture]
    [Category("Spells")]
    public class SpellsTests
    {
        private GameObject _scrollManagerGo;
        private SpellScrollManager _scrollManager;
        private SpellData _tempSpell;

        [SetUp]
        public void SetUp()
        {
            _tempSpell = ScriptableObject.CreateInstance<SpellData>();
            _tempSpell.InitializeRuntime(
                SpellKind.Meteor,
                "TestMeteor",
                "Test Desc",
                Color.red,
                new[]
                {
                    new SpellLevelStats { primaryDamage = 100, areaRadius = 1f },
                    new SpellLevelStats { primaryDamage = 200, areaRadius = 1.5f },
                    new SpellLevelStats { primaryDamage = 300, areaRadius = 2f },
                    new SpellLevelStats { primaryDamage = 400, areaRadius = 2.5f }
                });
        }

        [TearDown]
        public void TearDown()
        {
            if (_scrollManagerGo != null)
            {
                Object.DestroyImmediate(_scrollManagerGo);
                _scrollManagerGo = null;
                _scrollManager = null;
            }
            if (_tempSpell != null)
            {
                Object.DestroyImmediate(_tempSpell);
                _tempSpell = null;
            }
        }

        [Test]
        public void SpellDataAssets_AllFiveExistWithAssignedEffectStrategies()
        {
            var expected = new (string path, System.Type effectType, SpellKind kind)[]
            {
                ("Assets/ScriptbleObjects/Spells/Spell_Meteor.asset", typeof(MeteorSpellEffect), SpellKind.Meteor),
                ("Assets/ScriptbleObjects/Spells/Spell_IceStorm.asset", typeof(IceStormSpellEffect), SpellKind.IceStorm),
                ("Assets/ScriptbleObjects/Spells/Spell_HeavenlyWrath.asset", typeof(HeavenlyWrathSpellEffect), SpellKind.HeavenlyWrath),
                ("Assets/ScriptbleObjects/Spells/Spell_DeadLegion.asset", typeof(DeadLegionSpellEffect), SpellKind.DeadLegion),
                ("Assets/ScriptbleObjects/Spells/Spell_NewLife.asset", typeof(NewLifeSpellEffect), SpellKind.NewLife),
            };

            foreach ((string path, System.Type effectType, SpellKind kind) in expected)
            {
                SpellData data = AssetDatabase.LoadAssetAtPath<SpellData>(path);
                Assert.NotNull(data, $"Missing SpellData asset: {path}");
                Assert.AreEqual(kind, data.Kind, $"{path} has unexpected SpellKind");
                Assert.NotNull(data.Effect, $"{path} has null SpellEffect strategy");
                Assert.IsInstanceOf(effectType, data.Effect, $"{path} has wrong SpellEffect strategy type");
            }
        }

        [Test]
        public void SpellData_GetStats_ClampsOutOfRangeLevels()
        {
            Assert.AreEqual(100, _tempSpell.GetStats(0).primaryDamage);
            Assert.AreEqual(100, _tempSpell.GetStats(1).primaryDamage);
            Assert.AreEqual(400, _tempSpell.GetStats(4).primaryDamage);
            Assert.AreEqual(400, _tempSpell.GetStats(99).primaryDamage);
        }

        [Test]
        public void SpellScrollManager_EnforcesCapacitySelectionToggleAndConsume()
        {
            _scrollManagerGo = new GameObject("Test_ScrollManager") { hideFlags = HideFlags.HideAndDontSave };
            _scrollManager = _scrollManagerGo.AddComponent<SpellScrollManager>();

            for (int i = 0; i < SpellScrollManager.MaxScrollSlots; i++)
            {
                Assert.IsTrue(_scrollManager.TryAddScroll(_tempSpell, i + 1));
            }

            Assert.IsTrue(_scrollManager.IsFull);
            Assert.IsFalse(_scrollManager.CanAddScroll());
            Assert.IsFalse(_scrollManager.TryAddScroll(_tempSpell, 1));

            _scrollManager.SelectScroll(1);
            Assert.IsTrue(_scrollManager.HasSelection);
            Assert.AreEqual(1, _scrollManager.SelectedIndex);

            // Selecting same slot cancels selection
            _scrollManager.SelectScroll(1);
            Assert.IsFalse(_scrollManager.HasSelection);

            _scrollManager.SelectScroll(2);
            Assert.IsTrue(_scrollManager.TryConsumeSelectedScroll(out SpellScrollInstance consumed));
            Assert.AreSame(_tempSpell, consumed.Data);
            Assert.AreEqual(3, consumed.Level);
            Assert.AreEqual(SpellScrollManager.MaxScrollSlots - 1, _scrollManager.Scrolls.Count);
            Assert.IsFalse(_scrollManager.HasSelection);
        }
    }
}
