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

        private EnemyData _data;
        private GridFlowfield _flowfield;
        private Castle _castle;
        private GridMap _gridMap;

        private int _maxHP = 1;
        private int _currentHP;

        private Color _originalColor;
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
            if (_modelTransform == null)
            {
                _modelTransform = transform;
            }

            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
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
            _minSpeedRatio = Mathf.Clamp(_data.MinSpeedRatio, 0.05f, 1f);
            _currentSpeed = _baseSpeed;
            ClearSlows();
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

                    if (_spriteRenderer != null)
                    {
                        if (direction.x > 0.05f) _spriteRenderer.flipX = false;
                        else if (direction.x < -0.05f) _spriteRenderer.flipX = true;
                    }
                }
            }
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;

            _currentHP = Mathf.Max(0, _currentHP - amount);

            _flashTween?.Kill();
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _originalColor;
                _flashTween = _spriteRenderer.DOColor(_damageFlashColor, 0.06f)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetLink(gameObject)
                    .SetUpdate(true);
            }

            HealthChanged?.Invoke(this);

            if (_currentHP == 0)
            {
                Destroyed?.Invoke(this);
                Died?.Invoke(this);
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

                _punchTween?.Kill();
                _modelTransform.localScale = Vector3.one;
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
            if (_spriteRenderer != null) _spriteRenderer.color = _originalColor;
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
            _modelTransform.localScale = Vector3.one;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _originalColor;
            }
        }

        public void OnReturnToPool()
        {
            _flashTween?.Kill();
            _punchTween?.Kill();
            _modelTransform.localScale = Vector3.one;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _originalColor;
            }

            _currentHP = _maxHP;
            _currentSpeed = _baseSpeed;
            ClearSlows();
            _currentTarget = null;
            _attackTimer = 0f;

            gameObject.SetActive(false);
        }
    }
}
