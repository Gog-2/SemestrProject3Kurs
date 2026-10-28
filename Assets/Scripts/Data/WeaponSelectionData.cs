using UnityEngine;
using Alchemy.Inspector;
using Game.Weapons;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewWeaponSelectionData", menuName = "Game/Meta/Weapon Selection Data", order = 42)]
    public class WeaponSelectionData : ScriptableObject
    {
        [Title("Identity")]
        [SerializeField] private string _id = "heavy_ballista";
        [SerializeField] private string _weaponName = "Тяжёлая баллиста";
        [TextArea(2, 3)]
        [SerializeField] private string _description = "Стреляет мощными болтами, пробивающими всех врагов и отталкивающими их на 1 клетку.";
        [SerializeField] private Sprite _icon;
        [SerializeField] private HeavyBallistaData _ballistaData;

        [Title("Meta Unlock")]
        [SerializeField] private bool _unlockedByDefault = true;
        [SerializeField, Min(0)] private int _diamondCost = 0;

        public string Id => _id;
        public string WeaponName => _weaponName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public HeavyBallistaData BallistaData => _ballistaData;
        public bool UnlockedByDefault => _unlockedByDefault;
        public int DiamondCost => _diamondCost;

        public void InitializeRuntime(string id, string name, string description, bool unlocked, int cost, HeavyBallistaData data = null)
        {
            _id = id;
            _weaponName = name;
            _description = description;
            _unlockedByDefault = unlocked;
            _diamondCost = cost;
            _ballistaData = data;
        }
    }
}
