using System;
using UnityEngine;
using DG.Tweening;
using Alchemy.Inspector;
using Game.Core;
using Game.Buildings;
using Game.Pathfinding;
using Game.Enemies.Data;
using Game.Units;
using Game.Enemies.States;

namespace Game.Enemies
{
    [SelectionBase]
    public class Enemy : MonoBehaviour, IDamageable, IPoolable
    {
        [Title("Visuals")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform _modelTransform;
        [SerializeField] private Color _damageFlashColor = Color.red;
        [SerializeField] private Vector3 _punchScaleStrength = new(0.1f, 0.1f, 0f);

        [Title("Debug")]
        [ShowInInspector, ReadOnly]
        public EnemyData Data => _data;

        [ShowInInspector, ReadOnly]
        public int CurrentHP => _currentHP;

        [ShowInInspector, ReadOnly]
        public int MaxHP => _maxHP;

        [ShowInInspector, ReadOnly]
        public bool IsDead => _currentHP <= 0;

        [ShowInInspector, ReadOnly]
        public float CurrentSpeed => _currentSpeed;

        [ShowInInspector, ReadOnly]
        public bool IsAttacking => !_isRetreating && !IsFrozen && !IsStunned && IsTargetValid(_currentTarget);

        [ShowInInspector, ReadOnly]
        public bool IsRetreating => _isRetreating;

        public bool IsFrozen => _freezeRemaining > 0f;
        public bool IsStunned => _stunRemaining > 0f;

        public event Action<IDamageable> HealthChanged;
        public event Action<IDamageable> Destroyed;
        public event Action<Enemy> Died;
        public event Action<Enemy> Retreated;
        public static event Action<Enemy> GlobalEnemyDied;

        private static readonly System.Collections.Generic.List<Enemy> s_activeEnemies = new(64);
        public static System.Collections.Generic.IReadOnlyList<Enemy> ActiveEnemies => s_activeEnemies;

        private void OnEnable()
        {
            if (!s_activeEnemies.Contains(this))
            {
                s_activeEnemies.Add(this);
            }
        }

        private void OnDisable()
        {
            s_activeEnemies.Remove(this);
        }

        private EnemyData _data;
        private GridFlowfield _flowfield;
        private Castle _castle;
        private GridMap _gridMap;

        private int _maxHP = 1;
        private int _currentHP;

        private Color _originalColor;
        private Sprite _defaultSprite;
        private CircleCollider2D _circleCollider;
        private float _baseColliderRadius = 0.45f;
        private Tweener _flashTween;
        private Tweener _punchTween;

        private int _damage;
        private float _attackInterval;
        private float _attackRange;
        private float _sqrAttackRange;
        private float _attackTimer;
        private IDamageable _currentTarget;
        private AllyUnit _lockedUnitTarget;
        private Building _lockedCombatBuildingTarget;
        private bool _hasCastleBounds;
        private Bounds _castleBounds;

        private const float AllyUnitAggroRadius = 2.2f;
        private const float CombatBuildingAggroRadius = 2.0f;

        private float _baseSpeed = 2f;
        private float _minSpeedRatio = 0.8f;
        private float _currentSpeed;
        private bool _isRetreating;
        private float _freezeRemaining;
        private float _stunRemaining;
        private float _burnDps;
        private float _burnRemaining;
        private float _bleedDps;
        private float _bleedRemaining;
        private float _dotAccum;

        private struct SlowDebuff
        {
            public float Percent;
            public float RemainingTime;
            public bool IsActive;
        }

        private const int MaxSlowDebuffs = 4;
        private readonly SlowDebuff[] _activeSlows = new SlowDebuff[MaxSlowDebuffs];

        private readonly EnemyAdvanceState _advanceState = new();
        private readonly EnemyAttackState _attackState = new();
        private readonly EnemyRetreatState _retreatState = new();
        private readonly EnemyDisabledState _disabledState = new();
        private IEnemyState _currentState;

        internal EnemyAdvanceState AdvanceState => _advanceState;
        internal EnemyAttackState AttackState => _attackState;
        internal EnemyRetreatState RetreatState => _retreatState;
        internal EnemyDisabledState DisabledState => _disabledState;

        private void Awake()
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1)
            {
                gameObject.layer = enemyLayer;
            }

