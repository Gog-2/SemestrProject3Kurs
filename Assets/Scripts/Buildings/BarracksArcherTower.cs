using UnityEngine;
using Alchemy.Inspector;
using Game.Core;
using Game.Enemies;
using Game.Projectiles;
using Game.WaveSystem;

namespace Game.Buildings
{
    public class BarracksArcherTower : Building
    {
        [Title("Synergy Tower Settings (GIG-36)")]
        [SerializeField] private float _thornsInterval = 1.0f;
        [SerializeField] private int _thornsDamage = 6;

        private float _attackTimer;
        private float _thornsTimer;

        private static readonly int[] s_archerCounts = { 2, 2, 3, 3 };
        private static readonly float[] s_archerDamages = { 7.5f, 7.5f, 7.5f, 10f };

        public int ArcherCount => s_archerCounts[Mathf.Clamp(_currentLevel - 1, 0, 3)];
        public float AttackRange => 2.5f;

        private void Update()
        {
            if (IsDead) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (WaveController.Instance != null)
            {
                WaveState ws = WaveController.Instance.CurrentState;
                if (ws != WaveState.WaveActive && ws != WaveState.WaveClearing) return;
            }

            float dt = Time.deltaTime;

            if (_currentLevel >= 4)
            {
                TickPerimeterThorns(dt);
            }

            UnitSpawnerBuilding adjacentCamp = FindBestAdjacentArcherCamp();
            if (adjacentCamp == null)
            {
                // Requires adjacent Лагерь лучников to fire arrows (L4 thorns still work)
                return;
            }

            _attackTimer -= dt;
            if (_attackTimer <= 0f)
            {
                Enemy target = FindNearestEnemy();
                if (target != null)
                {
                    FireSalvo(target);
                    _attackTimer = adjacentCamp.EffectiveAttackInterval;
                }
            }
        }

        public UnitSpawnerBuilding FindBestAdjacentArcherCamp()
        {
            if (_gridMap == null) return null;

            UnitSpawnerBuilding best = null;
            float bestInterval = float.MaxValue;

            int minX = _originCell.x - 1;
            int maxX = _originCell.x + Size.x;
            int minY = _originCell.y - 1;
            int maxY = _originCell.y + Size.y;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (!_gridMap.InBounds(x, y)) continue;
                    IBuilding occ = _gridMap.GetOccupant(x, y);
                    if (occ is UnitSpawnerBuilding spawner && !spawner.IsDead &&
                        spawner.Data != null && spawner.Data.Id == "archer_camp")
                    {
                        float interval = spawner.EffectiveAttackInterval;
                        if (interval < bestInterval)
                        {
                            bestInterval = interval;
                            best = spawner;
                        }
                    }
                }
            }

            return best;
        }

        private void FireSalvo(Enemy target)
        {
            float baseDmg = s_archerDamages[Mathf.Clamp(_currentLevel - 1, 0, 3)];
            float dmgMult = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveUnitDamageMultiplier(isRanged: true, isArcherUnit: true)
                : 1f;

            int archers = ArcherCount;
            int totalSalvoDmg = Mathf.Max(1, Mathf.RoundToInt(baseDmg * dmgMult));

            for (int i = 0; i < archers; i++)
            {
                Vector3 origin = transform.position + new Vector3((i - 1) * 0.25f, 0.35f, 0f);
                Projectile_Lobbed.Spawn(
                    origin,
                    target.transform.position,
                    flightDuration: 0.14f + i * 0.03f,
                    arcHeight: 0.45f,
                    tint: new Color(0.95f, 0.88f, 0.45f, 1f),
                    sprite: null,
                    visualScale: new Vector3(0.24f, 0.1f, 1f),
                    impactDamage: totalSalvoDmg,
                    splashRadius: 0.7f,
                    edgeDamageMultiplier: 1f,
                    knockbackCells: 0f,
                    singleTargetImpact: true);
            }

            AddCombatXP(totalSalvoDmg * archers);
        }

        private void TickPerimeterThorns(float dt)
        {
            _thornsTimer -= dt;
            if (_thornsTimer > 0f) return;

            _thornsTimer = _thornsInterval;
            float thornsRadius = Mathf.Max(Size.x, Size.y) * 0.5f + 1.15f;
            float sqrR = thornsRadius * thornsRadius;

            var enemies = Enemy.ActiveEnemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy e = enemies[i];
                if (e == null || e.IsDead || e.IsRetreating) continue;
                if ((e.transform.position - transform.position).sqrMagnitude <= sqrR)
                {
                    e.TakeDamage(_thornsDamage);
                    AddCombatXP(_thornsDamage);
                }
            }
        }

        private Enemy FindNearestEnemy()
        {
            float effectiveRange = AttackRange + 0.5f;
            float bestSqr = effectiveRange * effectiveRange;
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
