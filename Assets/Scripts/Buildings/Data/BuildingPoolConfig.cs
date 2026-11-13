using System;
using System.Collections.Generic;
using UnityEngine;
using Alchemy.Inspector;
using Game.Data;
using Game.Upgrades;

namespace Game.Buildings
{
    [Serializable]
    public class BuildingPoolEntry
    {
        public BuildingData Data;
        [Min(0.01f)] public float Weight = 1f;
        [Min(1)] public int MinCastleLevel = 1;
        public string RequiredKnightId = string.Empty;
        [Tooltip("0 = uses Level 1 BuildingData cost as shop blueprint price.")]
        [Min(0)] public int OverrideShopPrice = 0;

        public int GetPrice(float discountMultiplier = 1f)
        {
            if (Data == null) return 0;
            int raw = OverrideShopPrice > 0 ? OverrideShopPrice : Mathf.RoundToInt(Data.GetStats(1).cost * 0.5f);
            return Mathf.Max(5, Mathf.RoundToInt(raw * discountMultiplier));
        }

        public bool Matches(int castleLevel, KnightData knight)
        {
            if (Data == null) return false;
            if (castleLevel < MinCastleLevel) return false;
            if (!string.IsNullOrEmpty(RequiredKnightId))
            {
                if (knight == null || knight.Id != RequiredKnightId) return false;
            }
            return true;
        }
    }

    [Serializable]
    public class ShopMinorBonusEntry
    {
        public string Id;
        public string Title;
        public string Description;
        public UpgradeEffectType EffectType;
        public float Value;
        [Min(1)] public int Cost = 35;
        [Min(0.01f)] public float Weight = 1f;
    }

    [CreateAssetMenu(fileName = "BuildingPoolConfig", menuName = "Game/Buildings/Building Pool Config", order = 16)]
    public class BuildingPoolConfig : ScriptableObject
    {
        [Title("Building Blueprints Pool")]
        [SerializeField] private List<BuildingPoolEntry> _buildingEntries = new();

        [Title("Minor Pause Bonuses Pool")]
        [SerializeField] private List<ShopMinorBonusEntry> _minorBonuses = new();

        public IReadOnlyList<BuildingPoolEntry> BuildingEntries => _buildingEntries;
        public IReadOnlyList<ShopMinorBonusEntry> MinorBonuses => _minorBonuses;

        public void SetEntries(List<BuildingPoolEntry> buildings, List<ShopMinorBonusEntry> bonuses)
        {
            _buildingEntries = buildings ?? new List<BuildingPoolEntry>();
            _minorBonuses = bonuses ?? new List<ShopMinorBonusEntry>();
        }

        public List<BuildingPoolEntry> RollBuildingOffers(int count, int castleLevel, KnightData knight, IReadOnlyCollection<BuildingData> exclude = null)
        {
            var candidates = new List<BuildingPoolEntry>();
            for (int i = 0; i < _buildingEntries.Count; i++)
            {
                BuildingPoolEntry e = _buildingEntries[i];
                if (e == null || !e.Matches(castleLevel, knight)) continue;
                if (exclude != null && ContainsBuilding(exclude, e.Data)) continue;
                candidates.Add(e);
            }

            var result = new List<BuildingPoolEntry>(count);
            for (int i = 0; i < count && candidates.Count > 0; i++)
            {
                int idx = PickWeightedBuildingIndex(candidates);
                result.Add(candidates[idx]);
                candidates.RemoveAt(idx);
            }

            return result;
        }

        public BuildingPoolEntry RollSingleReplacement(int castleLevel, KnightData knight, IReadOnlyCollection<BuildingData> activeOfferDatas)
        {
            List<BuildingPoolEntry> rolled = RollBuildingOffers(1, castleLevel, knight, activeOfferDatas);
            if (rolled.Count > 0) return rolled[0];

            // Fallback if pool has fewer unique buildings than 4: allow repeats from matching pool
            rolled = RollBuildingOffers(1, castleLevel, knight, null);
            return rolled.Count > 0 ? rolled[0] : null;
        }

