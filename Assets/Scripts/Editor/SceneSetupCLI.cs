#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Game.Core;
using Game.Buildings;
using Game.Projectiles;
using Game.Weapons;
using Game.Upgrades;
using Game.UI;
using Game.UI.BuildingUI;
using Game.CameraSystem;
using Game.WaveSystem;
using Game.Enemies.Data;

namespace Game.Editor
{
    public static class SceneSetupCLI
    {
        [MenuItem("Tools/Run Scene Setup & Assignment")]
        public static void RunSetupMenu()
        {
            RunSetup();
        }

        [MenuItem("Tools/Validate SampleScene")]
        public static void ValidateSceneMenu()
        {
            string report = ValidateScene();
            Debug.Log(report);
        }

        public static string RunSetup()
        {
            var log = new System.Text.StringBuilder();
            log.AppendLine("=== Starting Scene Setup & Assignment ===");

            // 1. Ensure asset directories exist
            EnsureDirectories();

            // 2. Create missing assets
            CreateProjectileData(log);
            CreateHeavyBallistaData(log);
            CreateUpgradeDatabase(log);
            CreateDefaultMetaData(log);
            CreateProjectilePrefab(log);
            CreateBuildingGhostPrefab(log);
            CreateEnemyAssetsAndSprites(log);
            CreateWaveConfigs(log);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3. Configure SampleScene
            SetupSampleScene(log);

            // 4. Configure MainMenu
            SetupMainMenu(log);

            // 5. Return to SampleScene as active
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

            log.AppendLine("=== Scene Setup & Assignment Finished Successfully ===");
            Debug.Log(log.ToString());
            return log.ToString();
        }

        public static string ValidateScene()
        {
            var log = new System.Text.StringBuilder();
            log.AppendLine("=== Validating SampleScene ===");

            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

            bool hasErrors = false;

            void Check(string label, bool condition, string detail)
            {
                if (condition)
                {
                    log.AppendLine($"[PASS] {label}: {detail}");
                }
                else
                {
                    log.AppendLine($"[FAIL] {label}: {detail}");
                    hasErrors = true;
                }
            }

            var inputManager = UnityEngine.Object.FindFirstObjectByType<InputManager>();
            Check("InputManager", inputManager != null, inputManager != null ? "Assigned in scene" : "MISSING");

            var eventSystem = UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            Check("EventSystem", eventSystem != null, eventSystem != null ? "Assigned in scene" : "MISSING");

            var runConfig = UnityEngine.Object.FindFirstObjectByType<RunConfig>();
            Check("RunConfig", runConfig != null, runConfig != null ? "Assigned in scene" : "MISSING");
            if (runConfig != null)
            {
                var so = new SerializedObject(runConfig);
                Check("RunConfig._selectedKnight", so.FindProperty("_selectedKnight").objectReferenceValue != null, "Assigned");
                Check("RunConfig._selectedArena", so.FindProperty("_selectedArena").objectReferenceValue != null, "Assigned");
                Check("RunConfig._selectedWeapon", so.FindProperty("_selectedWeapon").objectReferenceValue != null, "Assigned");
            }

            var bm = UnityEngine.Object.FindFirstObjectByType<BuildingManager>();
            if (bm != null)
            {
                var so = new SerializedObject(bm);
                Check("BuildingManager._gridMap", so.FindProperty("_gridMap").objectReferenceValue != null, "Assigned");
                Check("BuildingManager._ghostView", so.FindProperty("_ghostView").objectReferenceValue != null, "Assigned");
                Check("BuildingManager._wallet", so.FindProperty("_wallet").objectReferenceValue != null, "Assigned");
                Check("BuildingManager._mainCamera", so.FindProperty("_mainCamera").objectReferenceValue != null, "Assigned");
                Check("BuildingManager._contextMenuUI", so.FindProperty("_contextMenuUI").objectReferenceValue != null, "Assigned");
            }
            else Check("BuildingManager", false, "Not found");

            var pool = UnityEngine.Object.FindFirstObjectByType<ProjectilePool>();
            if (pool != null)
            {
                var so = new SerializedObject(pool);
                Check("ProjectilePool._projectilePrefab", so.FindProperty("_projectilePrefab").objectReferenceValue != null, "Assigned");
            }
            else Check("ProjectilePool", false, "Not found");

            var prog = UnityEngine.Object.FindFirstObjectByType<RunProgressionManager>();
            if (prog != null)
            {
                var so = new SerializedObject(prog);
                Check("RunProgressionManager._castle", so.FindProperty("_castle").objectReferenceValue != null, "Assigned");
                Check("RunProgressionManager._upgradeDatabase", so.FindProperty("_upgradeDatabase").objectReferenceValue != null, "Assigned");
            }
            else Check("RunProgressionManager", false, "Not found");

            var gm = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                var so = new SerializedObject(gm);
                Check("GameManager._pauseUI", so.FindProperty("_pauseUI").objectReferenceValue != null, "Assigned");
                Check("GameManager._gameOverUI", so.FindProperty("_gameOverUI").objectReferenceValue != null, "Assigned");
            }
            else Check("GameManager", false, "Not found");

            var ballista = UnityEngine.Object.FindFirstObjectByType<HeavyBallista>();
            if (ballista != null)
            {
                var so = new SerializedObject(ballista);
                Check("HeavyBallista._castle", so.FindProperty("_castle").objectReferenceValue != null, "Assigned");
                Check("HeavyBallista._data", so.FindProperty("_data").objectReferenceValue != null, "Assigned");
                Check("HeavyBallista._firePoint", so.FindProperty("_firePoint").objectReferenceValue != null, "Assigned");
            }
            else Check("HeavyBallista", false, "Not found");

            var aimUI = UnityEngine.Object.FindFirstObjectByType<WeaponAimUI>(FindObjectsInactive.Include);
            if (aimUI != null)
            {
                var so = new SerializedObject(aimUI);
                Check("WeaponAimUI._mainCamera", so.FindProperty("_mainCamera").objectReferenceValue != null, "Assigned");
                Check("WeaponAimUI._weapon", so.FindProperty("_weapon").objectReferenceValue != null, "Assigned");
                Check("WeaponAimUI._cooldownText", so.FindProperty("_cooldownText").objectReferenceValue != null, "Assigned");
                Check("WeaponAimUI._aimLine", so.FindProperty("_aimLine").objectReferenceValue != null, "Assigned");
                Check("WeaponAimUI._aimReticle", so.FindProperty("_aimReticle").objectReferenceValue != null, "Assigned");
            }
            else Check("WeaponAimUI", false, "Not found");

            var camCtrl = UnityEngine.Object.FindFirstObjectByType<CameraController>();
            if (camCtrl != null)
            {
                var so = new SerializedObject(camCtrl);
                Check("CameraController._targetCamera", so.FindProperty("_targetCamera").objectReferenceValue != null, "Assigned");
                Check("CameraController._gridMap", so.FindProperty("_gridMap").objectReferenceValue != null, "Assigned");
                Check("CameraController._initialTarget", so.FindProperty("_initialTarget").objectReferenceValue != null, "Assigned");
            }
            else Check("CameraController", false, "Not found");

            var healthBar = UnityEngine.Object.FindFirstObjectByType<CastleHealthBar>(FindObjectsInactive.Include);
            if (healthBar != null)
            {
                var so = new SerializedObject(healthBar);
                Check("CastleHealthBar._castle", so.FindProperty("_castle").objectReferenceValue != null, "Assigned");
            }