            if (_modelTransform == null)
            {
                _modelTransform = transform;
            }

            Debug.Assert(_spriteRenderer != null, $"[{name}] _spriteRenderer is not assigned!", this);
            _originalColor = _spriteRenderer.color;
            _defaultSprite = _spriteRenderer.sprite;
            _spriteRenderer.sortingLayerName = "Enemies";
            _spriteRenderer.sortingOrder = 10;

            if (_circleCollider == null)
            {
                _circleCollider = GetComponent<CircleCollider2D>();
                if (_circleCollider != null)
                {
                    _baseColliderRadius = _circleCollider.radius;
                }
            }
        }

        public void Initialize(EnemyData enemyData, GridFlowfield pathFlowfield, Castle targetCastle, GridMap map)
        {
            _data = enemyData;
            _flowfield = pathFlowfield;
            _castle = targetCastle;
            _gridMap = map;

            int waveNum = Game.WaveSystem.WaveController.Instance != null
                ? Mathf.Max(1, Game.WaveSystem.WaveController.Instance.CurrentWaveNumber)
                : 1;
            float waveHpScale = 1f + Mathf.Max(0f, _data.HpGrowthPerWave) * (waveNum - 1);
            _maxHP = Mathf.Max(1, Mathf.RoundToInt(_data.MaxHP * waveHpScale));
            _currentHP = _maxHP;

            DifficultyMode difficulty = RunConfig.ActiveDifficulty;
            float dmgMult = DifficultyModifiers.GetEnemyDamageMultiplier(difficulty);
            float speedMult = DifficultyModifiers.GetEnemySpeedMultiplier(difficulty);

            _damage = Mathf.Max(1, Mathf.RoundToInt(_data.Damage * dmgMult));
            _attackInterval = _data.AttackInterval;
            _attackRange = _data.AttackRange;
            _attackTimer = 0f;
            _currentTarget = null;
            ReleaseCombatLocks();
            CacheCombatBounds();

            _baseSpeed = _data.MoveSpeed * speedMult;
            float gddMinCap = _data.IsBoss ? 0.9f : 0.8f;
            _minSpeedRatio = Mathf.Max(gddMinCap, Mathf.Clamp(_data.MinSpeedRatio, 0.05f, 1f));
            _currentSpeed = _baseSpeed;
            _isRetreating = false;
            ClearAllStatuses();

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = _data.Sprite != null ? _data.Sprite : _defaultSprite;
                _originalColor = _data.TintColor;
                _spriteRenderer.color = _originalColor;
            }

            Vector3 scale = _data.VisualScale;
            if (scale.sqrMagnitude < 0.001f) scale = Vector3.one;
            _modelTransform.localScale = scale;

            if (_circleCollider != null)
            {
                _circleCollider.radius = _baseColliderRadius * Mathf.Max(0.5f, scale.x);
            }

