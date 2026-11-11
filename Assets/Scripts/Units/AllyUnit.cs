using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Game.Core;
using Game.Buildings;
using Game.Enemies;
using Game.Projectiles;
using Game.WaveSystem;

namespace Game.Units
{
    public class AllyUnit : MonoBehaviour, IDamageable
    {
        public static readonly List<AllyUnit> ActiveUnits = new(32);

        private int _maxHP = 40;
        private int _currentHP = 40;
        private int _baseDamage = 10;
        private float _attackInterval = 2f;
        private float _attackRange = 0.9f;
        private int _aggroLimit = 2;
        private float _patrolRadius = 3f;
        private float _moveSpeed = 2.6f;

        private bool _isRanged;
        private bool _isArcherUnit;
        private bool _isBerserker;
        private float _meleeRange = 1.15f;
        private int _meleeDamage = 5;
        private float _meleeInterval = 2f;
        private float _bleedDps;
        private float _bleedDuration;

        private float _knockbackCells;
        private int _pierceCount = 1;

        private bool _usesStealth;
        private float _stealthDuration = 5f;
        private float _stealthRemaining;

        private bool _isTemporarySummon;
        private float _lifetimeRemaining;

        private Vector3 _homePosition;
        private Building _spawnerBuilding;
        private SpriteRenderer _renderer;
        private Tweener _flashTween;
        private Color _baseColor = Color.white;

        private float _attackTimer;
        private float _patrolWaitTimer;
        private Vector3 _patrolTarget;
        private bool _hasPatrolTarget;
        private Enemy _currentTarget;

        private readonly List<Enemy> _engagedEnemies = new(8);

        public int CurrentHP => _currentHP;
        public int MaxHP => _maxHP;
        public bool IsDead => _currentHP <= 0;
        public bool IsStealthed => _usesStealth && _stealthRemaining > 0f;
        public bool IsRanged => _isRanged;
        public int EffectiveAggroLimit
        {
            get
            {
                int bonus = RunProgressionManager.Instance != null
                    ? RunProgressionManager.Instance.GetEffectiveUnitAggroBonus()
                    : 0;
                return Mathf.Max(1, _aggroLimit + bonus);
            }
        }

        public event Action<IDamageable> HealthChanged;
        public event Action<IDamageable> Destroyed;
        public event Action<AllyUnit> Died;

        private void OnEnable()
        {
            if (!ActiveUnits.Contains(this))
            {
                ActiveUnits.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveUnits.Remove(this);
        }

        public void Initialize(
            Building spawner,
            Vector3 homePos,
            int baseMaxHp,
            int baseDamage,
            float attackInterval,
            float attackRange,
            int aggroLimit,
            float patrolRadius,
            bool isRanged,
            bool isArcherUnit,
            Color tint,
            float knockbackCells = 0f,
            int pierceCount = 1,
            bool usesStealth = false,
            float stealthBaseSeconds = 5f,
            bool isBerserker = false,
            int meleeDamage = 5,
            float meleeInterval = 2f,
            float bleedDps = 0f,
            float bleedDuration = 0f,
            bool isTemporary = false,
            float lifetimeSeconds = 0f)
        {
            _spawnerBuilding = spawner;
            _homePosition = homePos;

            float hpMult = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveUnitMaxHpMultiplier()
                : 1f;
            _maxHP = Mathf.Max(1, Mathf.RoundToInt(baseMaxHp * hpMult));
            _currentHP = _maxHP;

            _baseDamage = Mathf.Max(1, baseDamage);
            _attackInterval = Mathf.Max(0.25f, attackInterval);
            _attackRange = Mathf.Max(0.75f, attackRange);
            _aggroLimit = Mathf.Max(1, aggroLimit);
            _patrolRadius = Mathf.Max(1f, patrolRadius);

            _isRanged = isRanged;
            _isArcherUnit = isArcherUnit;
            _knockbackCells = Mathf.Max(0f, knockbackCells);
            _pierceCount = Mathf.Max(1, pierceCount);

            _usesStealth = usesStealth;
            float stealthBonus = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetHunterStealthBonusSeconds()
                : 0f;
            _stealthDuration = Mathf.Max(0f, stealthBaseSeconds + stealthBonus);
            _stealthRemaining = _usesStealth ? _stealthDuration : 0f;

            _isBerserker = isBerserker;
            _meleeDamage = Mathf.Max(1, meleeDamage);
            _meleeInterval = Mathf.Max(0.25f, meleeInterval);
            _bleedDps = Mathf.Max(0f, bleedDps);
            _bleedDuration = Mathf.Max(0f, bleedDuration);

            _isTemporarySummon = isTemporary;
            _lifetimeRemaining = lifetimeSeconds;

            _baseColor = tint;
            EnsureVisual();
            RefreshStealthVisual();
            HealthChanged?.Invoke(this);
        }

        private void EnsureVisual()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
                if (_renderer == null)
                {
                    _renderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }
            _renderer.sprite = HazardZone.GetOrCreateCircleSprite();
            _renderer.color = _baseColor;
            _renderer.sortingLayerName = "Default";
            _renderer.sortingOrder = 9;
            transform.localScale = new Vector3(0.42f, 0.42f, 1f);
        }

