using UnityEngine;
using Alchemy.Inspector;
using Game.Projectiles;
using Game.Core;

namespace Game.Weapons
{
    public class HeavyBallista : CastleWeapon
    {
        [Title("Ballista Data")]
        [SerializeField] private HeavyBallistaData _data;
        [SerializeField] private Transform _firePoint;

        public override float MaxCooldown
        {
            get
            {
                if (_data != null)
                {
                    return _data.GetStats(_currentLevel).cooldown;
                }
                return 8f;
            }
        }

        public BallistaLevelStats CurrentStats => _data != null ? _data.GetStats(_currentLevel) : default;

        public override bool TryFire(Vector3 targetWorldPosition)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return false;
            if (!IsReady) return false;

            Vector3 origin = _firePoint != null ? _firePoint.position : GetOriginPosition();
            Vector2 direction = (targetWorldPosition - origin);

            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector2.up;
            }
            else
            {
                direction.Normalize();
            }

            BallistaLevelStats stats = _data != null ? _data.GetStats(_currentLevel) : new BallistaLevelStats
            {
                damage = 15,
                cooldown = 8f,
                speed = 15f,
                knockbackCells = 1f,
                pierceCount = 999
            };

            ProjectileData projData = _data != null ? _data.ProjectileData : null;

            if (ProjectilePool.Instance != null)
            {
                ProjectilePool.Instance.Spawn(origin, direction, projData, stats.damage);
            }
            else
            {
                Debug.LogWarning("[HeavyBallista] ProjectilePool.Instance is null! Spawning fallback.");
            }

            TriggerFired();
            return true;
        }

        [Button]
        public void TestFireUp()
        {
            TryFire(GetOriginPosition() + Vector3.up * 5f);
        }
    }
}