        public List<ShopMinorBonusEntry> RollMinorBonusOffers(int count)
        {
            EnsureDefaultBonusesIfEmpty();

            var candidates = new List<ShopMinorBonusEntry>(_minorBonuses);
            var result = new List<ShopMinorBonusEntry>(count);

            for (int i = 0; i < count && candidates.Count > 0; i++)
            {
                int idx = PickWeightedBonusIndex(candidates);
                result.Add(candidates[idx]);
                candidates.RemoveAt(idx);
            }

            return result;
        }

        private void EnsureDefaultBonusesIfEmpty()
        {
            if (_minorBonuses != null && _minorBonuses.Count > 0) return;
            _minorBonuses = CreateDefaultMinorBonuses();
        }

        public static List<ShopMinorBonusEntry> CreateDefaultMinorBonuses()
        {
            return new List<ShopMinorBonusEntry>
            {
                new() { Id = "shop_ranged_dmg", Title = "Пристрелка", Description = "+8% урона стрелков и осадных башен", EffectType = UpgradeEffectType.RangedDamagePercent, Value = 0.08f, Cost = 35, Weight = 1f },
                new() { Id = "shop_melee_dmg", Title = "Заточка клинков", Description = "+8% урона бойцов ближнего боя", EffectType = UpgradeEffectType.MeleeDamagePercent, Value = 0.08f, Cost = 35, Weight = 1f },
                new() { Id = "shop_attack_spd", Title = "Боевой темп", Description = "+6% скорости атаки построек", EffectType = UpgradeEffectType.AttackSpeedPercent, Value = 0.06f, Cost = 40, Weight = 1f },
                new() { Id = "shop_unit_hp", Title = "Усиленная броня", Description = "+10% HP союзных юнитов", EffectType = UpgradeEffectType.UnitHealthPercent, Value = 0.10f, Cost = 35, Weight = 1f },
                new() { Id = "shop_bldg_hp", Title = "Укрепление кладки", Description = "+10% HP построек", EffectType = UpgradeEffectType.BuildingHealthPercent, Value = 0.10f, Cost = 30, Weight = 1f },
                new() { Id = "shop_repair_all", Title = "Полевой ремонт", Description = "Чинит все постройки на 30% HP", EffectType = UpgradeEffectType.RepairAllBuildingsPercent, Value = 0.30f, Cost = 30, Weight = 0.9f },
                new() { Id = "shop_weapon_dmg", Title = "Усиление орудия", Description = "+10% урона орудия замка", EffectType = UpgradeEffectType.WeaponDamagePercent, Value = 0.10f, Cost = 40, Weight = 0.9f },
                new() { Id = "shop_economy_spd", Title = "Тяжёлые кирки", Description = "+12% скорости добычи шахт", EffectType = UpgradeEffectType.EconomicSpeedPercent, Value = 0.12f, Cost = 45, Weight = 0.8f },
                new() { Id = "shop_spell_pwr", Title = "Эфирный концентрат", Description = "+10% силы заклинаний", EffectType = UpgradeEffectType.SpellDamagePercent, Value = 0.10f, Cost = 40, Weight = 0.85f }
            };
        }

        private static bool ContainsBuilding(IReadOnlyCollection<BuildingData> set, BuildingData target)
        {
            foreach (var b in set)
            {
                if (b == target) return true;
            }
            return false;
        }

        private static int PickWeightedBuildingIndex(List<BuildingPoolEntry> list)
        {
            float total = 0f;
            for (int i = 0; i < list.Count; i++) total += Mathf.Max(0.01f, list[i].Weight);
            float roll = UnityEngine.Random.Range(0f, total);
            float acc = 0f;
            for (int i = 0; i < list.Count; i++)
            {
                acc += Mathf.Max(0.01f, list[i].Weight);
                if (roll <= acc) return i;
            }
            return list.Count - 1;
        }

        private static int PickWeightedBonusIndex(List<ShopMinorBonusEntry> list)
        {
            float total = 0f;
            for (int i = 0; i < list.Count; i++) total += Mathf.Max(0.01f, list[i].Weight);
            float roll = UnityEngine.Random.Range(0f, total);
            float acc = 0f;
            for (int i = 0; i < list.Count; i++)
            {
                acc += Mathf.Max(0.01f, list[i].Weight);
                if (roll <= acc) return i;
            }
            return list.Count - 1;
        }
    }
}
