using System;
using UnityEngine;
using Alchemy.Inspector;

namespace Game.Spells
{
    public enum SpellKind
    {
        Meteor = 0,
        IceStorm = 1,
        HeavenlyWrath = 2,
        DeadLegion = 3,
        NewLife = 4
    }

    [Serializable]
    public struct SpellLevelStats
    {
        public int primaryDamage;
        public float areaRadius; // 1.0 = 2x2, 1.5 = 3x3, 2.0 = 4x4, 2.5 = 5x5, 3.0 = 6x6
        public float delaySeconds;
        public float durationSeconds;
        public float secondaryDps;
        public float freezeThreshold;
        public float spawnRatePerSec;
    }

    [CreateAssetMenu(fileName = "NewSpellData", menuName = "Game/Spells/Spell Data", order = 25)]
    public class SpellData : ScriptableObject
    {
        [Title("Identity")]
        [SerializeField] private SpellKind _spellKind = SpellKind.Meteor;
        [SerializeField] private SpellEffect _effect;
        [SerializeField] private string _spellName = "Метеор";
        [TextArea(2, 3)]
        [SerializeField] private string _description = "Огромный метеор падает в выбранную зону и оставляет горящую область.";
        [SerializeField] private Sprite _icon;
        [SerializeField] private Color _accentColor = new(1f, 0.45f, 0.12f, 1f);

        [Title("Levels (1 to 4)")]
        [SerializeField] private SpellLevelStats[] _levels = new SpellLevelStats[4];

        public SpellKind Kind => _spellKind;
        public SpellEffect Effect => _effect;
        public string SpellName => _spellName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public Color AccentColor => _accentColor;

        public SpellLevelStats GetStats(int level)
        {
            if (_levels == null || _levels.Length == 0) return default;
            int idx = Mathf.Clamp(level - 1, 0, _levels.Length - 1);
            return _levels[idx];
        }

        public void InitializeRuntime(
            SpellKind kind,
            string name,
            string description,
            Color color,
            SpellLevelStats[] levels,
            Sprite icon = null,
            SpellEffect effect = null)
        {
            _spellKind = kind;
            _spellName = name;
            _description = description;
            _accentColor = color;
            _levels = levels;
            _icon = icon;
            if (effect != null)
            {
                _effect = effect;
            }
        }
    }
}