            var walletUI = UnityEngine.Object.FindFirstObjectByType<WalletUI>(FindObjectsInactive.Include);
            if (walletUI != null)
            {
                var so = new SerializedObject(walletUI);
                Check("WalletUI._wallet", so.FindProperty("_wallet").objectReferenceValue != null, "Assigned");
            }

            var timerUI = UnityEngine.Object.FindFirstObjectByType<WaveTimerUI>(FindObjectsInactive.Include);
            if (timerUI != null)
            {
                var so = new SerializedObject(timerUI);
                Check("WaveTimerUI._waveController", so.FindProperty("_waveController").objectReferenceValue != null, "Assigned");
            }

            var waveCtrl = UnityEngine.Object.FindFirstObjectByType<WaveController>();
            Check("WaveController", waveCtrl != null, waveCtrl != null ? "Assigned in scene" : "MISSING");
            if (waveCtrl != null)
            {
                var so = new SerializedObject(waveCtrl);
                var waveConfigsProp = so.FindProperty("_waveConfigs");
                Check("WaveController._waveConfigs", waveConfigsProp != null && waveConfigsProp.arraySize >= 5, $"Configured with {waveConfigsProp?.arraySize ?? 0} waves");
            }

            log.AppendLine(hasErrors ? "=== VALIDATION FAILED ===" : "=== ALL SCENE CHECKS PASSED ===");
            return log.ToString();
        }

        private static void EnsureDirectories()
        {
            string[] dirs = new[]
            {
                "Assets/Prefabs",
                "Assets/ScriptbleObjects",
                "Assets/ScriptbleObjects/Projectiles",
                "Assets/ScriptbleObjects/Weapons",
                "Assets/ScriptbleObjects/Upgrades",
                "Assets/ScriptbleObjects/Data",
                "Assets/ScriptbleObjects/Enemy",
                "Assets/ScriptbleObjects/WaveConfig",
                "Assets/Visual/Sprite"
            };

            foreach (var d in dirs)
            {
                if (!AssetDatabase.IsValidFolder(d))
                {
                    string parent = Path.GetDirectoryName(d).Replace('\\', '/');
                    string folder = Path.GetFileName(d);
                    AssetDatabase.CreateFolder(parent, folder);
                }
            }
        }

        private static void CreateProjectileData(System.Text.StringBuilder log)
        {
            string path = "Assets/ScriptbleObjects/Projectiles/Projectile_BallistaBolt.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ProjectileData>(path);
            if (existing == null)
            {
                var data = ScriptableObject.CreateInstance<ProjectileData>();
                data.SetRuntimeStats(damage: 15, speed: 15f, pierceCount: 999, knockbackCells: 1f);
                AssetDatabase.CreateAsset(data, path);
                log.AppendLine($"Created: {path}");
            }
            else
            {
                log.AppendLine($"Already exists: {path}");
            }
        }

        private static void CreateHeavyBallistaData(System.Text.StringBuilder log)
        {
            string path = "Assets/ScriptbleObjects/Weapons/HeavyBallistaData.asset";
            var existing = AssetDatabase.LoadAssetAtPath<HeavyBallistaData>(path);
            var projData = AssetDatabase.LoadAssetAtPath<ProjectileData>("Assets/ScriptbleObjects/Projectiles/Projectile_BallistaBolt.asset");

            if (existing == null)
            {
                var data = ScriptableObject.CreateInstance<HeavyBallistaData>();
                
                // Set projectile data via reflection or serialized object
                var so = new SerializedObject(data);
                var projProp = so.FindProperty("_projectileData");
                if (projProp != null)
                {
                    projProp.objectReferenceValue = projData;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                AssetDatabase.CreateAsset(data, path);
                log.AppendLine($"Created: {path}");
            }
            else
            {
                if (existing.ProjectileData == null && projData != null)
                {
                    var so = new SerializedObject(existing);
                    so.FindProperty("_projectileData").objectReferenceValue = projData;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(existing);
                }
                log.AppendLine($"Already exists: {path}");
            }
        }

        private static void CreateUpgradeDatabase(System.Text.StringBuilder log)
        {
            string path = "Assets/ScriptbleObjects/Upgrades/UpgradeDatabase.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UpgradeDatabase>(path);
            if (existing == null)
            {
                var data = ScriptableObject.CreateInstance<UpgradeDatabase>();
                data.PopulateWithDefaultUpgrades();
                AssetDatabase.CreateAsset(data, path);
                log.AppendLine($"Created: {path}");
            }
            else
            {
                if (existing.AllUpgrades.Count == 0)
                {
                    existing.PopulateWithDefaultUpgrades();
                    EditorUtility.SetDirty(existing);
                }
                log.AppendLine($"Already exists: {path}");
            }
        }

        private static void CreateDefaultMetaData(System.Text.StringBuilder log)
        {
            string dir = "Assets/ScriptbleObjects/Data";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                AssetDatabase.CreateFolder("Assets/ScriptbleObjects", "Data");
            }

            string knightPath = "Assets/ScriptbleObjects/Data/DefaultKnight.asset";
            var knight = AssetDatabase.LoadAssetAtPath<Game.Data.KnightData>(knightPath);
            if (knight == null)
            {
                knight = ScriptableObject.CreateInstance<Game.Data.KnightData>();
                knight.InitializeRuntime("hador_oplot", "Хадор Оплот", "Рыцарь-Бастион", "Камень не молит о пощаде, он просто стоит.", 2000, 125, 4, new[] { "Стены" }, new[] { "Несокрушимый фундамент" }, true, 0);
                AssetDatabase.CreateAsset(knight, knightPath);
                log.AppendLine($"Created: {knightPath}");
            }

            string arenaPath = "Assets/ScriptbleObjects/Data/DefaultArena.asset";
            var arena = AssetDatabase.LoadAssetAtPath<Game.Data.ArenaData>(arenaPath);
            if (arena == null)
            {
                arena = ScriptableObject.CreateInstance<Game.Data.ArenaData>();
                arena.InitializeRuntime("arena_plains", "Зелёные равнины", "Базовая арена.", "SampleScene", true, 0);
                AssetDatabase.CreateAsset(arena, arenaPath);
                log.AppendLine($"Created: {arenaPath}");
            }

            string weaponPath = "Assets/ScriptbleObjects/Data/DefaultWeapon.asset";
            var weapon = AssetDatabase.LoadAssetAtPath<Game.Data.WeaponSelectionData>(weaponPath);
            if (weapon == null)
            {
                var ballistaData = AssetDatabase.LoadAssetAtPath<HeavyBallistaData>("Assets/ScriptbleObjects/Weapons/HeavyBallistaData.asset");
                weapon = ScriptableObject.CreateInstance<Game.Data.WeaponSelectionData>();
                weapon.InitializeRuntime("heavy_ballista", "Тяжёлая баллиста", "Стреляет мощными болтами.", true, 0, ballistaData);
                AssetDatabase.CreateAsset(weapon, weaponPath);
                log.AppendLine($"Created: {weaponPath}");
            }
        }

        private static void CreateProjectilePrefab(System.Text.StringBuilder log)
        {
            string path = "Assets/Prefabs/Projectile.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing == null)
            {
                var go = new GameObject("Projectile");
                var sr = go.AddComponent<SpriteRenderer>();
                // Assign a default sprite (e.g., standard square or triangle if available)
                var defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                if (defaultSprite != null) sr.sprite = defaultSprite;

                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.2f;

                var proj = go.AddComponent<Projectile>();
                var so = new SerializedObject(proj);
                so.FindProperty("_spriteRenderer").objectReferenceValue = sr;
                so.FindProperty("_collider").objectReferenceValue = col;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(go, path);
                UnityEngine.Object.DestroyImmediate(go);
                log.AppendLine($"Created: {path}");
            }
            else
            {
                log.AppendLine($"Already exists: {path}");
            }
        }