        public int ActiveAggroCount
        {
            get
            {
                PruneAggroList();
                return _engagedEnemies.Count;
            }
        }

        public bool HasAggroSlot => ActiveAggroCount < EffectiveAggroLimit;

        public bool TryClaimAggro(Enemy enemy)
        {
            if (enemy == null || enemy.IsDead || IsDead || IsStealthed) return false;
            PruneAggroList();

            if (_engagedEnemies.Contains(enemy)) return true;
            if (_engagedEnemies.Count >= EffectiveAggroLimit) return false;

            _engagedEnemies.Add(enemy);
            return true;
        }

        public void ReleaseAggro(Enemy enemy)
        {
            _engagedEnemies.Remove(enemy);
        }

        private void PruneAggroList()
        {
            for (int i = _engagedEnemies.Count - 1; i >= 0; i--)
            {
                Enemy e = _engagedEnemies[i];
                if (e == null || e.IsDead || e.IsRetreating)
                {
                    _engagedEnemies.RemoveAt(i);
                }
            }
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            _currentHP = Mathf.Min(_maxHP, _currentHP + amount);
            HealthChanged?.Invoke(this);
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;
            _currentHP = Mathf.Max(0, _currentHP - amount);

            if (_renderer != null)
            {
                _flashTween?.Kill();
                _flashTween = _renderer.DOColor(Color.red, 0.06f)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetLink(gameObject)
                    .OnComplete(RefreshStealthVisual);
            }

            HealthChanged?.Invoke(this);
            if (_currentHP <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            _flashTween?.Kill();
            _engagedEnemies.Clear();
            Destroyed?.Invoke(this);
            Died?.Invoke(this);
            Destroy(gameObject);
        }

        private void Update()
        {
            if (IsDead) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            float dt = Time.deltaTime;

            if (_isTemporarySummon)
            {
                _lifetimeRemaining -= dt;
                if (_lifetimeRemaining <= 0f)
                {
                    Die();
                    return;
                }
            }

            if (_attackTimer > 0f) _attackTimer -= dt;
            if (_usesStealth && _stealthRemaining > 0f)
            {
                _stealthRemaining = Mathf.Max(0f, _stealthRemaining - dt);
                if (_stealthRemaining <= 0f) RefreshStealthVisual();
            }

            bool waveInProgress = WaveController.Instance == null ||
                                  WaveController.Instance.CurrentState == WaveState.WaveActive ||
                                  WaveController.Instance.CurrentState == WaveState.WaveClearing;

            if (!waveInProgress && !_isTemporarySummon)
            {
                _currentTarget = null;
                ReturnToHome(dt);
                return;
            }

            _currentTarget = FindBestTarget();
            if (_currentTarget != null && !_currentTarget.IsDead)
            {
                TickCombat(_currentTarget, dt);
                return;
            }

            // Re-enter stealth if Hunter is idle out of combat
            if (_usesStealth && _stealthRemaining <= 0f)
            {
                _stealthRemaining = _stealthDuration;
                RefreshStealthVisual();
            }

            TickIdlePatrol(dt);
        }

        private Enemy FindBestTarget()
        {
            float searchRadius = Mathf.Max(_patrolRadius, _attackRange) + 2f;
            float bestSqr = searchRadius * searchRadius;
            Enemy best = null;

            PruneAggroList();
            if (_engagedEnemies.Count > 0)
            {
                for (int i = 0; i < _engagedEnemies.Count; i++)
                {
                    Enemy e = _engagedEnemies[i];
                    if (e != null && !e.IsDead && !e.IsRetreating)
                    {
                        return e;
                    }
                }
            }

            var enemies = Enemy.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy e = enemies[i];
                if (e == null || e.IsDead || e.IsRetreating) continue;

                float sqrFromHome = (e.transform.position - _homePosition).sqrMagnitude;
                if (sqrFromHome > bestSqr && !_isTemporarySummon) continue;

                float sqrDist = (e.transform.position - transform.position).sqrMagnitude;
                if (sqrDist < bestSqr)
                {
                    bestSqr = sqrDist;
                    best = e;
                }
            }

            return best;
        }

