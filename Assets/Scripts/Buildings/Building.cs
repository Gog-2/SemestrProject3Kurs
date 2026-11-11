using System;
using System.Collections.Generic;
using UnityEngine;
using Alchemy.Inspector;
using DG.Tweening;
using Game.Core;
using Game.Upgrades;

namespace Game.Buildings
{
    public class Building : MonoBehaviour, IBuilding
    {
        private static readonly List<Building> s_activeBuildings = new(32);
        public static IReadOnlyList<Building> ActiveBuildings => s_activeBuildings;

        [Title("Data & Identity")]
        [SerializeField] protected BuildingData _data;
        [SerializeField] protected SpriteRenderer _spriteRenderer;
        [SerializeField] protected Transform _modelTransform;

        [Title("State")]
        [ShowInInspector, ReadOnly] protected int _currentLevel = 1;
        [ShowInInspector, ReadOnly] protected int _currentHP;
        [ShowInInspector, ReadOnly] protected int _combatXP;
        [ShowInInspector, ReadOnly] protected Vector2Int _originCell;

        [Title("Animation & Highlight")]
        [SerializeField] protected Color _damageColor = new(1f, 0.3f, 0.3f, 1f);
        [SerializeField] protected Vector3 _punchStrength = new(0.12f, 0.12f, 0f);
        [SerializeField] protected SpriteRenderer _xpHighlightRenderer;

        protected GridMap _gridMap;
        protected Color _originalColor = Color.white;
        protected Tweener _punchTween;
        protected Tweener _colorTween;

        public BuildingData Data => _data;
        public int CurrentLevel => _currentLevel;
        public int CombatXP => _combatXP;
        public BuildingLevelStats CurrentStats => _data != null ? _data.GetStats(_currentLevel) : default;

        public bool IsCombatXpReady
        {
            get
            {
                if (_data == null || _currentLevel >= 4) return false;
                BuildingLevelStats nextStats = _data.GetStats(_currentLevel + 1);
                return nextStats.combatXpRequirement > 0 && _combatXP >= nextStats.combatXpRequirement;
            }
        }

        public int NextLevelCombatXpRequirement
        {
            get
            {
                if (_data == null || _currentLevel >= 4) return 0;
                return _data.GetStats(_currentLevel + 1).combatXpRequirement;
            }
        }

        // IBuilding
        public float BreakCost => PathBreakCost;
        public virtual float PathBreakCost => CurrentHP;
        public virtual bool CanBeTargetedByEnemies => _data == null || _data.IsDestructible;
        public Vector2Int Origin => _originCell;
        public Vector2Int Size => _data != null ? _data.Size : Vector2Int.one;

        // IDamageable
        public int CurrentHP => _currentHP;
        public int MaxHP => CalculateEffectiveMaxHP();
        public bool IsDead => _currentHP <= 0;

        public event Action<IDamageable> HealthChanged;
        public event Action<IDamageable> Destroyed;
        public event Action<IBuilding> BuildingHealthChanged;
        public event Action<IBuilding> BuildingDestroyed;
        public event Action<int> LevelChanged;
        public event Action<int> CombatXPChanged;

        protected virtual void OnEnable()
        {
            if (!s_activeBuildings.Contains(this))
            {
                s_activeBuildings.Add(this);
            }

            if (RunProgressionManager.Instance != null)
            {
                RunProgressionManager.Instance.UpgradeApplied -= OnGlobalUpgradeApplied;
                RunProgressionManager.Instance.UpgradeApplied += OnGlobalUpgradeApplied;
            }
        }

        protected virtual void OnDisable()
        {
            s_activeBuildings.Remove(this);

            if (RunProgressionManager.Instance != null)
            {
                RunProgressionManager.Instance.UpgradeApplied -= OnGlobalUpgradeApplied;
            }
        }

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

            Debug.Assert(_spriteRenderer != null, $"[{name}] _spriteRenderer is missing!", this);
            Debug.Assert(_modelTransform != null, $"[{name}] _modelTransform is missing!", this);

            _originalColor = _spriteRenderer.color;
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
            UpdateCombatXpHighlight();
            OnPlaced();
        }

        protected virtual int CalculateEffectiveMaxHP()
        {
            int baseHp = CurrentStats.maxHP;
            if (baseHp <= 0) return 0;

            if (RunProgressionManager.Instance != null)
            {
                float mult = RunProgressionManager.Instance.GetBuildingMaxHpMultiplier(_data);
                int flat = RunProgressionManager.Instance.GetBuildingMaxHpFlatBonus(_data);
                return Mathf.Max(1, Mathf.RoundToInt(baseHp * mult) + flat);
            }

            return baseHp;
        }

        private void OnGlobalUpgradeApplied(UpgradeData upgrade)
        {
            if (upgrade != null && upgrade.EffectType != UpgradeEffectType.BuildingHealthPercent) return;
            RefreshMaxHealthAfterModifier();
        }