        private static void CreateBuildingGhostPrefab(System.Text.StringBuilder log)
        {
            string path = "Assets/Prefabs/BuildingGhost.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing == null)
            {
                var go = new GameObject("BuildingGhost");
                var sr = go.AddComponent<SpriteRenderer>();
                var ghostView = go.AddComponent<BuildingGhostView>();

                var so = new SerializedObject(ghostView);
                so.FindProperty("_spriteRenderer").objectReferenceValue = sr;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(go, path);
                UnityEngine.Object.DestroyImmediate(go);
                log.AppendLine($"Created: {path}");
            }
            else
            {
                log.AppendLine($"Already exists: {path}");
            }
        }

        private static void CreateEnemyAssetsAndSprites(System.Text.StringBuilder log)
        {
            // 1. Ensure/Generate Sprites
            string fastSpritePath = "Assets/Visual/Sprite/Enemy_Fast.png";
            if (!File.Exists(Path.Combine(Application.dataPath, "Visual/Sprite/Enemy_Fast.png")))
            {
                SaveSpritePng(fastSpritePath, CreateEnemyFastTexture(), log);
            }

            string tankSpritePath = "Assets/Visual/Sprite/Enemy_Tank.png";
            if (!File.Exists(Path.Combine(Application.dataPath, "Visual/Sprite/Enemy_Tank.png")))
            {
                SaveSpritePng(tankSpritePath, CreateEnemyTankTexture(), log);
            }

            string archerSpritePath = "Assets/Visual/Sprite/Enemy_Archer.png";
            if (!File.Exists(Path.Combine(Application.dataPath, "Visual/Sprite/Enemy_Archer.png")))
            {
                SaveSpritePng(archerSpritePath, CreateEnemyArcherTexture(), log);
            }

            string bossSpritePath = "Assets/Visual/Sprite/Enemy_Boss.png";
            if (!File.Exists(Path.Combine(Application.dataPath, "Visual/Sprite/Enemy_Boss.png")))
            {
                SaveSpritePng(bossSpritePath, CreateEnemyBossTexture(), log);
            }

            AssetDatabase.Refresh();

            var regularSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Visual/Sprite/Enemy_Regular.png");
            var fastSprite = AssetDatabase.LoadAssetAtPath<Sprite>(fastSpritePath);
            var tankSprite = AssetDatabase.LoadAssetAtPath<Sprite>(tankSpritePath);
            var archerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(archerSpritePath);
            var bossSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bossSpritePath);

            // 2. Create/Update EnemyData ScriptableObjects
            // Regular Enemy (Update existing)
            GetOrCreateEnemyData(
                path: "Assets/ScriptbleObjects/Enemy/Enemy.asset",
                id: "regular_enemy",
                displayName: "Пехотинец",
                isBoss: false,
                maxHP: 30,
                speed: 2.0f,
                minSpeedRatio: 0.8f,
                damage: 5,
                attackInterval: 1.5f,
                attackRange: 0.8f,
                isRanged: false,
                visualScale: Vector3.one,
                tintColor: Color.white,
                honorReward: 10,
                sprite: regularSprite,
                minCoins: 1,
                maxCoins: 3,
                coinDropChance: 1.0f,
                diamondDropChance: 0.05f,
                diamondAmount: 1,
                log: log);

            // Fast Goblin
            GetOrCreateEnemyData(
                path: "Assets/ScriptbleObjects/Enemy/Enemy_Fast.asset",
                id: "fast_goblin",
                displayName: "Гоблин-бегун",
                isBoss: false,
                maxHP: 16,
                speed: 3.2f,
                minSpeedRatio: 0.8f,
                damage: 3,
                attackInterval: 0.9f,
                attackRange: 0.7f,
                isRanged: false,
                visualScale: new Vector3(0.85f, 0.85f, 1f),
                tintColor: new Color(0.75f, 1f, 0.45f, 1f),
                honorReward: 8,
                sprite: fastSprite != null ? fastSprite : regularSprite,
                minCoins: 1,
                maxCoins: 2,
                coinDropChance: 1.0f,
                diamondDropChance: 0.05f,
                diamondAmount: 1,
                log: log);

            // Armored Tank
            GetOrCreateEnemyData(
                path: "Assets/ScriptbleObjects/Enemy/Enemy_Tank.asset",
                id: "armored_tank",
                displayName: "Тяжелый латник",
                isBoss: false,
                maxHP: 140,
                speed: 1.2f,
                minSpeedRatio: 0.8f,
                damage: 16,
                attackInterval: 2.2f,
                attackRange: 1.0f,
                isRanged: false,
                visualScale: new Vector3(1.35f, 1.35f, 1f),
                tintColor: new Color(0.9f, 0.45f, 0.45f, 1f),
                honorReward: 25,
                sprite: tankSprite != null ? tankSprite : regularSprite,
                minCoins: 4,
                maxCoins: 8,
                coinDropChance: 1.0f,
                diamondDropChance: 0.15f,
                diamondAmount: 1,
                log: log);

            // Ranged Archer
            GetOrCreateEnemyData(
                path: "Assets/ScriptbleObjects/Enemy/Enemy_Archer.asset",
                id: "ranged_archer",
                displayName: "Лучник тьмы",
                isBoss: false,
                maxHP: 24,
                speed: 1.7f,
                minSpeedRatio: 0.8f,
                damage: 6,
                attackInterval: 2.0f,
                attackRange: 3.5f,
                isRanged: true,
                visualScale: new Vector3(0.95f, 0.95f, 1f),
                tintColor: new Color(0.75f, 0.5f, 1f, 1f),
                honorReward: 14,
                sprite: archerSprite != null ? archerSprite : regularSprite,
                minCoins: 2,
                maxCoins: 4,
                coinDropChance: 1.0f,
                diamondDropChance: 0.08f,
                diamondAmount: 1,
                log: log);

