#if UNITY_EDITOR
using System;
using System.IO;
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
                "Assets/ScriptbleObjects/Upgrades"
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
    }
}
#endif
