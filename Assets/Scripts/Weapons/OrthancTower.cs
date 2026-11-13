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
            new() { dps = 9f, radius = 1.8f, duration = 4.5f, cooldown = 8f, pullSpeed = 1.8f },
            new() { dps = 13f, radius = 2.0f, duration = 5.5f, cooldown = 7f, pullSpeed = 2.0f },
            new() { dps = 18f, radius = 2.3f, duration = 6.5f, cooldown = 6f, pullSpeed = 2.3f },
            new() { dps = 25f, radius = 2.6f, duration = 8.0f, cooldown = 5f, pullSpeed = 2.6f }
        };

        public LevelStats CurrentStats => _levels[Mathf.Clamp(_currentLevel - 1, 0, _levels.Length - 1)];
        public override float MaxCooldown => ApplyWeaponCooldownModifiers(CurrentStats.cooldown);
        public override float AoERadius => CurrentStats.radius;

        public override bool TryFire(Vector3 targetWorldPosition)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return false;
            if (!IsReady) return false;

            LevelStats stats = CurrentStats;
            float dmgMult = Modifiers != null
                ? Modifiers.GetEffectiveWeaponDamageMultiplier()
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
