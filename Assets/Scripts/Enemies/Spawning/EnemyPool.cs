using UnityEngine;
using UnityEngine.Pool;
using Alchemy.Inspector;

namespace Game.Enemies.Spawning
{
    public class EnemyPool : MonoBehaviour
    {
        [Title("Prefab Reference")]
        [SerializeField] private Enemy _enemyPrefab;
        [SerializeField, Min(5)] private int _defaultCapacity = 20;
        [SerializeField, Min(10)] private int _maxPoolSize = 100;

        private IObjectPool<Enemy> _pool;

        private void Awake()
        {
            _pool = new ObjectPool<Enemy>(
                createFunc: CreateEnemy,
                actionOnGet: OnGetFromPool,
                actionOnRelease: OnReleaseToPool,
                actionOnDestroy: OnDestroyPoolObject,
                collectionCheck: true,
                defaultCapacity: _defaultCapacity,
                maxSize: _maxPoolSize
            );
        }

        public Enemy Get()
        {
            return _pool.Get();
        }

        public void Release(Enemy enemy)
        {
            _pool.Release(enemy);
        }

        private void OnDestroy()
        {
            _pool?.Clear();
        }

        private Enemy CreateEnemy()
        {
            Enemy enemy = Instantiate(_enemyPrefab, transform);
            enemy.gameObject.SetActive(false);
            return enemy;
        }

        private void OnGetFromPool(Enemy enemy)
        {
            enemy.OnSpawnFromPool();
        }

        private void OnReleaseToPool(Enemy enemy)
        {
            enemy.OnReturnToPool();
        }

        private void OnDestroyPoolObject(Enemy enemy)
        {
            Destroy(enemy.gameObject);
        }
    }
}
