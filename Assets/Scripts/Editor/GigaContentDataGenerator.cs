#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Game.Buildings;
using Game.Data;
using Game.Enemies.Data;
using Game.Projectiles;
using Game.Spells;
using Game.Units;
using Game.Upgrades;
using Game.Weapons;

namespace Game.Editor
{
    internal static class GigaContentDataGenerator
    {
        private const string PoolConfigAssetPath = "Assets/ScriptbleObjects/Buildings/BuildingPoolConfig.asset";

        private static readonly string[] KnightAssetPaths =
        {
            "Assets/ScriptbleObjects/Data/DefaultKnight.asset",
            "Assets/ScriptbleObjects/Data/Knight_Finrod.asset",
            "Assets/ScriptbleObjects/Data/Knight_Borin.asset",
            "Assets/ScriptbleObjects/Data/Knight_Aradan.asset",
            "Assets/ScriptbleObjects/Data/Knight_Baron.asset"
        };

        private static readonly string[] WeaponAssetPaths =
        {
            "Assets/ScriptbleObjects/Data/DefaultWeapon.asset",
            "Assets/ScriptbleObjects/Data/Weapon_Mortar.asset",
            "Assets/ScriptbleObjects/Data/Weapon_TarCannon.asset",
            "Assets/ScriptbleObjects/Data/Weapon_Orthanc.asset",
            "Assets/ScriptbleObjects/Data/Weapon_ArcherVolley.asset"
        };

        public static BuildingPoolConfig LoadPoolConfig()
        {
            return AssetDatabase.LoadAssetAtPath<BuildingPoolConfig>(PoolConfigAssetPath);
        }

        public static List<KnightData> LoadAllKnights() => LoadAssetList<KnightData>(KnightAssetPaths);
        public static List<WeaponSelectionData> LoadAllWeapons() => LoadAssetList<WeaponSelectionData>(WeaponAssetPaths);

        private static List<T> LoadAssetList<T>(string[] paths) where T : Object
        {
            var result = new List<T>(paths.Length);
            for (int i = 0; i < paths.Length; i++)
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(paths[i]);
                if (asset != null) result.Add(asset);
            }
            return result;
        }

        public struct GeneratedContent
        {
            public List<BuildingData> AllBuildings;
            public Dictionary<string, BuildingData> BuildingsById;
            public List<KnightData> Knights;
            public List<WeaponSelectionData> Weapons;
            public BuildingPoolConfig PoolConfig;
        }

        public static GeneratedContent GenerateAllData(System.Text.StringBuilder log)
        {
            Sprite squareSprite = GigaAssetUtility.EnsureGeneratedSprite("Assets/Sprites/Generated/CellSquare.png", false, Color.white);
            Sprite circleSprite = GigaAssetUtility.EnsureGeneratedSprite("Assets/Sprites/Generated/CircleIcon.png", true, Color.white);

            Dictionary<SpellKind, SpellData> spells = GenerateSpells(circleSprite, log);
            ProjectileData siegeBolt = GenerateSiegeBoltProjectile();

            var buildings = new Dictionary<string, BuildingData>();
            var buildingList = new List<BuildingData>();

            void Register(BuildingData d)
            {
                if (d == null) return;
                buildings[d.Id] = d;
                if (!buildingList.Contains(d)) buildingList.Add(d);
            }

            // Existing Phase 0 preserved buildings
            var goldMine = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/ScriptbleObjects/Buildings/Building_GoldMine.asset");
            var wall = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/ScriptbleObjects/Buildings/Building_Wall.asset");
            var turret = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/ScriptbleObjects/Buildings/Building_Turret.asset");
            if (goldMine != null) goldMine.SetIcon(GetBuildingSprite("gold_mine", squareSprite));
            if (wall != null) wall.SetIcon(GetBuildingSprite("wall_single", squareSprite));
            if (turret != null) turret.SetIcon(GetBuildingSprite("turret", squareSprite));
            Register(goldMine);
            Register(wall);
            Register(turret);

            var skelSprite = GetUnitSprite("skeleton");
            if (skelSprite != null) AllyUnit.SetSkeletonSprite(skelSprite);
            var sheepSprite = GetUnitSprite("sheep");
            if (sheepSprite != null) DecoySheep.SetSheepSprite(sheepSprite);

            GenerateArcherBuildings(squareSprite, Register);
            GenerateMeleeBuildings(squareSprite, Register);
            GenerateMageBuildings(squareSprite, spells, Register);
            GenerateSiegeBuildings(squareSprite, siegeBolt, Register);
            GenerateSupportBuildings(squareSprite, Register);
            GenerateEconomyBuildings(squareSprite, Register);
            GenerateControlBuildings(squareSprite, Register);

            BuildingPoolConfig poolConfig = GenerateBuildingPool(buildings, log);
            List<WeaponSelectionData> weapons = GenerateWeapons(circleSprite, log);
            List<KnightData> knights = GenerateKnights(buildings, weapons, log);
            UpdateEnemyDefenseStats(log);

            log.AppendLine($"[Data] Generated/verified Spells={spells.Count}, Buildings={buildingList.Count}, Knights={knights.Count}, Weapons={weapons.Count}");
            return new GeneratedContent
            {
                AllBuildings = buildingList,
                BuildingsById = buildings,
                Knights = knights,
                Weapons = weapons,
                PoolConfig = poolConfig
            };
        }

