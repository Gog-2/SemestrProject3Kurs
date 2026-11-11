using System;
using UnityEngine;
using Alchemy.Inspector;
using Game.Core;
using Game.Projectiles;

namespace Game.Weapons
{
    public class OrthancTower : CastleWeapon
    {
        [Serializable]
        public struct LevelStats
        {
            public float dps;
            public float radius;
            public float duration;
            public float cooldown;
            public float pullSpeed;
        }

        [Title("Orthanc Vortex Levels (GDD 1..4)")]
        [SerializeField]
        private LevelStats[] _levels = new LevelStats[4]
        {
            new() { dps = 3f, radius = 1.0f, duration = 4f, cooldown = 20f, pullSpeed = 1.4f },
            new() { dps = 4f, radius = 1.0f, duration = 6f, cooldown = 20f, pullSpeed = 1.6f },
            new() { dps = 6f, radius = 1.5f, duration = 8f, cooldown = 20f, pullSpeed = 1.8f },
            new() { dps = 8f, radius = 2.0f, duration = 10f, cooldown = 20f, pullSpeed = 2.1f }
        };

        public LevelStats CurrentStats => _levels[Mathf.Clamp(_currentLevel - 1, 0, _levels.Length - 1)];
        public override float MaxCooldown => ApplyWeaponCooldownModifiers(CurrentStats.cooldown);
        public override float AoERadius => CurrentStats.radius;

        public override bool TryFire(Vector3 targetWorldPosition)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return false;
            if (!IsReady) return false;

            LevelStats stats = CurrentStats;
            float dmgMult = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveWeaponDamageMultiplier()
                : 1f;

            HazardZone.Spawn(
                targetWorldPosition,
                HazardKind.OrthancVortex,
                stats.radius,
                stats.duration,
                stats.dps * dmgMult,
                slowPercent: 0f,
                color: new Color(0.55f, 0.25f, 0.95f, 0.45f),
                freezeThreshold: 2f,
                pullSpeed: stats.pullSpeed);

            TriggerFired();
            return true;
        }
    }
}
