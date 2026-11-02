using UnityEngine;
using UnityEngine.Pool;
using Alchemy.Inspector;

namespace Game.Projectiles
{
    public class ProjectilePool : MonoBehaviour
    {
        public static ProjectilePool Instance { get; private set; }

        [Title("Prefab Reference")]
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField, Min(5)] private int _defaultCapacity = 30;
        [SerializeField, Min(10)] private int _maxPoolSize = 150;

        private IObjectPool<Projectile> _pool;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _pool = new ObjectPool<Projectile>(
                createFunc: CreateProjectile,
                actionOnGet: OnGetFromPool,
                actionOnRelease: OnReleaseToPool,
                actionOnDestroy: OnDestroyPoolObject,
                collectionCheck: true,
                defaultCapacity: _defaultCapacity,
                maxSize: _maxPoolSize
            );
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            _pool?.Clear();
        }

        public Projectile Spawn(Vector3 position, Vector2 direction, ProjectileData data, int damageOverride = -1)
        {
            Projectile projectile = _pool.Get();
            projectile.Initialize(position, direction, data, damageOverride, Release);
            return projectile;
        }

        public void Release(Projectile projectile)
        {
            if (projectile != null && projectile.gameObject.activeSelf)
            {
                _pool.Release(projectile);
            }
        }

        private Projectile CreateProjectile()
        {
            if (_projectilePrefab == null)
            {
                throw new System.InvalidOperationException("ProjectilePool requires _projectilePrefab to be assigned.");
            }

            Projectile projectile = Instantiate(_projectilePrefab, transform);
            projectile.gameObject.SetActive(false);
            return projectile;
        }

        private void OnGetFromPool(Projectile projectile)
        {
            projectile.OnSpawnFromPool();
        }

        private void OnReleaseToPool(Projectile projectile)
        {
            projectile.OnReturnToPool();
        }

        private void OnDestroyPoolObject(Projectile projectile)
        {
            if (projectile != null)
            {
                Destroy(projectile.gameObject);
            }
        }
    }
}
