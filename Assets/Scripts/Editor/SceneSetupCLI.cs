#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Game.Core;
using Game.Data;
using Game.Buildings;
using Game.Projectiles;
using Game.Weapons;
using Game.Upgrades;
using Game.UI;
using Game.UI.BuildingUI;
using Game.UI.MainMenu;
using Game.CameraSystem;
using Game.WaveSystem;
using Game.Enemies.Data;
using Game.Enemies.Spawning;
using Game.Spells;
using Game.Units;

namespace Game.Editor
{
    public static class SceneSetupCLI
    {
        [MenuItem("Tools/Run Scene Setup & Assignment", false, 200)]
        public static void RunSetupMenu()
        {
            RunSetup();
        }

        public static string ReconfigureSampleSceneOnly()
        {
            var log = new System.Text.StringBuilder();
            log.AppendLine("=== Reconfiguring SampleScene & Links ===");
            SpellEffectAssetMigrator.EnsureAllSpellEffectsLinked();
            SetupSampleScene(log);
            AssetDatabase.SaveAssets();
            string validation = ValidateScene();
            log.AppendLine(validation);
            string result = log.ToString();
            Debug.Log(result);
            return result;
        }

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

            // Strip dangerous Alchemy HierarchyHeaders from prefabs to prevent build-time GameObject deletion
            StripDangerousHierarchyHeaders(log);

            UpdateHudPrefabCardIcons(log);

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

            var bootstrapper = UnityEngine.Object.FindAnyObjectByType<SceneBootstrapper>();
            Check("SceneBootstrapper", bootstrapper != null, bootstrapper != null ? "Assigned on [GameSystems]" : "MISSING");

            var headersInScene = UnityEngine.Object.FindObjectsByType<Alchemy.Hierarchy.HierarchyObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Check("HierarchyHeader in Scene", headersInScene.Length == 0,
                headersInScene.Length == 0 ? "Clean (no dangerous stripping components)" : $"FOUND {headersInScene.Length} HierarchyObjects! Will cause deletions in build!");

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

            var allyPool = UnityEngine.Object.FindFirstObjectByType<AllyUnitPool>();
            Check("AllyUnitPool", allyPool != null, allyPool != null ? "Assigned in scene" : "MISSING");

            var scrollMgr = UnityEngine.Object.FindFirstObjectByType<SpellScrollManager>();
            Check("SpellScrollManager", scrollMgr != null, scrollMgr != null ? "Assigned in scene" : "MISSING");

            var spellCaster = UnityEngine.Object.FindFirstObjectByType<SpellCaster>();
            if (spellCaster != null)
            {
                var so = new SerializedObject(spellCaster);
                Check("SpellCaster._mainCamera", so.FindProperty("_mainCamera").objectReferenceValue != null, "Assigned");
            }
            else Check("SpellCaster", false, "Not found");

