using System;
using UnityEngine;
using DG.Tweening;
using Alchemy.Inspector;
using Game.Core;

namespace Game.Buildings
{
    public class Castle : MonoBehaviour, IBuilding
    {
        [Title("References")]
        [SerializeField] private GridMap _gridMap;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Title("Stats & Placement")]
        [SerializeField, Min(1)] private int _maxHP = 1500;
        [SerializeField] private Vector2Int _size = new(3, 3);
        [SerializeField] private bool _placeAtMapCenter = true;
        [SerializeField] private Vector2Int _originCell;

        [Title("Damage Animation")]
        [SerializeField] private Color _damageColor = Color.red;
        [SerializeField] private Vector3 _punchStrength = new(0.12f, 0.12f, 0f);

        [Title("Debug")]
        [ShowInInspector, ReadOnly]
        public int CurrentHP => _currentHP < 0 ? _maxHP : _currentHP;
        public int MaxHP => _maxHP;
        public bool IsDead => CurrentHP <= 0;
        public float BreakCost => CurrentHP;
        public Vector2Int Origin => _originCell;
        public Vector2Int Size => _size;

        private int _currentHP = -1;
        private Color _originalColor;
        private Tweener _punchTween;
        private Tweener _colorTween;

        public event Action<IDamageable> HealthChanged;
        public event Action<IDamageable> Destroyed;
        public event Action<IBuilding> BuildingHealthChanged;
        public event Action<IBuilding> BuildingDestroyed;
        public event Action OnCastleDestroyed;

        private void Awake()
        {
            if (_currentHP < 0)
            {
                _currentHP = _maxHP;
            }

            _originalColor = _spriteRenderer.color;
        }

        private void Start()
        {
            if (_placeAtMapCenter)
            {
                _originCell = new Vector2Int((_gridMap.Width - _size.x) / 2, (_gridMap.Height - _size.y) / 2);
            }

            transform.position = _gridMap.CellToWorld(_originCell.x, _originCell.y) +
                                 new Vector3((_size.x - 1) * 0.5f, (_size.y - 1) * 0.5f, 0f);

            _gridMap.TryPlace(this);
            _spriteRenderer.transform.localScale = new Vector3(_size.x, _size.y, 1f);
        }

        private void OnValidate()
        {
            if (_maxHP < 1) _maxHP = 1;
            if (_size.x < 1) _size.x = 1;
            if (_size.y < 1) _size.y = 1;
        }

        public void TakeDamage(int amount)
        {
            if (_currentHP <= 0 || amount <= 0) return;

            _currentHP = Mathf.Max(0, _currentHP - amount);

            _punchTween?.Complete();
            _colorTween?.Kill();
            _spriteRenderer.color = _originalColor;

            _punchTween = _spriteRenderer.transform.DOPunchScale(_punchStrength, 0.15f, 10, 1f)
                .SetLink(gameObject)
                .SetUpdate(true);

            _colorTween = _spriteRenderer.DOColor(_damageColor, 0.075f)
                .SetLoops(2, LoopType.Yoyo)
                .SetLink(gameObject)
                .SetUpdate(true);

            HealthChanged?.Invoke(this);
            BuildingHealthChanged?.Invoke(this);

            if (_currentHP == 0)
            {
                Destroyed?.Invoke(this);
                BuildingDestroyed?.Invoke(this);
                OnCastleDestroyed?.Invoke();
            }
        }

        [Button]
        public void Take10Damage()
        {
            TakeDamage(10);
        }

        [Button]
        public void HealFull()
        {
            _currentHP = _maxHP;
            _gridMap.TryPlace(this);
            HealthChanged?.Invoke(this);
            BuildingHealthChanged?.Invoke(this);
        }
    }
}
