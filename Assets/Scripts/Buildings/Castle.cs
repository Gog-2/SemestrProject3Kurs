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
        [SerializeField] private GridMap gridMap;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Title("Stats & Placement")]
        [SerializeField, Min(1)] private int maxHP = 100;
        [SerializeField] private Vector2Int size = new(3, 3);
        [SerializeField] private bool placeAtMapCenter = true;
        [SerializeField] private Vector2Int originCell;

        [Title("Damage Animation")]
        [SerializeField] private Color damageColor = Color.red;
        [SerializeField] private Vector3 punchStrength = new(0.12f, 0.12f, 0f);

        [Title("Debug")]
        [ShowInInspector, ReadOnly]
        public int CurrentHP => currentHP < 0 ? maxHP : currentHP;
        public int MaxHP => maxHP;
        public float BreakCost => CurrentHP;
        public Vector2Int Origin => originCell;
        public Vector2Int Size => size;

        private int currentHP = -1;
        private Color originalColor;
        private Tweener punchTween;
        private Tweener colorTween;

        public event Action<IBuilding> HealthChanged;
        public event Action<IBuilding> Destroyed;
        public event Action OnCastleDestroyed;

        private void Awake()
        {
            if (currentHP < 0)
            {
                currentHP = maxHP;
            }
            originalColor = spriteRenderer.color;
        }

        private void Start()
        {
            if (placeAtMapCenter)
            {
                originCell = new Vector2Int((gridMap.Width - size.x) / 2, (gridMap.Height - size.y) / 2);
            }

            // Центрирование относительно футпринта клеток (1 клетка = 1 единица)
            transform.position = gridMap.CellToWorld(originCell.x, originCell.y) +
                                 new Vector3((size.x - 1) * 0.5f, (size.y - 1) * 0.5f, 0f);

            spriteRenderer.transform.localScale = new Vector3(size.x, size.y, 1f);

            gridMap.TryPlace(this);
        }

        private void OnValidate()
        {
            if (maxHP < 1) maxHP = 1;
            if (size.x < 1) size.x = 1;
            if (size.y < 1) size.y = 1;

            if (placeAtMapCenter && gridMap != null)
            {
                originCell = new Vector2Int((gridMap.Width - size.x) / 2, (gridMap.Height - size.y) / 2);
            }
        }

        public void TakeDamage(int amount)
        {
            if (currentHP <= 0) return;

            currentHP = Mathf.Max(0, currentHP - amount);

            // Завершаем предыдущий твин масштаба перед новым, чтобы не накапливать смещение
            punchTween?.Complete();
            colorTween?.Kill();
            spriteRenderer.color = originalColor;

            punchTween = spriteRenderer.transform.DOPunchScale(punchStrength, 0.15f, 10, 1f)
                .SetLink(gameObject)
                .SetUpdate(true);

            colorTween = spriteRenderer.DOColor(damageColor, 0.075f)
                .SetLoops(2, LoopType.Yoyo)
                .SetLink(gameObject)
                .SetUpdate(true);

            HealthChanged?.Invoke(this);

            if (currentHP == 0)
            {
                Destroyed?.Invoke(this);
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
            currentHP = maxHP;
            HealthChanged?.Invoke(this);
        }

        private void OnDestroy()
        {
            punchTween?.Kill();
            colorTween?.Kill();
        }
    }
}
