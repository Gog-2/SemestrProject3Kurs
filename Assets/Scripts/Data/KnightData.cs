using UnityEngine;
using Alchemy.Inspector;
using Game.Buildings;

namespace Game.Data
{
    public enum KnightPassiveId
    {
        Custom = 0,
        BastionHador = 1,
        StormcallerFinrod = 2,
        FireforgeBorin = 3,
        ShadowstepAradan = 4,
        GoldlickBaron = 5
    }

    [CreateAssetMenu(fileName = "NewKnightData", menuName = "Game/Meta/Knight Data", order = 40)]
    public class KnightData : ScriptableObject
    {
        [Title("Identity")]
        [SerializeField] private string _id = "hador_oplot";
        [SerializeField] private string _knightName = "Хадор Оплот";
        [SerializeField] private string _title = "Рыцарь-Бастион";
        [TextArea(2, 3)]
        [SerializeField] private string _quote = "«Камень не молит о пощаде, он просто стоит.»";
        [SerializeField] private Sprite _portrait;

        [Title("Castle Stats")]
        [SerializeField, Min(100)] private int _castleMaxHP = 2000;
        [SerializeField, Min(0)] private int _hpPerLevel = 125;
        [SerializeField, Min(1)] private int _initialBuildSlots = 4;

        [Title("Starting Buildings & Skills")]
        [SerializeField] private BuildingData[] _startingBuildingAssets = System.Array.Empty<BuildingData>();
        [SerializeField] private WeaponSelectionData _preferredStartingWeapon;
        [SerializeField] private string[] _startingBuildings = new[] { "Стены", "Казарма щитовиков" };
        [SerializeField] private string[] _startingSkills = new[] { "Несокрушимый фундамент (+30% HP построек)", "Стена щитов (+1 удержание, +20% HP юнитов)" };

        [Title("Structured Passive Profile (GIG-97..102)")]
        [SerializeField] private KnightPassiveId _passiveId = KnightPassiveId.BastionHador;
        [SerializeField] private float _destructibleBuildingHpBonus = 0.30f;
        [SerializeField] private float _nonEconomyBuildingHpBonus = 0f;
        [SerializeField] private int _economyBuildingHpFlatBonus = 0;
        [SerializeField] private float _economySpeedBonus = 0f;
        [SerializeField] private float _unitMaxHpBonus = 0.20f;
        [SerializeField] private int _unitAggroBonus = 1;
        [SerializeField] private float _mageCooldownReduction = 0f;
        [SerializeField] private float _supportEffectivenessBonus = 0f;
        [SerializeField] private float _explosiveDamageBonus = 0f;
        [SerializeField] private float _explosiveRadiusBonus = 0f;
        [SerializeField] private float _piercingCooldownSpeedBonus = 0f;
        [SerializeField] private float _piercingRangeBonus = 0f;
        [SerializeField] private float _builderRepairSpeedBonus = 0f;
        [SerializeField] private float _builderRangeBonus = 0f;
        [SerializeField] private float _trapDamageBonus = 0f;
        [SerializeField] private float _trapUsesBonus = 0f;
        [SerializeField] private float _controlSlowPercent = 0f;
        [SerializeField] private float _controlSlowDuration = 0f;
        [SerializeField] private float _rangedUnitDamageBonus = 0f;
        [SerializeField] private float _hunterStealthBonusSeconds = 0f;
        [SerializeField] private int _bonusCoinPerKill = 0;

        [Title("Meta Unlock")]
        [SerializeField] private bool _unlockedByDefault = true;
        [SerializeField, Min(0)] private int _diamondCost = 0;

        public string Id => _id;
        public string KnightName => _knightName;
        public string Title => _title;
        public string Quote => _quote;
        public Sprite Portrait => _portrait;
        public int CastleMaxHP => _castleMaxHP;
        public int HPPerLevel => _hpPerLevel;
        public int InitialBuildSlots => _initialBuildSlots;
        public BuildingData[] StartingBuildingAssets => _startingBuildingAssets;
        public WeaponSelectionData PreferredStartingWeapon => _preferredStartingWeapon;
        public string[] StartingBuildings => _startingBuildings;
        public string[] StartingSkills => _startingSkills;
        public bool UnlockedByDefault => _unlockedByDefault;
        public int DiamondCost => _diamondCost;

