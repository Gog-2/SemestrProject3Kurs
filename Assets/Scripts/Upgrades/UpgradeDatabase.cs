using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Alchemy.Inspector;
using Game.Buildings;
using Game.Data;

namespace Game.Upgrades
{
    [CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "Game/Upgrade Database", order = 31)]
    public class UpgradeDatabase : ScriptableObject
    {
        [Title("All Upgrades")]
        [SerializeField] private List<UpgradeData> _upgrades = new();

        public IReadOnlyList<UpgradeData> AllUpgrades => _upgrades;

        public List<UpgradeData> GetRandomUpgrades(int count, IReadOnlyCollection<UpgradeData> excluded = null)
        {
            return GetRandomUpgrades(count, excluded, null, 1, null, null);
        }

        /// <summary>
        /// Weighted random selection per GIG-25 (directed affinity ramping by Castle level 5)
        /// and GIG-26 (-30% weight per already-taken copy of the same upgrade).
        /// </summary>
        public List<UpgradeData> GetRandomUpgrades(
            int count,
            IReadOnlyCollection<UpgradeData> excluded,
            IReadOnlyDictionary<string, int> takenCounts,
            int castleLevel,
            IReadOnlyList<Building> activeBuildings,
            KnightData selectedKnight)
        {
            var pool = new List<UpgradeData>();
            var weights = new List<float>();

            Dictionary<BuildingCategory, int> categoryCounts = CountActiveBuildingCategories(activeBuildings);
            float biasStrength = Mathf.Clamp01(Mathf.Max(0, castleLevel - 1) / 4f);

            for (int i = 0; i < _upgrades.Count; i++)
            {
                UpgradeData upgrade = _upgrades[i];
                if (upgrade == null) continue;
                if (excluded != null && excluded.Contains(upgrade)) continue;

                float weight = CalculateUpgradeWeight(upgrade, takenCounts, biasStrength, categoryCounts, selectedKnight);
                if (weight <= 0.0001f) continue;

                pool.Add(upgrade);
                weights.Add(weight);
            }

            var result = new List<UpgradeData>();
            int picksNeeded = Mathf.Min(count, pool.Count);

            for (int i = 0; i < picksNeeded; i++)
            {
                int pickedIdx = PickWeightedIndex(weights);
                result.Add(pool[pickedIdx]);
                pool.RemoveAt(pickedIdx);
                weights.RemoveAt(pickedIdx);
            }

            return result;
        }

        private static float CalculateUpgradeWeight(
            UpgradeData upgrade,
            IReadOnlyDictionary<string, int> takenCounts,
            float biasStrength,
            Dictionary<BuildingCategory, int> categoryCounts,
            KnightData selectedKnight)
        {
            float weight = upgrade.Rarity switch
            {
                UpgradeRarity.Common => 1.0f,
                UpgradeRarity.Rare => 0.8f,
                UpgradeRarity.Epic => 0.6f,
                UpgradeRarity.Legendary => 0.45f,
                _ => 1.0f
            };

            // GIG-26: -30% weight per previously taken copy (additive stacking remains enabled)
            if (takenCounts != null && !string.IsNullOrEmpty(upgrade.Id) && takenCounts.TryGetValue(upgrade.Id, out int timesTaken) && timesTaken > 0)
            {
                weight *= Mathf.Pow(0.7f, timesTaken);
            }

            // GIG-25: Directional bias based on placed buildings and knight archetype toward Castle level 5
            if (biasStrength > 0f)
            {
                float affinityScore = EvaluateAffinityMatch(upgrade, categoryCounts, selectedKnight);
                weight *= 1f + affinityScore * 1.5f * biasStrength;
            }

            return Mathf.Max(0.05f, weight);
        }

        private static Dictionary<BuildingCategory, int> CountActiveBuildingCategories(IReadOnlyList<Building> buildings)
        {
            var counts = new Dictionary<BuildingCategory, int>();
            if (buildings == null) return counts;

            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                if (b == null || b.IsDead || b.Data == null) continue;
                counts.TryGetValue(b.Data.Category, out int current);
                counts[b.Data.Category] = current + 1;
            }

            return counts;
        }

        private static float EvaluateAffinityMatch(
            UpgradeData upgrade,
            Dictionary<BuildingCategory, int> categoryCounts,
            KnightData knight)
        {
            float score = 0f;
            int totalBuildings = 0;
            foreach (var kv in categoryCounts) totalBuildings += kv.Value;

            if (totalBuildings > 0)
            {
                switch (upgrade.AffinityTag)
                {
                    case UpgradeAffinityTag.Ranged:
                        score += GetShare(categoryCounts, BuildingCategory.Archers, totalBuildings)
                               + GetShare(categoryCounts, BuildingCategory.Siege, totalBuildings);
                        break;
                    case UpgradeAffinityTag.Melee:
                        score += GetShare(categoryCounts, BuildingCategory.Melee, totalBuildings);
                        break;
                    case UpgradeAffinityTag.Control:
                        score += GetShare(categoryCounts, BuildingCategory.Control, totalBuildings);
                        break;
                    case UpgradeAffinityTag.Economy:
                        score += GetShare(categoryCounts, BuildingCategory.Economy, totalBuildings);
                        break;
                    case UpgradeAffinityTag.Magic:
                        score += GetShare(categoryCounts, BuildingCategory.Mages, totalBuildings)
                               + GetShare(categoryCounts, BuildingCategory.Support, totalBuildings);
                        break;
                    case UpgradeAffinityTag.Defense:
                        score += GetShare(categoryCounts, BuildingCategory.Control, totalBuildings) * 0.5f
                               + GetShare(categoryCounts, BuildingCategory.Melee, totalBuildings) * 0.5f;
                        break;
                }
            }

            if (knight != null)
            {
                KnightPassiveId pid = knight.PassiveId;
                bool knightAffinity =
                    (pid == KnightPassiveId.BastionHador && (upgrade.AffinityTag == UpgradeAffinityTag.Castle || upgrade.AffinityTag == UpgradeAffinityTag.Defense || upgrade.AffinityTag == UpgradeAffinityTag.Melee)) ||
                    (pid == KnightPassiveId.StormcallerFinrod && (upgrade.AffinityTag == UpgradeAffinityTag.Magic || upgrade.AffinityTag == UpgradeAffinityTag.Weapon)) ||
                    (pid == KnightPassiveId.FireforgeBorin && (upgrade.AffinityTag == UpgradeAffinityTag.Weapon || upgrade.EffectType == UpgradeEffectType.BombDamagePercent || upgrade.AffinityTag == UpgradeAffinityTag.Ranged)) ||
                    (pid == KnightPassiveId.ShadowstepAradan && (upgrade.AffinityTag == UpgradeAffinityTag.Control || upgrade.AffinityTag == UpgradeAffinityTag.Ranged)) ||
                    (pid == KnightPassiveId.GoldlickBaron && upgrade.AffinityTag == UpgradeAffinityTag.Economy);

                if (knightAffinity)
                {
                    score += 0.6f;
                }
            }

            return Mathf.Clamp01(score);
        }

        private static float GetShare(Dictionary<BuildingCategory, int> counts, BuildingCategory cat, int total)
        {
            return counts.TryGetValue(cat, out int count) && total > 0 ? (float)count / total : 0f;
        }

        private static int PickWeightedIndex(List<float> weights)
        {
            float totalWeight = 0f;
            for (int i = 0; i < weights.Count; i++) totalWeight += weights[i];
            if (totalWeight <= 0f) return Random.Range(0, weights.Count);

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < weights.Count; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative) return i;
            }
            return weights.Count - 1;
        }

        [Button]
        public void PopulateWithDefaultUpgrades()
        {
            _upgrades.Clear();
            List<UpgradeData> defaults = CreateDefaultUpgradesList();

#if UNITY_EDITOR
            if (UnityEditor.AssetDatabase.Contains(this))
            {
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
                Object[] existingSubAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
                for (int i = 0; i < existingSubAssets.Length; i++)
                {
                    if (existingSubAssets[i] != null &&
                        existingSubAssets[i] != this &&
                        UnityEditor.AssetDatabase.IsSubAsset(existingSubAssets[i]) &&
                        existingSubAssets[i] is UpgradeData)
                    {
                        DestroyImmediate(existingSubAssets[i], true);
                    }
                }

                for (int i = 0; i < defaults.Count; i++)
                {
                    UpgradeData u = defaults[i];
                    u.name = !string.IsNullOrEmpty(u.Id) ? u.Id : $"Upgrade_{i}";
                    UnityEditor.AssetDatabase.AddObjectToAsset(u, this);
                }

                _upgrades.AddRange(defaults);
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
                return;
            }
#endif

            _upgrades.AddRange(defaults);
        }

        public static List<UpgradeData> CreateDefaultUpgradesList()
        {
            var list = new List<UpgradeData>();

            void Add(string id, string title, string desc, UpgradeEffectType effect, float val, UpgradeRarity rarity)
            {
                var u = CreateInstance<UpgradeData>();
                u.InitializeRuntime(id, title, desc, effect, val, rarity);
                list.Add(u);
            }

            // Castle
            Add("upg_fort_walls", "Укреплённые стены", "+15% к максимальной прочности замка", UpgradeEffectType.CastleMaxHealthPercent, 0.15f, UpgradeRarity.Common);
            Add("upg_stone_masonry", "Каменная кладка", "+50 единиц к макс. прочности замка", UpgradeEffectType.CastleMaxHealthFlat, 50f, UpgradeRarity.Common);
            Add("upg_repair", "Ремонт", "Восстанавливает 25% прочности замка", UpgradeEffectType.CastleHealPercent, 0.25f, UpgradeRarity.Common);
            Add("upg_expansion", "Расширение", "+1 дополнительный слот для построек", UpgradeEffectType.BuildSlotAdd, 1f, UpgradeRarity.Rare);

            // Combat
            Add("upg_sharpened_arrows", "Отточенные стрелы", "+15% урона дальнобойным юнитам", UpgradeEffectType.RangedDamagePercent, 0.15f, UpgradeRarity.Common);
            Add("upg_tempered_blades", "Закалённые мечи", "+15% урона ближнобойным юнитам", UpgradeEffectType.MeleeDamagePercent, 0.15f, UpgradeRarity.Common);
            Add("upg_battle_cry", "Боевой клич", "+10% скорости атаки всех войск", UpgradeEffectType.AttackSpeedPercent, 0.10f, UpgradeRarity.Rare);
            Add("upg_heavy_shells", "Тяжёлые снаряды", "+20% урона орудия замка", UpgradeEffectType.WeaponDamagePercent, 0.20f, UpgradeRarity.Rare);
            Add("upg_rapid_fire", "Скорострельность", "-15% времени перезарядки орудия замка", UpgradeEffectType.WeaponCooldownPercent, 0.15f, UpgradeRarity.Rare);

            // Defense
            Add("upg_thick_armor", "Толстая броня", "+20% HP всех дружественных юнитов", UpgradeEffectType.UnitHealthPercent, 0.20f, UpgradeRarity.Common);
            Add("upg_quick_respawn", "Быстрое возрождение", "-20% время респавна юнитов из лагерей", UpgradeEffectType.UnitRespawnPercent, 0.20f, UpgradeRarity.Rare);
            Add("upg_fort_buildings", "Крепкие постройки", "+20% прочности всех зданий", UpgradeEffectType.BuildingHealthPercent, 0.20f, UpgradeRarity.Common);

            // Economy
            Add("upg_greed", "Жадность", "+1 дополнительная монета за каждого убитого врага", UpgradeEffectType.CoinPerKill, 1f, UpgradeRarity.Common);
            Add("upg_diamond_luck", "Алмазная удача", "+3% шанс выпадения алмаза с врагов", UpgradeEffectType.DiamondDropChanceAdd, 0.03f, UpgradeRarity.Rare);
            Add("upg_gold_rush", "Золотая лихорадка", "Шахты производят золото на 25% быстрее", UpgradeEffectType.EconomicSpeedPercent, 0.25f, UpgradeRarity.Rare);

            // Control
            Add("upg_frost_touch", "Ледяное касание", "Все атаки замедляют врагов на 10% на 2 сек", UpgradeEffectType.SlowOnHitPercent, 0.10f, UpgradeRarity.Rare);
            Add("upg_spiked_ground", "Шипованная земля", "+30% урона ловушек с шипами", UpgradeEffectType.TrapDamagePercent, 0.30f, UpgradeRarity.Common);
            Add("upg_explosive_potential", "Взрывной потенциал", "+25% урона и радиуса бомб", UpgradeEffectType.BombDamagePercent, 0.25f, UpgradeRarity.Rare);

            return list;
        }
    }
}