            string[] spellGuids = AssetDatabase.FindAssets("t:SpellData", new[] { "Assets/ScriptbleObjects/Spells" });
            Check("SpellData assets", spellGuids.Length >= 5, $"Found {spellGuids.Length}");
            foreach (string guid in spellGuids)
            {
                string spPath = AssetDatabase.GUIDToAssetPath(guid);
                SpellData spData = AssetDatabase.LoadAssetAtPath<SpellData>(spPath);
                if (spData != null)
                {
                    Check($"SpellData.{spData.name}.Effect", spData.Effect != null, spData.Effect != null ? spData.Effect.GetType().Name : "MISSING");
                }
            }

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
                Check("WaveController._castle", so.FindProperty("_castle").objectReferenceValue != null, "Assigned");
                Check("WaveController._spawner", so.FindProperty("_spawner").objectReferenceValue != null, "Assigned");
                Check("WaveController._initialWaveConfig", so.FindProperty("_initialWaveConfig").objectReferenceValue != null, "Assigned");
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
                AssetDatabase.CreateAsset(data, path);
                data.PopulateWithDefaultUpgrades();
                log.AppendLine($"Created: {path}");
            }
            else
            {
                bool hasNulls = existing.AllUpgrades.Count == 0;
                if (!hasNulls)
                {
                    for (int i = 0; i < existing.AllUpgrades.Count; i++)
                    {
                        if (existing.AllUpgrades[i] == null)
                        {
                            hasNulls = true;
                            break;
                        }
                    }
                }

                if (hasNulls)
                {
                    existing.PopulateWithDefaultUpgrades();
                    EditorUtility.SetDirty(existing);
                    log.AppendLine($"Repaired UpgradeDatabase sub-assets: {path}");
                }
                else
                {
                    log.AppendLine($"Already exists: {path}");
                }
            }
        }

        private static bool IsCorruptedText(string value)
        {
            return string.IsNullOrEmpty(value) || value.Contains("??");
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
                knight.InitializeRuntime("hador_oplot", "Хадор Оплот", "Рыцарь-Бастион", "«Камень не молит о пощаде, он просто стоит.»", 2000, 125, 4, new[] { "Стены" }, new[] { "Несокрушимый фундамент" }, true, 0);
                AssetDatabase.CreateAsset(knight, knightPath);
                log.AppendLine($"Created: {knightPath}");
            }
            else if (IsCorruptedText(knight.KnightName) || IsCorruptedText(knight.Title) || IsCorruptedText(knight.Quote))
            {
                knight.InitializeRuntime("hador_oplot", "Хадор Оплот", "Рыцарь-Бастион", "«Камень не молит о пощаде, он просто стоит.»", 2000, 125, 4, new[] { "Стены" }, new[] { "Несокрушимый фундамент" }, true, 0);
                EditorUtility.SetDirty(knight);
                log.AppendLine($"Repaired UTF-8 text: {knightPath}");
            }

            string arenaPath = "Assets/ScriptbleObjects/Data/DefaultArena.asset";
            var arena = AssetDatabase.LoadAssetAtPath<Game.Data.ArenaData>(arenaPath);
            if (arena == null)
            {
                arena = ScriptableObject.CreateInstance<Game.Data.ArenaData>();
                arena.InitializeRuntime("arena_plains", "Зелёные равнины", "Базовая арена. Клетки равнин и воды, замедляющие врагов.", "SampleScene", true, 0);
                AssetDatabase.CreateAsset(arena, arenaPath);
                log.AppendLine($"Created: {arenaPath}");
            }
            else if (IsCorruptedText(arena.ArenaName) || IsCorruptedText(arena.Description))
            {
                arena.InitializeRuntime("arena_plains", "Зелёные равнины", "Базовая арена. Клетки равнин и воды, замедляющие врагов.", "SampleScene", true, 0);
                EditorUtility.SetDirty(arena);
                log.AppendLine($"Repaired UTF-8 text: {arenaPath}");
            }

            string weaponPath = "Assets/ScriptbleObjects/Data/DefaultWeapon.asset";
            var weapon = AssetDatabase.LoadAssetAtPath<Game.Data.WeaponSelectionData>(weaponPath);
            if (weapon == null)
            {
                var ballistaData = AssetDatabase.LoadAssetAtPath<HeavyBallistaData>("Assets/ScriptbleObjects/Weapons/HeavyBallistaData.asset");
                weapon = ScriptableObject.CreateInstance<Game.Data.WeaponSelectionData>();
                weapon.InitializeRuntime("heavy_ballista", "Тяжёлая баллиста", "Выстреливает массивными болтами, пробивающими всех врагов и отталкивающими их на одну клетку.", true, 0, ballistaData);
                AssetDatabase.CreateAsset(weapon, weaponPath);
                log.AppendLine($"Created: {weaponPath}");
            }
            else if (IsCorruptedText(weapon.WeaponName) || IsCorruptedText(weapon.Description))
            {
                var ballistaData = AssetDatabase.LoadAssetAtPath<HeavyBallistaData>("Assets/ScriptbleObjects/Weapons/HeavyBallistaData.asset");
                weapon.InitializeRuntime("heavy_ballista", "Тяжёлая баллиста", "Выстреливает массивными болтами, пробивающими всех врагов и отталкивающими их на одну клетку.", true, 0, ballistaData);
                EditorUtility.SetDirty(weapon);
                log.AppendLine($"Repaired UTF-8 text: {weaponPath}");
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
                maxHP: 22,
                speed: 1.75f,
                minSpeedRatio: 0.8f,
                damage: 4,
                attackInterval: 1.5f,
                attackRange: 0.8f,
                isRanged: false,
                visualScale: Vector3.one,
                tintColor: Color.white,
                honorReward: 10,
                sprite: regularSprite,
                minCoins: 4,
                maxCoins: 6,
                coinDropChance: 1.0f,
                diamondDropChance: 0.01f,
                minDiamonds: 1,
                maxDiamonds: 1,
                log: log);

            // Fast Goblin
            GetOrCreateEnemyData(
                path: "Assets/ScriptbleObjects/Enemy/Enemy_Fast.asset",
                id: "fast_goblin",
                displayName: "Гоблин-бегун",
                isBoss: false,
                maxHP: 14,
                speed: 2.8f,
                minSpeedRatio: 0.8f,
                damage: 3,
                attackInterval: 0.9f,
                attackRange: 0.7f,
                isRanged: false,
                visualScale: new Vector3(0.85f, 0.85f, 1f),
                tintColor: new Color(0.75f, 1f, 0.45f, 1f),
                honorReward: 8,
                sprite: fastSprite != null ? fastSprite : regularSprite,
                minCoins: 3,
                maxCoins: 5,
                coinDropChance: 1.0f,
                diamondDropChance: 0.01f,
                minDiamonds: 1,
                maxDiamonds: 1,
                log: log);

            // Armored Tank
            GetOrCreateEnemyData(
                path: "Assets/ScriptbleObjects/Enemy/Enemy_Tank.asset",
                id: "armored_tank",
                displayName: "Тяжелый латник",
                isBoss: false,
                maxHP: 115,
                speed: 1.15f,
                minSpeedRatio: 0.8f,
                damage: 12,
                attackInterval: 2.2f,
                attackRange: 1.0f,
                isRanged: false,
                visualScale: new Vector3(1.35f, 1.35f, 1f),
                tintColor: new Color(0.9f, 0.45f, 0.45f, 1f),
                honorReward: 25,
                sprite: tankSprite != null ? tankSprite : regularSprite,
                minCoins: 12,
                maxCoins: 18,
                coinDropChance: 1.0f,
                diamondDropChance: 0.01f,
                minDiamonds: 1,
                maxDiamonds: 1,
                log: log);

            // Ranged Archer
            GetOrCreateEnemyData(
                path: "Assets/ScriptbleObjects/Enemy/Enemy_Archer.asset",
                id: "ranged_archer",
                displayName: "Лучник тьмы",
                isBoss: false,
                maxHP: 24,
                speed: 1.6f,
                minSpeedRatio: 0.8f,
                damage: 5,
                attackInterval: 2.0f,
                attackRange: 3.5f,
                isRanged: true,
                visualScale: new Vector3(0.95f, 0.95f, 1f),
                tintColor: new Color(0.75f, 0.5f, 1f, 1f),
                honorReward: 14,
                sprite: archerSprite != null ? archerSprite : regularSprite,
                minCoins: 6,
                maxCoins: 9,
                coinDropChance: 1.0f,
                diamondDropChance: 0.01f,
                minDiamonds: 1,
                maxDiamonds: 1,
                log: log);

            // Arena Boss
            GetOrCreateEnemyData(
                path: "Assets/ScriptbleObjects/Enemy/Enemy_Boss.asset",
                id: "arena_boss",
                displayName: "Владыка рейда",
                isBoss: true,
                maxHP: 950,
                speed: 1.2f,
                minSpeedRatio: 0.9f,
                damage: 30,
                attackInterval: 2.5f,
                attackRange: 1.5f,
                isRanged: false,
                visualScale: new Vector3(2.2f, 2.2f, 1f),
                tintColor: new Color(1f, 0.8f, 0.25f, 1f),
                honorReward: 200,
                sprite: bossSprite != null ? bossSprite : regularSprite,
                minCoins: 50,
                maxCoins: 80,
                coinDropChance: 1.0f,
                diamondDropChance: 1.0f,
                minDiamonds: 3,
                maxDiamonds: 5,
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
                durationSeconds: 50f,
                buildingPhaseDuration: 25f,
                isBossWave: false,
                isFinalWave: false,
                spawnGroups: new List<EnemySpawnGroup>
                {
                    new(regularEnemy, count: 14, startDelay: 1.5f, spawnInterval: 2.2f, batchSize: 1)
                },
                log: log);

            // Wave 2
            GetOrCreateWaveConfig(
                path: "Assets/ScriptbleObjects/WaveConfig/Wave_02.asset",
                waveNumber: 2,
                durationSeconds: 60f,
                buildingPhaseDuration: 25f,
                isBossWave: false,
                isFinalWave: false,
                spawnGroups: new List<EnemySpawnGroup>
                {
                    new(regularEnemy, count: 14, startDelay: 0f, spawnInterval: 2.5f, batchSize: 1),
                    new(fastEnemy, count: 8, startDelay: 10f, spawnInterval: 2.2f, batchSize: 1)
                },
                log: log);

            // Wave 3
            GetOrCreateWaveConfig(
                path: "Assets/ScriptbleObjects/WaveConfig/Wave_03.asset",
                waveNumber: 3,
                durationSeconds: 65f,
                buildingPhaseDuration: 25f,
                isBossWave: false,
                isFinalWave: false,
                spawnGroups: new List<EnemySpawnGroup>
                {
                    new(regularEnemy, count: 16, startDelay: 0f, spawnInterval: 2.2f, batchSize: 1),
                    new(fastEnemy, count: 10, startDelay: 10f, spawnInterval: 2.0f, batchSize: 1),
                    new(tankEnemy, count: 4, startDelay: 18f, spawnInterval: 5.5f, batchSize: 1)
                },
                log: log);

            // Wave 4
            GetOrCreateWaveConfig(
                path: "Assets/ScriptbleObjects/WaveConfig/Wave_04.asset",
                waveNumber: 4,
                durationSeconds: 75f,
                buildingPhaseDuration: 25f,
                isBossWave: false,
                isFinalWave: false,
                spawnGroups: new List<EnemySpawnGroup>
                {
                    new(regularEnemy, count: 15, startDelay: 0f, spawnInterval: 2.2f, batchSize: 1),
                    new(archerEnemy, count: 8, startDelay: 10f, spawnInterval: 3.5f, batchSize: 1),
                    new(tankEnemy, count: 6, startDelay: 16f, spawnInterval: 4.5f, batchSize: 1),
                    new(fastEnemy, count: 12, startDelay: 24f, spawnInterval: 1.6f, batchSize: 1)
                },
                log: log);

            // Wave 5 (Boss Wave)
            GetOrCreateWaveConfig(
                path: "Assets/ScriptbleObjects/WaveConfig/Wave_05_Boss.asset",
                waveNumber: 5,
                durationSeconds: 120f,
                buildingPhaseDuration: 30f,
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
            int minDiamonds,
            int maxDiamonds,
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
                lootProp.FindPropertyRelative("_minDiamondAmount").intValue = minDiamonds;
                lootProp.FindPropertyRelative("_maxDiamondAmount").intValue = maxDiamonds;
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
                    var knightProp = so.FindProperty("_selectedKnight");
                    var arenaProp = so.FindProperty("_selectedArena");
                    var weaponProp = so.FindProperty("_selectedWeapon");
                    if (knightProp.objectReferenceValue == null) knightProp.objectReferenceValue = defaultKnight;
                    if (arenaProp.objectReferenceValue == null) arenaProp.objectReferenceValue = defaultArena;
                    if (weaponProp.objectReferenceValue == null) weaponProp.objectReferenceValue = defaultWeapon;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(rc);
                    log.AppendLine("Verified/Wired [RunConfig] defaults in SampleScene (preserved existing non-null selections)");
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
            var spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
            var ballista = UnityEngine.Object.FindFirstObjectByType<HeavyBallista>();
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
                    if (ballista != null) so.FindProperty("_castleWeapon").objectReferenceValue = ballista;
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

                // AllyUnitPool
                var allyPool = gameSystemsGo.GetComponent<AllyUnitPool>();
                if (allyPool == null)
                {
                    allyPool = gameSystemsGo.AddComponent<AllyUnitPool>();
                    log.AppendLine("Added AllyUnitPool on [GameSystems]");
                }
                EditorUtility.SetDirty(allyPool);

                // SpellScrollManager & SpellCaster
                if (gameSystemsGo.GetComponent<SpellScrollManager>() == null)
                {
                    gameSystemsGo.AddComponent<SpellScrollManager>();
                    log.AppendLine("Added SpellScrollManager on [GameSystems]");
                }
                var spellCaster = gameSystemsGo.GetComponent<SpellCaster>() ?? gameSystemsGo.AddComponent<SpellCaster>();
                {
                    var so = new SerializedObject(spellCaster);
                    so.FindProperty("_mainCamera").objectReferenceValue = mainCamera;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(spellCaster);
                    log.AppendLine("Wired SpellCaster on [GameSystems]");
                }

                // RunProgressionManager
                var prog = gameSystemsGo.GetComponent<RunProgressionManager>();
                if (prog != null)
                {
                    var so = new SerializedObject(prog);
                    so.FindProperty("_castle").objectReferenceValue = castle;
                    so.FindProperty("_upgradeDatabase").objectReferenceValue = upgradeDb;
                    if (ballista != null) so.FindProperty("_castleWeapon").objectReferenceValue = ballista;
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

                // SceneBootstrapper
                var bootstrapper = gameSystemsGo.GetComponent<SceneBootstrapper>();
                if (bootstrapper == null)
                {
                    bootstrapper = gameSystemsGo.AddComponent<SceneBootstrapper>();
                    log.AppendLine("Added SceneBootstrapper on [GameSystems]");
                }
                EditorUtility.SetDirty(gameSystemsGo);
            }

            // Wire HeavyBallista
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
                        lineRenderer.sortingLayerName = "Effects";
                        lineRenderer.sortingOrder = 100;
                        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                        if (shader != null) lineRenderer.material = new Material(shader) { color = aimCol };
                    }
                    else
                    {
                        lineRenderer = lineTransform.GetComponent<LineRenderer>();
                        if (lineRenderer != null)
                        {
                            lineRenderer.sortingLayerName = "Effects";
                            lineRenderer.sortingOrder = 100;
                        }
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
                        reticleLine.sortingLayerName = "Effects";
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
                    else
                    {
                        var existingReticleLine = reticleTransform.GetComponent<LineRenderer>();
                        if (existingReticleLine != null)
                        {
                            existingReticleLine.sortingLayerName = "Effects";
                            existingReticleLine.sortingOrder = 101;
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
                if (castle != null) so.FindProperty("_castle").objectReferenceValue = castle;
                if (spawner != null) so.FindProperty("_spawner").objectReferenceValue = spawner;
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
                    var prefabCastle = mainGamePrefab.GetComponentInChildren<Castle>(true);
                    var prefabSpawner = mainGamePrefab.GetComponentInChildren<EnemySpawner>(true);
                    if (prefabCastle != null) so.FindProperty("_castle").objectReferenceValue = prefabCastle;
                    if (prefabSpawner != null) so.FindProperty("_spawner").objectReferenceValue = prefabSpawner;
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
                    var diffProp = so.FindProperty("_selectedDifficulty");
                    if (diffProp != null) diffProp.enumValueIndex = (int)DifficultyMode.Base;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(rc);
                    log.AppendLine("Wired [RunConfig] in MainMenu");
                }
            }

            // Populate MainMenu selection lists
            var knightSelectionUI = UnityEngine.Object.FindFirstObjectByType<KnightSelectionUI>(FindObjectsInactive.Include);
            if (knightSelectionUI != null)
            {
                AssignAllAssetsToList(knightSelectionUI, "_allKnights", defaultKnight);
                EditorUtility.SetDirty(knightSelectionUI);
                log.AppendLine("Wired KnightSelectionUI._allKnights");
            }

            var arenaSelectionUI = UnityEngine.Object.FindFirstObjectByType<ArenaSelectionUI>(FindObjectsInactive.Include);
            if (arenaSelectionUI != null)
            {
                AssignAllAssetsToList(arenaSelectionUI, "_allArenas", defaultArena);
                EditorUtility.SetDirty(arenaSelectionUI);
                log.AppendLine("Wired ArenaSelectionUI._allArenas");
            }

            var weaponSelectionUI = UnityEngine.Object.FindFirstObjectByType<WeaponSelectionUI>(FindObjectsInactive.Include);
            if (weaponSelectionUI != null)
            {
                AssignAllAssetsToList(weaponSelectionUI, "_allWeapons", defaultWeapon);
                EditorUtility.SetDirty(weaponSelectionUI);
                log.AppendLine("Wired WeaponSelectionUI._allWeapons");
            }

            var mainMenuMgr = UnityEngine.Object.FindFirstObjectByType<MainMenuManager>(FindObjectsInactive.Include);
            var shopUI = UnityEngine.Object.FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
            if (shopUI != null && mainMenuMgr != null)
            {
                var so = new SerializedObject(shopUI);
                var mgrProp = so.FindProperty("_mainMenuManager");
                if (mgrProp != null)
                {
                    mgrProp.objectReferenceValue = mainMenuMgr;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(shopUI);
                    log.AppendLine("Wired ShopUI._mainMenuManager");
                }
            }

            // Setup Difficulty Selection UI on WeaponPanel
            SetupMainMenuDifficultyUI(log);

            EditorSceneManager.SaveScene(scene);
            log.AppendLine("Saved MainMenu.unity");
        }

        private static void AssignAllAssetsToList<T>(UnityEngine.Object target, string propertyName, T preferredFirst) where T : UnityEngine.Object
        {
            var items = new List<T>();
            if (preferredFirst != null) items.Add(preferredFirst);

            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null && !items.Contains(asset))
                {
                    items.Add(asset);
                }
            }

            var so = new SerializedObject(target);
            var prop = so.FindProperty(propertyName);
            if (prop != null && prop.isArray)
            {
                prop.ClearArray();
                for (int i = 0; i < items.Count; i++)
                {
                    prop.InsertArrayElementAtIndex(i);
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetupMainMenuDifficultyUI(System.Text.StringBuilder log)
        {
            var canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null) return;

            Transform weaponPanel = canvasGo.transform.Find("WeaponPanel");
            if (weaponPanel == null) return;

            SetAnchorY(weaponPanel.Find("WeaponName"), 0.76f);
            SetAnchorY(weaponPanel.Find("PrevBtn"), 0.65f);
            SetAnchorY(weaponPanel.Find("NextBtn"), 0.65f);
            Transform weaponDesc = weaponPanel.Find("WeaponDesc");
            if (weaponDesc != null && weaponDesc is RectTransform descRt)
            {
                SetAnchorY(descRt, 0.60f);
                descRt.sizeDelta = new Vector2(descRt.sizeDelta.x, 90f);
            }
            SetAnchorY(weaponPanel.Find("ActionBtn"), 0.46f);
            SetAnchorY(weaponPanel.Find("StartRunBtn"), 0.11f);

            TMP_Text sampleTmp = weaponPanel.GetComponentInChildren<TMP_Text>(true);
            TMP_FontAsset font = sampleTmp != null ? sampleTmp.font : TMP_Settings.defaultFontAsset;

            Transform existingSection = weaponPanel.Find("DifficultySection");
            if (existingSection != null)
            {
                UnityEngine.Object.DestroyImmediate(existingSection.gameObject);
            }

            var sectionGo = new GameObject("DifficultySection", typeof(RectTransform), typeof(DifficultySelectionUI));
            sectionGo.transform.SetParent(weaponPanel, false);
            var sectionRt = (RectTransform)sectionGo.transform;
            sectionRt.anchorMin = Vector2.zero;
            sectionRt.anchorMax = Vector2.one;
            sectionRt.offsetMin = Vector2.zero;
            sectionRt.offsetMax = Vector2.zero;
            sectionRt.SetAsLastSibling();

            // Place before StartRunBtn so visual/focus order makes sense
            Transform startBtn = weaponPanel.Find("StartRunBtn");
            if (startBtn != null) startBtn.SetAsLastSibling();

            CreateMenuLabel(sectionGo.transform, "DifficultyHeader", "СЛОЖНОСТЬ ЗАБЕГА", new Vector2(0.50f, 0.37f), new Vector2(450f, 34f), 24f, Color.white, font);
            var (baseBtn, baseTxt) = CreateMenuButton(sectionGo.transform, "DiffBtn_Base", "Базовая", new Vector2(0.34f, 0.30f), new Vector2(220f, 48f), 20f, font);
            var (hardBtn, hardTxt) = CreateMenuButton(sectionGo.transform, "DiffBtn_Hard", "Усложнённая", new Vector2(0.50f, 0.30f), new Vector2(220f, 48f), 20f, font);
            var (legBtn, legTxt) = CreateMenuButton(sectionGo.transform, "DiffBtn_Legendary", "Легендарная", new Vector2(0.66f, 0.30f), new Vector2(220f, 48f), 20f, font);
            TMP_Text diffDesc = CreateMenuLabel(sectionGo.transform, "DifficultyDesc", "Стандартный баланс волны и наград", new Vector2(0.50f, 0.22f), new Vector2(920f, 48f), 18f, new Color(0.85f, 0.85f, 0.85f, 1f), font);

            var diffUI = sectionGo.GetComponent<DifficultySelectionUI>();
            diffUI.InitializeReferences(baseBtn, hardBtn, legBtn, baseTxt, hardTxt, legTxt, diffDesc);
            EditorUtility.SetDirty(diffUI);
            log.AppendLine("Configured DifficultySelectionUI on MainMenu Canvas/WeaponPanel");
        }

        private static void SetAnchorY(Transform target, float anchorY)
        {
            if (target is RectTransform rt)
            {
                rt.anchorMin = new Vector2(rt.anchorMin.x, anchorY);
                rt.anchorMax = new Vector2(rt.anchorMax.x, anchorY);
                rt.anchoredPosition = Vector2.zero;
            }
        }

        private static TMP_Text CreateMenuLabel(Transform parent, string name, string text, Vector2 anchor, Vector2 size, float fontSize, Color color, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            return tmp;
        }

        private static (Button button, TMP_Text label) CreateMenuButton(Transform parent, string name, string text, Vector2 anchor, Vector2 size, float fontSize, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.20f, 0.35f, 0.55f, 1f);
            var btn = go.GetComponent<Button>();

            var labelGo = new GameObject("Text", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return (btn, tmp);
        }

        private static void UpdateHudPrefabCardIcons(System.Text.StringBuilder log)
        {
            const string hudPath = "Assets/Prefabs/HUD.prefab";
            var prefabRoot = PrefabUtility.LoadPrefabContents(hudPath);
            if (prefabRoot == null)
            {
                log.AppendLine($"[WARN] Could not load {hudPath}");
                return;
            }

            try
            {
                UpgradeCardUI[] cards = prefabRoot.GetComponentsInChildren<UpgradeCardUI>(true);
                for (int i = 0; i < cards.Length; i++)
                {
                    UpgradeCardUI card = cards[i];
                    if (card == null) continue;

                    Transform titleT = card.transform.Find("Title");
                    if (titleT is RectTransform titleRt)
                    {
                        SetAnchorY(titleRt, 0.80f);
                        titleRt.sizeDelta = new Vector2(titleRt.sizeDelta.x, 50f);
                    }

                    Transform descT = card.transform.Find("Desc");
                    if (descT is RectTransform descRt)
                    {
                        SetAnchorY(descRt, 0.40f);
                        descRt.sizeDelta = new Vector2(descRt.sizeDelta.x, 125f);
                    }

                    Transform iconT = card.transform.Find("Icon");
                    GameObject iconGo;
                    if (iconT == null)
                    {
                        iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                        iconGo.transform.SetParent(card.transform, false);
                    }
                    else
                    {
                        iconGo = iconT.gameObject;
                    }

                    var iconRt = (RectTransform)iconGo.transform;
                    iconRt.anchorMin = new Vector2(0.5f, 0.65f);
                    iconRt.anchorMax = new Vector2(0.5f, 0.65f);
                    iconRt.anchoredPosition = Vector2.zero;
                    iconRt.sizeDelta = new Vector2(56f, 56f);

                    var iconImg = iconGo.GetComponent<Image>() ?? iconGo.AddComponent<Image>();
                    iconImg.preserveAspect = true;
                    iconImg.enabled = iconImg.sprite != null;

                    var cardSo = new SerializedObject(card);
                    var iconProp = cardSo.FindProperty("_iconImage");
                    if (iconProp != null)
                    {
                        iconProp.objectReferenceValue = iconImg;
                        cardSo.ApplyModifiedPropertiesWithoutUndo();
                    }
                    EditorUtility.SetDirty(card);
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, hudPath);
                log.AppendLine($"Updated UpgradeCardUI Icon slots on {hudPath} ({cards.Length} cards)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        public static void StripDangerousHierarchyHeaders(System.Text.StringBuilder log)
        {
            log.AppendLine("=== Checking and Stripping Dangerous HierarchyHeaders ===");
            string[] prefabs = new[]
            {
                "Assets/Prefabs/MainGame.prefab",
                "Assets/Prefabs/HUD.prefab",
                "Assets/Prefabs/Movement.prefab"
            };

            foreach (var path in prefabs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var headers = prefab.GetComponentsInChildren<Alchemy.Hierarchy.HierarchyObject>(true);
                if (headers != null && headers.Length > 0)
                {
                    foreach (var h in headers)
                    {
                        log.AppendLine($"Removing {h.GetType().Name} from {h.gameObject.name} in {path}");
                        UnityEngine.Object.DestroyImmediate(h, true);
                    }
                    EditorUtility.SetDirty(prefab);
                    PrefabUtility.SavePrefabAsset(prefab);
                    log.AppendLine($"Saved stripped prefab: {path}");
                }
                else
                {
                    log.AppendLine($"[CLEAN] No HierarchyObject on {path}");
                }
            }
        }
    }
}
#endif
