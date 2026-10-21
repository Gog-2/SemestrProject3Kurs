using System;
using UnityEngine;
using Alchemy.Inspector;

namespace Game.Core
{
    public class PlayerWallet : MonoBehaviour
    {
        [Title("Starting Currency")]
        [SerializeField, Min(0)] private int _initialCoins = 100;
        [SerializeField, Min(0)] private int _initialDiamonds = 0;

        [Title("Debug")]
        [ShowInInspector, ReadOnly]
        public int Coins => _coins;

        [ShowInInspector, ReadOnly]
        public int Diamonds => _diamonds;

        private int _coins;
        private int _diamonds;

        public event Action<int> CoinsChanged;
        public event Action<int> DiamondsChanged;

        private void Awake()
        {
            _coins = _initialCoins;
            _diamonds = _initialDiamonds;
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
