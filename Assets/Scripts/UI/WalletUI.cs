using UnityEngine;
using TMPro;
using DG.Tweening;
using Alchemy.Inspector;
using Game.Core;

namespace Game.UI
{
    public class WalletUI : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private PlayerWallet _wallet;
        [SerializeField] private TextMeshProUGUI _coinsText;
        [SerializeField] private TextMeshProUGUI _diamondsText;

        [Title("Punch Animation")]
        [SerializeField] private Vector3 _punchStrength = new(0.1f, 0.1f, 0f);

        private Tweener _coinsTween;
        private Tweener _diamondsTween;

        private void OnEnable()
        {
            _wallet.CoinsChanged += OnCoinsChanged;
            _wallet.DiamondsChanged += OnDiamondsChanged;
            UpdateCoinsInstant(_wallet.Coins);
            UpdateDiamondsInstant(_wallet.Diamonds);
        }

        private void OnDisable()
        {
            _wallet.CoinsChanged -= OnCoinsChanged;
            _wallet.DiamondsChanged -= OnDiamondsChanged;
            KillTweens();
        }

        private void OnCoinsChanged(int newCoins)
        {
            _coinsText.text = newCoins.ToString();

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
            _coinsText.text = coins.ToString();
        }

        private void UpdateDiamondsInstant(int diamonds)
        {
            _diamondsText.text = diamonds.ToString();
        }

        private void KillTweens()
        {
            if (_coinsTween.IsActive()) _coinsTween.Kill();
            if (_diamondsTween.IsActive()) _diamondsTween.Kill();
        }
    }
}