            // Arena Boss
            GetOrCreateEnemyData(
                path: "Assets/ScriptbleObjects/Enemy/Enemy_Boss.asset",
                id: "arena_boss",
                displayName: "Владыка рейда",
                isBoss: true,
                maxHP: 1200,
                speed: 1.3f,
                minSpeedRatio: 0.9f,
                damage: 40,
                attackInterval: 2.5f,
                attackRange: 1.5f,
                isRanged: false,
                visualScale: new Vector3(2.2f, 2.2f, 1f),
                tintColor: new Color(1f, 0.8f, 0.25f, 1f),
                honorReward: 200,
                sprite: bossSprite != null ? bossSprite : regularSprite,
                minCoins: 20,
                maxCoins: 35,
                coinDropChance: 1.0f,
                diamondDropChance: 1.0f,
                diamondAmount: 3,
                log: log);
        }

        private static void CreateWaveConfigs(System.Text.StringBuilder log)
        {
            var regularEnemy = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptbleObjects/Enemy/Enemy.asset");
            var fastEnemy = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptbleObjects/Enemy/Enemy_Fast.asset");
            var tankEnemy = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptbleObjects/Enemy/Enemy_Tank.asset");
            var archerEnemy = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptbleObjects/Enemy/Enemy_Archer.asset");
            var bossEnemy = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptbleObjects/Enemy/Enemy_Boss.asset");

            // Wave 1
            GetOrCreateWaveConfig(
                path: "Assets/ScriptbleObjects/WaveConfig/Wave_01.asset",
                waveNumber: 1,
                durationSeconds: 45f,
                buildingPhaseDuration: 15f,
                isBossWave: false,
                isFinalWave: false,
                spawnGroups: new List<EnemySpawnGroup>
                {
                    new(regularEnemy, count: 12, startDelay: 0f, spawnInterval: 3.2f, batchSize: 1)
                },
                log: log);

            // Wave 2
            GetOrCreateWaveConfig(
                path: "Assets/ScriptbleObjects/WaveConfig/Wave_02.asset",
                waveNumber: 2,
                durationSeconds: 55f,
                buildingPhaseDuration: 20f,
                isBossWave: false,
                isFinalWave: false,
                spawnGroups: new List<EnemySpawnGroup>
                {
                    new(regularEnemy, count: 14, startDelay: 0f, spawnInterval: 2.8f, batchSize: 1),
                    new(fastEnemy, count: 8, startDelay: 12f, spawnInterval: 2.0f, batchSize: 1)
                },
                log: log);

            // Wave 3
            GetOrCreateWaveConfig(
                path: "Assets/ScriptbleObjects/WaveConfig/Wave_03.asset",
                waveNumber: 3,
                durationSeconds: 65f,
                buildingPhaseDuration: 20f,
                isBossWave: false,
                isFinalWave: false,
                spawnGroups: new List<EnemySpawnGroup>
                {
                    new(regularEnemy, count: 16, startDelay: 0f, spawnInterval: 2.5f, batchSize: 1),
                    new(fastEnemy, count: 10, startDelay: 10f, spawnInterval: 1.8f, batchSize: 1),
                    new(tankEnemy, count: 4, startDelay: 20f, spawnInterval: 6.0f, batchSize: 1)
                },
                log: log);

            // Wave 4
            GetOrCreateWaveConfig(
                path: "Assets/ScriptbleObjects/WaveConfig/Wave_04.asset",
                waveNumber: 4,
                durationSeconds: 75f,
                buildingPhaseDuration: 20f,
                isBossWave: false,
                isFinalWave: false,
                spawnGroups: new List<EnemySpawnGroup>
                {
                    new(regularEnemy, count: 15, startDelay: 0f, spawnInterval: 2.2f, batchSize: 1),
                    new(archerEnemy, count: 8, startDelay: 10f, spawnInterval: 4.0f, batchSize: 1),
                    new(tankEnemy, count: 6, startDelay: 18f, spawnInterval: 5.0f, batchSize: 1),
                    new(fastEnemy, count: 12, startDelay: 30f, spawnInterval: 1.5f, batchSize: 1)
                },
                log: log);

            // Wave 5 (Boss Wave)
            GetOrCreateWaveConfig(
                path: "Assets/ScriptbleObjects/WaveConfig/Wave_05_Boss.asset",
                waveNumber: 5,
                durationSeconds: 120f,
                buildingPhaseDuration: 25f,
                isBossWave: true,
                isFinalWave: true,
                spawnGroups: new List<EnemySpawnGroup>
                {
                    new(bossEnemy, count: 1, startDelay: 2f, spawnInterval: 1.0f, batchSize: 1),
                    new(archerEnemy, count: 8, startDelay: 10f, spawnInterval: 4.5f, batchSize: 1),
                    new(tankEnemy, count: 6, startDelay: 15f, spawnInterval: 5.0f, batchSize: 1),
                    new(fastEnemy, count: 15, startDelay: 25f, spawnInterval: 2.0f, batchSize: 1)
                },
                log: log);
        }

        private static void SaveSpritePng(string relativePath, Texture2D tex, System.Text.StringBuilder log)
        {
            byte[] bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            string fullPath = Path.Combine(Application.dataPath, relativePath.Substring("Assets/".Length));
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(fullPath, bytes);
            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            log.AppendLine($"Generated Sprite: {relativePath}");
        }

        private static EnemyData GetOrCreateEnemyData(
            string path,
            string id,
            string displayName,
            bool isBoss,
            int maxHP,
            float speed,
            float minSpeedRatio,
            int damage,
            float attackInterval,
            float attackRange,
            bool isRanged,
            Vector3 visualScale,
            Color tintColor,
            int honorReward,
            Sprite sprite,
            int minCoins,
            int maxCoins,
            float coinDropChance,
            float diamondDropChance,
            int diamondAmount,
            System.Text.StringBuilder log)
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(existing, path);
                log.AppendLine($"Created EnemyData: {path}");
            }

            var so = new SerializedObject(existing);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_isBoss").boolValue = isBoss;
            so.FindProperty("_maxHP").intValue = maxHP;
            so.FindProperty("_moveSpeed").floatValue = speed;
            so.FindProperty("_minSpeedRatio").floatValue = minSpeedRatio;
            so.FindProperty("_damage").intValue = damage;
            so.FindProperty("_attackInterval").floatValue = attackInterval;
            so.FindProperty("_attackRange").floatValue = attackRange;
            so.FindProperty("_isRanged").boolValue = isRanged;
            so.FindProperty("_visualScale").vector3Value = visualScale;
            so.FindProperty("_tintColor").colorValue = tintColor;
            so.FindProperty("_honorReward").intValue = honorReward;
            if (sprite != null) so.FindProperty("_sprite").objectReferenceValue = sprite;

            var lootProp = so.FindProperty("_lootTable");
            if (lootProp != null)
            {
                lootProp.FindPropertyRelative("_minCoins").intValue = minCoins;
                lootProp.FindPropertyRelative("_maxCoins").intValue = maxCoins;
                lootProp.FindPropertyRelative("_coinDropChance").floatValue = coinDropChance;
                lootProp.FindPropertyRelative("_diamondDropChance").floatValue = diamondDropChance;
                lootProp.FindPropertyRelative("_diamondAmount").intValue = diamondAmount;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static WaveConfig GetOrCreateWaveConfig(
            string path,
            int waveNumber,
            float durationSeconds,
            float buildingPhaseDuration,
            bool isBossWave,
            bool isFinalWave,
            List<EnemySpawnGroup> spawnGroups,
            System.Text.StringBuilder log)
        {
            var existing = AssetDatabase.LoadAssetAtPath<WaveConfig>(path);
            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<WaveConfig>();
                AssetDatabase.CreateAsset(existing, path);
                log.AppendLine($"Created WaveConfig: {path}");
            }

            var so = new SerializedObject(existing);
            so.FindProperty("_waveNumber").intValue = waveNumber;
            so.FindProperty("_durationSeconds").floatValue = durationSeconds;
            so.FindProperty("_buildingPhaseDuration").floatValue = buildingPhaseDuration;
            so.FindProperty("_isBossWave").boolValue = isBossWave;
            so.FindProperty("_isFinalWave").boolValue = isFinalWave;

            var groupsProp = so.FindProperty("_spawnGroups");
            groupsProp.ClearArray();
            for (int i = 0; i < spawnGroups.Count; i++)
            {
                groupsProp.InsertArrayElementAtIndex(i);
                var elem = groupsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("_enemyData").objectReferenceValue = spawnGroups[i].EnemyData;
                elem.FindPropertyRelative("_count").intValue = spawnGroups[i].Count;
                elem.FindPropertyRelative("_startDelay").floatValue = spawnGroups[i].StartDelay;
                elem.FindPropertyRelative("_spawnInterval").floatValue = spawnGroups[i].SpawnInterval;
                elem.FindPropertyRelative("_spawnBatchSize").intValue = spawnGroups[i].SpawnBatchSize;
            }

            if (spawnGroups.Count > 0)
            {
                so.FindProperty("_enemyData").objectReferenceValue = spawnGroups[0].EnemyData;
                so.FindProperty("_totalEnemiesToSpawn").intValue = spawnGroups[0].Count;
                so.FindProperty("_initialSpawnDelay").floatValue = spawnGroups[0].StartDelay;
                so.FindProperty("_spawnInterval").floatValue = spawnGroups[0].SpawnInterval;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static Texture2D CreateEnemyFastTexture()
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color empty = Color.clear;
            Color skin = new Color(0.35f, 0.78f, 0.22f, 1f);
            Color shadowSkin = new Color(0.22f, 0.55f, 0.15f, 1f);
            Color clothes = new Color(0.38f, 0.26f, 0.16f, 1f);
            Color eyes = new Color(1f, 0.95f, 0.15f, 1f);
            Color blade = new Color(0.85f, 0.88f, 0.92f, 1f);

            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    tex.SetPixel(x, y, empty);

            for (int y = 17; y <= 24; y++)
                for (int x = 12; x <= 19; x++)
                    tex.SetPixel(x, y, skin);

            tex.SetPixel(10, 22, skin); tex.SetPixel(11, 21, skin); tex.SetPixel(11, 22, skin);
            tex.SetPixel(21, 22, skin); tex.SetPixel(20, 21, skin); tex.SetPixel(20, 22, skin);
            tex.SetPixel(14, 21, eyes); tex.SetPixel(17, 21, eyes);

            for (int y = 9; y <= 16; y++)
                for (int x = 11; x <= 20; x++)
                    tex.SetPixel(x, y, (x == 11 || x == 20 || y == 9) ? shadowSkin : clothes);

            for (int y = 4; y <= 8; y++)
            {
                tex.SetPixel(10, y, skin); tex.SetPixel(11, y, skin);
                tex.SetPixel(18, y, skin); tex.SetPixel(19, y, skin);
            }
            tex.SetPixel(9, 4, shadowSkin); tex.SetPixel(20, 4, shadowSkin);

            tex.SetPixel(22, 13, blade); tex.SetPixel(23, 14, blade); tex.SetPixel(24, 15, blade);
            tex.SetPixel(21, 12, shadowSkin);

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateEnemyTankTexture()
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color empty = Color.clear;
            Color plate = new Color(0.32f, 0.36f, 0.42f, 1f);
            Color darkPlate = new Color(0.20f, 0.22f, 0.27f, 1f);
            Color trim = new Color(0.65f, 0.22f, 0.22f, 1f);
            Color visor = new Color(1f, 0.15f, 0.15f, 1f);

            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    tex.SetPixel(x, y, empty);

            for (int y = 18; y <= 26; y++)
                for (int x = 11; x <= 20; x++)
                    tex.SetPixel(x, y, plate);

            tex.SetPixel(9, 27, darkPlate); tex.SetPixel(10, 26, darkPlate);
            tex.SetPixel(22, 27, darkPlate); tex.SetPixel(21, 26, darkPlate);

            for (int x = 13; x <= 18; x++)
                tex.SetPixel(x, 22, visor);

            for (int y = 9; y <= 18; y++)
                for (int x = 8; x <= 23; x++)
                    tex.SetPixel(x, y, (x <= 10 || x >= 21 || y == 9) ? darkPlate : plate);

            for (int y = 10; y <= 18; y++)
                for (int x = 5; x <= 9; x++)
                    tex.SetPixel(x, y, (x == 5 || x == 9 || y == 10 || y == 18) ? trim : darkPlate);

            for (int y = 3; y <= 8; y++)
            {
                for (int x = 9; x <= 13; x++) tex.SetPixel(x, y, darkPlate);
                for (int x = 18; x <= 22; x++) tex.SetPixel(x, y, darkPlate);
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateEnemyArcherTexture()
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color empty = Color.clear;
            Color hood = new Color(0.25f, 0.16f, 0.38f, 1f);
            Color bone = new Color(0.88f, 0.86f, 0.82f, 1f);
            Color dark = new Color(0.12f, 0.08f, 0.18f, 1f);
            Color wood = new Color(0.55f, 0.35f, 0.18f, 1f);
            Color bowstring = new Color(0.85f, 0.85f, 0.9f, 1f);

            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    tex.SetPixel(x, y, empty);

            for (int y = 18; y <= 27; y++)
                for (int x = 11; x <= 20; x++)
                    tex.SetPixel(x, y, hood);

            for (int y = 19; y <= 23; y++)
                for (int x = 13; x <= 18; x++)
                    tex.SetPixel(x, y, bone);

            tex.SetPixel(14, 21, dark); tex.SetPixel(17, 21, dark);

            for (int y = 6; y <= 18; y++)
                for (int x = 11; x <= 20; x++)
                    tex.SetPixel(x, y, (x == 11 || x == 20) ? dark : hood);

            tex.SetPixel(13, 5, bone); tex.SetPixel(14, 5, bone);
            tex.SetPixel(17, 5, bone); tex.SetPixel(18, 5, bone);

            for (int y = 8; y <= 24; y++)
            {
                int bowX = 23 + (int)(Mathf.Sin((y - 8) / 16f * Mathf.PI) * 3f);
                tex.SetPixel(bowX, y, wood);
                tex.SetPixel(23, y, bowstring);
            }

            for (int x = 16; x <= 25; x++) tex.SetPixel(x, 16, bone);

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateEnemyBossTexture()
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color empty = Color.clear;
            Color obsidian = new Color(0.14f, 0.10f, 0.12f, 1f);
            Color armor = new Color(0.24f, 0.14f, 0.16f, 1f);
            Color magma = new Color(1f, 0.45f, 0.05f, 1f);
            Color goldHorn = new Color(0.85f, 0.65f, 0.15f, 1f);
            Color eyeGlow = new Color(1f, 0.95f, 0.2f, 1f);

            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    tex.SetPixel(x, y, empty);

            for (int y = 23; y <= 30; y++)
            {
                int leftH = 11 - (y - 23);
                if (leftH >= 5) { tex.SetPixel(leftH, y, goldHorn); tex.SetPixel(leftH + 1, y, obsidian); }
                int rightH = 20 + (y - 23);
                if (rightH <= 26) { tex.SetPixel(rightH, y, goldHorn); tex.SetPixel(rightH - 1, y, obsidian); }
            }

            for (int y = 18; y <= 26; y++)
                for (int x = 11; x <= 20; x++)
                    tex.SetPixel(x, y, armor);

            tex.SetPixel(13, 27, goldHorn); tex.SetPixel(15, 28, goldHorn);
            tex.SetPixel(16, 28, goldHorn); tex.SetPixel(18, 27, goldHorn);

            tex.SetPixel(13, 22, eyeGlow); tex.SetPixel(14, 22, magma);
            tex.SetPixel(17, 22, magma); tex.SetPixel(18, 22, eyeGlow);

            for (int y = 7; y <= 18; y++)
            {
                for (int x = 6; x <= 25; x++)
                {
                    Color c = (x <= 9 || x >= 22) ? obsidian : armor;
                    tex.SetPixel(x, y, c);
                }
            }

            for (int y = 11; y <= 15; y++)
                for (int x = 14; x <= 17; x++)
                    tex.SetPixel(x, y, magma);

            for (int y = 2; y <= 6; y++)
            {
                for (int x = 8; x <= 13; x++) tex.SetPixel(x, y, obsidian);
                for (int x = 18; x <= 23; x++) tex.SetPixel(x, y, obsidian);
            }

            tex.Apply();
            return tex;
        }

        private static void SetupSampleScene(System.Text.StringBuilder log)
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            EditorSceneManager.MarkSceneDirty(scene);
            log.AppendLine($"Opened scene: {scenePath}");

            // 1. Ensure [InputManager] exists in scene
            var inputManagerGo = GameObject.Find("[InputManager]");
            if (inputManagerGo == null)
            {
                inputManagerGo = new GameObject("[InputManager]");
                inputManagerGo.AddComponent<InputManager>();
                log.AppendLine("Created [InputManager] in SampleScene");
            }

            // Ensure EventSystem exists in scene
            var eventSystemGo = GameObject.Find("EventSystem") ?? UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>()?.gameObject;
            if (eventSystemGo == null)
            {
                eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                log.AppendLine("Created EventSystem in SampleScene");
            }

            // Ensure [RunConfig] exists in scene
            var runConfigGo = GameObject.Find("[RunConfig]");
            if (runConfigGo == null)
            {
                runConfigGo = new GameObject("[RunConfig]");
                runConfigGo.AddComponent<RunConfig>();
                log.AppendLine("Created [RunConfig] in SampleScene");
            }
            var defaultKnight = AssetDatabase.LoadAssetAtPath<Game.Data.KnightData>("Assets/ScriptbleObjects/Data/DefaultKnight.asset");
            var defaultArena = AssetDatabase.LoadAssetAtPath<Game.Data.ArenaData>("Assets/ScriptbleObjects/Data/DefaultArena.asset");
            var defaultWeapon = AssetDatabase.LoadAssetAtPath<Game.Data.WeaponSelectionData>("Assets/ScriptbleObjects/Data/DefaultWeapon.asset");
            if (runConfigGo != null)
            {
                var rc = runConfigGo.GetComponent<RunConfig>();
                if (rc != null)
                {
                    var so = new SerializedObject(rc);
                    so.FindProperty("_selectedKnight").objectReferenceValue = defaultKnight;
                    so.FindProperty("_selectedArena").objectReferenceValue = defaultArena;
                    so.FindProperty("_selectedWeapon").objectReferenceValue = defaultWeapon;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(rc);
                    log.AppendLine("Wired [RunConfig] in SampleScene");
                }
            }

            // 2. Ensure BuildingGhost exists in scene
            var ghostGo = GameObject.Find("BuildingGhost");
            if (ghostGo == null)
            {
                var ghostPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BuildingGhost.prefab");
                if (ghostPrefab != null)
                {
                    ghostGo = (GameObject)PrefabUtility.InstantiatePrefab(ghostPrefab);
                }
                else
                {
                    ghostGo = new GameObject("BuildingGhost");
                    ghostGo.AddComponent<SpriteRenderer>();
                    ghostGo.AddComponent<BuildingGhostView>();
                }
                log.AppendLine("Created BuildingGhost in SampleScene");
            }
            var ghostView = ghostGo.GetComponent<BuildingGhostView>();

            // Find key scene objects
            var mainCamera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            var gridMap = UnityEngine.Object.FindFirstObjectByType<GridMap>();
            var wallet = UnityEngine.Object.FindFirstObjectByType<PlayerWallet>();
            var castle = UnityEngine.Object.FindFirstObjectByType<Castle>();
            var waveController = UnityEngine.Object.FindFirstObjectByType<WaveController>();
            var contextMenuUI = UnityEngine.Object.FindFirstObjectByType<BuildingContextMenuUI>(FindObjectsInactive.Include);
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<Projectile>("Assets/Prefabs/Projectile.prefab");
            var upgradeDb = AssetDatabase.LoadAssetAtPath<UpgradeDatabase>("Assets/ScriptbleObjects/Upgrades/UpgradeDatabase.asset");
            var ballistaData = AssetDatabase.LoadAssetAtPath<HeavyBallistaData>("Assets/ScriptbleObjects/Weapons/HeavyBallistaData.asset");

            // Ensure FirePoint on Castle
            Transform firePoint = null;
            if (castle != null)
            {
                var fpTransform = castle.transform.Find("FirePoint");
                if (fpTransform == null)
                {
                    var fpGo = new GameObject("FirePoint");
                    fpGo.transform.SetParent(castle.transform, false);
                    fpGo.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                    firePoint = fpGo.transform;
                    log.AppendLine("Created FirePoint child on Castle");
                }
                else
                {
                    firePoint = fpTransform;
                }
            }

            // Wire [GameSystems] components
            var gameSystemsGo = GameObject.Find("[GameSystems]");
            if (gameSystemsGo != null)
            {
                // BuildingManager
                var bm = gameSystemsGo.GetComponent<BuildingManager>();
                if (bm != null)
                {
                    var so = new SerializedObject(bm);
                    so.FindProperty("_gridMap").objectReferenceValue = gridMap;
                    so.FindProperty("_ghostView").objectReferenceValue = ghostView;
                    so.FindProperty("_wallet").objectReferenceValue = wallet;
                    so.FindProperty("_mainCamera").objectReferenceValue = mainCamera;
                    if (contextMenuUI != null) so.FindProperty("_contextMenuUI").objectReferenceValue = contextMenuUI;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(bm);
                    log.AppendLine("Wired BuildingManager on [GameSystems]");
                }

                // ProjectilePool
                var pool = gameSystemsGo.GetComponent<ProjectilePool>();
                if (pool != null)
                {
                    var so = new SerializedObject(pool);
                    so.FindProperty("_projectilePrefab").objectReferenceValue = projectilePrefab;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(pool);
                    log.AppendLine("Wired ProjectilePool on [GameSystems]");
                }

                // RunProgressionManager
                var prog = gameSystemsGo.GetComponent<RunProgressionManager>();
                if (prog != null)
                {
                    var so = new SerializedObject(prog);
                    so.FindProperty("_castle").objectReferenceValue = castle;
                    so.FindProperty("_upgradeDatabase").objectReferenceValue = upgradeDb;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(prog);
                    log.AppendLine("Wired RunProgressionManager on [GameSystems]");
                }

                // GameManager
                var gm = gameSystemsGo.GetComponent<GameManager>();
                if (gm != null)
                {
                    var pauseUI = UnityEngine.Object.FindFirstObjectByType<PauseUI>(FindObjectsInactive.Include);
                    var gameOverUI = UnityEngine.Object.FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
                    var so = new SerializedObject(gm);
                    if (pauseUI != null) so.FindProperty("_pauseUI").objectReferenceValue = pauseUI;
                    if (gameOverUI != null) so.FindProperty("_gameOverUI").objectReferenceValue = gameOverUI;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(gm);
                    log.AppendLine("Wired GameManager on [GameSystems]");
                }
            }

            // Wire HeavyBallista
            var ballista = UnityEngine.Object.FindFirstObjectByType<HeavyBallista>();
            if (ballista != null)
            {
                var so = new SerializedObject(ballista);
                so.FindProperty("_castle").objectReferenceValue = castle;
                so.FindProperty("_data").objectReferenceValue = ballistaData;
                so.FindProperty("_firePoint").objectReferenceValue = firePoint;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(ballista);
                log.AppendLine("Wired HeavyBallista");
            }

            // Wire WeaponAimUI
            var aimUI = UnityEngine.Object.FindFirstObjectByType<WeaponAimUI>(FindObjectsInactive.Include);
            if (aimUI != null)
            {
                var so = new SerializedObject(aimUI);
                so.FindProperty("_mainCamera").objectReferenceValue = mainCamera;
                so.FindProperty("_weapon").objectReferenceValue = ballista;
                var txtProp = so.FindProperty("_cooldownText");
                if (txtProp != null && txtProp.objectReferenceValue == null)
                {
                    var tmp = aimUI.GetComponentInChildren<TMPro.TMP_Text>(true);
                    if (tmp != null) txtProp.objectReferenceValue = tmp;
                }

                var lineProp = so.FindProperty("_aimLine");
                var reticleProp = so.FindProperty("_aimReticle");
                if ((lineProp != null && lineProp.objectReferenceValue == null) ||
                    (reticleProp != null && reticleProp.objectReferenceValue == null))
                {
                    Transform visualsRoot = ballista != null ? ballista.transform.Find("WeaponAimVisuals") : null;
                    if (visualsRoot == null)
                    {
                        var visualsGo = new GameObject("WeaponAimVisuals");
                        if (ballista != null) visualsGo.transform.SetParent(ballista.transform, false);
                        visualsRoot = visualsGo.transform;
                    }

                    Transform lineTransform = visualsRoot.Find("AimLine");
                    LineRenderer lineRenderer = null;
                    if (lineTransform == null)
                    {
                        var lineGo = new GameObject("AimLine");
                        lineGo.transform.SetParent(visualsRoot, false);
                        lineRenderer = lineGo.AddComponent<LineRenderer>();
                        lineRenderer.useWorldSpace = true;
                        lineRenderer.positionCount = 2;
                        lineRenderer.startWidth = 0.05f;
                        lineRenderer.endWidth = 0.05f;
                        Color aimCol = new Color(1f, 0.85f, 0.2f, 0.9f);
                        lineRenderer.startColor = aimCol;
                        lineRenderer.endColor = aimCol;
                        lineRenderer.sortingLayerName = "Default";
                        lineRenderer.sortingOrder = 100;
                        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                        if (shader != null) lineRenderer.material = new Material(shader) { color = aimCol };
                    }
                    else
                    {
                        lineRenderer = lineTransform.GetComponent<LineRenderer>();
                    }

                    Transform reticleTransform = visualsRoot.Find("AimReticle");
                    if (reticleTransform == null)
                    {
                        var reticleGo = new GameObject("AimReticle");
                        reticleGo.transform.SetParent(visualsRoot, false);
                        reticleTransform = reticleGo.transform;
                        var reticleLine = reticleGo.AddComponent<LineRenderer>();
                        reticleLine.useWorldSpace = false;
                        reticleLine.loop = true;
                        reticleLine.positionCount = 48;
                        reticleLine.startWidth = 0.05f;
                        reticleLine.endWidth = 0.05f;
                        Color aimCol = new Color(1f, 0.85f, 0.2f, 0.9f);
                        reticleLine.startColor = aimCol;
                        reticleLine.endColor = aimCol;
                        reticleLine.sortingLayerName = "Default";
                        reticleLine.sortingOrder = 101;
                        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                        if (shader != null) reticleLine.material = new Material(shader) { color = aimCol };
                        int segs = reticleLine.positionCount;
                        for (int i = 0; i < segs; i++)
                        {
                            float angle = (i / (float)segs) * Mathf.PI * 2f;
                            reticleLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.45f, Mathf.Sin(angle) * 0.45f, 0f));
                        }
                    }

                    if (lineProp != null && lineRenderer != null) lineProp.objectReferenceValue = lineRenderer;
                    if (reticleProp != null && reticleTransform != null) reticleProp.objectReferenceValue = reticleTransform;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(aimUI);
                log.AppendLine("Wired WeaponAimUI");
            }

            // Wire CameraController
            var camCtrl = UnityEngine.Object.FindFirstObjectByType<CameraController>();
            if (camCtrl != null)
            {
                var so = new SerializedObject(camCtrl);
                so.FindProperty("_targetCamera").objectReferenceValue = mainCamera;
                so.FindProperty("_gridMap").objectReferenceValue = gridMap;
                if (castle != null) so.FindProperty("_initialTarget").objectReferenceValue = castle.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(camCtrl);
                log.AppendLine("Wired CameraController");
            }

            // Wire HUD
            var healthBar = UnityEngine.Object.FindFirstObjectByType<CastleHealthBar>(FindObjectsInactive.Include);
            if (healthBar != null && castle != null)
            {
                var so = new SerializedObject(healthBar);
                so.FindProperty("_castle").objectReferenceValue = castle;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(healthBar);
                log.AppendLine("Wired CastleHealthBar");
            }

            var walletUI = UnityEngine.Object.FindFirstObjectByType<WalletUI>(FindObjectsInactive.Include);
            if (walletUI != null && wallet != null)
            {
                var so = new SerializedObject(walletUI);
                so.FindProperty("_wallet").objectReferenceValue = wallet;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(walletUI);
                log.AppendLine("Wired WalletUI");
            }

            var timerUI = UnityEngine.Object.FindFirstObjectByType<WaveTimerUI>(FindObjectsInactive.Include);
            if (timerUI != null && waveController != null)
            {
                var so = new SerializedObject(timerUI);
                so.FindProperty("_waveController").objectReferenceValue = waveController;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(timerUI);
                log.AppendLine("Wired WaveTimerUI");
            }

            // Wire WaveController with Waves 1-5
            if (waveController != null)
            {
                var so = new SerializedObject(waveController);
                var listProp = so.FindProperty("_waveConfigs");
                listProp.ClearArray();
                for (int i = 1; i <= 5; i++)
                {
                    string wavePath = i == 5
                        ? "Assets/ScriptbleObjects/WaveConfig/Wave_05_Boss.asset"
                        : $"Assets/ScriptbleObjects/WaveConfig/Wave_0{i}.asset";
                    var waveCfg = AssetDatabase.LoadAssetAtPath<WaveConfig>(wavePath);
                    if (waveCfg != null)
                    {
                        listProp.InsertArrayElementAtIndex(listProp.arraySize);
                        listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = waveCfg;
                    }
                }
                var wave1 = AssetDatabase.LoadAssetAtPath<WaveConfig>("Assets/ScriptbleObjects/WaveConfig/Wave_01.asset");
                if (wave1 != null) so.FindProperty("_initialWaveConfig").objectReferenceValue = wave1;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(waveController);
                log.AppendLine("Wired WaveController with Waves 1-5");
            }

            // Also wire MainGame prefab
            var mainGamePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MainGame.prefab");
            if (mainGamePrefab != null)
            {
                var prefabWaveCtrl = mainGamePrefab.GetComponentInChildren<WaveController>(true);
                if (prefabWaveCtrl != null)
                {
                    var so = new SerializedObject(prefabWaveCtrl);
                    var listProp = so.FindProperty("_waveConfigs");
                    listProp.ClearArray();
                    for (int i = 1; i <= 5; i++)
                    {
                        string wavePath = i == 5
                            ? "Assets/ScriptbleObjects/WaveConfig/Wave_05_Boss.asset"
                            : $"Assets/ScriptbleObjects/WaveConfig/Wave_0{i}.asset";
                        var waveCfg = AssetDatabase.LoadAssetAtPath<WaveConfig>(wavePath);
                        if (waveCfg != null)
                        {
                            listProp.InsertArrayElementAtIndex(listProp.arraySize);
                            listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = waveCfg;
                        }
                    }
                    var wave1 = AssetDatabase.LoadAssetAtPath<WaveConfig>("Assets/ScriptbleObjects/WaveConfig/Wave_01.asset");
                    if (wave1 != null) so.FindProperty("_initialWaveConfig").objectReferenceValue = wave1;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(mainGamePrefab);
                    PrefabUtility.SavePrefabAsset(mainGamePrefab);
                    log.AppendLine("Wired WaveController on MainGame.prefab with Waves 1-5");
                }
            }

            EditorSceneManager.SaveScene(scene);
            log.AppendLine("Saved SampleScene.unity");
        }

        private static void SetupMainMenu(System.Text.StringBuilder log)
        {
            string scenePath = "Assets/Scenes/MainMenu.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            EditorSceneManager.MarkSceneDirty(scene);
            log.AppendLine($"Opened scene: {scenePath}");

            // Ensure [InputManager] exists in MainMenu
            var inputManagerGo = GameObject.Find("[InputManager]");
            if (inputManagerGo == null)
            {
                inputManagerGo = new GameObject("[InputManager]");
                inputManagerGo.AddComponent<InputManager>();
                log.AppendLine("Created [InputManager] in MainMenu");
            }

            // Ensure [RunConfig] exists in MainMenu
            var runConfigGo = GameObject.Find("[RunConfig]");
            if (runConfigGo == null)
            {
                runConfigGo = new GameObject("[RunConfig]");
                runConfigGo.AddComponent<RunConfig>();
                log.AppendLine("Created [RunConfig] in MainMenu");
            }
            var defaultKnight = AssetDatabase.LoadAssetAtPath<Game.Data.KnightData>("Assets/ScriptbleObjects/Data/DefaultKnight.asset");
            var defaultArena = AssetDatabase.LoadAssetAtPath<Game.Data.ArenaData>("Assets/ScriptbleObjects/Data/DefaultArena.asset");
            var defaultWeapon = AssetDatabase.LoadAssetAtPath<Game.Data.WeaponSelectionData>("Assets/ScriptbleObjects/Data/DefaultWeapon.asset");
            if (runConfigGo != null)
            {
                var rc = runConfigGo.GetComponent<RunConfig>();
                if (rc != null)
                {
                    var so = new SerializedObject(rc);
                    so.FindProperty("_selectedKnight").objectReferenceValue = defaultKnight;
                    so.FindProperty("_selectedArena").objectReferenceValue = defaultArena;
                    so.FindProperty("_selectedWeapon").objectReferenceValue = defaultWeapon;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(rc);
                    log.AppendLine("Wired [RunConfig] in MainMenu");
                }
            }

            EditorSceneManager.SaveScene(scene);
            log.AppendLine("Saved MainMenu.unity");
        }

        [MenuItem("Tools/Verify Wave and Enemies")]
        public static string VerifyWaveAndEnemies()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Verifying Enemies & Wave System ===");

            // 1. Verify EnemyData assets
            var enemyPaths = new[]
            {
                "Assets/ScriptbleObjects/Enemy/Enemy.asset",
                "Assets/ScriptbleObjects/Enemy/Enemy_Fast.asset",
                "Assets/ScriptbleObjects/Enemy/Enemy_Tank.asset",
                "Assets/ScriptbleObjects/Enemy/Enemy_Archer.asset",
                "Assets/ScriptbleObjects/Enemy/Enemy_Boss.asset"
            };

            foreach (var path in enemyPaths)
            {
                var ed = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                if (ed == null)
                {
                    sb.AppendLine($"[FAIL] Missing EnemyData: {path}");
                    continue;
                }
                sb.AppendLine($"[PASS] Enemy '{ed.DisplayName}' ({ed.Id}): HP={ed.MaxHP}, Spd={ed.MoveSpeed}, Dmg={ed.Damage}, Range={ed.AttackRange}, Boss={ed.IsBoss}, Sprite={(ed.Sprite != null ? ed.Sprite.name : "null")}, Scale={ed.VisualScale}, Coins={ed.LootTable.MinCoins}-{ed.LootTable.MaxCoins}, Dia={ed.LootTable.DiamondAmount} (chance={ed.LootTable.DiamondDropChance:P0})");
            }

            // 2. Verify WaveConfigs
            for (int i = 1; i <= 5; i++)
            {
                string path = i == 5 ? "Assets/ScriptbleObjects/WaveConfig/Wave_05_Boss.asset" : $"Assets/ScriptbleObjects/WaveConfig/Wave_0{i}.asset";
                var wc = AssetDatabase.LoadAssetAtPath<WaveConfig>(path);
                if (wc == null)
                {
                    sb.AppendLine($"[FAIL] Missing WaveConfig: {path}");
                    continue;
                }
                sb.AppendLine($"[PASS] Wave {wc.WaveNumber}: Duration={wc.DurationSeconds}s, BuildPhase={wc.BuildingPhaseDuration}s, Groups={wc.SpawnGroups.Count}, TotalEnemies={wc.TotalEnemiesToSpawn}, BossWave={wc.IsBossWave}, FinalWave={wc.IsFinalWave}");
                for (int g = 0; g < wc.SpawnGroups.Count; g++)
                {
                    var grp = wc.SpawnGroups[g];
                    sb.AppendLine($"       - Group {g + 1}: {grp.Count}x {(grp.EnemyData != null ? grp.EnemyData.DisplayName : "null")} (Delay: {grp.StartDelay}s, Interval: {grp.SpawnInterval}s)");
                }
            }

            // 3. Verify Scene WaveController
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var waveCtrl = UnityEngine.Object.FindFirstObjectByType<WaveController>();
            if (waveCtrl != null)
            {
                sb.AppendLine($"[PASS] Scene WaveController found. Configured waves: {waveCtrl.WaveConfigs.Count}");
                sb.AppendLine($"[PASS] Current wave: {waveCtrl.CurrentWaveNumber}, Index: {waveCtrl.CurrentWaveIndex}");
            }
            else
            {
                sb.AppendLine("[FAIL] WaveController not found in SampleScene");
            }

            // 4. Test Enemy Runtime Initialization from Prefab
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Regular.prefab");
            var fastData = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptbleObjects/Enemy/Enemy_Fast.asset");
            var bossData = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptbleObjects/Enemy/Enemy_Boss.asset");
            if (prefab != null && fastData != null && bossData != null)
            {
                var testGo = UnityEngine.Object.Instantiate(prefab);
                var enemyComp = testGo.GetComponent<Game.Enemies.Enemy>();

                enemyComp.Initialize(fastData, null, null, null);
                sb.AppendLine($"[PASS] Runtime Fast Enemy: HP={enemyComp.MaxHP}, Speed={enemyComp.CurrentSpeed}, Scale={testGo.transform.localScale}");

                enemyComp.Initialize(bossData, null, null, null);
                sb.AppendLine($"[PASS] Runtime Boss Enemy: HP={enemyComp.MaxHP}, Speed={enemyComp.CurrentSpeed}, Scale={testGo.transform.localScale}");

                UnityEngine.Object.DestroyImmediate(testGo);
            }

            sb.AppendLine("=== Verification Complete ===");
            Debug.Log(sb.ToString());
            return sb.ToString();
        }
    }
}
#endif
