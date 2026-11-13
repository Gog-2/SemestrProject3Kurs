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
            new() { impactDamage = 24, splashRadius = 1.6f, flightTimePerCell = 0.14f, puddleDuration = 4f, puddleDps = 8f, cooldown = 5.5f },
            new() { impactDamage = 36, splashRadius = 1.8f, flightTimePerCell = 0.12f, puddleDuration = 4.5f, puddleDps = 10f, cooldown = 4.5f },
            new() { impactDamage = 52, splashRadius = 2.0f, flightTimePerCell = 0.10f, puddleDuration = 5f, puddleDps = 13f, cooldown = 3.8f },
            new() { impactDamage = 75, splashRadius = 2.3f, flightTimePerCell = 0.08f, puddleDuration = 6f, puddleDps = 16f, cooldown = 3.0f }
        };

        public LevelStats CurrentStats => _levels[Mathf.Clamp(_currentLevel - 1, 0, _levels.Length - 1)];
        public override float MaxCooldown => ApplyWeaponCooldownModifiers(CurrentStats.cooldown);
        public override float AoERadius => CurrentStats.splashRadius + (Modifiers != null ? Modifiers.GetExplosiveRadiusBonus() * 0.5f : 0f);

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
            float effectiveDps = stats.puddleDps * (Modifiers != null
                ? Modifiers.GetEffectiveWeaponDamageMultiplier()
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
