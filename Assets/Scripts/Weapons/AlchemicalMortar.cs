using System;
using UnityEngine;
using Alchemy.Inspector;
using Game.Core;
using Game.Projectiles;

namespace Game.Weapons
{
    public class AlchemicalMortar : CastleWeapon
    {
        [Serializable]
        public struct LevelStats
        {
            public int impactDamage;
            public float splashRadius;
            public float flightTimePerCell;
            public float puddleDuration;
            public float puddleDps;
            public float cooldown;
        }

        [Title("Mortar Levels (GDD 1..4)")]
        [SerializeField]
        private LevelStats[] _levels = new LevelStats[4]
        {
            new() { impactDamage = 20, splashRadius = 1.0f, flightTimePerCell = 0.5f, puddleDuration = 3f, puddleDps = 5f, cooldown = 10f },
            new() { impactDamage = 35, splashRadius = 1.0f, flightTimePerCell = 0.4f, puddleDuration = 4f, puddleDps = 5f, cooldown = 8f },
            new() { impactDamage = 50, splashRadius = 1.0f, flightTimePerCell = 0.3f, puddleDuration = 5f, puddleDps = 5f, cooldown = 6f },
            new() { impactDamage = 75, splashRadius = 1.0f, flightTimePerCell = 0.2f, puddleDuration = 5f, puddleDps = 6f, cooldown = 4f }
        };

        public LevelStats CurrentStats => _levels[Mathf.Clamp(_currentLevel - 1, 0, _levels.Length - 1)];
        public override float MaxCooldown => ApplyWeaponCooldownModifiers(CurrentStats.cooldown);
        public override float AoERadius => CurrentStats.splashRadius + (RunProgressionManager.Instance != null ? RunProgressionManager.Instance.GetExplosiveRadiusBonus() * 0.5f : 0f);

        public override bool TryFire(Vector3 targetWorldPosition)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return false;
            if (!IsReady) return false;

            RotateTowardsWorldPoint(targetWorldPosition);

            LevelStats stats = CurrentStats;
            Vector3 origin = AimOrigin;
            float dist = Mathf.Max(1f, Vector2.Distance(origin, targetWorldPosition));
            float flightDuration = Mathf.Max(0.35f, dist * stats.flightTimePerCell);
            int effectiveDmg = ApplyWeaponDamageModifiers(stats.impactDamage, isExplosive: true);
            float effectiveRadius = AoERadius;
            float effectiveDps = stats.puddleDps * (RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveWeaponDamageMultiplier()
                : 1f);

            Projectile_Lobbed.Spawn(
                origin,
                targetWorldPosition,
                flightDuration,
                arcHeight: 2.2f,
                tint: new Color(1f, 0.55f, 0.12f, 1f),
                sprite: null,
                visualScale: new Vector3(0.45f, 0.45f, 1f),
                impactDamage: effectiveDmg,
                splashRadius: effectiveRadius,
                edgeDamageMultiplier: 1f,
                knockbackCells: 0f,
                singleTargetImpact: false,
                onImpact: pos =>
                {
                    HazardZone.Spawn(
                        pos,
                        HazardKind.FireBurn,
                        effectiveRadius,
                        stats.puddleDuration,
                        effectiveDps,
                        slowPercent: 0f,
                        color: new Color(1f, 0.38f, 0.08f, 0.4f));
                });

            TriggerFired();
            return true;
        }
    }
}
