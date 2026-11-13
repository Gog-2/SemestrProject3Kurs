using System;
using UnityEngine;
using Alchemy.Inspector;

namespace Game.Core
{
    [DefaultExecutionOrder(-50)]
    public class PlayerWallet : MonoBehaviour
    {
        [Title("Starting Currency")]
        [SerializeField, Min(0)] private int _initialCoins = 180;
        [SerializeField, Min(0)] private int _initialDiamonds = 0;

        [Title("Debug")]
        [ShowInInspector, ReadOnly]
        public int Coins => _coins;

        [ShowInInspector, ReadOnly]
        public int Diamonds => _diamonds;

        private int _coins;
        private int _diamonds;

        public static PlayerWallet Instance { get; private set; }
        public bool IsInitialized { get; private set; }

        public event Action<int> CoinsChanged;
        public event Action<int> DiamondsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            int metaStartingCoins = RunConfig.Instance != null
                ? Mathf.RoundToInt(MetaBonusCatalog.GetTotalBonus(MetaBonusType.StartingCoinsFlat))
                : 0;
            _coins = _initialCoins + metaStartingCoins;
            _diamonds = _initialDiamonds;
            IsInitialized = true;
            ForceRefresh();
        }

        private void Start()
        {
            ForceRefresh();
        }

        public void ForceRefresh()
        {
            CoinsChanged?.Invoke(_coins);
            DiamondsChanged?.Invoke(_diamonds);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;

            _coins += amount;
            CoinsChanged?.Invoke(_coins);
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0) return true;
            if (_coins < amount) return false;

            _coins -= amount;
            CoinsChanged?.Invoke(_coins);
            return true;
        }

        public void AddDiamonds(int amount)
        {
            if (amount <= 0) return;

            _diamonds += amount;
            DiamondsChanged?.Invoke(_diamonds);
        }

        public bool TrySpendDiamonds(int amount)
        {
            if (amount <= 0) return true;
            if (_diamonds < amount) return false;

            _diamonds -= amount;
            DiamondsChanged?.Invoke(_diamonds);
            return true;
        }
    }
}
