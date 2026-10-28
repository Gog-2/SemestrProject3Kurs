using System;
using UnityEngine;
using Alchemy.Inspector;
using DG.Tweening;
using Game.Core;

namespace Game.Buildings
{
    public class Building : MonoBehaviour, IBuilding
    {
        [Title("Data & Identity")]
        [SerializeField] protected BuildingData _data;
        [SerializeField] protected SpriteRenderer _spriteRenderer;
        [SerializeField] protected Transform _modelTransform;

        [Title("State")]
        [ShowInInspector, ReadOnly] protected int _currentLevel = 1;
        [ShowInInspector, ReadOnly] protected int _currentHP;
        [ShowInInspector, ReadOnly] protected int _combatXP;
        [ShowInInspector, ReadOnly] protected Vector2Int _originCell;

        [Title("Animation")]
        [SerializeField] protected Color _damageColor = new(1f, 0.3f, 0.3f, 1f);
        [SerializeField] protected Vector3 _punchStrength = new(0.12f, 0.12f, 0f);

        protected GridMap _gridMap;
        protected Color _originalColor = Color.white;
        protected Tweener _punchTween;
        protected Tweener _colorTween;

        public BuildingData Data => _data;
        public int CurrentLevel => _currentLevel;
        public int CombatXP => _combatXP;
        public BuildingLevelStats CurrentStats => _data != null ? _data.GetStats(_currentLevel) : default;

        // IBuilding
        public float BreakCost => CurrentHP;
        public Vector2Int Origin => _originCell;
        public Vector2Int Size => _data != null ? _data.Size : Vector2Int.one;

        // IDamageable
        public int CurrentHP => _currentHP;
        public int MaxHP => CurrentStats.maxHP > 0 ? CurrentStats.maxHP : 100;
        public bool IsDead => _currentHP <= 0;

        public event Action<IDamageable> HealthChanged;
        public event Action<IDamageable> Destroyed;
        public event Action<IBuilding> BuildingHealthChanged;
        public event Action<IBuilding> BuildingDestroyed;
        public event Action<int> LevelChanged;
        public event Action<int> CombatXPChanged;

        protected virtual void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_modelTransform == null)
            {
                _modelTransform = _spriteRenderer != null ? _spriteRenderer.transform : transform;
            }

            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
            }
        }

        public virtual void Initialize(BuildingData data, GridMap map, Vector2Int origin, int level = 1)
        {
            _data = data;
            _gridMap = map;
            _originCell = origin;
            _currentLevel = Mathf.Clamp(level, 1, 4);
            _combatXP = 0;

            _currentHP = MaxHP;

            UpdateWorldPosition();
            OnPlaced();
        }

        public void UpdateWorldPosition()
        {
            if (_gridMap == null) return;

            Vector2Int size = Size;
            Vector3 worldCenter = _gridMap.CellToWorld(_originCell.x, _originCell.y) +
                                  new Vector3((size.x - 1) * 0.5f, (size.y - 1) * 0.5f, 0f);
            transform.position = worldCenter;
        }

        public void SetOrigin(Vector2Int newOrigin)
        {
            _originCell = newOrigin;
            UpdateWorldPosition();
        }

        public virtual void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;
            if (_data != null && !_data.IsDestructible) return;

            _currentHP = Mathf.Max(0, _currentHP - amount);

            _punchTween?.Complete();
            _colorTween?.Kill();

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _originalColor;
                _colorTween = _spriteRenderer.DOColor(_damageColor, 0.08f)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetLink(gameObject)
                    .SetUpdate(true);
            }

            if (_modelTransform != null)
            {
                _punchTween = _modelTransform.DOPunchScale(_punchStrength, 0.12f, 8, 1f)
                    .SetLink(gameObject)
                    .SetUpdate(true);
            }

            HealthChanged?.Invoke(this);
            BuildingHealthChanged?.Invoke(this);

            if (_currentHP == 0)
            {
                Die();
            }
        }

        public virtual void Repair(int amount)
        {
            if (IsDead || amount <= 0) return;

            _currentHP = Mathf.Min(MaxHP, _currentHP + amount);
            HealthChanged?.Invoke(this);
            BuildingHealthChanged?.Invoke(this);
        }

        public virtual void AddCombatXP(int xp)
        {
            if (xp <= 0) return;
            _combatXP += xp;
            CombatXPChanged?.Invoke(_combatXP);
        }

        public virtual bool CanUpgrade(out int cost)
        {
            cost = 0;
            if (_data == null || _currentLevel >= 4) return false;

            BuildingLevelStats nextStats = _data.GetStats(_currentLevel + 1);
            cost = nextStats.cost;

            // Combat XP discount: if building has accumulated required combat XP, 25% discount
            if (nextStats.combatXpRequirement > 0 && _combatXP >= nextStats.combatXpRequirement)
            {
                cost = Mathf.RoundToInt(cost * 0.75f);
            }

            return true;
        }

        public virtual void Upgrade()
        {
            if (_currentLevel >= 4) return;

            int prevMaxHP = MaxHP;
            _currentLevel++;
            int newMaxHP = MaxHP;

            // Heal difference on upgrade
            int hpIncrease = Mathf.Max(0, newMaxHP - prevMaxHP);
            _currentHP += hpIncrease;

            LevelChanged?.Invoke(_currentLevel);
            HealthChanged?.Invoke(this);
            BuildingHealthChanged?.Invoke(this);

            OnLevelUp(_currentLevel);
        }

        public virtual void SetLevel(int level)
        {
            _currentLevel = Mathf.Clamp(level, 1, 4);
            _currentHP = MaxHP;
            LevelChanged?.Invoke(_currentLevel);
            HealthChanged?.Invoke(this);
            BuildingHealthChanged?.Invoke(this);
            OnLevelUp(_currentLevel);
        }

        public virtual void DestroyBuilding()
        {
            Die();
        }

        protected virtual void Die()
        {
            _punchTween?.Kill();
            _colorTween?.Kill();

            OnRemoved();

            Destroyed?.Invoke(this);
            BuildingDestroyed?.Invoke(this);

            if (_gridMap != null)
            {
                _gridMap.Remove(this);
            }

            Destroy(gameObject);
        }

        protected virtual void OnPlaced() { }
        protected virtual void OnRemoved() { }
        protected virtual void OnLevelUp(int newLevel) { }
    }
}
