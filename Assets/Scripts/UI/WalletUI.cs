using UnityEngine;
using TMPro;
using DG.Tweening;
using Alchemy.Inspector;
using Game.Core;

namespace Game.UI
{
    public class WalletUI : MonoBehaviour
    {
        public static WalletUI Instance { get; private set; }

        [Title("References")]
        [SerializeField] private PlayerWallet _wallet;
        [SerializeField] private TextMeshProUGUI _coinsText;
        [SerializeField] private TextMeshProUGUI _diamondsText;

        [Title("Punch & Smooth Roll Animation")]
        [SerializeField] private Vector3 _punchStrength = new(0.1f, 0.1f, 0f);
        [SerializeField] private float _countRollDuration = 0.25f;

        private Tweener _coinsTween;
        private Tweener _coinsRollTween;
        private Tweener _diamondsTween;
        private int _displayedCoins;
        private int _previewIncomeCoins;

        private void Awake()
        {
            if (Instance == null || Instance == this)
            {
                Instance = this;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnable()
        {
            if (Instance == null) Instance = this;

            if (_wallet == null)
            {
                _wallet = PlayerWallet.Instance ?? FindAnyObjectByType<PlayerWallet>();
            }

            if (_wallet != null)
            {
                BindWallet(_wallet);
            }
        }

        private void Start()
        {
            if (_wallet == null)
            {
                _wallet = PlayerWallet.Instance ?? FindAnyObjectByType<PlayerWallet>();
            }

            if (_wallet != null)
            {
                BindWallet(_wallet);
            }
        }

        public void BindWallet(PlayerWallet wallet)
        {
            if (_wallet != null && _wallet != wallet)
            {
                _wallet.CoinsChanged -= OnCoinsChanged;
                _wallet.DiamondsChanged -= OnDiamondsChanged;
            }

            _wallet = wallet;

            if (_wallet != null)
            {
                _wallet.CoinsChanged -= OnCoinsChanged;
                _wallet.CoinsChanged += OnCoinsChanged;
                _wallet.DiamondsChanged -= OnDiamondsChanged;
                _wallet.DiamondsChanged += OnDiamondsChanged;
                UpdateCoinsInstant(_wallet.Coins);
                UpdateDiamondsInstant(_wallet.Diamonds);
            }
        }

        private void OnDisable()
        {
            if (_wallet != null)
            {
                _wallet.CoinsChanged -= OnCoinsChanged;
                _wallet.DiamondsChanged -= OnDiamondsChanged;
            }
            KillTweens();
        }

        public void ShowSellPreview(int refundAmount)
        {
            _previewIncomeCoins = Mathf.Max(0, refundAmount);
            RenderCoinsText();
        }

        public void HideSellPreview()
        {
            if (_previewIncomeCoins == 0) return;
            _previewIncomeCoins = 0;
            RenderCoinsText();
        }

        private void OnCoinsChanged(int newCoins)
        {
            _coinsRollTween?.Kill();
            int startVal = _displayedCoins;

            if (startVal == newCoins)
            {
                RenderCoinsText();
            }
            else
            {
                _coinsRollTween = DOTween.To(() => _displayedCoins, v =>
                    {
                        _displayedCoins = v;
                        RenderCoinsText();
                    }, newCoins, _countRollDuration)
                    .SetEase(Ease.OutQuad)
                    .SetLink(gameObject)
                    .SetUpdate(true);
            }

            _coinsTween?.Kill();
            _coinsText.transform.localScale = Vector3.one;
            _coinsTween = _coinsText.transform.DOPunchScale(_punchStrength, 0.15f, 6, 1f)
                .SetLink(gameObject)
                .SetUpdate(true);
        }

        private void OnDiamondsChanged(int newDiamonds)
        {
            _diamondsText.text = newDiamonds.ToString();

            _diamondsTween?.Kill();
            _diamondsText.transform.localScale = Vector3.one;
            _diamondsTween = _diamondsText.transform.DOPunchScale(_punchStrength, 0.15f, 6, 1f)
                .SetLink(gameObject)
                .SetUpdate(true);
        }

        private void UpdateCoinsInstant(int coins)
        {
            _coinsRollTween?.Kill();
            _displayedCoins = coins;
            RenderCoinsText();
        }

        private void RenderCoinsText()
        {
            if (_coinsText == null) return;
            if (_previewIncomeCoins > 0)
            {
                _coinsText.text = $"{_displayedCoins} <color=#55FF66>+{_previewIncomeCoins}</color>";
            }
            else
            {
                _coinsText.text = _displayedCoins.ToString();
            }
        }

        private void UpdateDiamondsInstant(int diamonds)
        {
            _diamondsText.text = diamonds.ToString();
        }

        private void KillTweens()
        {
            if (_coinsTween.IsActive()) _coinsTween.Kill();
            if (_coinsRollTween.IsActive()) _coinsRollTween.Kill();
            if (_diamondsTween.IsActive()) _diamondsTween.Kill();
        }
    }
}
