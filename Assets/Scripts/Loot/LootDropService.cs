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
            LootTable loot = enemy.Data.LootTable;
            Vector3 deathPos = enemy.transform.position;

            if (UnityEngine.Random.value <= loot.CoinDropChance)
            {
                int coinsToDrop = UnityEngine.Random.Range(loot.MinCoins, loot.MaxCoins + 1);
                if (coinsToDrop > 0)
                {
                    LootPickup coin = _coinPool.Get();
                    coin.transform.position = deathPos;
                    coin.Initialize(LootType.Coin, coinsToDrop, OnPickupCollected);
                }
            }

            if (UnityEngine.Random.value <= loot.DiamondDropChance)
            {
                LootPickup diamond = _diamondPool.Get();
                diamond.transform.position = deathPos;
                diamond.Initialize(LootType.Diamond, loot.DiamondAmount, OnPickupCollected);
            }
        }

        private void OnPickupCollected(LootPickup pickup)
        {
            if (pickup.Type == LootType.Coin)
            {
                _wallet.AddCoins(pickup.Amount);
                _coinPool.Release(pickup);
            }
            else if (pickup.Type == LootType.Diamond)
            {
                _wallet.AddDiamonds(pickup.Amount);
                _diamondPool.Release(pickup);
            }
        }
    }
}