        private void TickCombat(Enemy target, float dt)
        {
            float dist = Vector2.Distance(transform.position, target.transform.position);
            float effectiveRange = (_isBerserker && dist <= _meleeRange) ? _meleeRange : _attackRange;

            if (dist > effectiveRange * 0.92f)
            {
                Vector3 nextPos = Vector3.MoveTowards(transform.position, target.transform.position, _moveSpeed * dt);
                // Keep units anchored within patrol + engage tether unless temporary skeleton
                if (_isTemporarySummon || Vector2.Distance(nextPos, _homePosition) <= Mathf.Max(_patrolRadius, _attackRange) + 2f)
                {
                    transform.position = nextPos;
                    UpdateFlip(target.transform.position.x - transform.position.x);
                }
                return;
            }

            if (_attackTimer <= 0f)
            {
                PerformAttack(target, dist);
            }
        }

        private void PerformAttack(Enemy target, float dist)
        {
            if (_usesStealth && _stealthRemaining > 0f)
            {
                _stealthRemaining = 0f;
                RefreshStealthVisual();
            }

            bool inMeleeRange = dist <= _meleeRange + 0.15f;
            bool useBerserkerMelee = _isBerserker && inMeleeRange;

            SupportAuraBuilding.GetMeleeBuffsAt(transform.position, out float bannerDmgBonus, out float bannerAtkSpeedBonus);

            int baseDmg = useBerserkerMelee ? _meleeDamage : _baseDamage;
            float dmgMult = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveUnitDamageMultiplier(_isRanged && !useBerserkerMelee, _isArcherUnit)
                : 1f;
            if (!_isRanged || useBerserkerMelee)
            {
                dmgMult += bannerDmgBonus;
            }

            int finalDmg = Mathf.Max(1, Mathf.RoundToInt(baseDmg * dmgMult));

            float baseInterval = useBerserkerMelee ? _meleeInterval : _attackInterval;
            float speedMult = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveAttackSpeedMultiplier()
                : 1f;
            if (!_isRanged || useBerserkerMelee)
            {
                speedMult += bannerAtkSpeedBonus;
            }
            _attackTimer = baseInterval / Mathf.Max(0.2f, speedMult);

            if (_isRanged && !useBerserkerMelee)
            {
                bool applyBleed = _isBerserker && _bleedDps > 0f;
                float bDps = _bleedDps;
                float bDur = _bleedDuration;
                Enemy capturedTarget = target;
                Building capturedSpawner = _spawnerBuilding;

                Projectile_Lobbed.Spawn(
                    transform.position,
                    target.transform.position,
                    flightDuration: Mathf.Clamp(dist * 0.06f, 0.08f, 0.25f),
                    arcHeight: 0.35f,
                    tint: _isBerserker ? new Color(0.9f, 0.35f, 0.2f) : new Color(0.95f, 0.9f, 0.55f),
                    sprite: null,
                    visualScale: new Vector3(0.22f, 0.12f, 1f),
                    impactDamage: finalDmg,
                    splashRadius: 0.7f,
                    edgeDamageMultiplier: 1f,
                    knockbackCells: 0f,
                    singleTargetImpact: true,
                    onImpact: _ =>
                    {
                        if (capturedTarget != null && !capturedTarget.IsDead && applyBleed)
                        {
                            capturedTarget.ApplyBleed(bDps, bDur);
                        }
                        if (capturedSpawner != null && !capturedSpawner.IsDead)
                        {
                            capturedSpawner.AddCombatXP(finalDmg);
                        }
                    });
                return;
            }

            // Melee (supports Shieldman pierce + Cyclops knockback)
            int hits = 0;
            var enemies = Enemy.ActiveEnemies;
            for (int i = enemies.Count - 1; i >= 0 && hits < _pierceCount; i--)
            {
                Enemy e = enemies[i];
                if (e == null || e.IsDead || e.IsRetreating) continue;
                if (Vector2.Distance(transform.position, e.transform.position) <= _meleeRange + 0.4f)
                {
                    e.TakeDamage(finalDmg);
                    if (_spawnerBuilding != null && !_spawnerBuilding.IsDead)
                    {
                        _spawnerBuilding.AddCombatXP(finalDmg);
                    }
                    if (_knockbackCells > 0f && !e.IsDead)
                    {
                        Vector2 kbDir = (e.transform.position - transform.position);
                        e.ApplyKnockback(kbDir.sqrMagnitude > 0.001f ? kbDir.normalized : Vector2.right, _knockbackCells);
                    }
                    hits++;
                }
            }
        }

