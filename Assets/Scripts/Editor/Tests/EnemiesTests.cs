using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Game.Enemies;
using Game.Enemies.Data;

namespace Game.Editor.Tests
{
    [TestFixture]
    [Category("Enemies")]
    public class EnemiesTests
    {
        private GameObject _enemyPrefab;
        private Enemy _enemy;
        private EnemyData _tempData;

        [SetUp]
        public void SetUp()
        {
            _enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Regular.prefab");
            Assert.NotNull(_enemyPrefab, "Missing Assets/Prefabs/Enemy_Regular.prefab");

            GameObject go = Object.Instantiate(_enemyPrefab);
            go.hideFlags = HideFlags.HideAndDontSave;
            _enemy = go.GetComponent<Enemy>();
            Assert.NotNull(_enemy, "Enemy_Regular.prefab is missing Enemy component");

            MethodInfo awake = typeof(Enemy).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            awake?.Invoke(_enemy, null);

            _tempData = ScriptableObject.CreateInstance<EnemyData>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_enemy != null)
            {
                Object.DestroyImmediate(_enemy.gameObject);
                _enemy = null;
            }
            if (_tempData != null)
            {
                Object.DestroyImmediate(_tempData);
                _tempData = null;
            }
        }

        private static EnemyData LoadEnemyAsset(string path)
        {
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            Assert.NotNull(data, $"Missing EnemyData asset: {path}");
            return data;
        }

        private static void InvokeEnemyUpdate(Enemy enemy)
        {
            MethodInfo update = typeof(Enemy).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            update?.Invoke(enemy, null);
        }

        [Test]
        public void Initialize_SetsScaledStatsFromEnemyData_AndClearsStateFlags()
        {
            EnemyData boss = LoadEnemyAsset("Assets/ScriptbleObjects/Enemy/Enemy_Boss.asset");

            _enemy.Initialize(boss, null, null, null);

            Assert.GreaterOrEqual(_enemy.MaxHP, boss.MaxHP);
            Assert.AreEqual(_enemy.MaxHP, _enemy.CurrentHP);
            Assert.Greater(_enemy.CurrentSpeed, 0f);
            Assert.IsFalse(_enemy.IsDead);
            Assert.IsFalse(_enemy.IsRetreating);
            Assert.IsFalse(_enemy.IsFrozen);
            Assert.IsFalse(_enemy.IsStunned);
        }

        [Test]
        public void TakeDamage_RespectsDefenseAndResistance_WithMinimumOneDamage()
        {
            _tempData.SetDefenseStats(defense: 10, damageResistance: 0.5f);
            _enemy.Initialize(_tempData, null, null, null);
            int initialHp = _enemy.CurrentHP;

            _enemy.TakeDamage(8); // below flat defense -> minimum 1 damage
            Assert.AreEqual(initialHp - 1, _enemy.CurrentHP);

            _enemy.TakeDamage(30); // (30 - 10) * (1 - 0.5) = 10
            Assert.AreEqual(initialHp - 11, _enemy.CurrentHP);
        }

        [Test]
        public void TakeDamage_IgnoreDefense_BypassesArmorAndResistance()
        {
            _tempData.SetDefenseStats(defense: 15, damageResistance: 0.8f);
            _enemy.Initialize(_tempData, null, null, null);
            int initialHp = _enemy.CurrentHP;

            _enemy.TakeDamage(15, ignoreDefense: true);

            Assert.AreEqual(initialHp - 15, _enemy.CurrentHP);
        }

        [Test]
        public void BeginRetreat_EntersRetreatState_AndCompleteRetreatRaisesEvent()
        {
            _enemy.Initialize(_tempData, null, null, null);
            int retreatedEvents = 0;
            _enemy.Retreated += _ => retreatedEvents++;

            _enemy.BeginRetreat();
            _enemy.BeginRetreat(); // second call must be ignored while retreating

            Assert.IsTrue(_enemy.IsRetreating);
            Assert.IsFalse(_enemy.IsAttacking);

            MethodInfo completeRetreat = typeof(Enemy).GetMethod("CompleteRetreat", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(completeRetreat);
            completeRetreat.Invoke(_enemy, null);

            Assert.IsFalse(_enemy.IsRetreating);
            Assert.AreEqual(1, retreatedEvents);
        }

        [Test]
        public void FreezeAndStun_SetDisabledFlags_AndClearAllStatusesResetsThem()
        {
            _enemy.Initialize(_tempData, null, null, null);

            _enemy.Freeze(2f);
            _enemy.Stun(1.5f);
            InvokeEnemyUpdate(_enemy);

            Assert.IsTrue(_enemy.IsFrozen);
            Assert.IsTrue(_enemy.IsStunned);
            Assert.AreEqual(0f, _enemy.CurrentSpeed, 0.0001f);

            _enemy.ClearAllStatuses();
            InvokeEnemyUpdate(_enemy);

            Assert.IsFalse(_enemy.IsFrozen);
            Assert.IsFalse(_enemy.IsStunned);
            Assert.Greater(_enemy.CurrentSpeed, 0f);
        }

        [Test]
        public void ApplySlow_RespectsRegularEnemyMinimumSpeedFloor()
        {
            EnemyData regular = LoadEnemyAsset("Assets/ScriptbleObjects/Enemy/Enemy.asset");
            _enemy.Initialize(regular, null, null, null);
            float baseSpeed = _enemy.CurrentSpeed;

            _enemy.ApplySlow(0.95f, 3f);
            InvokeEnemyUpdate(_enemy);

            // Regular enemies clamp slow down to 80% of base speed per GDD
            Assert.AreEqual(baseSpeed * 0.8f, _enemy.CurrentSpeed, 0.01f);
        }
    }
}
