using System;
using UnityEngine;
using Alchemy.Inspector;
using Game.Core;
using Game.Projectiles;

namespace Game.Weapons
{
    public class ArcherVolley : CastleWeapon
    {
        [Serializable]
        public struct LevelStats
        {
            public int arrowCount;
            public int damagePerArrow;
            public float splashRadius;
            public float maxRange;
            public float flightTimePerCell;
            public float cooldown;
        }

        [Title("Archer Volley Levels (GDD 1..4)")]
        [SerializeField]
        private LevelStats[] _levels = new LevelStats[4]
        {
            new() { arrowCount = 8, damagePerArrow = 14, splashRadius = 1.6f, maxRange = 15f, flightTimePerCell = 0.12f, cooldown = 6f },
            new() { arrowCount = 11, damagePerArrow = 18, splashRadius = 1.8f, maxRange = 17f, flightTimePerCell = 0.10f, cooldown = 5f },
            new() { arrowCount = 14, damagePerArrow = 22, splashRadius = 2.0f, maxRange = 19f, flightTimePerCell = 0.09f, cooldown = 4.2f },
            new() { arrowCount = 18, damagePerArrow = 28, splashRadius = 2.4f, maxRange = 22f, flightTimePerCell = 0.08f, cooldown = 3.5f }
        };

        public LevelStats CurrentStats => _levels[Mathf.Clamp(_currentLevel - 1, 0, _levels.Length - 1)];
        public override float MaxCooldown => ApplyWeaponCooldownModifiers(CurrentStats.cooldown);
        public override float MaxRange => CurrentStats.maxRange;
        public override float AoERadius => CurrentStats.splashRadius;

        public override bool TryFire(Vector3 targetWorldPosition)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return false;
            if (!IsReady || !IsTargetInRange(targetWorldPosition)) return false;

            LevelStats stats = CurrentStats;
            Vector3 origin = AimOrigin;
            float dist = Mathf.Max(1f, Vector2.Distance(origin, targetWorldPosition));
            float baseFlight = Mathf.Max(0.25f, dist * stats.flightTimePerCell);
            int effectiveArrowDmg = ApplyWeaponDamageModifiers(stats.damagePerArrow);

            for (int i = 0; i < stats.arrowCount; i++)
            {
                Vector2 scatter = UnityEngine.Random.insideUnitCircle * (stats.splashRadius * 0.85f);
                Vector3 impactPos = targetWorldPosition + new Vector3(scatter.x, scatter.y, 0f);
                float stagger = UnityEngine.Random.Range(0f, 0.18f);

                Projectile_Lobbed.Spawn(
                    origin + new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), UnityEngine.Random.Range(-0.25f, 0.25f), 0f),
                    impactPos,
                    baseFlight + stagger,
                    arcHeight: 1.8f,
                    tint: new Color(0.95f, 0.9f, 0.6f, 1f),
                    sprite: null,
                    visualScale: new Vector3(0.32f, 0.12f, 1f),
                    impactDamage: effectiveArrowDmg,
                    splashRadius: 0.65f,
                    edgeDamageMultiplier: 1f,
                    knockbackCells: 0f,
                    singleTargetImpact: true);
            }

            TriggerFired();
            return true;
        }
    }
}
