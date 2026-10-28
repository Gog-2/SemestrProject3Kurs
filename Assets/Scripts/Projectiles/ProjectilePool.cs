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
            Projectile projectile;
            if (_projectilePrefab != null)
            {
                projectile = Instantiate(_projectilePrefab, transform);
            }
            else
            {
                // Fallback runtime creation if prefab not assigned
                GameObject go = new GameObject("RuntimeProjectile");
                go.transform.SetParent(transform);
                var sr = go.AddComponent<SpriteRenderer>();
                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.25f;
                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.useFullKinematicContacts = true;
                int projLayer = LayerMask.NameToLayer("Projectile");
                if (projLayer != -1) go.layer = projLayer;
                sr.sortingLayerName = "Projectiles";
                sr.sortingOrder = 15;
                projectile = go.AddComponent<Projectile>();
            }

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
