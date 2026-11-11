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

        public override float MaxCooldown => ApplyWeaponCooldownModifiers(
            _data != null ? _data.GetStats(_currentLevel).cooldown : 8f,
            isPiercing: true);

        public BallistaLevelStats CurrentStats => _data != null ? _data.GetStats(_currentLevel) : default;
        public override Vector3 AimOrigin => _firePoint != null ? _firePoint.position : base.AimOrigin;

        public void Configure(HeavyBallistaData data, Transform firePoint = null)
        {
            if (data != null) _data = data;
            if (firePoint != null) _firePoint = firePoint;
        }

        public override bool TryFire(Vector3 targetWorldPosition)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return false;
            if (!IsReady || _data == null) return false;

            RotateTowardsWorldPoint(targetWorldPosition);

            Vector3 origin = AimOrigin;
            Vector2 direction = targetWorldPosition - origin;
            direction = direction.sqrMagnitude < 0.001f ? Vector2.up : direction.normalized;

            BallistaLevelStats stats = _data.GetStats(_currentLevel);
            ProjectileData projData = _data.ProjectileData;
            int effectiveDamage = ApplyWeaponDamageModifiers(stats.damage);

            ProjectilePool.Instance.Spawn(origin, direction, projData, effectiveDamage);

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
