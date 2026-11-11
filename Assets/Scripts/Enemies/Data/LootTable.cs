using System;
using UnityEngine;
using UnityEngine.Serialization;
using Alchemy.Inspector;

namespace Game.Enemies.Data
{
    [Serializable]
    public class LootTable
    {
        [Title("Coins Drop")]
        [SerializeField, Min(0)] private int _minCoins = 1;
        [SerializeField, Min(0)] private int _maxCoins = 3;
        [SerializeField, Range(0f, 1f)] private float _coinDropChance = 1f;

        [Title("Diamonds Drop")]
        [SerializeField, Range(0f, 1f)] private float _diamondDropChance = 0.01f;
        [SerializeField, FormerlySerializedAs("_diamondAmount"), Min(1)] private int _minDiamondAmount = 1;
        [SerializeField, Min(1)] private int _maxDiamondAmount = 1;

        public int MinCoins => _minCoins;
        public int MaxCoins => _maxCoins;
        public float CoinDropChance => _coinDropChance;
        public float DiamondDropChance => _diamondDropChance;
        public int MinDiamonds => _minDiamondAmount;
        public int MaxDiamonds => Mathf.Max(_minDiamondAmount, _maxDiamondAmount);
        public int DiamondAmount => MinDiamonds;
    }
}
