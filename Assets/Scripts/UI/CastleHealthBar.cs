using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Alchemy.Inspector;
using Game.Buildings;

namespace Game.UI
{
    [DisallowMultipleComponent]
    public class CastleHealthBar : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private Castle castle;
        [SerializeField] private RectTransform barRoot;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image damageTrailImage;
        [SerializeField] private TextMeshProUGUI hpText;

        [Title("Damage Effects")]
        [SerializeField] private Color damageFlashColor = Color.white;
        [SerializeField] private Vector3 punchStrength = new(0.08f, 0.08f, 0f);

        private Color originalFillColor;
        private int currentDisplayedHP;

        private Tweener fillTween;
        private Tweener trailTween;
        private Tweener textTween;
        private Tweener punchTween;
        private Tweener flashTween;

        private void Awake()
        {
            originalFillColor = fillImage.color;
        }

        private void Start()
        {
            UpdateVisualsInstant();
        }

        private void OnEnable()
        {
            castle.HealthChanged += OnHealthChanged;
            UpdateVisualsInstant();
        }

        private void UpdateVisualsInstant()
        {
            if (castle == null) return;

            currentDisplayedHP = castle.CurrentHP;
            float initialFill = castle.MaxHP > 0 ? (float)castle.CurrentHP / castle.MaxHP : 0f;
            
            fillImage.fillAmount = initialFill;
            
            damageTrailImage.fillAmount = initialFill;

            hpText.text = $"{currentDisplayedHP} / {castle.MaxHP}";

        }

        private void OnDisable()
        {
            castle.HealthChanged -= OnHealthChanged;


            fillTween?.Kill();
            trailTween?.Kill();
            textTween?.Kill();
            punchTween?.Kill();
            flashTween?.Kill();
        }

        private void OnHealthChanged(IBuilding building)
        {
            float targetFill = building.MaxHP > 0 ? (float)building.CurrentHP / building.MaxHP : 0f;
            int targetHP = building.CurrentHP;
            int maxHP = building.MaxHP;

            // Основная полоска
            fillTween?.Kill();
            fillTween = fillImage.DOFillAmount(targetFill, 0.25f)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject)
                .SetUpdate(true);

            // Отстающая полоска урона с задержкой 0.3s (если назначена)
            trailTween?.Kill();
            trailTween = damageTrailImage.DOFillAmount(targetFill, 0.35f)
                .SetDelay(0.3f)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject)
                .SetUpdate(true);

            // Плавная интерполяция чисел в тексте
            textTween?.Kill();
            textTween = DOTween.To(() => currentDisplayedHP, x => 
                {
                    currentDisplayedHP = x;
                    hpText.text = $"{currentDisplayedHP} / {maxHP}";
                }, targetHP, 0.25f)
                    .SetLink(gameObject)
                    .SetUpdate(true);

            // Тряска корня полоски и вспышка цвета
            punchTween?.Complete();
            barRoot.localScale = Vector3.one;
            punchTween = barRoot.DOPunchScale(punchStrength, 0.15f, 10, 1f)
                .SetLink(gameObject)
                .SetUpdate(true);
                
            flashTween?.Kill();
            fillImage.color = originalFillColor;
            flashTween = fillImage.DOColor(damageFlashColor, 0.08f)
                .SetLoops(2, LoopType.Yoyo)
                .SetLink(gameObject)
                .SetUpdate(true);
        }
    }
}
