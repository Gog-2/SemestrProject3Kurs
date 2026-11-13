#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Game.Buildings;
using Game.Data;
using Game.Enemies.Data;
using Game.Units;

namespace Game.Editor
{
    public static class GigaSpriteApplier
    {
        private static readonly HashSet<string> s_twoByTwoBuildings = new(StringComparer.OrdinalIgnoreCase)
        {
            "building_siege_ballista.png",
            "building_siege_cannon.png",
            "building_siege_mortar.png",
            "building_archer_tower.png",
            "building_temple.png",
            "building_builder_hut.png",
            "building_diamond_cavern.png"
        };

        [MenuItem("Tools/GIGA Apply All Sprites", false, 150)]
        public static void ApplyAllSpritesMenu()
        {
            ApplyAllSprites();
        }

        public static void ApplyAllSpritesCLI()
        {
            string log = ApplyAllSprites();
            Debug.Log(log);
        }

        public static string ApplyAllSprites()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== GIGA Apply All Sprites ===");

            ConfigureAllTextureImporters(sb);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ApplyEnemies(sb);
            ApplyKnights(sb);
            ApplyWeapons(sb);
            ApplyWalls(sb);
            ApplyTurretAndGoldMine(sb);
            ApplySummonUnits(sb);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Run master content generator to re-wire all building ScriptableObjects & prefabs
            sb.AppendLine("[Applier] Regenerating all GIGA game data with new sprites...");
            string genLog = GigaContentGenerator.GenerateAllContent();
            sb.AppendLine(genLog);

            sb.AppendLine("=== GIGA Apply All Sprites Complete ===");
            Debug.Log(sb.ToString());
            return sb.ToString();
        }

        public static void ConfigureAllTextureImporters(System.Text.StringBuilder log)
        {
            string[] searchDirs =
            {
                "Assets/Visual/Sprite/Enemies",
                "Assets/Visual/Sprite/Knights",
                "Assets/Visual/Sprite/Weapons",
                "Assets/Visual/Sprite/Buildings",
                "Assets/Visual/Sprite/Units"
            };

            int count = 0;
            foreach (string dir in searchDirs)
            {
                if (!Directory.Exists(dir)) continue;

                string[] files = Directory.GetFiles(dir, "*.png", SearchOption.TopDirectoryOnly);
                foreach (string file in files)
                {
                    string assetPath = file.Replace('\\', '/');
                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer == null) continue;

                    string filename = Path.GetFileName(assetPath);
                    float ppu = s_twoByTwoBuildings.Contains(filename) ? 32f : 64f;

                    bool changed = false;
                    if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
                    if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
                    if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; changed = true; }
                    if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
                    if (Math.Abs(importer.spritePixelsPerUnit - ppu) > 0.01f) { importer.spritePixelsPerUnit = ppu; changed = true; }
                    if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
                    if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }

