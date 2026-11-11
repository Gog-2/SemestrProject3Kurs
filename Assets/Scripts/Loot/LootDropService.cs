using System;
using UnityEngine;
using UnityEngine.Pool;
using Alchemy.Inspector;
using Game.Core;
using Game.Enemies;
using Game.Enemies.Data;
using Game.Enemies.Spawning;

namespace Game.Loot
{
    public class LootDropService : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private EnemySpawner _spawner;
        [SerializeField] private PlayerWallet _wallet;

        [Title("Prefabs")]
        [SerializeField] private LootPickup _coinPrefab;
        [SerializeField] private LootPickup _diamondPrefab;

        private IObjectPool<LootPickup> _coinPool;
        private IObjectPool<LootPickup> _diamondPool;

        private void Awake()
        {
            _coinPool = new ObjectPool<LootPickup>(
                createFunc: () => Instantiate(_coinPrefab, transform),
                actionOnGet: p => p.OnSpawnFromPool(),
                actionOnRelease: p => p.OnReturnToPool(),
                actionOnDestroy: p => Destroy(p.gameObject),
                defaultCapacity: 20,
                maxSize: 60
            );

            _diamondPool = new ObjectPool<LootPickup>(
                createFunc: () => Instantiate(_diamondPrefab, transform),
                actionOnGet: p => p.OnSpawnFromPool(),
                actionOnRelease: p => p.OnReturnToPool(),
                actionOnDestroy: p => Destroy(p.gameObject),
                defaultCapacity: 10,
                maxSize: 30
            );
        }

        private void OnEnable()
        {
            _spawner.EnemyDied += OnEnemyDied;
        }

        private void OnDisable()
        {
            _spawner.EnemyDied -= OnEnemyDied;
        }

        private void OnEnemyDied(Enemy enemy)
        {
            if (enemy == null || enemy.Data == null) return;

            LootTable loot = enemy.Data.LootTable;
            if (loot == null) return;

            Vector3 deathPos = enemy.transform.position;

            if (UnityEngine.Random.value <= loot.CoinDropChance)
            {
                int rawCoins = UnityEngine.Random.Range(loot.MinCoins, loot.MaxCoins + 1);
                int coinsToDrop = ScaleCoinsByDifficulty(rawCoins);
                if (coinsToDrop > 0)
                {
                    LootPickup coin = _coinPool.Get();
                    coin.transform.position = deathPos;
                    coin.Initialize(LootType.Coin, coinsToDrop, OnPickupCollected);
                }
            }

            float extraDiamondChance = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveDiamondDropChanceBonus()
                : 0f;
            float totalDiamondChance = Mathf.Clamp01(loot.DiamondDropChance + extraDiamondChance);

            if (UnityEngine.Random.value <= totalDiamondChance)
            {
                int minDiamonds = Mathf.Max(1, loot.MinDiamonds);
                int maxDiamonds = Mathf.Max(minDiamonds, loot.MaxDiamonds);
                int diamondAmount = UnityEngine.Random.Range(minDiamonds, maxDiamonds + 1);

                LootPickup diamond = _diamondPool.Get();
                diamond.transform.position = deathPos;
                diamond.Initialize(LootType.Diamond, diamondAmount, OnPickupCollected);
            }
        }

        private static int ScaleCoinsByDifficulty(int rawCoins)
        {
            if (rawCoins <= 0) return 0;

            float mult = DifficultyModifiers.GetCoinDropMultiplier(RunConfig.ActiveDifficulty);
            float scaled = rawCoins * mult;
            int whole = Mathf.FloorToInt(scaled);
            float fraction = scaled - whole;
            if (fraction > 0f && UnityEngine.Random.value < fraction)
            {
                whole++;
            }

            return whole;
        }

        private void OnPickupCollected(LootPickup pickup)
        {
            if (pickup == null) return;

            if (pickup.Type == LootType.Coin)
            {
                int bonus = RunProgressionManager.Instance != null
                    ? RunProgressionManager.Instance.GetEffectiveCoinPerKillBonus()
                    : 0;
                int totalCoins = pickup.Amount + bonus;
                if (_wallet != null) _wallet.AddCoins(totalCoins);
                else PlayerWallet.Instance?.AddCoins(totalCoins);
                RunStatistics.Instance?.AddCoinsEarned(totalCoins);
                RunProgressionManager.Instance?.AddCombatGoldXP(totalCoins);
                _coinPool.Release(pickup);
            }
            else if (pickup.Type == LootType.Diamond)
            {
                if (_wallet != null) _wallet.AddDiamonds(pickup.Amount);
                else PlayerWallet.Instance?.AddDiamonds(pickup.Amount);
                RunStatistics.Instance?.AddDiamondsEarned(pickup.Amount);
                _diamondPool.Release(pickup);
            }
        }
    }
}
