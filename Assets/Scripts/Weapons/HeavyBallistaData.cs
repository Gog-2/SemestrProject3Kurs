using System;
using UnityEngine;
using Alchemy.Inspector;
using Game.Projectiles;

namespace Game.Weapons
{
    [Serializable]
    public struct BallistaLevelStats
    {
        public int damage;
        public float cooldown;
        public float speed;
        public float knockbackCells;
        public int pierceCount;
    }

    [CreateAssetMenu(fileName = "HeavyBallistaData", menuName = "Game/Weapons/Heavy Ballista Data", order = 10)]
    public class HeavyBallistaData : ScriptableObject
    {
        [Title("Identity")]
        [SerializeField] private string _weaponName = "Тяжелая баллиста";
        [TextArea(2, 4)]
        [SerializeField] private string _description = "Выстреливает массивными болтами, болты пробивают всех врагов насквозь и отталкивают их назад на одну клетку.";
        [SerializeField] private Sprite _icon;

        [Title("Projectile Reference")]
        [SerializeField] private ProjectileData _projectileData;

        [Title("Levels (1 to 4)")]
        [SerializeField]
        private BallistaLevelStats[] _levels = new BallistaLevelStats[4]
        {
            new() { damage = 15, cooldown = 8f, speed = 15f, knockbackCells = 1f, pierceCount = 999 },
            new() { damage = 20, cooldown = 6f, speed = 15f, knockbackCells = 1f, pierceCount = 999 },
            new() { damage = 30, cooldown = 4f, speed = 15f, knockbackCells = 1f, pierceCount = 999 },
            new() { damage = 40, cooldown = 2f, speed = 15f, knockbackCells = 1f, pierceCount = 999 }
        };

        public string WeaponName => _weaponName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public ProjectileData ProjectileData => _projectileData;

        public BallistaLevelStats GetStats(int level)
        {
            int index = Mathf.Clamp(level - 1, 0, _levels.Length - 1);
            return _levels[index];
        }
    }
}