                    if (changed)
                    {
                        importer.SaveAndReimport();
                        count++;
                    }
                }
            }

            log.AppendLine($"[Importers] Verified/updated TextureImporters for {count} sprite textures (PPU: 64 standard / 32 for 2x2 buildings).");
        }

        private static void ApplyEnemies(System.Text.StringBuilder log)
        {
            void SetEnemy(string assetPath, string spriteName)
            {
                var ed = AssetDatabase.LoadAssetAtPath<EnemyData>(assetPath);
                var s = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Visual/Sprite/Enemies/{spriteName}.png");
                if (ed != null && s != null)
                {
                    ed.SetVisuals(s, Color.white);
                    EditorUtility.SetDirty(ed);
                }
            }

            SetEnemy("Assets/ScriptbleObjects/Enemy/Enemy.asset", "enemy_regular");
            SetEnemy("Assets/ScriptbleObjects/Enemy/Enemy_Fast.asset", "enemy_fast");
            SetEnemy("Assets/ScriptbleObjects/Enemy/Enemy_Archer.asset", "enemy_archer");
            SetEnemy("Assets/ScriptbleObjects/Enemy/Enemy_Tank.asset", "enemy_tank");
            SetEnemy("Assets/ScriptbleObjects/Enemy/Enemy_Boss.asset", "enemy_boss");

            log.AppendLine("[Enemies] Configured sprites and reset tint color to white for 5 enemy ScriptableObjects.");
        }

        private static void ApplyKnights(System.Text.StringBuilder log)
        {
            void SetKnight(string assetPath, string spriteName)
            {
                var kd = AssetDatabase.LoadAssetAtPath<KnightData>(assetPath);
                var s = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Visual/Sprite/Knights/{spriteName}.png");
                if (kd != null && s != null)
                {
                    kd.SetPortrait(s);
                    EditorUtility.SetDirty(kd);
                }
            }

            SetKnight("Assets/ScriptbleObjects/Data/DefaultKnight.asset", "knight_hador");
            SetKnight("Assets/ScriptbleObjects/Data/Knight_Finrod.asset", "knight_finrod");
            SetKnight("Assets/ScriptbleObjects/Data/Knight_Borin.asset", "knight_borin");
            SetKnight("Assets/ScriptbleObjects/Data/Knight_Aradan.asset", "knight_aradan");
            SetKnight("Assets/ScriptbleObjects/Data/Knight_Baron.asset", "knight_baron");

            log.AppendLine("[Knights] Assigned 5 knight portraits.");
        }

        private static void ApplyWeapons(System.Text.StringBuilder log)
        {
            void SetWeapon(string assetPath, string spriteName)
            {
                var wd = AssetDatabase.LoadAssetAtPath<WeaponSelectionData>(assetPath);
                var s = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Visual/Sprite/Weapons/{spriteName}.png");
                if (wd != null && s != null)
                {
                    wd.SetIcon(s);
                    EditorUtility.SetDirty(wd);
                }
            }

            SetWeapon("Assets/ScriptbleObjects/Data/DefaultWeapon.asset", "weapon_heavy_ballista");
            SetWeapon("Assets/ScriptbleObjects/Data/Weapon_Mortar.asset", "weapon_alchemical_mortar");
            SetWeapon("Assets/ScriptbleObjects/Data/Weapon_TarCannon.asset", "weapon_tar_cannon");
            SetWeapon("Assets/ScriptbleObjects/Data/Weapon_Orthanc.asset", "weapon_orthanc");
            SetWeapon("Assets/ScriptbleObjects/Data/Weapon_ArcherVolley.asset", "weapon_archer_volley");

            log.AppendLine("[Weapons] Assigned 5 castle weapon icons.");
        }

        private static void ApplyWalls(System.Text.StringBuilder log)
        {
            var single = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Visual/Sprite/Buildings/building_wall_single.png");
            var horiz = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Visual/Sprite/Buildings/building_wall_horizontal.png");
            var vert = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Visual/Sprite/Buildings/building_wall_vertical.png");
            var cross = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Visual/Sprite/Buildings/building_wall_cross.png");

            const string prefabPath = "Assets/Prefabs/Buildings/Building_Wall.prefab";
            GameObject wallGo = PrefabUtility.LoadPrefabContents(prefabPath);
            if (wallGo != null)
            {
                try
                {
                    var wallComp = wallGo.GetComponent<WallBuilding>();
                    if (wallComp != null)
                    {
                        wallComp.SetWallSprites(single, horiz, vert, cross);
                    }
                    var sr = wallGo.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sprite = single;
                        sr.color = Color.white;
                    }
                    PrefabUtility.SaveAsPrefabAsset(wallGo, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(wallGo);
                }
            }

            var wallData = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/ScriptbleObjects/Buildings/Building_Wall.asset");
            if (wallData != null)
            {
                wallData.SetIcon(single);
                EditorUtility.SetDirty(wallData);
            }

            log.AppendLine("[Walls] Assigned single, horizontal, vertical, and cross sprites to Building_Wall prefab & asset.");
        }

        private static void ApplyTurretAndGoldMine(System.Text.StringBuilder log)
        {
            void UpdateBld(string prefabPath, string dataPath, string spriteName)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Visual/Sprite/Buildings/{spriteName}.png");
                if (s == null) return;

                GameObject go = PrefabUtility.LoadPrefabContents(prefabPath);
                if (go != null)
                {
                    try
                    {
                        var sr = go.GetComponent<SpriteRenderer>();
                        if (sr == null) sr = go.GetComponentInChildren<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.sprite = s;
                            sr.color = Color.white;
                        }
                        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(go);
                    }
                }

                var data = AssetDatabase.LoadAssetAtPath<BuildingData>(dataPath);
                if (data != null)
                {
                    data.SetIcon(s);
                    EditorUtility.SetDirty(data);
                }
            }

            UpdateBld("Assets/Prefabs/Buildings/Building_Turret.prefab", "Assets/ScriptbleObjects/Buildings/Building_Turret.asset", "building_turret");
            UpdateBld("Assets/Prefabs/Buildings/Building_GoldMine.prefab", "Assets/ScriptbleObjects/Buildings/Building_GoldMine.asset", "building_gold_mine");
            log.AppendLine("[Buildings] Assigned Turret and GoldMine sprites & icons.");
        }

        private static void ApplySummonUnits(System.Text.StringBuilder log)
        {
            var skel = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Visual/Sprite/Units/unit_skeleton.png");
            if (skel != null) AllyUnit.SetSkeletonSprite(skel);

            var sheep = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Visual/Sprite/Units/unit_sheep.png");
            if (sheep != null) DecoySheep.SetSheepSprite(sheep);

            log.AppendLine("[Units] Registered Skeleton and Sheep summon sprites.");
        }
    }
}
#endif
