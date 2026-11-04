using System;
using UnityEngine;
using Alchemy.Inspector;
using Game.Buildings;
using Game.Enemies.Spawning;
using Game.Loot;
using Game.Pathfinding;
using Game.Projectiles;
using Game.UI;
using Game.WaveSystem;
using Game.Weapons;

namespace Game.Core
{
    [DefaultExecutionOrder(-200)]
    public class SceneBootstrapper : MonoBehaviour
    {
        public static SceneBootstrapper Instance { get; private set; }
        public static event Action SceneBootstrapped;

        [Title("Status")]
        [ShowInInspector, ReadOnly] public bool IsReady { get; private set; }

        [Title("Settings")]
        [SerializeField] private bool _logDetailedReport = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializeScene();
        }

        private void Start()
        {
            // Second pass in Start to catch any late-subscribing UI elements
            FinalizeSetup();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void InitializeScene()
        {
            if (IsReady) return;

            var log = new System.Text.StringBuilder();
            log.AppendLine("[SceneBootstrapper] Initializing Scene Systems...");

            // 1. Core Managers
            var gameManager = GameManager.Instance ?? FindAnyObjectByType<GameManager>();
            var wallet = PlayerWallet.Instance ?? FindAnyObjectByType<PlayerWallet>();
            var progression = RunProgressionManager.Instance ?? FindAnyObjectByType<RunProgressionManager>();
            var stats = RunStatistics.Instance ?? FindAnyObjectByType<RunStatistics>();
            var buildingManager = FindAnyObjectByType<BuildingManager>();
            var projectilePool = FindAnyObjectByType<ProjectilePool>();

            // 2. World & Castle
            var castle = Castle.Instance ?? FindAnyObjectByType<Castle>();
            var gridMap = FindAnyObjectByType<GridMap>();
            var flowfield = FindAnyObjectByType<GridFlowfield>();

            // 3. Enemies & Waves
            var waveController = FindAnyObjectByType<WaveController>();
            var spawner = FindAnyObjectByType<EnemySpawner>();
            var enemyPool = FindAnyObjectByType<EnemyPool>();
            var lootDrop = FindAnyObjectByType<LootDropService>();

            // 4. Combat & Weapons
            var ballista = FindAnyObjectByType<HeavyBallista>();

            // 5. Verification & Auto-Healing of Links
            if (wallet != null)
            {
                wallet.ForceRefresh();
                log.AppendLine($"[PASS] PlayerWallet ready (Coins: {wallet.Coins}, Diamonds: {wallet.Diamonds})");
            }
            else
            {
                log.AppendLine("[WARN] PlayerWallet not found in scene!");
            }

            if (castle != null)
            {
                log.AppendLine($"[PASS] Castle ready (HP: {castle.CurrentHP}/{castle.MaxHP})");
            }
            else
            {
                log.AppendLine("[WARN] Castle not found in scene!");
            }

            if (waveController != null)
            {
                log.AppendLine($"[PASS] WaveController ready (State: {waveController.CurrentState}, Waves: {waveController.WaveConfigs.Count})");
            }
            else
            {
                log.AppendLine("[WARN] WaveController not found in scene!");
            }

            if (buildingManager != null)
            {
                log.AppendLine("[PASS] BuildingManager ready");
            }
            else
            {
                log.AppendLine("[WARN] BuildingManager not found in scene!");
            }

            // 6. Refresh UI components
            var walletUI = FindAnyObjectByType<WalletUI>(FindObjectsInactive.Include);
            if (walletUI != null && wallet != null)
            {
                walletUI.BindWallet(wallet);
            }

            var waveTimerUI = FindAnyObjectByType<WaveTimerUI>(FindObjectsInactive.Include);
            if (waveTimerUI != null && waveController != null)
            {
                waveTimerUI.BindWaveController(waveController);
            }

            IsReady = true;
            log.AppendLine("[SceneBootstrapper] Scene Bootstrap Completed Successfully.");

            if (_logDetailedReport)
            {
                Debug.Log(log.ToString());
            }

            SceneBootstrapped?.Invoke();
        }

        private void FinalizeSetup()
        {
            var wallet = PlayerWallet.Instance ?? FindAnyObjectByType<PlayerWallet>();
            if (wallet != null)
            {
                wallet.ForceRefresh();
            }
        }
    }
}
