using System;
using System.Collections.Generic;
using UnityEngine;
using Alchemy.Inspector;
using Game.Core;
using Game.Buildings;
using Game.Pathfinding;
using Game.Enemies.Data;

namespace Game.Enemies.Spawning
{
    public class EnemySpawner : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private EnemyPool _enemyPool;
        [SerializeField] private GridMap _gridMap;
        [SerializeField] private Castle _castle;
        [SerializeField] private GridFlowfield _flowfield;

        [Title("Spawn Bounds Settings")]
        [SerializeField, Min(0.5f)] private float _spawnOffsetOutsideMap = 1.5f;

        [Title("Debug")]
        [SerializeField, ReadOnly] private int _activeEnemyCount = 0;

        public int ActiveEnemyCount => _activeEnemies.Count;

        private readonly List<Enemy> _activeEnemies = new(32);

        public event Action<Enemy> EnemySpawned;
        public event Action<Enemy> EnemyDied;
        public event Action<int> ActiveEnemyCountChanged;

        public Enemy SpawnEnemy(EnemyData data)
        {
            Vector3 spawnPosition = GetRandomPerimeterSpawnPosition();
            Enemy enemy = _enemyPool.Get();
            enemy.transform.position = spawnPosition;
            enemy.Initialize(data, _flowfield, _castle, _gridMap);

            _activeEnemies.Add(enemy);
            _activeEnemyCount = _activeEnemies.Count;
            enemy.Died += OnEnemyDiedHandler;
            enemy.Retreated += OnEnemyRetreatedHandler;

            EnemySpawned?.Invoke(enemy);
            ActiveEnemyCountChanged?.Invoke(_activeEnemyCount);
            return enemy;
        }

        public void SpawnBatch(EnemyData data, int count)
        {
            if (data == null || count <= 0) return;
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy(data);
            }
        }

        private void OnEnemyDiedHandler(Enemy enemy)
        {
            enemy.Died -= OnEnemyDiedHandler;
            enemy.Retreated -= OnEnemyRetreatedHandler;
            _activeEnemies.Remove(enemy);
            _activeEnemyCount = _activeEnemies.Count;
            _flowfield?.RecordDeathHeat(enemy.transform.position);
            EnemyDied?.Invoke(enemy);
            ActiveEnemyCountChanged?.Invoke(_activeEnemyCount);
            _enemyPool.Release(enemy);
        }

        private void OnEnemyRetreatedHandler(Enemy enemy)
        {
            enemy.Died -= OnEnemyDiedHandler;
            enemy.Retreated -= OnEnemyRetreatedHandler;
            _activeEnemies.Remove(enemy);
            _activeEnemyCount = _activeEnemies.Count;
            ActiveEnemyCountChanged?.Invoke(_activeEnemyCount);
            _enemyPool.Release(enemy);
        }

        public void RetreatAllActiveEnemies()
        {
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = _activeEnemies[i];
                if (enemy != null && !enemy.IsDead)
                {
                    enemy.BeginRetreat();
                }
            }
        }

        public void ClearAllEnemies()
        {
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = _activeEnemies[i];
                enemy.Died -= OnEnemyDiedHandler;
                enemy.Retreated -= OnEnemyRetreatedHandler;
                _enemyPool.Release(enemy);
            }
            _activeEnemies.Clear();
            _activeEnemyCount = 0;
            ActiveEnemyCountChanged?.Invoke(0);
        }

        private Vector3 GetRandomPerimeterSpawnPosition()
        {
            Bounds b = _gridMap.Bounds;
            float minX = b.min.x - _spawnOffsetOutsideMap;
            float maxX = b.max.x + _spawnOffsetOutsideMap;
            float minY = b.min.y - _spawnOffsetOutsideMap;
            float maxY = b.max.y + _spawnOffsetOutsideMap;

            int side = UnityEngine.Random.Range(0, 4);
            return side switch
            {
                0 => new Vector3(UnityEngine.Random.Range(minX, maxX), minY, 0f),
                1 => new Vector3(UnityEngine.Random.Range(minX, maxX), maxY, 0f),
                2 => new Vector3(minX, UnityEngine.Random.Range(minY, maxY), 0f),
                _ => new Vector3(maxX, UnityEngine.Random.Range(minY, maxY), 0f)
            };
        }
    }
}
