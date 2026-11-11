using System;
using UnityEngine;
using Alchemy.Inspector;
using Game.Buildings;
using Game.Enemies.Spawning;
using Game.Loot;
using Game.Pathfinding;
using Game.Projectiles;
using Game.Spells;
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

            // 4. Combat & Weapons (GIG-85: Equip weapon selected in MainMenu)
            CastleWeapon activeWeapon = ResolveSelectedCastleWeapon(castle, progression);
            if (activeWeapon != null)
            {
                log.AppendLine($"[PASS] CastleWeapon active: {activeWeapon.GetType().Name} (Lv.{activeWeapon.CurrentLevel})");
            }

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
                buildingManager.RegisterPlacementValidator(MageTowerBuilding.CanPlaceMageTower);
                log.AppendLine("[PASS] BuildingManager ready (MageTower cap validator registered)");
            }
            else
            {
                log.AppendLine("[WARN] BuildingManager not found in scene!");
            }

            EnsureSpellSystems(log);

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

            var weaponAimUI = FindAnyObjectByType<WeaponAimUI>(FindObjectsInactive.Include);
            if (weaponAimUI != null && activeWeapon != null)
            {
                weaponAimUI.BindWeapon(activeWeapon);
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

        private CastleWeapon ResolveSelectedCastleWeapon(Castle castle, RunProgressionManager progression)
        {
            CastleWeapon existing = FindAnyObjectByType<CastleWeapon>();
            string selectedId = RunConfig.Instance != null && RunConfig.Instance.SelectedWeapon != null
                ? RunConfig.Instance.SelectedWeapon.Id
                : null;

            if (string.IsNullOrEmpty(selectedId) || selectedId == "heavy_ballista")
            {
                if (existing is HeavyBallista hb && RunConfig.Instance?.SelectedWeapon?.BallistaData != null)
                {
                    hb.Configure(RunConfig.Instance.SelectedWeapon.BallistaData);
                }
                progression?.BindWeapon(existing);
                return existing;
            }

            GameObject mountGo = existing != null
                ? existing.gameObject
                : (castle != null ? castle.gameObject : null);
            if (mountGo == null) return existing;

            if (existing != null)
            {
                existing.enabled = false;
                Destroy(existing);
            }

            CastleWeapon spawned = selectedId switch
            {
                "alchemical_mortar" => mountGo.AddComponent<AlchemicalMortar>(),
                "tar_cannon" => mountGo.AddComponent<TarCannon>(),
                "orthanc" => mountGo.AddComponent<OrthancTower>(),
                "archer_volley" => mountGo.AddComponent<ArcherVolley>(),
                _ => mountGo.AddComponent<HeavyBallista>()
            };

            if (spawned is HeavyBallista newHb && RunConfig.Instance?.SelectedWeapon?.BallistaData != null)
            {
                newHb.Configure(RunConfig.Instance.SelectedWeapon.BallistaData);
            }

            progression?.BindWeapon(spawned);
            return spawned;
        }

        private void EnsureSpellSystems(System.Text.StringBuilder log)
        {
            var scrollMgr = SpellScrollManager.Instance ?? FindAnyObjectByType<SpellScrollManager>();
            if (scrollMgr == null)
            {
                scrollMgr = gameObject.AddComponent<SpellScrollManager>();
            }

            var caster = FindAnyObjectByType<SpellCaster>();
            if (caster == null)
            {
                caster = gameObject.AddComponent<SpellCaster>();
            }

            var panel = FindAnyObjectByType<SpellPanelUI>(FindObjectsInactive.Include);
            if (panel != null && scrollMgr != null)
            {
                panel.Bind(scrollMgr);
            }

            log.AppendLine($"[PASS] Spell systems ready (ScrollManager max={scrollMgr.MaxScrolls})");
        }
    }
}
