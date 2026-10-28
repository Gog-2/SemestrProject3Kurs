using UnityEngine;
using Alchemy.Inspector;
using DG.Tweening;
using Game.Core;

namespace Game.Buildings
{
    public class EconomyBuilding : Building
    {
        [Title("Economy Output")]
        [SerializeField] private bool _producesDiamonds = false;
        [SerializeField] private Transform _coinSpawnPoint;

        private float _productionTimer;

        protected override void OnPlaced()
        {
            base.OnPlaced();
            ResetTimer();
        }

        private void Update()
        {
            if (IsDead) return;

            if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            {
                return;
            }

            _productionTimer -= Time.deltaTime;
            if (_productionTimer <= 0f)
            {
                ProduceCurrency();
                ResetTimer();
            }
        }

        private void ProduceCurrency()
        {
            int amount = CurrentStats.productionAmount > 0 ? CurrentStats.productionAmount : 1;

            if (_producesDiamonds)
            {
                PlayerWallet.Instance?.AddDiamonds(amount);
                RunStatistics.Instance?.AddDiamondsEarned(amount);
            }
            else
            {
                PlayerWallet.Instance?.AddCoins(amount);
                RunStatistics.Instance?.AddCoinsEarned(amount);
            }

            // Punch animation on production
            if (_modelTransform != null)
            {
                _modelTransform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 6, 1f)
                    .SetLink(gameObject)
                    .SetUpdate(true);
            }
        }

        private void ResetTimer()
        {
            float baseInterval = CurrentStats.productionInterval > 0 ? CurrentStats.productionInterval : 10f;

            // Economic bonus from progression cards
            if (RunProgressionManager.Instance != null && !_producesDiamonds)
            {
                baseInterval = Mathf.Max(1f, baseInterval * (1f - (RunProgressionManager.Instance.ExtraDiamondDropChance > 0 ? 0f : 0f)));
            }

            _productionTimer = baseInterval;
        }
    }
}
