using UnityEngine;
using Alchemy.Inspector;

namespace Game.Upgrades
{
    [CreateAssetMenu(fileName = "NewUpgrade", menuName = "Game/Upgrade Data", order = 30)]
    public class UpgradeData : ScriptableObject
    {
        [Title("Identity")]
        [SerializeField] private string _id = "upgrade_id";
        [SerializeField] private string _title = "Название улучшения";
        [TextArea(2, 4)]
        [SerializeField] private string _description = "Описание эффекта улучшения";
        [SerializeField] private Sprite _icon;
        [SerializeField] private UpgradeRarity _rarity = UpgradeRarity.Common;

        [Title("Effect")]
        [SerializeField] private UpgradeEffectType _effectType = UpgradeEffectType.CastleMaxHealthPercent;
        [SerializeField] private float _value = 0.15f;

        public string Id => _id;
        public string Title => _title;
        public string Description => _description;
        public Sprite Icon => _icon;
        public UpgradeRarity Rarity => _rarity;
        public UpgradeEffectType EffectType => _effectType;
        public float Value => _value;

        public void InitializeRuntime(string id, string title, string description, UpgradeEffectType effectType, float value, UpgradeRarity rarity = UpgradeRarity.Common)
        {
            _id = id;
            _title = title;
            _description = description;
            _effectType = effectType;
            _value = value;
            _rarity = rarity;
        }
    }
}
