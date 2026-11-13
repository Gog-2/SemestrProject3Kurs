using System;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Units
{
    public class AllyUnitPool : MonoBehaviour
    {
        public static AllyUnitPool Instance { get; private set; }

        [SerializeField] private AllyUnit _prefab;
        [SerializeField, Min(4)] private int _defaultCapacity = 16;
        [SerializeField, Min(8)] private int _maxPoolSize = 100;

        private IObjectPool<AllyUnit> _pool;

        public static AllyUnitPool GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var poolGo = new GameObject("[Pool] AllyUnitPool");
            return poolGo.AddComponent<AllyUnitPool>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsurePool();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            _pool?.Clear();
        }

        public AllyUnit Spawn(Vector3 position, string unitName = null)
        {
            EnsurePool();
            AllyUnit unit = _pool.Get();
            if (!string.IsNullOrEmpty(unitName))
            {
                unit.gameObject.name = unitName;
            }
            unit.transform.position = position;
            unit.SetReleaseCallback(Release);
            return unit;
        }

        public void Release(AllyUnit unit)
        {
            if (unit == null || !unit.gameObject.activeSelf)
            {
                return;
            }

            EnsurePool();
            _pool.Release(unit);
        }

        private void EnsurePool()
        {
            if (_pool != null)
            {
                return;
            }

            _pool = new ObjectPool<AllyUnit>(
                createFunc: CreateUnit,
                actionOnGet: OnGetFromPool,
                actionOnRelease: OnReleaseToPool,
                actionOnDestroy: OnDestroyPoolObject,
                collectionCheck: true,
                defaultCapacity: _defaultCapacity,
                maxSize: _maxPoolSize
            );
        }

        private AllyUnit CreateUnit()
        {
            AllyUnit unit;
            if (_prefab != null)
            {
                unit = Instantiate(_prefab, transform);
            }
            else
            {
                var go = new GameObject("AllyUnit");
                go.transform.SetParent(transform, false);
                unit = go.AddComponent<AllyUnit>();
            }

            unit.gameObject.SetActive(false);
            return unit;
        }

        private void OnGetFromPool(AllyUnit unit)
        {
            unit.OnSpawnFromPool();
        }

        private void OnReleaseToPool(AllyUnit unit)
        {
            unit.OnReturnToPool();
        }

        private void OnDestroyPoolObject(AllyUnit unit)
        {
            if (unit != null)
            {
                Destroy(unit.gameObject);
            }
        }
    }
}
