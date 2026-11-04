using System;
using System.Collections.Generic;
using UnityEngine;
using Alchemy.Inspector;
using Game.Enemies.Data;

namespace Game.WaveSystem
{
    [Serializable]
    public class EnemySpawnGroup
    {
        [SerializeField] private EnemyData _enemyData;
        [SerializeField, Min(1)] private int _count = 10;
        [SerializeField, Min(0f)] private float _startDelay = 0f;
        [SerializeField, Min(0.1f)] private float _spawnInterval = 2f;
        [SerializeField, Min(1)] private int _spawnBatchSize = 1;

        public EnemyData EnemyData => _enemyData;
        public int Count => _count;
        public float StartDelay => _startDelay;
        public float SpawnInterval => _spawnInterval;
        public int SpawnBatchSize => _spawnBatchSize;

        public EnemySpawnGroup() { }

        public EnemySpawnGroup(EnemyData data, int count, float startDelay, float spawnInterval, int batchSize = 1)
        {
            _enemyData = data;
            _count = count;
            _startDelay = startDelay;
            _spawnInterval = spawnInterval;
            _spawnBatchSize = batchSize;
        }
    }

    [CreateAssetMenu(fileName = "NewWaveConfig", menuName = "Game/Wave Config", order = 20)]
    public class WaveConfig : ScriptableObject
    {
        [Title("Wave Info")]
        [SerializeField, Min(1)] private int _waveNumber = 1;
        [Tooltip("Hard timer duration of the wave in seconds.")]
        [SerializeField, Min(5f)] private float _durationSeconds = 60f;
        [Tooltip("Duration of building phase before this wave in seconds.")]
        [SerializeField, Min(5f)] private float _buildingPhaseDuration = 20f;
        [SerializeField] private bool _isBossWave = false;
        [SerializeField] private bool _isFinalWave = false;

        [Title("Spawn Groups")]
        [SerializeField] private List<EnemySpawnGroup> _spawnGroups = new();

        [Title("Legacy Enemy Spawning (Fallback if Groups Empty)")]
        [SerializeField] private EnemyData _enemyData;
        [SerializeField, Min(1)] private int _totalEnemiesToSpawn = 20;
        [SerializeField, Min(0.2f)] private float _initialSpawnDelay = 1f;
        [SerializeField, Min(0.2f)] private float _spawnInterval = 2.5f;

        public int WaveNumber => _waveNumber;
        public float DurationSeconds => _durationSeconds;
        public float BuildingPhaseDuration => _buildingPhaseDuration;
        public bool IsBossWave => _isBossWave;
        public bool IsFinalWave => _isFinalWave;
        public EnemyData EnemyData => _enemyData;
        public int TotalEnemiesToSpawn => GetTotalEnemiesCount();
        public float InitialSpawnDelay => _initialSpawnDelay;
        public float SpawnInterval => _spawnInterval;
        public IReadOnlyList<EnemySpawnGroup> SpawnGroups => _spawnGroups;

        public IReadOnlyList<EnemySpawnGroup> GetActiveSpawnGroups()
        {
            if (_spawnGroups != null && _spawnGroups.Count > 0)
            {
                return _spawnGroups;
            }

            if (_enemyData != null)
            {
                return new List<EnemySpawnGroup>
                {
                    new(_enemyData, _totalEnemiesToSpawn, _initialSpawnDelay, _spawnInterval, 1)
                };
            }

            return Array.Empty<EnemySpawnGroup>();
        }

        public int GetTotalEnemiesCount()
        {
            if (_spawnGroups != null && _spawnGroups.Count > 0)
            {
                int sum = 0;
                for (int i = 0; i < _spawnGroups.Count; i++)
                {
                    if (_spawnGroups[i] != null) sum += _spawnGroups[i].Count;
                }
                return sum;
            }

            return _totalEnemiesToSpawn;
        }

        private void OnValidate()
        {
            if (_waveNumber < 1) _waveNumber = 1;
            if (_durationSeconds < 5f) _durationSeconds = 5f;
            if (_buildingPhaseDuration < 5f) _buildingPhaseDuration = 5f;
            if (_totalEnemiesToSpawn < 1) _totalEnemiesToSpawn = 1;
            if (_initialSpawnDelay < 0.2f) _initialSpawnDelay = 0.2f;
            if (_spawnInterval < 0.2f) _spawnInterval = 0.2f;
        }
    }
}
