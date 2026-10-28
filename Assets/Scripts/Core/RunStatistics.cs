using System;
using UnityEngine;
using Alchemy.Inspector;
using Game.Enemies;
using Game.WaveSystem;

namespace Game.Core
{
    public class RunStatistics : MonoBehaviour
    {
        public static RunStatistics Instance { get; private set; }

        [Title("Statistics")]
        [ShowInInspector, ReadOnly] private int _totalKills = 0;
        [ShowInInspector, ReadOnly] private float _totalTime = 0f;
        [ShowInInspector, ReadOnly] private int _wavesCompleted = 0;
        [ShowInInspector, ReadOnly] private int _coinsEarned = 0;
        [ShowInInspector, ReadOnly] private int _diamondsEarned = 0;
        [ShowInInspector, ReadOnly] private bool _isVictory = false;

        public int TotalKills => _totalKills;
        public float TotalTime => _totalTime;
        public int WavesCompleted => _wavesCompleted;
        public int CoinsEarned => _coinsEarned;
        public int DiamondsEarned => _diamondsEarned;
        public bool IsVictory => _isVictory;

        public string FormattedTime
        {
            get
            {
                int minutes = Mathf.FloorToInt(_totalTime / 60f);
                int seconds = Mathf.FloorToInt(_totalTime % 60f);
                return $"{minutes:00}:{seconds:00}";
            }
        }

        public event Action StatisticsUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            Enemy.GlobalEnemyDied += OnEnemyDied;
        }

        private void OnDisable()
        {
            Enemy.GlobalEnemyDied -= OnEnemyDied;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing)
            {
                _totalTime += Time.deltaTime;
            }
        }

        private void OnEnemyDied(Enemy enemy)
        {
            _totalKills++;
            StatisticsUpdated?.Invoke();
        }

        public void RecordWaveCleared(int waveNumber)
        {
            _wavesCompleted = Mathf.Max(_wavesCompleted, waveNumber);
            StatisticsUpdated?.Invoke();
        }

        public void AddCoinsEarned(int amount)
        {
            if (amount > 0)
            {
                _coinsEarned += amount;
                StatisticsUpdated?.Invoke();
            }
        }

        public void AddDiamondsEarned(int amount)
        {
            if (amount > 0)
            {
                _diamondsEarned += amount;
                StatisticsUpdated?.Invoke();
            }
        }

        public void SetOutcome(bool isVictory)
        {
            _isVictory = isVictory;
            StatisticsUpdated?.Invoke();
        }
    }
}
