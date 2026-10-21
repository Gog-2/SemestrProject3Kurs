using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Alchemy.Inspector;
using Game.Buildings;
using Game.Core;

namespace Game.UI
{
    public class CastleHealthBar : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private Castle _castle;
        [SerializeField] private RectTransform _barRoot;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Image _damageTrailImage;
        [SerializeField] private TextMeshProUGUI _hpText;

        [Title("Damage Effects")]
        [SerializeField] private Color _damageFlashColor = Color.white;
        [SerializeField] private Vector3 _punchStrength = new(0.08f, 0.08f, 0f);

        private Color _originalFillColor;
        private int _currentDisplayedHP;

        private Tweener _fillTween;
        private Tweener _trailTween;
        private Tweener _textTween;
        private Tweener _punchTween;
        private Tweener _flashTween;

        private void Awake()
        {
            _originalFillColor = _fillImage.color;
        }

        private void OnEnable()
        {
            _castle.HealthChanged += OnHealthChanged;
            UpdateVisualsInstant();
        }

        private void OnDisable()
        {
            _castle.HealthChanged -= OnHealthChanged;
            KillActiveTweens();
        }

        private void UpdateVisualsInstant()
        {
            _currentDisplayedHP = _castle.CurrentHP;
            float initialFill = (float)_castle.CurrentHP / _castle.MaxHP;

            _fillImage.fillAmount = initialFill;
            _damageTrailImage.fillAmount = initialFill;
            _hpText.text = $"{_currentDisplayedHP} / {_castle.MaxHP}";
        }

        private void OnHealthChanged(IDamageable damageable)
        {
            float targetFill = (float)damageable.CurrentHP / damageable.MaxHP;
            int targetHP = damageable.CurrentHP;
            int maxHP = damageable.MaxHP;

            _fillTween?.Kill();
            _fillTween = _fillImage.DOFillAmount(targetFill, 0.25f)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject)
                .SetUpdate(true);

            _trailTween?.Kill();
            _trailTween = _damageTrailImage.DOFillAmount(targetFill, 0.35f)
                .SetDelay(0.3f)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject)
                .SetUpdate(true);

            _textTween?.Kill();
            _textTween = DOTween.To(() => _currentDisplayedHP, x =>
            {
                _currentDisplayedHP = x;
                _hpText.text = $"{_currentDisplayedHP} / {maxHP}";
            }, targetHP, 0.25f)
                .SetLink(gameObject)
                .SetUpdate(true);

            _punchTween?.Complete();
            _barRoot.localScale = Vector3.one;
            _punchTween = _barRoot.DOPunchScale(_punchStrength, 0.15f, 10, 1f)
                .SetLink(gameObject)
                .SetUpdate(true);

            _flashTween?.Kill();
            _fillImage.color = _originalFillColor;
            _flashTween = _fillImage.DOColor(_damageFlashColor, 0.08f)
                .SetLoops(2, LoopType.Yoyo)
                .SetLink(gameObject)
                .SetUpdate(true);
        }

        private void KillActiveTweens()
        {
            if (_fillTween.IsActive()) _fillTween.Kill();
            if (_trailTween.IsActive()) _trailTween.Kill();
            if (_textTween.IsActive()) _textTween.Kill();
            if (_punchTween.IsActive()) _punchTween.Kill();
            if (_flashTween.IsActive()) _flashTween.Kill();
        }
    }
}
