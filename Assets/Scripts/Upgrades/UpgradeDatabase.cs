using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Alchemy.Inspector;

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
            var pool = new List<UpgradeData>();

            for (int i = 0; i < _upgrades.Count; i++)
            {
                UpgradeData upgrade = _upgrades[i];
                if (upgrade == null) continue;
                if (excluded != null && excluded.Contains(upgrade)) continue;
                pool.Add(upgrade);
            }

            // Fallback: if pool is empty or not enough, populate with all non-null upgrades
            if (pool.Count < count)
            {
                pool.Clear();
                for (int i = 0; i < _upgrades.Count; i++)
                {
                    if (_upgrades[i] != null) pool.Add(_upgrades[i]);
                }
            }

            var result = new List<UpgradeData>();
            int picksNeeded = Mathf.Min(count, pool.Count);

            for (int i = 0; i < picksNeeded; i++)
            {
                int randomIndex = Random.Range(0, pool.Count);
                result.Add(pool[randomIndex]);
                pool.RemoveAt(randomIndex);
            }

            return result;
        }

        [Button]
        public void PopulateWithDefaultUpgrades()
        {
            _upgrades.Clear();
            _upgrades.AddRange(CreateDefaultUpgradesList());
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