        public KnightPassiveId PassiveId => _passiveId;
        public float DestructibleBuildingHpBonus => GetEffectivePassive(KnightPassiveId.BastionHador, _destructibleBuildingHpBonus, 0.30f);
        public float NonEconomyBuildingHpBonus => GetEffectivePassive(KnightPassiveId.GoldlickBaron, _nonEconomyBuildingHpBonus, -0.25f);
        public int EconomyBuildingHpFlatBonus => GetEffectivePassiveInt(KnightPassiveId.GoldlickBaron, _economyBuildingHpFlatBonus, 25);
        public float EconomySpeedBonus => GetEffectivePassive(KnightPassiveId.GoldlickBaron, _economySpeedBonus, 0.30f);
        public float UnitMaxHpBonus => GetEffectivePassive(KnightPassiveId.BastionHador, _unitMaxHpBonus, 0.20f);
        public int UnitAggroBonus => GetEffectivePassiveInt(KnightPassiveId.BastionHador, _unitAggroBonus, 1);
        public float MageCooldownReduction => GetEffectivePassive(KnightPassiveId.StormcallerFinrod, _mageCooldownReduction, 0.15f);
        public float SupportEffectivenessBonus => GetEffectivePassive(KnightPassiveId.StormcallerFinrod, _supportEffectivenessBonus, 0.10f);
        public float ExplosiveDamageBonus => GetEffectivePassive(KnightPassiveId.FireforgeBorin, _explosiveDamageBonus, 0.20f);
        public float ExplosiveRadiusBonus => GetEffectivePassive(KnightPassiveId.FireforgeBorin, _explosiveRadiusBonus, 1f);
        public float PiercingCooldownSpeedBonus => GetEffectivePassive(KnightPassiveId.FireforgeBorin, _piercingCooldownSpeedBonus, 0.20f);
        public float PiercingRangeBonus => GetEffectivePassive(KnightPassiveId.FireforgeBorin, _piercingRangeBonus, 1f);
        public float BuilderRepairSpeedBonus => GetEffectivePassive(KnightPassiveId.FireforgeBorin, _builderRepairSpeedBonus, 0.25f);
        public float BuilderRangeBonus => GetEffectivePassive(KnightPassiveId.FireforgeBorin, _builderRangeBonus, 1f);
        public float TrapDamageBonus => GetEffectivePassive(KnightPassiveId.ShadowstepAradan, _trapDamageBonus, 0.10f);
        public float TrapUsesBonus => GetEffectivePassive(KnightPassiveId.ShadowstepAradan, _trapUsesBonus, 0.20f);
        public float ControlSlowPercent => GetEffectivePassive(KnightPassiveId.ShadowstepAradan, _controlSlowPercent, 0.40f);
        public float ControlSlowDuration => GetEffectivePassive(KnightPassiveId.ShadowstepAradan, _controlSlowDuration, 3f);
        public float RangedUnitDamageBonus => GetEffectivePassive(KnightPassiveId.ShadowstepAradan, _rangedUnitDamageBonus, 0.25f);
        public float HunterStealthBonusSeconds => GetEffectivePassive(KnightPassiveId.ShadowstepAradan, _hunterStealthBonusSeconds, 2f);
        public int BonusCoinPerKill => GetEffectivePassiveInt(KnightPassiveId.GoldlickBaron, _bonusCoinPerKill, 1);

        private void OnEnable()
        {
            if (_passiveId == KnightPassiveId.Custom)
            {
                InferPresetFromId();
            }
        }

        private float GetEffectivePassive(KnightPassiveId targetPreset, float serializedValue, float presetFallback)
        {
            if (Mathf.Abs(serializedValue) > 0.0001f) return serializedValue;
            return ResolvePreset() == targetPreset ? presetFallback : 0f;
        }

        private int GetEffectivePassiveInt(KnightPassiveId targetPreset, int serializedValue, int presetFallback)
        {
            if (serializedValue != 0) return serializedValue;
            return ResolvePreset() == targetPreset ? presetFallback : 0;
        }

