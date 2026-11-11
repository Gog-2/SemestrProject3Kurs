using System;
using UnityEngine;
using Alchemy.Inspector;
using Game.Core;
using Game.Projectiles;

namespace Game.Weapons
{
    public class TarCannon : CastleWeapon
    {
        [Serializable]
        public struct LevelStats
        {
            public int impactDamage;
            public float splashRadius;
            public float maxRange;
            public float puddleDuration;
            public float slowPercent;
            public float cooldown;
            public float flightTimePerCell;
        }

        [Title("Tar Cannon Levels (GDD 1..4)")]
        [SerializeField]
        private LevelStats[] _levels = new LevelStats[4]
        {
            new() { impactDamage = 25, splashRadius = 1.0f, maxRange = 10f, puddleDuration = 4f, slowPercent = 0.30f, cooldown = 9f, flightTimePerCell = 0.14f },
            new() { impactDamage = 40, splashRadius = 1.0f, maxRange = 12f, puddleDuration = 5f, slowPercent = 0.40f, cooldown = 8f, flightTimePerCell = 0.12f },
            new() { impactDamage = 60, splashRadius = 1.0f, maxRange = 15f, puddleDuration = 6f, slowPercent = 0.50f, cooldown = 6f, flightTimePerCell = 0.10f },
            new() { impactDamage = 85, splashRadius = 1.5f, maxRange = 20f, puddleDuration = 8f, slowPercent = 0.80f, cooldown = 4f, flightTimePerCell = 0.08f }
        };

        public LevelStats CurrentStats => _levels[Mathf.Clamp(_currentLevel - 1, 0, _levels.Length - 1)];
        public override float MaxCooldown => ApplyWeaponCooldownModifiers(CurrentStats.cooldown);
        public override float MaxRange => CurrentStats.maxRange;
        public override float AoERadius => CurrentStats.splashRadius;

        public override bool TryFire(Vector3 targetWorldPosition)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return false;
            if (!IsReady || !IsTargetInRange(targetWorldPosition)) return false;

            RotateTowardsWorldPoint(targetWorldPosition);

            LevelStats stats = CurrentStats;
            Vector3 origin = AimOrigin;
            float dist = Mathf.Max(1f, Vector2.Distance(origin, targetWorldPosition));
            float flightDuration = Mathf.Max(0.25f, dist * stats.flightTimePerCell);
            int effectiveDmg = ApplyWeaponDamageModifiers(stats.impactDamage);

            Projectile_Lobbed.Spawn(
                origin,
                targetWorldPosition,
                flightDuration,
                arcHeight: 1.5f,
                tint: new Color(0.22f, 0.2f, 0.25f, 1f),
                sprite: null,
                visualScale: new Vector3(0.45f, 0.45f, 1f),
                impactDamage: effectiveDmg,
                splashRadius: stats.splashRadius,
                edgeDamageMultiplier: 1f,
                knockbackCells: 0f,
                singleTargetImpact: false,
                onImpact: pos =>
                {
                    HazardZone.Spawn(
                        pos,
                        HazardKind.TarSlow,
                        stats.splashRadius,
                        stats.puddleDuration,
                        dps: 0f,
                        slowPercent: stats.slowPercent,
                        color: new Color(0.14f, 0.12f, 0.16f, 0.55f));
                });

            TriggerFired();
            return true;
        }
    }
}
