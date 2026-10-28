using UnityEngine;
using Alchemy.Inspector;

namespace Game.Data
{
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
        [SerializeField] private string[] _startingBuildings = new[] { "Стены", "Казарма щитовиков" };
        [SerializeField] private string[] _startingSkills = new[] { "Несокрушимый фундамент (+30% HP построек)", "Стена щитов (+1 удержание, +20% HP юнитов)" };

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
        public string[] StartingBuildings => _startingBuildings;
        public string[] StartingSkills => _startingSkills;
        public bool UnlockedByDefault => _unlockedByDefault;
        public int DiamondCost => _diamondCost;

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
        }
    }
}
