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

        private float _attackTimer;
        private Enemy _currentTarget;

        private void Update()
        {
            if (IsDead) return;

            // Do not fire if paused
            if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            {
                return;
            }

            float dt = Time.deltaTime;
            _attackTimer -= dt;

            // Target evaluation
            if (_currentTarget == null || _currentTarget.IsDead || !IsTargetInRange(_currentTarget))
            {
                _currentTarget = FindNearestEnemy();
            }

            if (_currentTarget != null)
            {
                Vector3 origin = _firePoint != null ? _firePoint.position : transform.position;
                Vector2 dir = (_currentTarget.transform.position - origin).normalized;

                // Rotate towards enemy
                if (dir.sqrMagnitude > 0.001f)
                {
                    float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    Quaternion targetRot = Quaternion.Euler(0, 0, targetAngle);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _rotationSpeed * dt);
                }

                if (_attackTimer <= 0f)
                {
                    Fire(dir);
                    float interval = CurrentStats.attackInterval > 0 ? CurrentStats.attackInterval : 2f;
                    _attackTimer = interval;
                }
            }
        }

        private void Fire(Vector2 direction)
        {
            Vector3 origin = _firePoint != null ? _firePoint.position : transform.position;
            int damage = CurrentStats.damage > 0 ? CurrentStats.damage : 10;

            if (ProjectilePool.Instance != null)
            {
                ProjectilePool.Instance.Spawn(origin, direction, _projectileData, damage);
            }

            AddCombatXP(damage);
        }

        private bool IsTargetInRange(Enemy enemy)
        {
            if (enemy == null) return false;
            float range = CurrentStats.attackRange > 0 ? CurrentStats.attackRange : 4f;
            return Vector2.Distance(transform.position, enemy.transform.position) <= range;
        }

        private Enemy FindNearestEnemy()
        {
            float range = CurrentStats.attackRange > 0 ? CurrentStats.attackRange : 4f;
            float bestDistance = range * range;
            Enemy bestEnemy = null;

            // Find enemies in scene
            var enemies = FindObjectsByType<Enemy>();
            for (int i = 0; i < enemies.Length; i++)
            {
                Enemy e = enemies[i];
                if (e == null || e.IsDead) continue;

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
            float range = CurrentStats.attackRange > 0 ? CurrentStats.attackRange : 4f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
