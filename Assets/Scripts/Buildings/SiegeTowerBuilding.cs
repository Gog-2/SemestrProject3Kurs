using System;
using UnityEngine;
using Alchemy.Inspector;
using Game.Core;
using Game.Enemies;
using Game.Projectiles;
using Game.WaveSystem;

namespace Game.Buildings
{
    public enum SiegeTowerMode
    {
        PiercingBallista = 0,
        SplashCannon = 1,
        LobbedMortar = 2
    }

    public class SiegeTowerBuilding : Building
    {
        [Serializable]
        public struct SiegeLevelConfig
        {
            public int damage;
            public float attackRange;
            public float attackInterval;
            public int pierceCount;
            public float knockbackCells;
            public float splashRadius;
            public float flightTimePerCell;
        }

        [Title("Siege Config")]
        [SerializeField] private SiegeTowerMode _mode = SiegeTowerMode.PiercingBallista;
        [SerializeField] private ProjectileData _boltProjectileData;
        [SerializeField] private SiegeLevelConfig[] _levels = new SiegeLevelConfig[4];

        private float _attackTimer;

        public SiegeTowerMode Mode => _mode;

        public SiegeLevelConfig CurrentSiegeStats => _levels != null && _levels.Length > 0
            ? _levels[Mathf.Clamp(_currentLevel - 1, 0, _levels.Length - 1)]
            : default;

        public void ConfigureSiege(SiegeTowerMode mode, ProjectileData boltData, SiegeLevelConfig[] levels)
        {
            _mode = mode;
            _boltProjectileData = boltData;
            _levels = levels;
        }

        protected override void OnPlaced()
        {
            base.OnPlaced();
            _attackTimer = 0f;
        }

        private void Update()
        {
            if (IsDead) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (WaveController.Instance != null)
            {
                WaveState ws = WaveController.Instance.CurrentState;
                if (ws != WaveState.WaveActive && ws != WaveState.WaveClearing) return;
            }

            _attackTimer -= Time.deltaTime;
            if (_attackTimer > 0f) return;

            Enemy target = FindNearestEnemy();
            if (target == null) return;

            FireAt(target);
            _attackTimer = GetCalculatedAttackInterval();
        }

        public float GetEffectiveRange()
        {
            float range = CurrentSiegeStats.attackRange;
            if (_mode == SiegeTowerMode.PiercingBallista && RunProgressionManager.Instance != null)
            {
                range += RunProgressionManager.Instance.GetPiercingRangeBonus();
            }
            return Mathf.Max(1f, range);
        }

        public int GetCalculatedDamage()
        {
            SiegeLevelConfig cfg = CurrentSiegeStats;
            float mult = RunProgressionManager.Instance != null
                ? (_mode == SiegeTowerMode.LobbedMortar
                    ? RunProgressionManager.Instance.GetEffectiveExplosiveDamageMultiplier()
                    : RunProgressionManager.Instance.GetEffectiveBuildingAttackDamageMultiplier(BuildingCategory.Siege))
                : 1f;
            return Mathf.Max(1, Mathf.RoundToInt(cfg.damage * mult));
        }

        public float GetCalculatedAttackInterval()
        {
            float interval = Mathf.Max(0.25f, CurrentSiegeStats.attackInterval);
            if (RunProgressionManager.Instance != null)
            {
                float speedMult = RunProgressionManager.Instance.GetEffectiveAttackSpeedMultiplier();
                if (_mode == SiegeTowerMode.PiercingBallista)
                {
                    interval *= RunProgressionManager.Instance.GetEffectivePiercingCooldownMultiplier();
                }
                interval /= Mathf.Max(0.2f, speedMult);
            }
            return interval;
        }

        private void FireAt(Enemy target)
        {
            SiegeLevelConfig cfg = CurrentSiegeStats;
            int dmg = GetCalculatedDamage();
            Vector3 origin = transform.position;
            Vector3 targetPos = target.transform.position;

            if (_mode == SiegeTowerMode.PiercingBallista && ProjectilePool.Instance != null && _boltProjectileData != null)
            {
                Vector2 dir = targetPos - origin;
                dir = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.up;
                ProjectilePool.Instance.Spawn(origin, dir, _boltProjectileData, dmg, Mathf.Max(1, cfg.pierceCount), cfg.knockbackCells);
                AddCombatXP(dmg);
                return;
            }

            float radiusBonus = _mode == SiegeTowerMode.LobbedMortar && RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetExplosiveRadiusBonus() * 0.5f
                : 0f;
            float splashR = Mathf.Max(0.75f, cfg.splashRadius + radiusBonus);
            float dist = Mathf.Max(1f, Vector2.Distance(origin, targetPos));
            float flight = _mode == SiegeTowerMode.LobbedMortar
                ? Mathf.Max(0.45f, dist * Mathf.Max(0.15f, cfg.flightTimePerCell))
                : Mathf.Clamp(dist * 0.06f, 0.08f, 0.25f);
            float arc = _mode == SiegeTowerMode.LobbedMortar ? 2.0f : 0.4f;
            Color tint = _mode switch
            {
                SiegeTowerMode.LobbedMortar => new Color(1f, 0.45f, 0.15f, 1f),
                SiegeTowerMode.SplashCannon => new Color(0.35f, 0.35f, 0.38f, 1f),
                _ => new Color(0.95f, 0.9f, 0.65f, 1f)
            };

            Projectile_Lobbed.Spawn(
                origin,
                targetPos,
                flightDuration: flight,
                arcHeight: arc,
                tint: tint,
                sprite: null,
                visualScale: new Vector3(0.38f, 0.38f, 1f),
                impactDamage: dmg,
                splashRadius: splashR,
                edgeDamageMultiplier: 1f,
                knockbackCells: cfg.knockbackCells,
                singleTargetImpact: false);

            AddCombatXP(dmg);
        }

        private Enemy FindNearestEnemy()
        {
            float range = GetEffectiveRange() + 0.5f;
            float bestSqr = range * range;
            Enemy best = null;

            var enemies = Enemy.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy e = enemies[i];
                if (e == null || e.IsDead || e.IsRetreating) continue;
                float sqr = (e.transform.position - transform.position).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = e;
                }
            }

            return best;
        }
    }
}
