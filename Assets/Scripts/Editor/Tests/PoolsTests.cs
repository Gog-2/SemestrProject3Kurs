using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Game.Enemies;
using Game.Enemies.Data;
using Game.Enemies.Spawning;
using Game.Units;

namespace Game.Editor.Tests
{
    [TestFixture]
    [Category("Pools")]
    public class PoolsTests
    {
        private GameObject _enemyPoolGo;
        private EnemyPool _enemyPool;
        private GameObject _allyPoolGo;
        private AllyUnitPool _allyPool;
        private EnemyData _enemyData;

        [SetUp]
        public void SetUp()
        {
            _enemyData = ScriptableObject.CreateInstance<EnemyData>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_enemyPoolGo != null)
            {
                Object.DestroyImmediate(_enemyPoolGo);
                _enemyPoolGo = null;
                _enemyPool = null;
            }
            if (_allyPoolGo != null)
            {
                Object.DestroyImmediate(_allyPoolGo);
                _allyPoolGo = null;
                _allyPool = null;
            }
            if (_enemyData != null)
            {
                Object.DestroyImmediate(_enemyData);
                _enemyData = null;
            }
        }

        [Test]
        public void EnemyPool_GetAndRelease_ReusesInstanceAndResetsHpStatusesAndRetreat()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Regular.prefab");
            Assert.NotNull(prefab, "Missing Assets/Prefabs/Enemy_Regular.prefab");

            _enemyPoolGo = new GameObject("Test_EnemyPool") { hideFlags = HideFlags.HideAndDontSave };
            _enemyPool = _enemyPoolGo.AddComponent<EnemyPool>();

            var so = new SerializedObject(_enemyPool);
            so.FindProperty("_enemyPrefab").objectReferenceValue = prefab.GetComponent<Enemy>();
            so.ApplyModifiedPropertiesWithoutUndo();

            MethodInfo poolAwake = typeof(EnemyPool).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            poolAwake?.Invoke(_enemyPool, null);

            Enemy first = _enemyPool.Get();
            Assert.NotNull(first);
            MethodInfo enemyAwake = typeof(Enemy).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            enemyAwake?.Invoke(first, null);

            first.Initialize(_enemyData, null, null, null);
            first.TakeDamage(5);
            first.Freeze(3f);
            first.BeginRetreat();

            Assert.Less(first.CurrentHP, first.MaxHP);
            Assert.IsTrue(first.IsFrozen);
            Assert.IsTrue(first.IsRetreating);

            _enemyPool.Release(first);
            Assert.IsFalse(first.gameObject.activeSelf);

            Enemy reused = _enemyPool.Get();
            Assert.AreSame(first, reused);
            Assert.IsTrue(reused.gameObject.activeSelf);
            Assert.AreEqual(reused.MaxHP, reused.CurrentHP);
            Assert.IsFalse(reused.IsFrozen);
            Assert.IsFalse(reused.IsRetreating);
        }

        [Test]
        public void AllyUnitPool_SpawnAndRelease_ReusesUnitAndRestoresPooledState()
        {
            _allyPoolGo = new GameObject("Test_AllyUnitPool") { hideFlags = HideFlags.HideAndDontSave };
            _allyPool = _allyPoolGo.AddComponent<AllyUnitPool>();

            AllyUnit first = _allyPool.Spawn(Vector3.zero, "KnightGuard");
            Assert.NotNull(first);
            Assert.IsTrue(first.gameObject.activeSelf);

            first.Initialize(
                spawner: null,
                homePos: Vector3.zero,
                baseMaxHp: 60,
                baseDamage: 12,
                attackInterval: 1f,
                attackRange: 1f,
                aggroLimit: 2,
                patrolRadius: 2f,
                isRanged: false,
                isArcherUnit: false,
                tint: Color.white);

            int deathCount = 0;
            first.Died += _ => deathCount++;
            first.TakeDamage(999);

            Assert.AreEqual(1, deathCount);
            Assert.IsFalse(first.gameObject.activeSelf);
            Assert.IsFalse(AllyUnit.ActiveUnits.Contains(first));

            AllyUnit second = _allyPool.Spawn(new Vector3(1f, 2f, 0f), "KnightGuard_2");
            Assert.AreSame(first, second);
            Assert.IsTrue(second.gameObject.activeSelf);
            Assert.AreEqual(new Vector3(1f, 2f, 0f), second.transform.position);

            // Re-initializing after pool reuse restores HP and must not carry over old Died subscribers
            second.Initialize(
                spawner: null,
                homePos: new Vector3(1f, 2f, 0f),
                baseMaxHp: 80,
                baseDamage: 15,
                attackInterval: 1f,
                attackRange: 1f,
                aggroLimit: 2,
                patrolRadius: 2f,
                isRanged: false,
                isArcherUnit: false,
                tint: Color.white);
            Assert.AreEqual(80, second.CurrentHP);
            Assert.IsFalse(second.IsDead);
            second.TakeDamage(999);
            Assert.AreEqual(1, deathCount); // previous Died handler was cleared on OnReturnToPool

            _allyPool.Spawn(Vector3.zero, "KnightGuard_3");
            _allyPool.Release(second);
            Assert.IsFalse(second.gameObject.activeSelf);
        }

        [Test]
        public void AllyUnit_OnEnableAndOnDisable_TracksActiveUnitsWithoutDuplicates()
        {
            var go = new GameObject("Test_AllyUnitActive") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                AllyUnit unit = go.AddComponent<AllyUnit>();
                MethodInfo onEnable = typeof(AllyUnit).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo onDisable = typeof(AllyUnit).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);

                int beforeCount = AllyUnit.ActiveUnits.Count;
                onEnable?.Invoke(unit, null);
                onEnable?.Invoke(unit, null); // duplicate call must not add twice
                Assert.IsTrue(AllyUnit.ActiveUnits.Contains(unit));
                Assert.AreEqual(beforeCount + 1, AllyUnit.ActiveUnits.Count);

                onDisable?.Invoke(unit, null);
                Assert.IsFalse(AllyUnit.ActiveUnits.Contains(unit));
                Assert.AreEqual(beforeCount, AllyUnit.ActiveUnits.Count);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
