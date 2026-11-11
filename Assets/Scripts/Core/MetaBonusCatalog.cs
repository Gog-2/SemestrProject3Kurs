using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    public enum MetaBonusType
    {
        CastleHealthPercent,
        StartingCoinsFlat,
        CombatDamagePercent,
        EconomySpeedPercent,
        WeaponAndSpellPowerPercent,
        DiamondDropChancePercent
    }

    [Serializable]
    public class MetaBonusDefinition
    {
        public string Id;
        public string Title;
        public string DescriptionFormat;
        public MetaBonusType BonusType;
        public float ValuePerLevel;
        public int MaxLevel;
        public int[] LevelCosts;

        public int GetCostForNextLevel(int currentLevel)
        {
            if (currentLevel < 0 || LevelCosts == null || currentLevel >= LevelCosts.Length)
            {
                return -1;
            }
            return LevelCosts[currentLevel];
        }

        public float GetTotalValue(int level)
        {
            return Mathf.Clamp(level, 0, MaxLevel) * ValuePerLevel;
        }
    }

    public static class MetaBonusCatalog
    {
        private static readonly List<MetaBonusDefinition> s_definitions = new()
        {
            new MetaBonusDefinition
            {
                Id = "meta_castle_hp",
                Title = "Каменная цитадель",
                DescriptionFormat = "+{0}% к максимальному HP замка",
                BonusType = MetaBonusType.CastleHealthPercent,
                ValuePerLevel = 0.05f,
                MaxLevel = 5,
                LevelCosts = new[] { 15, 30, 50, 80, 120 }
            },
            new MetaBonusDefinition
            {
                Id = "meta_starting_coins",
                Title = "Стартовая казна",
                DescriptionFormat = "+{0} стартовых монет в начале забега",
                BonusType = MetaBonusType.StartingCoinsFlat,
                ValuePerLevel = 25f,
                MaxLevel = 5,
                LevelCosts = new[] { 15, 30, 50, 80, 120 }
            },
            new MetaBonusDefinition
            {
                Id = "meta_combat_damage",
                Title = "Военная выучка",
                DescriptionFormat = "+{0}% к урону построек и юнитов",
                BonusType = MetaBonusType.CombatDamagePercent,
                ValuePerLevel = 0.05f,
                MaxLevel = 5,
                LevelCosts = new[] { 20, 40, 65, 95, 135 }
            },
            new MetaBonusDefinition
            {
                Id = "meta_economy_speed",
                Title = "Богатые жилы",
                DescriptionFormat = "+{0}% к скорости добычи ресурсов",
                BonusType = MetaBonusType.EconomySpeedPercent,
                ValuePerLevel = 0.06f,
                MaxLevel = 5,
                LevelCosts = new[] { 20, 40, 65, 95, 135 }
            },
            new MetaBonusDefinition
            {
                Id = "meta_weapon_spell",
                Title = "Тайное ремесло",
                DescriptionFormat = "+{0}% к урону орудия замка и магии",
                BonusType = MetaBonusType.WeaponAndSpellPowerPercent,
                ValuePerLevel = 0.06f,
                MaxLevel = 5,
                LevelCosts = new[] { 20, 40, 65, 95, 135 }
            },
            new MetaBonusDefinition
            {
                Id = "meta_diamond_luck",
                Title = "Охотник за кристаллами",
                DescriptionFormat = "+{0}% к шансу выпадения алмазов",
                BonusType = MetaBonusType.DiamondDropChancePercent,
                ValuePerLevel = 0.005f,
                MaxLevel = 5,
                LevelCosts = new[] { 25, 50, 80, 120, 170 }
            }
        };

        public static IReadOnlyList<MetaBonusDefinition> Definitions => s_definitions;

        public static MetaBonusDefinition FindById(string id)
        {
            for (int i = 0; i < s_definitions.Count; i++)
            {
                if (s_definitions[i].Id == id) return s_definitions[i];
            }
            return null;
        }

        public static float GetTotalBonus(MetaBonusType type)
        {
            float sum = 0f;
            for (int i = 0; i < s_definitions.Count; i++)
            {
                if (s_definitions[i].BonusType == type)
                {
                    int lvl = MetaSaveManager.GetBonusLevel(s_definitions[i].Id);
                    sum += s_definitions[i].GetTotalValue(lvl);
                }
            }
            return sum;
        }

        public static string FormatDescription(MetaBonusDefinition def, int currentLevel)
        {
            if (def == null) return string.Empty;

            if (def.BonusType == MetaBonusType.StartingCoinsFlat)
            {
                int nextBonus = Mathf.RoundToInt(def.ValuePerLevel);
                return string.Format(def.DescriptionFormat, nextBonus);
            }

            if (def.BonusType == MetaBonusType.DiamondDropChancePercent)
            {
                float pct = def.ValuePerLevel * 100f;
                return string.Format(def.DescriptionFormat, $"{pct:0.#}");
            }

            int percentPerLvl = Mathf.RoundToInt(def.ValuePerLevel * 100f);
            return string.Format(def.DescriptionFormat, percentPerLvl);
        }
    }
}
