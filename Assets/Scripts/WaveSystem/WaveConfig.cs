using UnityEngine;
using Alchemy.Inspector;
using Game.Enemies.Data;

namespace Game.WaveSystem
{
    [CreateAssetMenu(fileName = "NewWaveConfig", menuName = "Game/Wave Config", order = 20)]
    public class WaveConfig : ScriptableObject
    {
        [Title("Wave Info")]
        [SerializeField, Min(1)] private int _waveNumber = 1;
        [Tooltip("Hard timer duration of the wave in seconds.")]
        [SerializeField, Min(5f)] private float _durationSeconds = 60f;

        [Title("Enemy Spawning")]
        [SerializeField] private EnemyData _enemyData;
        [SerializeField, Min(1)] private int _totalEnemiesToSpawn = 20;
        [SerializeField, Min(0.2f)] private float _initialSpawnDelay = 1f;
        [SerializeField, Min(0.2f)] private float _spawnInterval = 2.5f;

        public int WaveNumber => _waveNumber;
        public float DurationSeconds => _durationSeconds;
        public EnemyData EnemyData => _enemyData;
        public int TotalEnemiesToSpawn => _totalEnemiesToSpawn;
        public float InitialSpawnDelay => _initialSpawnDelay;
        public float SpawnInterval => _spawnInterval;

        private void OnValidate()
        {
            if (_waveNumber < 1) _waveNumber = 1;
            if (_durationSeconds < 5f) _durationSeconds = 5f;
            if (_totalEnemiesToSpawn < 1) _totalEnemiesToSpawn = 1;
            if (_initialSpawnDelay < 0.2f) _initialSpawnDelay = 0.2f;
            if (_spawnInterval < 0.2f) _spawnInterval = 0.2f;
        }
    }
}
