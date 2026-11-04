using UnityEngine;
using Alchemy.Inspector;
using Game.Enemies;
using Game.Projectiles;
using Game.Core;

namespace Game.Buildings
{
    public class TurretBuilding : Building
    {
        [Title("Turret Settings")]
        [SerializeField] private ProjectileData _projectileData;
        [SerializeField] private Transform _firePoint;
        [SerializeField] private float _rotationSpeed = 360f;
        [SerializeField] private float _firingAngleThreshold = 15f;

        private float _attackTimer;
        private Enemy _currentTarget;

        public float AttackRange => CurrentStats.attackRange;
        public float AttackInterval => CurrentStats.attackInterval;
        public int BaseDamage => CurrentStats.damage;
        public Vector3 FireOrigin => _firePoint != null ? _firePoint.position : transform.position;

        protected override void OnPlaced()
        {
            base.OnPlaced();
            _attackTimer = GetCalculatedAttackInterval();
        }

        private void Update()
        {
            if (IsDead) return;

            // Only attack during active playing state
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            float dt = Time.deltaTime;
            _attackTimer -= dt;

            // Target evaluation
            if (_currentTarget == null || !_currentTarget.gameObject.activeInHierarchy || _currentTarget.IsDead || !IsTargetInRange(_currentTarget))
            {
                _currentTarget = FindNearestEnemy();
            }

            if (_currentTarget != null)
            {
                Vector3 targetDelta = _currentTarget.transform.position - transform.position;
                float sqrDist = targetDelta.sqrMagnitude;

                Vector2 dir = sqrDist > 0.0001f ? (Vector2)(targetDelta / Mathf.Sqrt(sqrDist)) : Vector2.up;

                // Turret sprite and fire point face UP (+Y) by default.
                // Subtract 90 degrees to align +Y with target direction.
                float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                Quaternion targetRot = Quaternion.Euler(0, 0, targetAngle);

                Transform rotTarget = _modelTransform != null ? _modelTransform : transform;
                rotTarget.rotation = Quaternion.RotateTowards(rotTarget.rotation, targetRot, _rotationSpeed * dt);

                float angleDiff = Quaternion.Angle(rotTarget.rotation, targetRot);

                // Fire when aligned within threshold and timer is ready
                float threshold = Mathf.Max(5f, _firingAngleThreshold);
                if (angleDiff <= threshold && _attackTimer <= 0f)
                {
                    Vector2 fireDirection = rotTarget.up;
                    Fire(fireDirection);
                    _attackTimer = GetCalculatedAttackInterval();
                }
            }
        }

        private void Fire(Vector2 direction)
        {
            if (ProjectilePool.Instance == null)
            {
                Debug.LogWarning("[TurretBuilding] ProjectilePool instance not found.", this);
                return;
            }

            if (_projectileData == null)
            {
                Debug.LogWarning("[TurretBuilding] _projectileData is not assigned.", this);
                return;
            }

            int damage = GetCalculatedDamage();
            ProjectilePool.Instance.Spawn(FireOrigin, direction, _projectileData, damage);

            AddCombatXP(damage);
        }

        public int GetCalculatedDamage()
        {
            int damage = BaseDamage;
            if (RunProgressionManager.Instance != null)
            {
                damage = Mathf.RoundToInt(damage * RunProgressionManager.Instance.RangedDamageMultiplier);
            }
            return damage;
        }

        public float GetCalculatedAttackInterval()
        {
            float interval = AttackInterval;
            if (RunProgressionManager.Instance != null && RunProgressionManager.Instance.AttackSpeedMultiplier > 0.001f)
            {
                interval /= RunProgressionManager.Instance.AttackSpeedMultiplier;
            }
            return interval;
        }

        private bool IsTargetInRange(Enemy enemy)
        {
            if (enemy == null) return false;
            return Vector2.Distance(transform.position, enemy.transform.position) <= AttackRange;
        }

        private Enemy FindNearestEnemy()
        {
            float range = AttackRange;
            float bestDistance = range * range;
            Enemy bestEnemy = null;

            // Find enemies in scene via active registry (no FindObjectsByType)
            var enemies = Enemy.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy e = enemies[i];
                if (e == null || !e.gameObject.activeInHierarchy || e.IsDead) continue;

                float sqrDist = (e.transform.position - transform.position).sqrMagnitude;
                if (sqrDist <= bestDistance)
                {
                    bestDistance = sqrDist;
                    bestEnemy = e;
                }
            }

            return bestEnemy;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, AttackRange);
        }
    }
}
