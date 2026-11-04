using System;
using UnityEngine;
using DG.Tweening;
using Alchemy.Inspector;
using Game.Core;
using Game.Buildings;
using Game.Pathfinding;
using Game.Enemies.Data;

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
        public bool IsAttacking => _currentTarget != null && _currentTarget.IsDead == false;

        public event Action<IDamageable> HealthChanged;
        public event Action<IDamageable> Destroyed;
        public event Action<Enemy> Died;
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
        private float _attackTimer;
        private IDamageable _currentTarget;

        private float _baseSpeed = 2f;
        private float _minSpeedRatio = 0.8f;
        private float _currentSpeed;

        private struct SlowDebuff
        {
            public float Percent;
            public float RemainingTime;
            public bool IsActive;
        }

        private const int MaxSlowDebuffs = 4;
        private readonly SlowDebuff[] _activeSlows = new SlowDebuff[MaxSlowDebuffs];

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

            _maxHP = _data.MaxHP;
            _currentHP = _maxHP;

            _damage = _data.Damage;
            _attackInterval = _data.AttackInterval;
            _attackRange = _data.AttackRange;
            _attackTimer = 0f;
            _currentTarget = null;

            _baseSpeed = _data.MoveSpeed;
            float gddMinCap = _data.IsBoss ? 0.9f : 0.8f;
            _minSpeedRatio = Mathf.Max(gddMinCap, Mathf.Clamp(_data.MinSpeedRatio, 0.05f, 1f));
            _currentSpeed = _baseSpeed;
            ClearSlows();

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
        }

        private void Update()
        {
            if (IsDead) return;

            float dt = Time.deltaTime;
            UpdateSlows(dt);

            Vector3 pos = transform.position;

            if (_castle != null && !_castle.IsDead)
            {
                Bounds castleBounds = new Bounds(_castle.transform.position, new Vector3(_castle.Size.x, _castle.Size.y, 1f));
                float sqrDistToCastle = castleBounds.SqrDistance(pos);
                if (sqrDistToCastle <= _attackRange * _attackRange)
                {
                    TickCombat(dt, _castle);
                    return;
                }
            }

            if (_flowfield != null && _gridMap != null)
            {
                Vector2Int cell = _flowfield.WorldToCell(pos);
                IBuilding occupant = _gridMap.GetOccupant(cell.x, cell.y);

                if ((occupant == null || occupant.IsDead) && _attackRange >= 0.8f)
                {
                    Vector2 dir = _flowfield.GetDirection(pos);
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        Vector2Int nextCell = cell + new Vector2Int(Mathf.RoundToInt(dir.x), Mathf.RoundToInt(dir.y));
                        IBuilding nextOccupant = _gridMap.GetOccupant(nextCell.x, nextCell.y);
                        if (nextOccupant != null && nextOccupant != (IBuilding)_castle && !nextOccupant.IsDead)
                        {
                            float distToNextCell = Vector2.Distance(pos, _gridMap.CellToWorld(nextCell.x, nextCell.y));
                            if (distToNextCell <= _attackRange)
                            {
                                occupant = nextOccupant;
                            }
                        }
                    }
                }

                if (occupant != null && occupant != (IBuilding)_castle && !occupant.IsDead)
                {
                    TickCombat(dt, occupant);
                    return;
                }
            }

            _currentTarget = null;

            if (_flowfield != null)
            {
                Vector2 direction = _flowfield.GetDirection(pos);
                if (direction.sqrMagnitude > 0.0001f)
                {
                    transform.position += (Vector3)(direction * (_currentSpeed * dt));

                    if (direction.x > 0.05f) _spriteRenderer.flipX = false;
                    else if (direction.x < -0.05f) _spriteRenderer.flipX = true;
                }
            }
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;

            _currentHP = Mathf.Max(0, _currentHP - amount);

            _flashTween?.Kill();
            _spriteRenderer.color = _originalColor;
            _flashTween = _spriteRenderer.DOColor(_damageFlashColor, 0.06f)
                .SetLoops(2, LoopType.Yoyo)
                .SetLink(gameObject)
                .SetUpdate(true);

            HealthChanged?.Invoke(this);

            if (_currentHP == 0)
            {
                RunProgressionManager.Instance.AddHonor(_data.HonorReward);

                Destroyed?.Invoke(this);
                Died?.Invoke(this);
                GlobalEnemyDied?.Invoke(this);
            }
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

            if (direction.sqrMagnitude > 0.0001f)
            {
                direction.Normalize();
                transform.position += (Vector3)(direction * cellDistance);
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

        private void TickCombat(float deltaTime, IDamageable target)
        {
            _currentTarget = target;
            if (target.IsDead) return;

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

        public void OnSpawnFromPool()
        {
            gameObject.SetActive(true);

            _currentHP = _maxHP;
            _currentSpeed = _baseSpeed;
            _currentTarget = null;
            _attackTimer = 0f;

            _flashTween?.Kill();
            _punchTween?.Kill();
            Vector3 currentScale = _data != null ? _data.VisualScale : Vector3.one;
            _modelTransform.localScale = currentScale;
            _spriteRenderer.color = _originalColor;
        }

        public void OnReturnToPool()
        {
            _flashTween?.Kill();
            _punchTween?.Kill();
            Vector3 currentScale = _data != null ? _data.VisualScale : Vector3.one;
            _modelTransform.localScale = currentScale;
            _spriteRenderer.color = _originalColor;

            _currentHP = _maxHP;
            _currentSpeed = _baseSpeed;
            ClearSlows();
            _currentTarget = null;
            _attackTimer = 0f;

            gameObject.SetActive(false);
        }
    }
}