            TransitionTo(_advanceState);
        }

        internal void TransitionTo(IEnemyState newState)
        {
            if (_currentState == newState) return;
            _currentState?.Exit(this);
            _currentState = newState;
            _currentState?.Enter(this);
        }

        private void Update()
        {
            if (IsDead) return;

            float dt = Time.deltaTime;
            UpdateStatusesAndDots(dt);
            if (IsDead) return;

            _currentState?.Tick(this, dt);
        }

        internal bool HasExitedRetreatBounds()
        {
            if (_gridMap == null) return false;
            Vector3 pos = transform.position;
            Bounds b = _gridMap.Bounds;
            return pos.x < b.min.x - 1.5f || pos.x > b.max.x + 1.5f ||
                   pos.y < b.min.y - 1.5f || pos.y > b.max.y + 1.5f;
        }

        internal void CompleteRetreat()
        {
            _isRetreating = false;
            _currentState = null;
            Retreated?.Invoke(this);
        }

        internal Vector2 GetRetreatDirection()
        {
            return _flowfield != null
                ? _flowfield.GetRetreatDirection(transform.position)
                : Vector2.right;
        }

        internal Vector2 GetFlowDirection()
        {
            return _flowfield != null
                ? _flowfield.GetDirection(transform.position)
                : Vector2.zero;
        }

        internal void MoveByDirection(Vector2 direction, float speed, float dt)
        {
            transform.position += (Vector3)(direction * (speed * dt));
            if (direction.x > 0.05f) _spriteRenderer.flipX = false;
            else if (direction.x < -0.05f) _spriteRenderer.flipX = true;
        }

        internal void MoveTowardsPoint(Vector3 targetPoint, float dt)
        {
            _currentTarget = null;
            Vector2 moveDir = targetPoint - transform.position;
            if (moveDir.sqrMagnitude > 0.0001f)
            {
                moveDir.Normalize();
                MoveByDirection(moveDir, _currentSpeed, dt);
            }
        }

        internal bool IsInAttackRange(Vector3 targetPoint)
        {
            return Vector2.Distance(transform.position, targetPoint) <= _attackRange + 0.1f;
        }

        internal void ClearTarget()
        {
            _currentTarget = null;
        }

        internal void SetCombatTarget(IDamageable target, Vector3 targetPoint, bool isPointTarget)
        {
            _currentTarget = target;
        }

        internal bool TryResolveCombatTarget(out IDamageable target, out Vector3 targetPoint, out bool isPointTarget)
        {
            Vector3 pos = transform.position;

            // Priority 0: DecoySheep taunt within 3 cells (GIG-94 New Life spell)
            DecoySheep tauntSheep = FindNearestTauntSheep(pos);
            if (tauntSheep != null)
            {
                target = tauntSheep;
                targetPoint = tauntSheep.transform.position;
                isPointTarget = true;
                return true;
            }

            // Priority 1: AllyUnits with available aggro slots (GIG-59 / GIG-64)
            AllyUnit allyTarget = ResolveAllyUnitTarget(pos);
            if (allyTarget != null)
            {
                target = allyTarget;
                targetPoint = allyTarget.transform.position;
                isPointTarget = true;
                return true;
            }

            // Priority 2: Combat/Defense Buildings in 2 cells, locked until destroyed or knocked back (GIG-59)
            Building combatBuilding = ResolveCombatBuildingTarget(pos);
            if (combatBuilding != null)
            {
                target = combatBuilding;
                targetPoint = GetClosestPointOnBuilding(combatBuilding, pos);
                isPointTarget = true;
                return true;
            }

            // Priority 3: Castle or Support/Economy building ONLY if blocking path (GIG-59)
            if (_hasCastleBounds && IsTargetValid(_castle))
            {
                float sqrDistToCastle = _castleBounds.SqrDistance(pos);
                if (sqrDistToCastle <= _sqrAttackRange)
                {
                    target = _castle;
                    targetPoint = _castle.transform.position;
                    isPointTarget = false;
                    return true;
                }
            }

            if (_flowfield != null && _gridMap != null)
            {
                Vector2Int cell = _flowfield.WorldToCell(pos);
                IBuilding occupant = _gridMap.GetOccupant(cell.x, cell.y);

                if (!CanEnemyAttackBuilding(occupant) && _attackRange >= 0.8f)
                {
                    Vector2 dir = _flowfield.GetDirection(pos);
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        Vector2Int nextCell = cell + new Vector2Int(Mathf.RoundToInt(dir.x), Mathf.RoundToInt(dir.y));
                        IBuilding nextOccupant = _gridMap.GetOccupant(nextCell.x, nextCell.y);
                        if (CanEnemyAttackBuilding(nextOccupant) && !ReferenceEquals(nextOccupant, _castle))
                        {
                            float distToNextCell = Vector2.Distance(pos, _gridMap.CellToWorld(nextCell.x, nextCell.y));
                            if (distToNextCell <= _attackRange)
                            {
                                occupant = nextOccupant;
                            }
                        }
                    }
                }

                if (CanEnemyAttackBuilding(occupant) && !ReferenceEquals(occupant, _castle))
                {
                    target = occupant;
                    targetPoint = pos;
                    isPointTarget = false;
                    return true;
                }
            }

            target = null;
            targetPoint = default;
            isPointTarget = false;
            return false;
        }

        private static bool CanEnemyAttackBuilding(IBuilding b)
        {
            if (!IsTargetValid(b)) return false;
            if (b is Building concrete) return concrete.CanBeTargetedByEnemies;
            return true;
        }

        private static bool IsCombatOrDefenseCategory(BuildingCategory category)
        {
            return category == BuildingCategory.Archers ||
                   category == BuildingCategory.Melee ||
                   category == BuildingCategory.Mages ||
                   category == BuildingCategory.Siege ||
                   category == BuildingCategory.Control;
        }

        private DecoySheep FindNearestTauntSheep(Vector3 pos)
        {
            DecoySheep best = null;
            float bestSqr = float.MaxValue;
            var list = DecoySheep.ActiveSheep;

            for (int i = 0; i < list.Count; i++)
            {
                DecoySheep s = list[i];
                if (s == null || s.IsDead) continue;
                float sqr = (s.transform.position - pos).sqrMagnitude;
                if (sqr <= s.TauntRadius * s.TauntRadius && sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = s;
                }
            }

            return best;
        }

        private AllyUnit ResolveAllyUnitTarget(Vector3 pos)
        {
            if (_lockedUnitTarget != null)
            {
                if (_lockedUnitTarget.IsDead || _lockedUnitTarget.IsStealthed ||
                    Vector2.Distance(pos, _lockedUnitTarget.transform.position) > 3.5f)
                {
                    _lockedUnitTarget.ReleaseAggro(this);
                    _lockedUnitTarget = null;
                }
                else
                {
                    return _lockedUnitTarget;
                }
            }

            AllyUnit best = null;
            float bestSqr = AllyUnitAggroRadius * AllyUnitAggroRadius;
            var units = AllyUnit.ActiveUnits;

            for (int i = 0; i < units.Count; i++)
            {
                AllyUnit u = units[i];
                if (u == null || u.IsDead || u.IsStealthed || !u.HasAggroSlot) continue;

                float sqr = (u.transform.position - pos).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = u;
                }
            }

            if (best != null && best.TryClaimAggro(this))
            {
                _lockedUnitTarget = best;
                return best;
            }

            return null;
        }

        private Building ResolveCombatBuildingTarget(Vector3 pos)
        {
            if (_lockedCombatBuildingTarget != null)
            {
                if (!_lockedCombatBuildingTarget.IsDead && _lockedCombatBuildingTarget.CanBeTargetedByEnemies)
                {
                    return _lockedCombatBuildingTarget;
                }
                _lockedCombatBuildingTarget = null;
            }

            Building best = null;
            float bestSqr = CombatBuildingAggroRadius * CombatBuildingAggroRadius;
            var buildings = Building.ActiveBuildings;

            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                if (b == null || b.IsDead || !b.CanBeTargetedByEnemies || b.Data == null) continue;
                if (!IsCombatOrDefenseCategory(b.Data.Category)) continue;

                Vector3 closest = GetClosestPointOnBuilding(b, pos);
                float sqr = (closest - pos).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = b;
                }
            }

            _lockedCombatBuildingTarget = best;
            return best;
        }

        private static Vector3 GetClosestPointOnBuilding(Building b, Vector3 pos)
        {
            var bounds = new Bounds(b.transform.position, new Vector3(b.Size.x, b.Size.y, 1f));
            return bounds.ClosestPoint(pos);
        }

        private void ReleaseCombatLocks()
        {
            if (_lockedUnitTarget != null)
            {
                _lockedUnitTarget.ReleaseAggro(this);
                _lockedUnitTarget = null;
            }
            _lockedCombatBuildingTarget = null;
        }

        public void TakeDamage(int amount)
        {
            TakeDamage(amount, ignoreDefense: false);
        }

        public void TakeDamage(int amount, bool ignoreDefense)
        {
            if (IsDead || amount <= 0) return;

            int finalDamage = amount;
            if (!ignoreDefense && _data != null)
            {
                int afterDef = Mathf.Max(1, amount - _data.Defense);
                float resistance = Mathf.Clamp(_data.DamageResistance, 0f, 0.8f);
                finalDamage = Mathf.Max(1, Mathf.RoundToInt(afterDef * (1f - resistance)));
            }

            _currentHP = Mathf.Max(0, _currentHP - finalDamage);

            _flashTween?.Kill();
            _spriteRenderer.color = _originalColor;
            _flashTween = _spriteRenderer.DOColor(_damageFlashColor, 0.06f)
                .SetLoops(2, LoopType.Yoyo)
                .SetLink(gameObject)
                .SetUpdate(true);

            HealthChanged?.Invoke(this);

            if (_currentHP == 0)
            {
                _isRetreating = false;
                ReleaseCombatLocks();
                Destroyed?.Invoke(this);
                Died?.Invoke(this);
                GlobalEnemyDied?.Invoke(this);
            }
        }

        public void BeginRetreat()
        {
            if (IsDead || _isRetreating) return;
            _isRetreating = true;
            _currentTarget = null;
            ReleaseCombatLocks();
            _attackTimer = 0f;
            TransitionTo(_retreatState);
        }

        public void PolymorphOut()
        {
            if (IsDead) return;
            _isRetreating = false;
            _currentTarget = null;
            _currentState = null;
            ReleaseCombatLocks();
            Retreated?.Invoke(this);
        }

        public void Freeze(float duration)
        {
            if (IsDead || duration <= 0f) return;
            _freezeRemaining = Mathf.Max(_freezeRemaining, duration);
        }

        public void Stun(float duration)
        {
            if (IsDead || duration <= 0f) return;
            _stunRemaining = Mathf.Max(_stunRemaining, duration);
        }

        public void ApplyBurn(float dps, float duration)
        {
            if (IsDead || dps <= 0f || duration <= 0f) return;
            _burnDps = Mathf.Max(_burnDps, dps);
            _burnRemaining = Mathf.Max(_burnRemaining, duration);
        }

        public void ApplyBleed(float dps, float duration)
        {
            if (IsDead || dps <= 0f || duration <= 0f) return;
            _bleedDps = Mathf.Max(_bleedDps, dps);
            _bleedRemaining = Mathf.Max(_bleedRemaining, duration);
        }

        public void ApplySlow(float slowPercent, float duration)
        {
            if (slowPercent <= 0f || duration <= 0f) return;

            int bestSlot = -1;
            int lowestSlot = 0;
            float minRemaining = float.MaxValue;

            for (int i = 0; i < MaxSlowDebuffs; i++)
            {
                if (_activeSlows[i].IsActive && Mathf.Approximately(_activeSlows[i].Percent, slowPercent))
                {
                    _activeSlows[i].RemainingTime = Mathf.Max(_activeSlows[i].RemainingTime, duration);
                    return;
                }

                if (!_activeSlows[i].IsActive && bestSlot == -1)
                {
                    bestSlot = i;
                }

                if (_activeSlows[i].IsActive && _activeSlows[i].RemainingTime < minRemaining)
                {
                    minRemaining = _activeSlows[i].RemainingTime;
                    lowestSlot = i;
                }
            }

            if (bestSlot == -1)
            {
                bestSlot = lowestSlot;
            }

            _activeSlows[bestSlot] = new SlowDebuff
            {
                Percent = slowPercent,
                RemainingTime = duration,
                IsActive = true
            };
        }

        public void ClearAllStatuses()
        {
            ClearSlows();
            _freezeRemaining = 0f;
            _stunRemaining = 0f;
            _burnDps = 0f;
            _burnRemaining = 0f;
            _bleedDps = 0f;
            _bleedRemaining = 0f;
            _dotAccum = 0f;
        }

        public void ClearSlows()
        {
            for (int i = 0; i < MaxSlowDebuffs; i++)
            {
                _activeSlows[i].IsActive = false;
            }
            _currentSpeed = _baseSpeed;
        }

        public void ApplyKnockback(Vector2 direction, float cellDistance = 1f)
        {
            if (IsDead) return;

            ReleaseCombatLocks();
            _currentTarget = null;

            if (direction.sqrMagnitude > 0.0001f)
            {
                direction.Normalize();
                transform.position += (Vector3)(direction * cellDistance);
            }
        }

        private void UpdateStatusesAndDots(float deltaTime)
        {
            if (_freezeRemaining > 0f) _freezeRemaining = Mathf.Max(0f, _freezeRemaining - deltaTime);
            if (_stunRemaining > 0f) _stunRemaining = Mathf.Max(0f, _stunRemaining - deltaTime);

            float totalDot = 0f;
            if (_burnRemaining > 0f)
            {
                _burnRemaining = Mathf.Max(0f, _burnRemaining - deltaTime);
                totalDot += _burnDps;
                if (_burnRemaining <= 0f) _burnDps = 0f;
            }

            if (_bleedRemaining > 0f)
            {
                _bleedRemaining = Mathf.Max(0f, _bleedRemaining - deltaTime);
                totalDot += _bleedDps;
                if (_bleedRemaining <= 0f) _bleedDps = 0f;
            }

            if (totalDot > 0f)
            {
                _dotAccum += totalDot * deltaTime;
                int dmg = Mathf.FloorToInt(_dotAccum);
                if (dmg >= 1)
                {
                    _dotAccum -= dmg;
                    TakeDamage(dmg, ignoreDefense: false);
                    if (IsDead) return;
                }
            }

            UpdateSlows(deltaTime);

            if (IsFrozen || IsStunned)
            {
                _currentSpeed = 0f;
            }
        }

        private void UpdateSlows(float deltaTime)
        {
            float maxSlowPercent = 0f;

            for (int i = 0; i < MaxSlowDebuffs; i++)
            {
                if (!_activeSlows[i].IsActive) continue;

                _activeSlows[i].RemainingTime -= deltaTime;
                if (_activeSlows[i].RemainingTime <= 0f)
                {
                    _activeSlows[i].IsActive = false;
                }
                else if (_activeSlows[i].Percent > maxSlowPercent)
                {
                    maxSlowPercent = _activeSlows[i].Percent;
                }
            }

            _currentSpeed = _baseSpeed * Mathf.Max(1f - maxSlowPercent, _minSpeedRatio);
        }

        private static bool IsTargetValid(IDamageable target)
        {
            if (target == null) return false;
            if (target is UnityEngine.Object unityObj && unityObj == null) return false;
            return !target.IsDead;
        }

        private void CacheCombatBounds()
        {
            _sqrAttackRange = _attackRange * _attackRange;
            if (_castle != null)
            {
                _hasCastleBounds = true;
                _castleBounds = new Bounds(_castle.transform.position, new Vector3(_castle.Size.x, _castle.Size.y, 1f));
            }
            else
            {
                _hasCastleBounds = false;
                _castleBounds = default;
            }
        }

        internal void TickCombat(float deltaTime, IDamageable target)
        {
            if (!IsTargetValid(target))
            {
                _currentTarget = null;
                return;
            }

            _currentTarget = target;

            _attackTimer -= deltaTime;
            if (_attackTimer <= 0f)
            {
                target.TakeDamage(_damage);

                Vector3 currentScale = _data != null ? _data.VisualScale : Vector3.one;
                _punchTween?.Kill();
                _modelTransform.localScale = currentScale;
                _punchTween = _modelTransform.DOPunchScale(_punchScaleStrength, 0.12f, 8, 1f)
                    .SetLink(gameObject)
                    .SetUpdate(true);

                _attackTimer = _attackInterval;
            }
        }

        [Button]
        public void Take10Damage()
        {
#if UNITY_EDITOR
            TakeDamage(10);
#endif
        }

        [Button]
        public void HealFull()
        {
#if UNITY_EDITOR
            _currentHP = _maxHP;
            _flashTween?.Kill();
            _spriteRenderer.color = _originalColor;
#endif
        }

        private void ResetToBaseState()
        {
            if (_modelTransform == null)
            {
                _modelTransform = transform;
            }

            _currentHP = _maxHP;
            _currentSpeed = _baseSpeed;
            _isRetreating = false;
            ReleaseCombatLocks();
            ClearAllStatuses();
            _currentTarget = null;
            _attackTimer = 0f;

            _flashTween?.Kill();
            _punchTween?.Kill();
            Vector3 currentScale = _data != null ? _data.VisualScale : Vector3.one;
            _modelTransform.localScale = currentScale;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _originalColor;
            }
        }

        public void OnSpawnFromPool()
        {
            gameObject.SetActive(true);
            ResetToBaseState();
            TransitionTo(_advanceState);
        }

        public void OnReturnToPool()
        {
            ResetToBaseState();
            _currentState = null;
            gameObject.SetActive(false);
        }
    }
}
