using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Alchemy.Inspector;
using Game.Buildings;
using Game.Core;
using Game.Enemies.Spawning;
using Game.UI.Shop;

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
        public static WaveController Instance { get; private set; }

        [Title("References")]
        [SerializeField] private List<WaveConfig> _waveConfigs = new();
        [SerializeField] private WaveConfig _initialWaveConfig;
        [SerializeField] private EnemySpawner _spawner;
        [SerializeField] private Castle _castle;

        [Title("Settings")]
        [SerializeField] private bool _autoStartFirstWave = false;
        [SerializeField, Min(0f)] private float _delayBeforeFirstWave = 1f;

        [Title("Debug")]
        [SerializeField, ReadOnly] private WaveState _currentState = WaveState.NotStarted;
        [SerializeField, ReadOnly] private int _currentWaveIndex = 0;
        [SerializeField, ReadOnly] private int _currentWaveNumber = 1;
        [SerializeField, ReadOnly] private float _buildingPhaseRemainingTime = 0f;

        public WaveState CurrentState => _currentState;
        public int CurrentWaveNumber => _currentWaveNumber;
        public int CurrentWaveIndex => _currentWaveIndex;
        public IReadOnlyList<WaveConfig> WaveConfigs => _waveConfigs;
        public float BuildingPhaseRemainingTime => _buildingPhaseRemainingTime;
        public IWaveTimer Timer => _waveTimer;
        public EnemySpawner Spawner => _spawner;

        public WaveConfig CurrentWaveConfig
        {
            get
            {
                if (_waveConfigs != null && _waveConfigs.Count > 0)
                {
                    int idx = Mathf.Clamp(_currentWaveIndex, 0, _waveConfigs.Count - 1);
                    return _waveConfigs[idx];
                }
                return _initialWaveConfig;
            }
        }

        private readonly WaveTimer _waveTimer = new();
        private CancellationTokenSource _waveCts;
        private bool _skipBuildingPhaseRequested;

        public event Action OnWaveStarted;
        public event Action OnWaveCleared;
        public event Action OnBuildingPhaseStarted;
        public event Action OnBuildingPhaseEnded;
        public event Action<float> BuildingPhaseTimeChanged;
        public event Action OnGameOver;
        public event Action<WaveState> StateChanged;

        private void Awake()
        {
            Instance = this;
            EnsureWaveConfigsPopulated();
            if (_castle == null)
            {
                _castle = Castle.Instance ?? FindAnyObjectByType<Castle>();
            }
            if (_spawner == null)
            {
                _spawner = GetComponentInChildren<EnemySpawner>() ?? FindAnyObjectByType<EnemySpawner>();
            }
        }

        private void Start()
        {
            if (_castle == null)
            {
                _castle = Castle.Instance ?? FindAnyObjectByType<Castle>();
            }
            if (_spawner == null)
            {
                _spawner = GetComponentInChildren<EnemySpawner>() ?? FindAnyObjectByType<EnemySpawner>();
            }

            if (_castle != null)
            {
                _castle.Destroyed -= OnCastleDestroyed;
                _castle.Destroyed += OnCastleDestroyed;
            }

            WaveConfig firstConfig = CurrentWaveConfig;
            if (firstConfig != null)
            {
                _currentWaveNumber = firstConfig.WaveNumber;
            }

            if (_autoStartFirstWave && firstConfig != null)
            {
                StartWaveWithDelayAsync(_delayBeforeFirstWave).Forget();
            }
            else
            {
                float duration = firstConfig != null ? firstConfig.BuildingPhaseDuration : 20f;
                RunFirstBuildingPhaseAsync(duration).Forget();
            }
        }

        private void OnDestroy()
        {
            CancelCurrentWave();
            if (_castle != null)
            {
                _castle.Destroyed -= OnCastleDestroyed;
            }
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void EnsureWaveConfigsPopulated()
        {
            if ((_waveConfigs == null || _waveConfigs.Count == 0) && _initialWaveConfig != null)
            {
                _waveConfigs = new List<WaveConfig> { _initialWaveConfig };
            }
        }

        [Button]
        public void SkipBuildingPhaseAndStartWave()
        {
            if (_currentState == WaveState.BuildingPhase)
            {
                _skipBuildingPhaseRequested = true;
            }
            else if (_currentState == WaveState.NotStarted)
            {
                StartWave();
            }
        }

        [Button]
        public void StartWave()
        {
            if (_castle == null) _castle = Castle.Instance ?? FindAnyObjectByType<Castle>();
            if (_spawner == null) _spawner = GetComponentInChildren<EnemySpawner>() ?? FindAnyObjectByType<EnemySpawner>();

            if (_currentState == WaveState.GameOver)
            {
                _castle?.HealFull();
                _spawner?.ClearAllEnemies();
                _currentWaveIndex = 0;
                EnsureWaveConfigsPopulated();
                WaveConfig cfg = CurrentWaveConfig;
                _currentWaveNumber = cfg != null ? cfg.WaveNumber : 1;
                RunFirstBuildingPhaseAsync(cfg != null ? cfg.BuildingPhaseDuration : 20f).Forget();
                return;
            }

            if (_currentState == WaveState.BuildingPhase)
            {
                _skipBuildingPhaseRequested = true;
                return;
            }

            if (_currentState == WaveState.WaveActive)
            {
                return;
            }

            StartWave(CurrentWaveConfig);
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

                if (_currentState != WaveState.GameOver && CurrentWaveConfig != null)
                {
                    StartWave(CurrentWaveConfig);
                }
            }
            catch (OperationCanceledException)
            {
                // Clean cancellation on destroy
            }
        }

        private async UniTaskVoid RunFirstBuildingPhaseAsync(float duration)
        {
            try
            {
                CancelCurrentWave();
                _waveCts = new CancellationTokenSource();
                await RunBuildingPhaseAsync(duration, _waveCts.Token);

                if (!_waveCts.IsCancellationRequested && _currentState != WaveState.GameOver)
                {
                    StartWave(CurrentWaveConfig);
                }
            }
            catch (OperationCanceledException)
            {
                // Clean cancellation
            }
        }

        private async UniTask RunBuildingPhaseAsync(float duration, CancellationToken ct)
        {
            _buildingPhaseRemainingTime = Mathf.Max(0.5f, duration);
            _skipBuildingPhaseRequested = false;
            SetState(WaveState.BuildingPhase);
            OnBuildingPhaseStarted?.Invoke();
            BuildingPhaseTimeChanged?.Invoke(_buildingPhaseRemainingTime);

            // GIG-43 / GIG-44: Present any queued Castle level-up chest(s) when entering pause
            if (RunProgressionManager.Instance != null)
            {
                RunProgressionManager.Instance.TryPresentNextPendingUpgrade(viaChest: true);
            }

            while (_buildingPhaseRemainingTime > 0f && !_skipBuildingPhaseRequested && !ct.IsCancellationRequested)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                if (_currentState == WaveState.GameOver) return;
                if (_skipBuildingPhaseRequested) break;

                // Pause countdown while in-run shop or chest/upgrade modal is open
                bool pauseTimer = InRunShopUI.IsAnyShopOpen ||
                    (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing);
                if (pauseTimer) continue;

                _buildingPhaseRemainingTime -= Time.deltaTime;
                if (_buildingPhaseRemainingTime < 0f) _buildingPhaseRemainingTime = 0f;
                BuildingPhaseTimeChanged?.Invoke(_buildingPhaseRemainingTime);
            }

            _skipBuildingPhaseRequested = false;
            OnBuildingPhaseEnded?.Invoke();
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

                // D-4 Option A / GIG-46: When timer hits 00:00, stop spawning immediately and order survivors to retreat
                using var spawnCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                UniTask timerTask = _waveTimer.RunAsync(config.DurationSeconds, ct);
                UniTask spawnTask = RunEnemySpawningAsync(config, spawnCts.Token);

                await timerTask;
                spawnCts.Cancel();

                try
                {
                    await spawnTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when wave timer ends mid-spawn interval
                }

                if (ct.IsCancellationRequested || _currentState == WaveState.GameOver)
                {
                    return;
                }

                SetState(WaveState.WaveClearing);

                if (_spawner != null && _spawner.ActiveEnemyCount > 0)
                {
                    _spawner.RetreatAllActiveEnemies();
                }

                float retreatGraceRemaining = 15f;
                while (_spawner != null && _spawner.ActiveEnemyCount > 0 && retreatGraceRemaining > 0f)
                {
                    await UniTask.Delay(200, cancellationToken: ct);
                    if (_currentState == WaveState.GameOver) return;
                    retreatGraceRemaining -= 0.2f;
                }

                // Force-release any stuck retreating enemies silently without granting coins/XP
                if (_spawner != null && _spawner.ActiveEnemyCount > 0)
                {
                    _spawner.ClearAllEnemies();
                }

                if (ct.IsCancellationRequested || _currentState == WaveState.GameOver)
                {
                    return;
                }

                SetState(WaveState.WaveCompleted);
                OnWaveCleared?.Invoke();

                // GIG-112: Award +15 CombatXP to surviving buildings at end of wave
                var activeBuildings = Building.ActiveBuildings;
                for (int i = activeBuildings.Count - 1; i >= 0; i--)
                {
                    Building b = activeBuildings[i];
                    if (b != null && !b.IsDead)
                    {
                        b.AddWaveSurvivalXP();
                    }
                }

                RunStatistics.Instance?.RecordWaveCleared(_currentWaveNumber);

                bool isLastAuthoredWave = _waveConfigs != null && _waveConfigs.Count > 0 && _currentWaveIndex >= _waveConfigs.Count - 1;
                if (config.IsFinalWave || isLastAuthoredWave)
                {
                    GameManager.Instance.SetState(GameState.Victory);
                    return;
                }

                while (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.UpgradeSelection)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    if (_currentState == WaveState.GameOver) return;
                }

                _currentWaveIndex++;
                WaveConfig nextConfig = CurrentWaveConfig;
                if (nextConfig != null)
                {
                    _currentWaveNumber = nextConfig.WaveNumber;
                    await RunBuildingPhaseAsync(nextConfig.BuildingPhaseDuration, ct);

                    if (ct.IsCancellationRequested || _currentState == WaveState.GameOver) return;

                    RunWaveLifecycleAsync(nextConfig, ct).Forget();
                }
            }
            catch (OperationCanceledException)
            {
                // Clean cancellation on wave cancel or game over
            }
        }

        private async UniTask RunEnemySpawningAsync(WaveConfig config, CancellationToken ct)
        {
            var groups = config.GetActiveSpawnGroups();
            if (groups == null || groups.Count == 0)
            {
                return;
            }

            var tasks = new List<UniTask>(groups.Count);
            for (int i = 0; i < groups.Count; i++)
            {
                tasks.Add(RunSpawnGroupAsync(groups[i], ct));
            }

            await UniTask.WhenAll(tasks);
        }

        private async UniTask RunSpawnGroupAsync(EnemySpawnGroup group, CancellationToken ct)
        {
            if (group.EnemyData == null) return;

            if (group.StartDelay > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(group.StartDelay), cancellationToken: ct);
            }

            int spawnedCount = 0;
            int batch = Mathf.Max(1, group.SpawnBatchSize);

            while (spawnedCount < group.Count && _waveTimer.IsRunning && !ct.IsCancellationRequested)
            {
                int countToSpawn = Mathf.Min(batch, group.Count - spawnedCount);
                for (int b = 0; b < countToSpawn; b++)
                {
                    _spawner?.SpawnEnemy(group.EnemyData);
                    spawnedCount++;
                }

                if (spawnedCount >= group.Count) break;

                await UniTask.Delay(TimeSpan.FromSeconds(group.SpawnInterval), cancellationToken: ct);
            }
        }

        private void OnCastleDestroyed(IDamageable damageable)
        {
            SetState(WaveState.GameOver);
            CancelCurrentWave();
            GameManager.Instance.SetState(GameState.GameOver);
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