        protected void RefreshMaxHealthAfterModifier()
        {
            int newMax = MaxHP;
            if (newMax <= 0) return;

            if (_currentHP > newMax)
            {
                _currentHP = newMax;
            }
            HealthChanged?.Invoke(this);
            BuildingHealthChanged?.Invoke(this);
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

            _spriteRenderer.color = _originalColor;
            _colorTween = _spriteRenderer.DOColor(_damageColor, 0.08f)
                .SetLoops(2, LoopType.Yoyo)
                .SetLink(gameObject)
                .SetUpdate(true);

            _punchTween = _modelTransform.DOPunchScale(_punchStrength, 0.12f, 8, 1f)
                .SetLink(gameObject)
                .SetUpdate(true);

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
            if (xp <= 0 || _currentLevel >= 4) return;
            if (IsCombatXpReady) return;

            int req = NextLevelCombatXpRequirement;
            _combatXP += xp;
            if (req > 0 && _combatXP > req)
            {
                _combatXP = req;
            }

            CombatXPChanged?.Invoke(_combatXP);
            UpdateCombatXpHighlight();
        }

        public void AddWaveSurvivalXP()
        {
            AddCombatXP(15);
        }

        public virtual bool CanUpgrade(out int cost)
        {
            cost = 0;
            if (_data == null || _currentLevel >= 4) return false;

            BuildingLevelStats nextStats = _data.GetStats(_currentLevel + 1);
            float discountMult = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveUpgradeCostMultiplier()
                : 1f;
            cost = Mathf.RoundToInt(nextStats.cost * discountMult);

            if (nextStats.combatXpRequirement > 0 && _combatXP >= nextStats.combatXpRequirement)
            {
                cost = Mathf.RoundToInt(cost * 0.75f);
            }

            return true;
        }

        public virtual void Upgrade()
        {
            if (_currentLevel >= 4) return;

            bool fullHealFromXp = IsCombatXpReady;
            int prevMaxHP = MaxHP;
            _currentLevel++;
            int newMaxHP = MaxHP;

            if (fullHealFromXp)
            {
                // Full-heal building itself on Combat XP upgrade (live units are NOT healed per GDD)
                _currentHP = newMaxHP;
                _combatXP = 0;
            }
            else
            {
                int hpIncrease = Mathf.Max(0, newMaxHP - prevMaxHP);
                _currentHP = Mathf.Min(newMaxHP, _currentHP + hpIncrease);
            }

            UpdateCombatXpHighlight();
            CombatXPChanged?.Invoke(_combatXP);
            LevelChanged?.Invoke(_currentLevel);
            HealthChanged?.Invoke(this);
            BuildingHealthChanged?.Invoke(this);

            OnLevelUp(_currentLevel);
        }

        public virtual void SetLevel(int level)
        {
            _currentLevel = Mathf.Clamp(level, 1, 4);
            _currentHP = MaxHP;
            _combatXP = 0;
            UpdateCombatXpHighlight();
            LevelChanged?.Invoke(_currentLevel);
            HealthChanged?.Invoke(this);
            BuildingHealthChanged?.Invoke(this);
            OnLevelUp(_currentLevel);
        }

        public virtual void SyncToLevel(int targetLevel)
        {
            int clampedTarget = Mathf.Clamp(targetLevel, 1, 4);
            while (_currentLevel < clampedTarget)
            {
                Upgrade();
            }
        }

        protected void UpdateCombatXpHighlight()
        {
            bool ready = IsCombatXpReady;
            if (!ready)
            {
                if (_xpHighlightRenderer != null)
                {
                    _xpHighlightRenderer.enabled = false;
                }
                return;
            }

            EnsureXpHighlightRenderer();
            if (_xpHighlightRenderer != null)
            {
                _xpHighlightRenderer.enabled = true;
            }
        }

        private void EnsureXpHighlightRenderer()
        {
            if (_xpHighlightRenderer != null || _spriteRenderer == null || _spriteRenderer.sprite == null) return;

            var go = new GameObject("CombatXpHighlight");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            Vector3 baseScale = _spriteRenderer.transform.localScale;
            go.transform.localScale = baseScale * 1.12f;

            _xpHighlightRenderer = go.AddComponent<SpriteRenderer>();
            _xpHighlightRenderer.sprite = _spriteRenderer.sprite;
            _xpHighlightRenderer.color = new Color(1f, 0.85f, 0.15f, 0.45f);
            _xpHighlightRenderer.sortingLayerID = _spriteRenderer.sortingLayerID;
            _xpHighlightRenderer.sortingOrder = _spriteRenderer.sortingOrder - 1;
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

            if (Application.isPlaying)
                Destroy(gameObject);
            else
                DestroyImmediate(gameObject);
        }

        protected virtual void OnPlaced() { }
        protected virtual void OnRemoved() { }
        protected virtual void OnLevelUp(int newLevel) { }
    }
}
