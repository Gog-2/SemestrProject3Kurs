using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Alchemy.Inspector;
using Game.Buildings;
using Game.Core;
using Game.Enemies.Spawning;

namespace Game.WaveSystem
{
    public enum WaveState
    {
        NotStarted,
        BuildingPhase,
        WaveActive,
        WaveClearing,
        WaveCompleted,
        GameOver
    }

    public class WaveController : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private WaveConfig _initialWaveConfig;
        [SerializeField] private EnemySpawner _spawner;
        [SerializeField] private Castle _castle;

        [Title("Settings")]
        [SerializeField] private bool _autoStartFirstWave = true;
        [SerializeField, Min(0f)] private float _delayBeforeFirstWave = 1f;

        [Title("Debug")]
        [SerializeField, ReadOnly] private WaveState _currentState = WaveState.NotStarted;
        [SerializeField, ReadOnly] private int _currentWaveNumber = 1;

        public WaveState CurrentState => _currentState;
        public int CurrentWaveNumber => _currentWaveNumber;
        public IWaveTimer Timer => _waveTimer;

        private readonly WaveTimer _waveTimer = new();
        private CancellationTokenSource _waveCts;

        public event Action OnWaveStarted;
        public event Action OnWaveCleared;
        public event Action OnBuildingPhaseStarted;
        public event Action OnBuildingPhaseEnded;
        public event Action OnGameOver;
        public event Action<WaveState> StateChanged;

        private void Start()
        {
            _castle.Destroyed += OnCastleDestroyed;

            if (_initialWaveConfig != null)
            {
                _currentWaveNumber = _initialWaveConfig.WaveNumber;
            }

            if (_autoStartFirstWave && _initialWaveConfig != null)
            {
                StartWaveWithDelayAsync(_delayBeforeFirstWave).Forget();
            }
            else
            {
                SetState(WaveState.BuildingPhase);
                OnBuildingPhaseStarted?.Invoke();
            }
        }

        private void OnDestroy()
        {
            CancelCurrentWave();
            _castle.Destroyed -= OnCastleDestroyed;
        }

        [Button]
        public void StartWave()
        {
            if (_currentState == WaveState.GameOver)
            {
                _castle.HealFull();
                _spawner.ClearAllEnemies();
                SetState(WaveState.BuildingPhase);
            }
            else if (_currentState == WaveState.WaveActive)
            {
                return;
            }

            StartWave(_initialWaveConfig);
        }

        public void StartWave(WaveConfig config)
        {
            if (config == null || _currentState == WaveState.WaveActive)
            {
                return;
            }

            _currentWaveNumber = config.WaveNumber;
            CancelCurrentWave();
            _waveCts = new CancellationTokenSource();

            RunWaveLifecycleAsync(config, _waveCts.Token).Forget();
        }

        private async UniTaskVoid StartWaveWithDelayAsync(float delay)
        {
            try
            {
                if (delay > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: destroyCancellationToken);
                }

                if (_currentState != WaveState.GameOver && _initialWaveConfig != null)
                {
                    StartWave(_initialWaveConfig);
                }
            }
            catch (OperationCanceledException)
            {
                // Clean cancellation on destroy
            }
        }

        private async UniTaskVoid RunWaveLifecycleAsync(WaveConfig config, CancellationToken ct)
        {
            try
            {
                if (_currentState == WaveState.BuildingPhase)
                {
                    OnBuildingPhaseEnded?.Invoke();
                }

                SetState(WaveState.WaveActive);
                OnWaveStarted?.Invoke();

                UniTask timerTask = _waveTimer.RunAsync(config.DurationSeconds, ct);
                UniTask spawnTask = RunEnemySpawningAsync(config, ct);

                await UniTask.WhenAll(timerTask, spawnTask);

                if (ct.IsCancellationRequested || _currentState == WaveState.GameOver)
                {
                    return;
                }

                SetState(WaveState.WaveClearing);

                while (_spawner.ActiveEnemyCount > 0)
                {
                    await UniTask.Delay(250, cancellationToken: ct);
                    if (_currentState == WaveState.GameOver) return;
                }

                if (ct.IsCancellationRequested || _currentState == WaveState.GameOver)
                {
                    return;
                }

                SetState(WaveState.WaveCompleted);
                OnWaveCleared?.Invoke();

                if (RunProgressionManager.Instance != null)
                {
                    RunProgressionManager.Instance.RequestUpgradeSelection();
                }

                SetState(WaveState.BuildingPhase);
                OnBuildingPhaseStarted?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // Clean cancellation on wave cancel or game over
            }
        }

        private async UniTask RunEnemySpawningAsync(WaveConfig config, CancellationToken ct)
        {
            if (config.EnemyData == null)
            {
                return;
            }

            if (config.InitialSpawnDelay > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(config.InitialSpawnDelay), cancellationToken: ct);
            }

            int spawnedCount = 0;
            while (spawnedCount < config.TotalEnemiesToSpawn && _waveTimer.IsRunning && !ct.IsCancellationRequested)
            {
                _spawner.SpawnEnemy(config.EnemyData);
                spawnedCount++;

                await UniTask.Delay(TimeSpan.FromSeconds(config.SpawnInterval), cancellationToken: ct);
            }
        }

        private void OnCastleDestroyed(IDamageable damageable)
        {
            SetState(WaveState.GameOver);
            CancelCurrentWave();
            GameManager.Instance?.SetState(GameState.GameOver);
            OnGameOver?.Invoke();
        }

        private void CancelCurrentWave()
        {
            if (_waveCts != null)
            {
                _waveCts.Cancel();
                _waveCts.Dispose();
                _waveCts = null;
            }
            _waveTimer.Stop();
        }

        private void SetState(WaveState newState)
        {
            _currentState = newState;
            StateChanged?.Invoke(_currentState);
        }
    }
}