        private KnightPassiveId ResolvePreset()
        {
            if (_passiveId != KnightPassiveId.Custom) return _passiveId;
            return InferPresetFromId();
        }

        private KnightPassiveId InferPresetFromId()
        {
            return _id switch
            {
                "hador_oplot" => _passiveId = KnightPassiveId.BastionHador,
                "finrod_burevestnik" => _passiveId = KnightPassiveId.StormcallerFinrod,
                "borin_ogneplav" => _passiveId = KnightPassiveId.FireforgeBorin,
                "aradan_tenehod" => _passiveId = KnightPassiveId.ShadowstepAradan,
                "baron_zlatoliz" => _passiveId = KnightPassiveId.GoldlickBaron,
                _ => KnightPassiveId.Custom
            };
        }

        public void InitializeRuntime(string id, string name, string title, string quote, int hp, int hpPerLvl, int slots, string[] buildings, string[] skills, bool unlocked, int cost)
        {
            _id = id;
            _knightName = name;
            _title = title;
            _quote = quote;
            _castleMaxHP = hp;
            _hpPerLevel = hpPerLvl;
            _initialBuildSlots = slots;
            _startingBuildings = buildings;
            _startingSkills = skills;
            _unlockedByDefault = unlocked;
            _diamondCost = cost;
            ApplyPresetDefaults(InferPresetFromId());
        }

        public void SetStartingBuildingAssets(BuildingData[] buildingAssets, WeaponSelectionData preferredWeapon = null)
        {
            _startingBuildingAssets = buildingAssets ?? System.Array.Empty<BuildingData>();
            _preferredStartingWeapon = preferredWeapon;
        }

        public void SetPortrait(Sprite portrait)
        {
            _portrait = portrait;
        }

        public void ApplyPresetDefaults(KnightPassiveId preset)
        {
            _passiveId = preset;
            _destructibleBuildingHpBonus = preset == KnightPassiveId.BastionHador ? 0.30f : 0f;
            _nonEconomyBuildingHpBonus = preset == KnightPassiveId.GoldlickBaron ? -0.25f : 0f;
            _economyBuildingHpFlatBonus = preset == KnightPassiveId.GoldlickBaron ? 25 : 0;
            _economySpeedBonus = preset == KnightPassiveId.GoldlickBaron ? 0.30f : 0f;
            _unitMaxHpBonus = preset == KnightPassiveId.BastionHador ? 0.20f : 0f;
            _unitAggroBonus = preset == KnightPassiveId.BastionHador ? 1 : 0;
            _mageCooldownReduction = preset == KnightPassiveId.StormcallerFinrod ? 0.15f : 0f;
            _supportEffectivenessBonus = preset == KnightPassiveId.StormcallerFinrod ? 0.10f : 0f;
            _explosiveDamageBonus = preset == KnightPassiveId.FireforgeBorin ? 0.20f : 0f;
            _explosiveRadiusBonus = preset == KnightPassiveId.FireforgeBorin ? 1f : 0f;
            _piercingCooldownSpeedBonus = preset == KnightPassiveId.FireforgeBorin ? 0.20f : 0f;
            _piercingRangeBonus = preset == KnightPassiveId.FireforgeBorin ? 1f : 0f;
            _builderRepairSpeedBonus = preset == KnightPassiveId.FireforgeBorin ? 0.25f : 0f;
            _builderRangeBonus = preset == KnightPassiveId.FireforgeBorin ? 1f : 0f;
            _trapDamageBonus = preset == KnightPassiveId.ShadowstepAradan ? 0.10f : 0f;
            _trapUsesBonus = preset == KnightPassiveId.ShadowstepAradan ? 0.20f : 0f;
            _controlSlowPercent = preset == KnightPassiveId.ShadowstepAradan ? 0.40f : 0f;
            _controlSlowDuration = preset == KnightPassiveId.ShadowstepAradan ? 3f : 0f;
            _rangedUnitDamageBonus = preset == KnightPassiveId.ShadowstepAradan ? 0.25f : 0f;
            _hunterStealthBonusSeconds = preset == KnightPassiveId.ShadowstepAradan ? 2f : 0f;
            _bonusCoinPerKill = preset == KnightPassiveId.GoldlickBaron ? 1 : 0;
        }
    }
}