        private static Sprite GetBuildingSprite(string id, Sprite fallback)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Visual/Sprite/Buildings/building_{id}.png");
            return s != null ? s : fallback;
        }

        private static Sprite GetUnitSprite(string id)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Visual/Sprite/Units/unit_{id}.png");
        }

        private static Dictionary<SpellKind, SpellData> GenerateSpells(Sprite icon, System.Text.StringBuilder log)
        {
            var dict = new Dictionary<SpellKind, SpellData>();

            SpellData CreateSpell<TEffect>(string file, string effectFile, SpellKind kind, string title, string desc, Color color, SpellLevelStats[] lvls)
                where TEffect : SpellEffect
            {
                var effect = GigaAssetUtility.GetOrCreateSO<TEffect>($"Assets/ScriptbleObjects/Spells/Effects/{effectFile}.asset");
                var so = GigaAssetUtility.GetOrCreateSO<SpellData>($"Assets/ScriptbleObjects/Spells/{file}.asset");
                so.InitializeRuntime(kind, title, desc, color, lvls, icon, effect);
                EditorUtility.SetDirty(so);
                dict[kind] = so;
                return so;
            }

            CreateSpell<MeteorSpellEffect>("Spell_Meteor", "SpellEffect_Meteor", SpellKind.Meteor, "Метеор",
                "Падает в выбранную зону с задержкой и оставляет горящую область.",
                new Color(1f, 0.42f, 0.12f, 1f),
                new[]
                {
                    new SpellLevelStats { primaryDamage = 50, areaRadius = 1.0f, delaySeconds = 10f, durationSeconds = 3f, secondaryDps = 5f },
                    new SpellLevelStats { primaryDamage = 100, areaRadius = 1.0f, delaySeconds = 7f, durationSeconds = 4f, secondaryDps = 5f },
                    new SpellLevelStats { primaryDamage = 200, areaRadius = 1.5f, delaySeconds = 5f, durationSeconds = 4f, secondaryDps = 8f },
                    new SpellLevelStats { primaryDamage = 300, areaRadius = 2.0f, delaySeconds = 3f, durationSeconds = 5f, secondaryDps = 10f }
                });

            CreateSpell<IceStormSpellEffect>("Spell_IceStorm", "SpellEffect_IceStorm", SpellKind.IceStorm, "Ледяная буря",
                "Наносит ледяной урон по площади и замораживает врагов после нахождения в зоне.",
                new Color(0.35f, 0.85f, 1f, 1f),
                new[]
                {
                    new SpellLevelStats { secondaryDps = 6f, areaRadius = 1.5f, durationSeconds = 5f, freezeThreshold = 4f },
                    new SpellLevelStats { secondaryDps = 8f, areaRadius = 2.0f, durationSeconds = 6f, freezeThreshold = 3f },
                    new SpellLevelStats { secondaryDps = 10f, areaRadius = 2.5f, durationSeconds = 8f, freezeThreshold = 2f },
                    new SpellLevelStats { secondaryDps = 15f, areaRadius = 3.0f, durationSeconds = 10f, freezeThreshold = 1f }
                });

            CreateSpell<HeavenlyWrathSpellEffect>("Spell_HeavenlyWrath", "SpellEffect_HeavenlyWrath", SpellKind.HeavenlyWrath, "Небесная кара",
                "Мгновенный удар молнией, цепь по ближайшим врагам и оглушение.",
                new Color(0.95f, 0.9f, 0.25f, 1f),
                new[]
                {
                    new SpellLevelStats { primaryDamage = 40, areaRadius = 0.65f, durationSeconds = 4f, secondaryDps = 2f },
                    new SpellLevelStats { primaryDamage = 50, areaRadius = 0.65f, durationSeconds = 6f, secondaryDps = 3f },
                    new SpellLevelStats { primaryDamage = 70, areaRadius = 0.65f, durationSeconds = 8f, secondaryDps = 4f },
                    new SpellLevelStats { primaryDamage = 100, areaRadius = 1.0f, durationSeconds = 10f, secondaryDps = 5f }
                });

            CreateSpell<DeadLegionSpellEffect>("Spell_DeadLegion", "SpellEffect_DeadLegion", SpellKind.DeadLegion, "Мёртвый легион",
                "Наносит урон в зоне, призывает скелетов и поднимает убитых врагов.",
                new Color(0.32f, 0.85f, 0.45f, 1f),
                new[]
                {
                    new SpellLevelStats { secondaryDps = 10f, areaRadius = 1.5f, durationSeconds = 3f, spawnRatePerSec = 1f },
                    new SpellLevelStats { secondaryDps = 12f, areaRadius = 1.5f, durationSeconds = 4f, spawnRatePerSec = 2f },
                    new SpellLevelStats { secondaryDps = 15f, areaRadius = 2.0f, durationSeconds = 5f, spawnRatePerSec = 3f },
                    new SpellLevelStats { secondaryDps = 20f, areaRadius = 2.5f, durationSeconds = 8f, spawnRatePerSec = 4f }
                });

            CreateSpell<NewLifeSpellEffect>("Spell_NewLife", "SpellEffect_NewLife", SpellKind.NewLife, "Новая жизнь",
                "Превращает обычных врагов в области в овец-приманок (40 HP, провокация 3 кл.).",
                new Color(0.95f, 0.65f, 0.85f, 1f),
                new[]
                {
                    new SpellLevelStats { areaRadius = 1.5f, durationSeconds = 12f },
                    new SpellLevelStats { areaRadius = 1.5f, durationSeconds = 12f },
                    new SpellLevelStats { areaRadius = 2.0f, durationSeconds = 15f },
                    new SpellLevelStats { areaRadius = 2.5f, durationSeconds = 15f }
                });

            log.AppendLine("[Data] Updated 5 SpellData assets.");
            return dict;
        }

        private static ProjectileData GenerateSiegeBoltProjectile()
        {
            var proj = GigaAssetUtility.GetOrCreateSO<ProjectileData>("Assets/ScriptbleObjects/Projectiles/Projectile_SiegeBallistaBolt.asset");
            proj.SetRuntimeStats(20, 16f, 2, 0f);
            proj.ConfigureClassAndAOE(ProjectileClass.DirectPiercing, DamageType.Physical, 0f, 1f, 0f, 0f);
            EditorUtility.SetDirty(proj);
            return proj;
        }

        private static BuildingLevelStats[] MakeStats(int[] costs, int[] hps, int[] dmgs = null, float[] ranges = null, float[] intervals = null, int[] xp = null, float[] prodInt = null, int[] prodAmt = null, float[] spawns = null, int[] units = null)
        {
            var arr = new BuildingLevelStats[4];
            for (int i = 0; i < 4; i++)
            {
                arr[i] = new BuildingLevelStats
                {
                    cost = costs[i],
                    maxHP = hps[i],
                    damage = dmgs != null ? dmgs[i] : 0,
                    attackRange = ranges != null ? ranges[i] : 0f,
                    attackInterval = intervals != null ? intervals[i] : 1f,
                    combatXpRequirement = xp != null ? xp[i] : (i == 0 ? 0 : i * 100),
                    productionInterval = prodInt != null ? prodInt[i] : 0f,
                    productionAmount = prodAmt != null ? prodAmt[i] : 0,
                    spawnInterval = spawns != null ? spawns[i] : 0f,
                    unitCount = units != null ? units[i] : 0
                };
            }
            return arr;
        }

        private static void GenerateArcherBuildings(Sprite sprite, System.Action<BuildingData> reg)
        {
            reg(GigaAssetUtility.SaveBuildingAndPrefab<UnitSpawnerBuilding>(
                "Assets/ScriptbleObjects/Buildings/Building_ArcherCamp.asset",
                "Assets/Prefabs/Buildings/Building_ArcherCamp.prefab",
                "archer_camp", "Лагерь лучников", "Спавнит лучников дальнего боя.",
                BuildingCategory.Archers, Vector2Int.one, BuildingUpgradeType.Local, false,
                MakeStats(new[] { 40, 75, 120, 180 }, new[] { 100, 100, 100, 100 }, new[] { 8, 10, 13, 16 }, new[] { 3.5f, 3.8f, 4.2f, 4.8f }, new[] { 1.4f, 1.3f, 1.1f, 0.85f }, new[] { 0, 100, 220, 400 }, null, null, new[] { 8f, 7f, 6f, 4f }, new[] { 2, 3, 3, 4 }),
                GetBuildingSprite("archer_camp", sprite), Color.white,
                c => c.ConfigureSpawner(true, true, false, false, 0f, Color.white, new[]
                {
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 2, unitMaxHP = 35, unitDamage = 8, attackInterval = 1.4f, attackRange = 3.5f, aggroLimit = 1, patrolRadius = 2.2f, respawnSeconds = 8f, pierceCount = 1 },
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 3, unitMaxHP = 45, unitDamage = 10, attackInterval = 1.3f, attackRange = 3.8f, aggroLimit = 1, patrolRadius = 2.4f, respawnSeconds = 7f, pierceCount = 1 },
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 3, unitMaxHP = 55, unitDamage = 13, attackInterval = 1.1f, attackRange = 4.2f, aggroLimit = 1, patrolRadius = 2.6f, respawnSeconds = 6f, pierceCount = 1 },
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 4, unitMaxHP = 70, unitDamage = 16, attackInterval = 0.85f, attackRange = 4.8f, aggroLimit = 1, patrolRadius = 3.0f, respawnSeconds = 4f, pierceCount = 1 }
                }, GetUnitSprite("archer"))));

            reg(GigaAssetUtility.SaveBuildingAndPrefab<UnitSpawnerBuilding>(
                "Assets/ScriptbleObjects/Buildings/Building_BerserkerCamp.asset",
                "Assets/Prefabs/Buildings/Building_BerserkerCamp.prefab",
                "berserker_camp", "Лагерь берсерков", "Метают топоры с кровотечением и рубят вблизи.",
                BuildingCategory.Archers, Vector2Int.one, BuildingUpgradeType.Local, false,
                MakeStats(new[] { 55, 100, 155, 230 }, new[] { 100, 100, 100, 100 }, new[] { 12, 16, 20, 26 }, new[] { 2.8f, 3.0f, 3.2f, 3.6f }, new[] { 1.6f, 1.4f, 1.2f, 1.0f }, new[] { 0, 120, 260, 450 }, null, null, new[] { 10f, 8f, 7f, 5f }, new[] { 2, 2, 3, 4 }),
                GetBuildingSprite("berserker_camp", sprite), Color.white,
                c => c.ConfigureSpawner(true, false, true, false, 0f, Color.white, new[]
                {
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 2, unitMaxHP = 45, unitDamage = 12, attackInterval = 1.6f, attackRange = 2.8f, aggroLimit = 2, patrolRadius = 2.4f, respawnSeconds = 10f, pierceCount = 1, meleeDamage = 10, meleeInterval = 1.4f, bleedDps = 2f, bleedDuration = 2f },
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 2, unitMaxHP = 55, unitDamage = 16, attackInterval = 1.4f, attackRange = 3.0f, aggroLimit = 2, patrolRadius = 2.6f, respawnSeconds = 8f, pierceCount = 1, meleeDamage = 13, meleeInterval = 1.2f, bleedDps = 2f, bleedDuration = 3f },
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 3, unitMaxHP = 70, unitDamage = 20, attackInterval = 1.2f, attackRange = 3.2f, aggroLimit = 2, patrolRadius = 2.8f, respawnSeconds = 7f, pierceCount = 1, meleeDamage = 17, meleeInterval = 1.0f, bleedDps = 2f, bleedDuration = 4f },
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 4, unitMaxHP = 95, unitDamage = 26, attackInterval = 1.0f, attackRange = 3.6f, aggroLimit = 2, patrolRadius = 3.2f, respawnSeconds = 5f, pierceCount = 1, meleeDamage = 22, meleeInterval = 0.8f, bleedDps = 3f, bleedDuration = 5f }
                }, GetUnitSprite("berserker"))));

            reg(GigaAssetUtility.SaveBuildingAndPrefab<UnitSpawnerBuilding>(
                "Assets/ScriptbleObjects/Buildings/Building_HunterCamp.asset",
                "Assets/Prefabs/Buildings/Building_HunterCamp.prefab",
                "hunter_camp", "Охотник", "Дальнобойный стрелок с уходом в невидимость.",
                BuildingCategory.Archers, Vector2Int.one, BuildingUpgradeType.Local, false,
                MakeStats(new[] { 45, 85, 135, 200 }, new[] { 100, 100, 100, 100 }, new[] { 14, 18, 24, 32 }, new[] { 4.2f, 4.6f, 5.0f, 5.5f }, new[] { 1.3f, 1.1f, 0.9f, 0.65f }, new[] { 0, 100, 220, 400 }, null, null, new[] { 9f, 8f, 6f, 5f }, new[] { 1, 1, 1, 1 }),
                GetBuildingSprite("hunter_camp", sprite), Color.white,
                c => c.ConfigureSpawner(true, true, false, true, 5f, Color.white, new[]
                {
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 1, unitMaxHP = 30, unitDamage = 14, attackInterval = 1.3f, attackRange = 4.2f, aggroLimit = 1, patrolRadius = 2.4f, respawnSeconds = 9f, pierceCount = 1 },
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 1, unitMaxHP = 40, unitDamage = 18, attackInterval = 1.1f, attackRange = 4.6f, aggroLimit = 1, patrolRadius = 2.6f, respawnSeconds = 8f, pierceCount = 1 },
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 1, unitMaxHP = 50, unitDamage = 24, attackInterval = 0.9f, attackRange = 5.0f, aggroLimit = 1, patrolRadius = 2.8f, respawnSeconds = 6f, pierceCount = 1 },
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 1, unitMaxHP = 65, unitDamage = 32, attackInterval = 0.65f, attackRange = 5.5f, aggroLimit = 1, patrolRadius = 3.2f, respawnSeconds = 5f, pierceCount = 1 }
                }, GetUnitSprite("hunter"))));
        }

        private static void GenerateMeleeBuildings(Sprite sprite, System.Action<BuildingData> reg)
        {
            reg(GigaAssetUtility.SaveBuildingAndPrefab<UnitSpawnerBuilding>(
                "Assets/ScriptbleObjects/Buildings/Building_Barracks.asset",
                "Assets/Prefabs/Buildings/Building_Barracks.prefab",
                "barracks", "Казармы", "Выпускает мечников ближнего боя.",
                BuildingCategory.Melee, Vector2Int.one, BuildingUpgradeType.Local, false,
                MakeStats(new[] { 45, 85, 135, 200 }, new[] { 100, 100, 100, 100 }, new[] { 11, 16, 22, 34 }, new[] { 1.1f, 1.1f, 1.1f, 1.1f }, new[] { 1.3f, 1.15f, 1.0f, 0.8f }, new[] { 0, 120, 260, 450 }, null, null, new[] { 7f, 6f, 5f, 3f }, new[] { 3, 3, 4, 5 }),
                GetBuildingSprite("barracks", sprite), Color.white,
                c => c.ConfigureSpawner(false, false, false, false, 0f, Color.white, new[]
                {
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 3, unitMaxHP = 55, unitDamage = 11, attackInterval = 1.3f, attackRange = 1.1f, aggroLimit = 2, patrolRadius = 2.8f, respawnSeconds = 7f, pierceCount = 1 },
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 3, unitMaxHP = 70, unitDamage = 16, attackInterval = 1.15f, attackRange = 1.1f, aggroLimit = 2, patrolRadius = 3.0f, respawnSeconds = 6f, pierceCount = 1 },
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 4, unitMaxHP = 95, unitDamage = 22, attackInterval = 1.0f, attackRange = 1.1f, aggroLimit = 2, patrolRadius = 3.2f, respawnSeconds = 5f, pierceCount = 1 },
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 5, unitMaxHP = 135, unitDamage = 34, attackInterval = 0.8f, attackRange = 1.1f, aggroLimit = 3, patrolRadius = 3.6f, respawnSeconds = 3f, pierceCount = 1 }
                }, GetUnitSprite("swordsman"))));

            reg(GigaAssetUtility.SaveBuildingAndPrefab<UnitSpawnerBuilding>(
                "Assets/ScriptbleObjects/Buildings/Building_CyclopsCave.asset",
                "Assets/Prefabs/Buildings/Building_CyclopsCave.prefab",
                "cyclops_cave", "Пещера циклопа", "Тяжёлый боец с мощным откидыванием и высоким аггро.",
                BuildingCategory.Melee, Vector2Int.one, BuildingUpgradeType.Local, false,
                MakeStats(new[] { 75, 135, 210, 310 }, new[] { 100, 100, 100, 100 }, new[] { 28, 45, 70, 100 }, new[] { 1.25f, 1.25f, 1.25f, 1.25f }, new[] { 2.8f, 2.5f, 2.2f, 1.8f }, new[] { 0, 150, 320, 550 }, null, null, new[] { 14f, 12f, 10f, 8f }, new[] { 1, 1, 1, 2 }),
                GetBuildingSprite("cyclops_cave", sprite), Color.white,
                c => c.ConfigureSpawner(false, false, false, false, 0f, Color.white, new[]
                {
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 1, unitMaxHP = 140, unitDamage = 28, attackInterval = 2.8f, attackRange = 1.25f, aggroLimit = 6, patrolRadius = 3.5f, respawnSeconds = 14f, knockbackCells = 1f, pierceCount = 1 },
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 1, unitMaxHP = 200, unitDamage = 45, attackInterval = 2.5f, attackRange = 1.25f, aggroLimit = 6, patrolRadius = 3.8f, respawnSeconds = 12f, knockbackCells = 1f, pierceCount = 1 },
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 1, unitMaxHP = 280, unitDamage = 70, attackInterval = 2.2f, attackRange = 1.25f, aggroLimit = 6, patrolRadius = 4.0f, respawnSeconds = 10f, knockbackCells = 1f, pierceCount = 1 },
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 2, unitMaxHP = 400, unitDamage = 100, attackInterval = 1.8f, attackRange = 1.25f, aggroLimit = 6, patrolRadius = 4.5f, respawnSeconds = 8f, knockbackCells = 1f, pierceCount = 1 }
                }, GetUnitSprite("cyclops"))));

            reg(GigaAssetUtility.SaveBuildingAndPrefab<UnitSpawnerBuilding>(
                "Assets/ScriptbleObjects/Buildings/Building_ShieldBarracks.asset",
                "Assets/Prefabs/Buildings/Building_ShieldBarracks.prefab",
                "shield_barracks", "Казарма щитовиков", "Стойкие щитовики с копьём (пробитие нескольких целей).",
                BuildingCategory.Melee, Vector2Int.one, BuildingUpgradeType.Local, false,
                MakeStats(new[] { 50, 95, 150, 220 }, new[] { 100, 100, 100, 100 }, new[] { 8, 12, 16, 22 }, new[] { 1.2f, 1.2f, 1.2f, 1.2f }, new[] { 1.8f, 1.6f, 1.4f, 1.1f }, new[] { 0, 120, 260, 450 }, null, null, new[] { 8f, 7f, 6f, 4f }, new[] { 3, 3, 4, 4 }),
                GetBuildingSprite("shield_barracks", sprite), Color.white,
                c => c.ConfigureSpawner(false, false, false, false, 0f, Color.white, new[]
                {
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 3, unitMaxHP = 65, unitDamage = 8, attackInterval = 1.8f, attackRange = 1.2f, aggroLimit = 4, patrolRadius = 3.2f, respawnSeconds = 8f, pierceCount = 2 },
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 3, unitMaxHP = 85, unitDamage = 12, attackInterval = 1.6f, attackRange = 1.2f, aggroLimit = 4, patrolRadius = 3.4f, respawnSeconds = 7f, pierceCount = 2 },
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 4, unitMaxHP = 120, unitDamage = 16, attackInterval = 1.4f, attackRange = 1.2f, aggroLimit = 4, patrolRadius = 3.6f, respawnSeconds = 6f, pierceCount = 2 },
                    new UnitSpawnerBuilding.LevelUnitConfig { unitCount = 4, unitMaxHP = 170, unitDamage = 22, attackInterval = 1.1f, attackRange = 1.2f, aggroLimit = 4, patrolRadius = 4.0f, respawnSeconds = 4f, pierceCount = 3 }
                }, GetUnitSprite("shieldman"))));
        }

        private static void GenerateMageBuildings(Sprite sprite, Dictionary<SpellKind, SpellData> spells, System.Action<BuildingData> reg)
        {
            float[] craftCds = { 22f, 19f, 16f, 13f }; // Monotonic per D-6
            int[] mageHp = { 150, 175, 200, 300 };

            void MakeMage(string assetName, string id, string name, string desc, SpellKind kind, int[] costs)
            {
                reg(GigaAssetUtility.SaveBuildingAndPrefab<MageTowerBuilding>(
                    $"Assets/ScriptbleObjects/Buildings/{assetName}.asset",
                    $"Assets/Prefabs/Buildings/{assetName}.prefab",
                    id, name, desc, BuildingCategory.Mages, Vector2Int.one, BuildingUpgradeType.Local, true,
                    MakeStats(costs, mageHp, null, null, craftCds, new[] { 0, 100, 220, 400 }),
                    GetBuildingSprite(id, sprite), Color.white,
                    c => c.ConfigureMageTower(spells.GetValueOrDefault(kind), craftCds)));
            }

            MakeMage("Building_FireMage", "fire_mage", "Башня огня", "Создаёт свитки «Метеор».", SpellKind.Meteor, new[] { 60, 110, 170, 250 });
            MakeMage("Building_IceMage", "ice_mage", "Башня льда", "Создаёт свитки «Ледяная буря».", SpellKind.IceStorm, new[] { 60, 110, 170, 250 });
            MakeMage("Building_LightningMage", "lightning_mage", "Башня молний", "Создаёт свитки «Небесная кара».", SpellKind.HeavenlyWrath, new[] { 65, 115, 180, 265 });
            MakeMage("Building_NecroMage", "necro_mage", "Башня некроманта", "Создаёт свитки «Мёртвый легион».", SpellKind.DeadLegion, new[] { 70, 125, 195, 285 });
            MakeMage("Building_PolymorphMage", "polymorph_mage", "Башня полиморфии", "Создаёт свитки «Новая жизнь».", SpellKind.NewLife, new[] { 65, 115, 180, 265 });
        }

        private static void GenerateSiegeBuildings(Sprite sprite, ProjectileData siegeBolt, System.Action<BuildingData> reg)
        {
            var twoByTwo = new Vector2Int(2, 2);

            reg(GigaAssetUtility.SaveBuildingAndPrefab<SiegeTowerBuilding>(
                "Assets/ScriptbleObjects/Buildings/Building_SiegeBallista.asset",
                "Assets/Prefabs/Buildings/Building_SiegeBallista.prefab",
                "siege_ballista", "Баллиста", "Выстреливает пробивными болтами на большую дистанцию.",
                BuildingCategory.Siege, twoByTwo, BuildingUpgradeType.Local, true,
                MakeStats(new[] { 65, 115, 180, 265 }, new[] { 100, 150, 200, 300 }, new[] { 24, 36, 52, 75 }, new[] { 6f, 6.5f, 7.5f, 8.5f }, new[] { 3.2f, 2.8f, 2.4f, 1.9f }, new[] { 0, 120, 260, 450 }),
                GetBuildingSprite("siege_ballista", sprite), Color.white,
                c => c.ConfigureSiege(SiegeTowerMode.PiercingBallista, siegeBolt, new[]
                {
                    new SiegeTowerBuilding.SiegeLevelConfig { damage = 24, attackRange = 6f, attackInterval = 3.2f, pierceCount = 2 },
                    new SiegeTowerBuilding.SiegeLevelConfig { damage = 36, attackRange = 6.5f, attackInterval = 2.8f, pierceCount = 3 },
                    new SiegeTowerBuilding.SiegeLevelConfig { damage = 52, attackRange = 7.5f, attackInterval = 2.4f, pierceCount = 3 },
                    new SiegeTowerBuilding.SiegeLevelConfig { damage = 75, attackRange = 8.5f, attackInterval = 1.9f, pierceCount = 4 }
                })));

            reg(GigaAssetUtility.SaveBuildingAndPrefab<SiegeTowerBuilding>(
                "Assets/ScriptbleObjects/Buildings/Building_SiegeCannon.asset",
                "Assets/Prefabs/Buildings/Building_SiegeCannon.prefab",
                "siege_cannon", "Пушка", "Бьёт ядрами по площади с мощным откидыванием.",
                BuildingCategory.Siege, twoByTwo, BuildingUpgradeType.Local, true,
                MakeStats(new[] { 75, 130, 205, 300 }, new[] { 125, 175, 250, 400 }, new[] { 28, 45, 68, 95 }, new[] { 4.5f, 5f, 5.5f, 6.5f }, new[] { 4.5f, 3.8f, 3.2f, 2.5f }, new[] { 0, 120, 260, 450 }),
                GetBuildingSprite("siege_cannon", sprite), Color.white,
                c => c.ConfigureSiege(SiegeTowerMode.SplashCannon, null, new[]
                {
                    new SiegeTowerBuilding.SiegeLevelConfig { damage = 28, attackRange = 4.5f, attackInterval = 4.5f, knockbackCells = 2f, splashRadius = 1.0f },
                    new SiegeTowerBuilding.SiegeLevelConfig { damage = 45, attackRange = 5f, attackInterval = 3.8f, knockbackCells = 2f, splashRadius = 1.0f },
                    new SiegeTowerBuilding.SiegeLevelConfig { damage = 68, attackRange = 5.5f, attackInterval = 3.2f, knockbackCells = 3f, splashRadius = 1.0f },
                    new SiegeTowerBuilding.SiegeLevelConfig { damage = 95, attackRange = 6.5f, attackInterval = 2.5f, knockbackCells = 3f, splashRadius = 1.5f }
                })));

            reg(GigaAssetUtility.SaveBuildingAndPrefab<SiegeTowerBuilding>(
                "Assets/ScriptbleObjects/Buildings/Building_SiegeMortar.asset",
                "Assets/Prefabs/Buildings/Building_SiegeMortar.prefab",
                "siege_mortar", "Мортира", "Навесная бомбардировка по большой площади.",
                BuildingCategory.Siege, twoByTwo, BuildingUpgradeType.Local, true,
                MakeStats(new[] { 80, 140, 220, 320 }, new[] { 150, 200, 250, 350 }, new[] { 35, 52, 75, 105 }, new[] { 6f, 6.5f, 7.5f, 8.5f }, new[] { 5f, 4.3f, 3.6f, 2.8f }, new[] { 0, 140, 300, 520 }),
                GetBuildingSprite("siege_mortar", sprite), Color.white,
                c => c.ConfigureSiege(SiegeTowerMode.LobbedMortar, null, new[]
                {
                    new SiegeTowerBuilding.SiegeLevelConfig { damage = 35, attackRange = 6f, attackInterval = 5f, splashRadius = 1.5f, flightTimePerCell = 0.14f },
                    new SiegeTowerBuilding.SiegeLevelConfig { damage = 52, attackRange = 6.5f, attackInterval = 4.3f, splashRadius = 1.5f, flightTimePerCell = 0.12f },
                    new SiegeTowerBuilding.SiegeLevelConfig { damage = 75, attackRange = 7.5f, attackInterval = 3.6f, splashRadius = 1.5f, flightTimePerCell = 0.10f },
                    new SiegeTowerBuilding.SiegeLevelConfig { damage = 105, attackRange = 8.5f, attackInterval = 2.8f, splashRadius = 2.0f, flightTimePerCell = 0.08f }
                })));

            reg(GigaAssetUtility.SaveBuildingAndPrefab<BarracksArcherTower>(
                "Assets/ScriptbleObjects/Buildings/Building_ArcherTower.asset",
                "Assets/Prefabs/Buildings/Building_ArcherTower.prefab",
                "archer_tower", "Башня лучников", "Стреляет залпами при наличии соседнего Лагеря лучников; на Ур.4 добавляет шипы.",
                BuildingCategory.Siege, twoByTwo, BuildingUpgradeType.Local, true,
                MakeStats(new[] { 70, 125, 195, 285 }, new[] { 300, 400, 500, 500 }, new[] { 10, 12, 14, 18 }, new[] { 4.5f, 4.5f, 4.5f, 4.5f }, new[] { 2.5f, 2.5f, 2f, 1f }, new[] { 0, 120, 260, 450 }),
                GetBuildingSprite("archer_tower", sprite), Color.white));
        }

        private static void GenerateSupportBuildings(Sprite sprite, System.Action<BuildingData> reg)
        {
            var twoByTwo = new Vector2Int(2, 2);

            reg(GigaAssetUtility.SaveBuildingAndPrefab<SupportAuraBuilding>(
                "Assets/ScriptbleObjects/Buildings/Building_BannerTower.asset",
                "Assets/Prefabs/Buildings/Building_BannerTower.prefab",
                "banner_tower", "Башня знамени", "Усиливает урон (и скорость на Ур.4) союзных мечников в радиусе.",
                BuildingCategory.Support, Vector2Int.one, BuildingUpgradeType.Local, true,
                MakeStats(new[] { 50, 90, 145, 215 }, new[] { 100, 150, 200, 300 }, null, new[] { 2f, 3f, 3f, 4f }, null, new[] { 0, 100, 220, 400 }),
                GetBuildingSprite("banner_tower", sprite), Color.white,
                c => c.ConfigureAura(SupportAuraMode.BannerMeleeAura, new[]
                {
                    new SupportAuraBuilding.AuraLevelConfig { damageBonusPercent = 0.10f, attackSpeedBonusPercent = 0f, auraRadius = 2.3f },
                    new SupportAuraBuilding.AuraLevelConfig { damageBonusPercent = 0.20f, attackSpeedBonusPercent = 0f, auraRadius = 3.3f },
                    new SupportAuraBuilding.AuraLevelConfig { damageBonusPercent = 0.30f, attackSpeedBonusPercent = 0f, auraRadius = 3.3f },
                    new SupportAuraBuilding.AuraLevelConfig { damageBonusPercent = 0.50f, attackSpeedBonusPercent = 0.20f, auraRadius = 4.3f }
                })));

            reg(GigaAssetUtility.SaveBuildingAndPrefab<SupportUnitSpawnerBuilding>(
                "Assets/ScriptbleObjects/Buildings/Building_Temple.asset",
                "Assets/Prefabs/Buildings/Building_Temple.prefab",
                "temple", "Храм", "Спавнит священников, исцеляющих раненых юнитов.",
                BuildingCategory.Support, twoByTwo, BuildingUpgradeType.Local, true,
                MakeStats(new[] { 60, 110, 170, 250 }, new[] { 150, 200, 250, 300 }, null, new[] { 1f, 1f, 1f, 2f }, new[] { 5f, 3f, 3f, 2f }, new[] { 0, 100, 220, 400 }),
                GetBuildingSprite("temple", sprite), Color.white,
                c => c.ConfigureSupportSpawner(SupportUnitKind.Priest, Color.white, new[]
                {
                    new SupportUnitSpawnerBuilding.SupportLevelConfig { unitMaxHP = 30, effectAmount = 5, actionInterval = 5f, actionRange = 1.4f, roamRadius = 3f, respawnSeconds = 30f },
                    new SupportUnitSpawnerBuilding.SupportLevelConfig { unitMaxHP = 40, effectAmount = 6, actionInterval = 3f, actionRange = 1.4f, roamRadius = 3f, respawnSeconds = 25f },
                    new SupportUnitSpawnerBuilding.SupportLevelConfig { unitMaxHP = 50, effectAmount = 8, actionInterval = 3f, actionRange = 1.4f, roamRadius = 3f, respawnSeconds = 20f },
                    new SupportUnitSpawnerBuilding.SupportLevelConfig { unitMaxHP = 60, effectAmount = 10, actionInterval = 2f, actionRange = 2.2f, roamRadius = 3f, respawnSeconds = 15f }
                }, GetUnitSprite("priest"))));

            reg(GigaAssetUtility.SaveBuildingAndPrefab<SupportUnitSpawnerBuilding>(
                "Assets/ScriptbleObjects/Buildings/Building_BuilderHut.asset",
                "Assets/Prefabs/Buildings/Building_BuilderHut.prefab",
                "builder_hut", "Строители", "Спавнит строителей, чинящих повреждённые здания.",
                BuildingCategory.Support, twoByTwo, BuildingUpgradeType.Local, true,
                MakeStats(new[] { 55, 100, 155, 230 }, new[] { 150, 200, 300, 400 }, null, new[] { 1f, 1f, 1f, 1f }, new[] { 5f, 4f, 3f, 2f }, new[] { 0, 100, 220, 400 }),
                GetBuildingSprite("builder_hut", sprite), Color.white,
                c => c.ConfigureSupportSpawner(SupportUnitKind.Builder, Color.white, new[]
                {
                    new SupportUnitSpawnerBuilding.SupportLevelConfig { unitMaxHP = 40, effectAmount = 10, actionInterval = 5f, actionRange = 1.5f, roamRadius = 3f, respawnSeconds = 30f },
                    new SupportUnitSpawnerBuilding.SupportLevelConfig { unitMaxHP = 50, effectAmount = 15, actionInterval = 4f, actionRange = 1.5f, roamRadius = 3f, respawnSeconds = 25f },
                    new SupportUnitSpawnerBuilding.SupportLevelConfig { unitMaxHP = 60, effectAmount = 25, actionInterval = 3f, actionRange = 1.5f, roamRadius = 3f, respawnSeconds = 20f },
                    new SupportUnitSpawnerBuilding.SupportLevelConfig { unitMaxHP = 80, effectAmount = 30, actionInterval = 2f, actionRange = 1.5f, roamRadius = 3f, respawnSeconds = 15f }
                }, GetUnitSprite("builder"))));

            reg(GigaAssetUtility.SaveBuildingAndPrefab<SupportAuraBuilding>(
                "Assets/ScriptbleObjects/Buildings/Building_Library.asset",
                "Assets/Prefabs/Buildings/Building_Library.prefab",
                "library", "Библиотека", "Усиливает ортогональные магические башни (Ур.1 слева .. Ур.4 все 4 стороны).",
                BuildingCategory.Support, Vector2Int.one, BuildingUpgradeType.Local, true,
                MakeStats(new[] { 55, 100, 155, 230 }, new[] { 100, 125, 150, 200 }, null, null, null, new[] { 0, 100, 220, 400 }),
                GetBuildingSprite("library", sprite), Color.white,
                c => c.ConfigureAura(SupportAuraMode.LibraryOrthogonalBuff, new[]
                {
                    new SupportAuraBuilding.AuraLevelConfig { damageBonusPercent = 0.10f, attackSpeedBonusPercent = 0.10f, auraRadius = 1f },
                    new SupportAuraBuilding.AuraLevelConfig { damageBonusPercent = 0.15f, attackSpeedBonusPercent = 0.25f, auraRadius = 1f },
                    new SupportAuraBuilding.AuraLevelConfig { damageBonusPercent = 0.30f, attackSpeedBonusPercent = 0.50f, auraRadius = 1f },
                    new SupportAuraBuilding.AuraLevelConfig { damageBonusPercent = 0.50f, attackSpeedBonusPercent = 0.70f, auraRadius = 1f }
                })));
        }

        private static void GenerateEconomyBuildings(Sprite sprite, System.Action<BuildingData> reg)
        {
            BuildingData dc = GigaAssetUtility.SaveBuildingAndPrefab<EconomyBuilding>(
                "Assets/ScriptbleObjects/Buildings/Building_DiamondCavern.asset",
                "Assets/Prefabs/Buildings/Building_DiamondCavern.prefab",
                "diamond_cavern", "Алмазные пещеры", "Медленно добывает мета-валюту — алмазы.",
                BuildingCategory.Economy, new Vector2Int(2, 2), BuildingUpgradeType.Local, true,
                MakeStats(new[] { 90, 150, 230, 340 }, new[] { 50, 75, 100, 150 }, null, null, null, new[] { 0, 100, 220, 400 }, new[] { 120f, 100f, 80f, 60f }, new[] { 1, 1, 1, 1 }),
                GetBuildingSprite("diamond_cavern", sprite), Color.white,
                c =>
                {
                    var so = new SerializedObject(c);
                    so.FindProperty("_producesDiamonds").boolValue = true;
                    so.ApplyModifiedPropertiesWithoutUndo();
                });
            reg(dc);
        }

        private static void GenerateControlBuildings(Sprite sprite, System.Action<BuildingData> reg)
        {
            reg(GigaAssetUtility.SaveBuildingAndPrefab<BombBuilding>(
                "Assets/ScriptbleObjects/Buildings/Building_Bomb.asset",
                "Assets/Prefabs/Buildings/Building_Bomb.prefab",
                "bomb", "Бомба", "Одноразовая мина на пути врагов с уроном по области. Глобальная прокачка.",
                BuildingCategory.Control, Vector2Int.one, BuildingUpgradeType.Global, false,
                MakeStats(new[] { 25, 45, 75, 115 }, new[] { 1, 1, 1, 1 }, new[] { 75, 150, 250, 500 }, null, null, new[] { 0, 0, 0, 0 }),
                GetBuildingSprite("bomb", sprite), Color.white));

            reg(GigaAssetUtility.SaveBuildingAndPrefab<TrapBuilding>(
                "Assets/ScriptbleObjects/Buildings/Building_SpikeTrap.asset",
                "Assets/Prefabs/Buildings/Building_SpikeTrap.prefab",
                "spike_trap", "Ловушка с шипами", "Наносит урон всем наступающим врагам до исчерпания зарядов. Глобальная прокачка.",
                BuildingCategory.Control, Vector2Int.one, BuildingUpgradeType.Global, false,
                MakeStats(new[] { 30, 55, 90, 135 }, new[] { 1, 1, 1, 1 }, new[] { 6, 10, 14, 20 }, null, new[] { 1f, 1f, 1f, 1f }, new[] { 0, 0, 0, 0 }),
                GetBuildingSprite("spike_trap", sprite), Color.white));
        }

        private static BuildingPoolConfig GenerateBuildingPool(Dictionary<string, BuildingData> byId, System.Text.StringBuilder log)
        {
            var cfg = GigaAssetUtility.GetOrCreateSO<BuildingPoolConfig>("Assets/ScriptbleObjects/Buildings/BuildingPoolConfig.asset");
            var entries = new List<BuildingPoolEntry>();

            void Add(string id, float w, int minLvl = 1)
            {
                if (byId.TryGetValue(id, out var d) && d != null)
                {
                    entries.Add(new BuildingPoolEntry { Data = d, Weight = w, MinCastleLevel = minLvl });
                }
            }

            Add("Building_Wall", 1.5f, 1);
            Add("Building_GoldMine", 1.3f, 1);
            Add("Building_Turret", 1.2f, 1);
            Add("archer_camp", 1.5f, 1);
            Add("barracks", 1.5f, 1);
            Add("shield_barracks", 1.2f, 1);
            Add("hunter_camp", 1.1f, 1);
            Add("berserker_camp", 1.0f, 1);
            Add("bomb", 1.3f, 1);
            Add("spike_trap", 1.3f, 1);
            Add("banner_tower", 1.1f, 1);
            Add("library", 1.0f, 1);
            Add("fire_mage", 1.0f, 1);
            Add("ice_mage", 1.0f, 1);
            Add("lightning_mage", 0.95f, 2);
            Add("necro_mage", 0.9f, 2);
            Add("polymorph_mage", 0.9f, 2);
            Add("cyclops_cave", 0.85f, 2);
            Add("temple", 1.0f, 2);
            Add("builder_hut", 1.1f, 1);
            Add("siege_ballista", 0.9f, 2);
            Add("siege_cannon", 0.85f, 2);
            Add("siege_mortar", 0.85f, 2);
            Add("archer_tower", 0.9f, 2);
            Add("diamond_cavern", 0.55f, 3);

            var bonuses = new List<ShopMinorBonusEntry>
            {
                new() { Id = "shop_repair", Title = "Полевой ремонт", Description = "Мгновенно чинит все постройки на 25%", EffectType = UpgradeEffectType.RepairAllBuildingsPercent, Value = 0.25f, Cost = 30, Weight = 1.3f },
                new() { Id = "shop_discount", Title = "Логистика", Description = "Постройки дешевле на 10%", EffectType = UpgradeEffectType.BuildingCostDiscountPercent, Value = 0.10f, Cost = 45, Weight = 1.0f },
                new() { Id = "shop_ranged", Title = "Точеный наконечник", Description = "+10% урона стрелков и башен", EffectType = UpgradeEffectType.RangedDamagePercent, Value = 0.10f, Cost = 35, Weight = 1.2f },
                new() { Id = "shop_melee", Title = "Заточка клинков", Description = "+10% урона ближнего боя", EffectType = UpgradeEffectType.MeleeDamagePercent, Value = 0.10f, Cost = 35, Weight = 1.2f },
                new() { Id = "shop_spell", Title = "Чародейская пыль", Description = "+15% урона заклинаний", EffectType = UpgradeEffectType.SpellDamagePercent, Value = 0.15f, Cost = 40, Weight = 1.0f },
                new() { Id = "shop_support", Title = "Поддержка авангарда", Description = "+15% эффективности поддержки", EffectType = UpgradeEffectType.SupportEffectivenessPercent, Value = 0.15f, Cost = 40, Weight = 1.0f },
                new() { Id = "shop_trap", Title = "Отравленные шипы", Description = "+20% урона ловушек и бомб", EffectType = UpgradeEffectType.TrapDamagePercent, Value = 0.20f, Cost = 35, Weight = 1.1f },
                new() { Id = "shop_econ", Title = "Кирка мастера", Description = "+15% скорости добычи", EffectType = UpgradeEffectType.EconomicSpeedPercent, Value = 0.15f, Cost = 40, Weight = 1.1f },
                new() { Id = "shop_aggro", Title = "Клич авангарда", Description = "+1 к лимиту удержания врагов юнитами", EffectType = UpgradeEffectType.AggroLimitAdd, Value = 1f, Cost = 45, Weight = 0.7f },
                new() { Id = "shop_slot", Title = "Карта застройки", Description = "+1 слот для построек", EffectType = UpgradeEffectType.BuildSlotAdd, Value = 1f, Cost = 60, Weight = 0.5f }
            };

            cfg.SetEntries(entries, bonuses);
            EditorUtility.SetDirty(cfg);
            log.AppendLine($"[Data] Populated BuildingPoolConfig with {entries.Count} buildings & {bonuses.Count} minor bonuses.");
            return cfg;
        }

        private static List<WeaponSelectionData> GenerateWeapons(Sprite icon, System.Text.StringBuilder log)
        {
            var ballistaData = AssetDatabase.LoadAssetAtPath<HeavyBallistaData>("Assets/ScriptbleObjects/Weapons/HeavyBallistaData.asset");
            var defWeapon = GigaAssetUtility.GetOrCreateSO<WeaponSelectionData>("Assets/ScriptbleObjects/Data/DefaultWeapon.asset");
            defWeapon.InitializeRuntime("heavy_ballista", "Тяжёлая баллиста",
                "Массивные болты пробивают всех врагов насквозь и отталкивают на 1 клетку.", true, 0, ballistaData);
            defWeapon.SetIcon(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Visual/Sprite/Weapons/weapon_heavy_ballista.png"));
            EditorUtility.SetDirty(defWeapon);

            WeaponSelectionData MakeWep(string file, string id, string title, string desc, int cost, string spriteName)
            {
                var w = GigaAssetUtility.GetOrCreateSO<WeaponSelectionData>($"Assets/ScriptbleObjects/Data/{file}.asset");
                w.InitializeRuntime(id, title, desc, false, cost, null);
                w.SetIcon(AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Visual/Sprite/Weapons/{spriteName}.png"));
                EditorUtility.SetDirty(w);
                return w;
            }

            var mortar = MakeWep("Weapon_Mortar", "alchemical_mortar", "Алхимическая мортира",
                "Навесные снаряды взрываются по площади и оставляют горящую лужу.", 15, "weapon_alchemical_mortar");
            var tar = MakeWep("Weapon_TarCannon", "tar_cannon", "Смоляная пушка",
                "Наносит урон ядром и оставляет смоляную лужу, замедляющую врагов.", 15, "weapon_tar_cannon");
            var orthanc = MakeWep("Weapon_Orthanc", "orthanc", "Ортхунк",
                "Мгновенно создаёт ураганный смерч, стягивающий врагов и наносящий урон в секунду.", 20, "weapon_orthanc");
            var volley = MakeWep("Weapon_ArcherVolley", "archer_volley", "Обстрел лучников",
                "Выпускает град стрел с небес в указанную область.", 20, "weapon_archer_volley");

            log.AppendLine("[Data] Generated/updated 5 WeaponSelectionData assets.");
            return new List<WeaponSelectionData> { defWeapon, mortar, tar, orthanc, volley };
        }

        private static List<KnightData> GenerateKnights(Dictionary<string, BuildingData> byId, List<WeaponSelectionData> weapons, System.Text.StringBuilder log)
        {
            BuildingData[] Pick(params string[] ids)
            {
                var list = new List<BuildingData>();
                for (int i = 0; i < ids.Length; i++)
                {
                    if (byId.TryGetValue(ids[i], out var b) && b != null) list.Add(b);
                }
                return list.ToArray();
            }

            WeaponSelectionData FindWep(string id) => weapons.Find(w => w != null && w.Id == id);
            Sprite GetKnightPortrait(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Visual/Sprite/Knights/knight_{name}.png");

            var hador = GigaAssetUtility.GetOrCreateSO<KnightData>("Assets/ScriptbleObjects/Data/DefaultKnight.asset");
            hador.InitializeRuntime("hador_oplot", "Хадор Оплот", "Рыцарь-Бастион",
                "«Камень не молит о пощаде, он просто стоит.»", 2000, 125, 4,
                new[] { "Стены", "Казарма щитовиков" },
                new[] { "Несокрушимый фундамент (+30% HP разруш. построек)", "Стена щитов (+1 удержание, +20% HP юнитов)" },
                true, 0);
            hador.SetStartingBuildingAssets(Pick("Building_Wall", "shield_barracks"), FindWep("heavy_ballista"));
            hador.SetPortrait(GetKnightPortrait("hador"));
            EditorUtility.SetDirty(hador);

            var finrod = GigaAssetUtility.GetOrCreateSO<KnightData>("Assets/ScriptbleObjects/Data/Knight_Finrod.asset");
            finrod.InitializeRuntime("finrod_burevestnik", "Финрод Буревестник", "Рыцарь-Маг",
                "«Моя магия не знает преград, а сознание не имеет границ.»", 1500, 75, 5,
                new[] { "Башня огня", "Библиотека" },
                new[] { "Поток маны (-15% КД маг. башен)", "Мудрость веков (+10% эффектов поддержки)" },
                false, 20);
            finrod.SetStartingBuildingAssets(Pick("fire_mage", "library"), FindWep("orthanc"));
            finrod.SetPortrait(GetKnightPortrait("finrod"));
            EditorUtility.SetDirty(finrod);

            var borin = GigaAssetUtility.GetOrCreateSO<KnightData>("Assets/ScriptbleObjects/Data/Knight_Borin.asset");
            borin.InitializeRuntime("borin_ogneplav", "Борин Огнеплав", "Рыцарь-Инженер",
                "«Хорошая взрывчатка и капля кислоты эффективнее, чем тысяча клинков.»", 1300, 150, 6,
                new[] { "Мортира", "Строители" },
                new[] { "Эврика (+20% взрывного урона и радиуса; +20% ск. пробивных)", "Золотые руки (+25% скорости починки, +1 кл. дальности)" },
                false, 25);
            borin.SetStartingBuildingAssets(Pick("siege_mortar", "builder_hut"), FindWep("alchemical_mortar"));
            borin.SetPortrait(GetKnightPortrait("borin"));
            EditorUtility.SetDirty(borin);

            var aradan = GigaAssetUtility.GetOrCreateSO<KnightData>("Assets/ScriptbleObjects/Data/Knight_Aradan.asset");
            aradan.InitializeRuntime("aradan_tenehod", "Арадан Тенеход", "Рыцарь-Следопыт",
                "«Они думают, что бегут к победе, но они еще не знают в какую западню они попали.»", 1650, 90, 4,
                new[] { "Охотник", "Бомба" },
                new[] { "Смертельная западня (+10% урон и +20% зарядов ловушек, замедление 40%/3с)", "Тень леса (+25% урон лучников, +2с скрытности Охотника)" },
                false, 25);
            aradan.SetStartingBuildingAssets(Pick("hunter_camp", "bomb"), FindWep("archer_volley"));
            aradan.SetPortrait(GetKnightPortrait("aradan"));
            EditorUtility.SetDirty(aradan);

            var baron = GigaAssetUtility.GetOrCreateSO<KnightData>("Assets/ScriptbleObjects/Data/Knight_Baron.asset");
            baron.InitializeRuntime("baron_zlatoliz", "Барон Златолиз", "Рыцарь-Казначей",
                "«Золото — лучшая броня.»", 1650, 90, 6,
                new[] { "Золотая шахта", "Строители" },
                new[] { "Кровь и золото (+1 монета за убийство)", "Ростовщичество (+30% скорости и +25 HP экономики)", "Скряга (-25% HP неэкономических построек)" },
                false, 30);
            baron.SetStartingBuildingAssets(Pick("Building_GoldMine", "builder_hut"), FindWep("heavy_ballista"));
            baron.SetPortrait(GetKnightPortrait("baron"));
            EditorUtility.SetDirty(baron);

            log.AppendLine("[Data] Generated/updated 5 KnightData assets.");
            return new List<KnightData> { hador, finrod, borin, aradan, baron };
        }

        private static void UpdateEnemyDefenseStats(System.Text.StringBuilder log)
        {
            void SetDef(string path, int def, float res, float hpGrowth, string spriteName)
            {
                var ed = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                if (ed != null)
                {
                    ed.SetDefenseStats(def, res, hpGrowth);
                    var s = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Visual/Sprite/Enemies/{spriteName}.png");
                    if (s != null)
                    {
                        ed.SetVisuals(s, Color.white);
                    }
                    EditorUtility.SetDirty(ed);
                }
            }

            SetDef("Assets/ScriptbleObjects/Enemy/Enemy.asset", 0, 0f, 0.10f, "enemy_regular");
            SetDef("Assets/ScriptbleObjects/Enemy/Enemy_Fast.asset", 0, 0f, 0.10f, "enemy_fast");
            SetDef("Assets/ScriptbleObjects/Enemy/Enemy_Archer.asset", 0, 0.05f, 0.10f, "enemy_archer");
            SetDef("Assets/ScriptbleObjects/Enemy/Enemy_Tank.asset", 2, 0.15f, 0.12f, "enemy_tank");
            SetDef("Assets/ScriptbleObjects/Enemy/Enemy_Boss.asset", 3, 0.20f, 0f, "enemy_boss");
            log.AppendLine("[Data] Updated EnemyData defense/resistance/wave HP growth and visuals.");
        }
    }
}
#endif
