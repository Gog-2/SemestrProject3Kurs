using UnityEngine;
using Alchemy.Inspector;
using DG.Tweening;
using Game.Core;
using Game.Upgrades;
using Game.WaveSystem;

namespace Game.Buildings
{
    public class EconomyBuilding : Building
    {
        [Title("Economy Output")]
        [SerializeField] private bool _producesDiamonds = false;
        [SerializeField] private Transform _coinSpawnPoint;

        private float _productionTimer;
        private float _appliedSpeedMultiplier = 1f;

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToUpgrades();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnsubscribeFromUpgrades();
        }

        protected override void OnPlaced()
        {
            base.OnPlaced();
            SubscribeToUpgrades();
            ResetTimer();
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            UnsubscribeFromUpgrades();
        }

        private void SubscribeToUpgrades()
        {
            if (RunProgressionManager.Instance == null) return;
            RunProgressionManager.Instance.UpgradeApplied -= OnUpgradeApplied;
            RunProgressionManager.Instance.UpgradeApplied += OnUpgradeApplied;
        }

        private void UnsubscribeFromUpgrades()
        {
            if (RunProgressionManager.Instance != null)
            {
                RunProgressionManager.Instance.UpgradeApplied -= OnUpgradeApplied;
            }
        }

        private void OnUpgradeApplied(UpgradeData upgrade)
        {
            if (upgrade != null && upgrade.EffectType != UpgradeEffectType.EconomicSpeedPercent) return;

            float newMultiplier = GetCurrentSpeedMultiplier();
            if (!Mathf.Approximately(newMultiplier, _appliedSpeedMultiplier) && newMultiplier > 0f)
            {
                _productionTimer *= _appliedSpeedMultiplier / newMultiplier;
                _appliedSpeedMultiplier = newMultiplier;
            }
        }

        private void Update()
        {
            if (IsDead) return;

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            if (WaveController.Instance != null)
            {
                WaveState waveState = WaveController.Instance.CurrentState;
                if (waveState != WaveState.WaveActive && waveState != WaveState.WaveClearing)
                {
                    return;
                }
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
            int amount = CurrentStats.productionAmount;

            if (_producesDiamonds)
            {
                PlayerWallet.Instance?.AddDiamonds(amount);
                RunStatistics.Instance?.AddDiamondsEarned(amount);
                AddCombatXP(amount * 50);
            }
            else
            {
                PlayerWallet.Instance?.AddCoins(amount);
                RunStatistics.Instance?.AddCoinsEarned(amount);
                RunProgressionManager.Instance?.AddCombatGoldXP(amount);
                AddCombatXP(amount * 10);
            }

            // Punch animation on production
            _modelTransform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 6, 1f)
                .SetLink(gameObject)
                .SetUpdate(true);
        }

        private float GetCurrentSpeedMultiplier()
        {
            return RunProgressionManager.Instance != null
                ? Mathf.Max(0.1f, RunProgressionManager.Instance.GetEffectiveEconomySpeedMultiplier())
                : 1f;
        }

        private void ResetTimer()
        {
            _appliedSpeedMultiplier = GetCurrentSpeedMultiplier();
            float baseInterval = CurrentStats.productionInterval;
            _productionTimer = baseInterval / _appliedSpeedMultiplier;
        }
    }
}
