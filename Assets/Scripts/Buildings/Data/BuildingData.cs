using System;
using UnityEngine;
using Alchemy.Inspector;

namespace Game.Buildings
{
    [CreateAssetMenu(fileName = "NewBuildingData", menuName = "Game/Buildings/Building Data", order = 15)]
    public class BuildingData : ScriptableObject
    {
        [Title("Identity")]
        [SerializeField] private string _id = "building_id";
        [SerializeField] private string _displayName = "Название постройки";
        [TextArea(2, 3)]
        [SerializeField] private string _description = "Описание постройки";
        [SerializeField] private Sprite _icon;
        [SerializeField] private Building _prefab;

        [Title("Classification & Footprint")]
        [SerializeField] private BuildingCategory _category = BuildingCategory.Control;
        [SerializeField] private Vector2Int _size = new(1, 1);
        [SerializeField] private BuildingUpgradeType _upgradeType = BuildingUpgradeType.Local;
        [SerializeField] private bool _isDestructible = true;

        [Title("Economy")]
        [SerializeField, Range(0f, 1f)] private float _placedSaleRefundRatio = 0.5f; // 50% per GDD
        [SerializeField, Range(0f, 1f)] private float _unplacedSaleRefundRatio = 1.0f; // 100%

        [Title("Level Stats (1 to 4)")]
        [SerializeField]
        private BuildingLevelStats[] _levels = new BuildingLevelStats[4]
        {
            new() { cost = 50, maxHP = 200, damage = 0, attackRange = 0f, attackInterval = 1f },
            new() { cost = 100, maxHP = 300, damage = 0, attackRange = 0f, attackInterval = 1f },
            new() { cost = 200, maxHP = 400, damage = 0, attackRange = 0f, attackInterval = 1f },
            new() { cost = 350, maxHP = 600, damage = 0, attackRange = 0f, attackInterval = 1f }
        };

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public Building Prefab => _prefab;
        public BuildingCategory Category => _category;
        public Vector2Int Size => _size;
        public BuildingUpgradeType UpgradeType => _upgradeType;
        public bool IsDestructible => _isDestructible;
        public float PlacedSaleRefundRatio => _placedSaleRefundRatio;
        public float UnplacedSaleRefundRatio => _unplacedSaleRefundRatio;

        public BuildingLevelStats GetStats(int level)
        {
            int index = Mathf.Clamp(level - 1, 0, _levels.Length - 1);
            return _levels[index];
        }

        public void InitializeRuntime(
            string id,
            string name,
            string description,
            BuildingCategory category,
            Vector2Int size,
            BuildingUpgradeType upgradeType,
            BuildingLevelStats[] levels,
            Building prefab = null,
            Sprite icon = null)
        {
            _id = id;
            _displayName = name;
            _description = description;
            _category = category;
            _size = size;
            _upgradeType = upgradeType;
            _levels = levels;
            _prefab = prefab;
            _icon = icon;
        }
    }
}