        private void TickIdlePatrol(float dt)
        {
            if (_patrolWaitTimer > 0f)
            {
                _patrolWaitTimer -= dt;
                return;
            }

            if (!_hasPatrolTarget)
            {
                PickNextPatrolPoint();
                _hasPatrolTarget = true;
            }

            Vector3 diff = _patrolTarget - transform.position;
            if (diff.sqrMagnitude <= 0.04f)
            {
                _hasPatrolTarget = false;
                _patrolWaitTimer = UnityEngine.Random.Range(2f, 3f);
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, _patrolTarget, _moveSpeed * 0.65f * dt);
            UpdateFlip(diff.x);
        }

        private void PickNextPatrolPoint()
        {
            float r = Mathf.Max(0.5f, _patrolRadius * 0.7f);
            for (int attempt = 0; attempt < 5; attempt++)
            {
                Vector2 rand = UnityEngine.Random.insideUnitCircle * r;
                Vector3 candidate = _homePosition + new Vector3(rand.x, rand.y, 0f);
                if (Vector2.Distance(candidate, transform.position) >= 0.75f)
                {
                    _patrolTarget = candidate;
                    return;
                }
            }
            _patrolTarget = _homePosition;
        }

        private void ReturnToHome(float dt)
        {
            if (Vector2.Distance(transform.position, _homePosition) <= 0.35f) return;
            Vector3 diff = _homePosition - transform.position;
            transform.position = Vector3.MoveTowards(transform.position, _homePosition, _moveSpeed * dt);
            UpdateFlip(diff.x);
        }

        private void RefreshStealthVisual()
        {
            if (_renderer == null) return;
            Color c = _baseColor;
            c.a = IsStealthed ? 0.42f : 1f;
            _renderer.color = c;
        }

        private void UpdateFlip(float dx)
        {
            if (_renderer == null) return;
            if (dx > 0.05f) _renderer.flipX = false;
            else if (dx < -0.05f) _renderer.flipX = true;
        }

        public static AllyUnit SpawnTemporarySkeleton(Vector3 worldPos, int damage = 20, int maxHp = 50, float lifetimeSeconds = 3f)
        {
            var go = new GameObject("Skeleton_Summon");
            go.transform.position = worldPos;
            var unit = go.AddComponent<AllyUnit>();
            unit.Initialize(
                spawner: null,
                homePos: worldPos,
                baseMaxHp: maxHp,
                baseDamage: damage,
                attackInterval: 1.0f,
                attackRange: 0.9f,
                aggroLimit: 2,
                patrolRadius: 2.5f,
                isRanged: false,
                isArcherUnit: false,
                tint: new Color(0.85f, 0.95f, 0.85f, 1f),
                isTemporary: true,
                lifetimeSeconds: lifetimeSeconds);
            return unit;
        }
    }
}
