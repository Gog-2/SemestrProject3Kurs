using UnityEngine;
using Alchemy.Inspector;

namespace Game.Enemies.Data
{
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "Game/Enemy Data", order = 10)]
    public class EnemyData : ScriptableObject
    {
        [Title("Identity")]
        [SerializeField] private string _id = "regular_enemy";
        [SerializeField] private string _displayName = "Regular Enemy";
        [SerializeField] private bool _isBoss = false;

        [Title("Visuals & Customization")]
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Color _tintColor = Color.white;
        [SerializeField] private Vector3 _visualScale = Vector3.one;

        [Title("Health & Defense")]
        [SerializeField, Min(1)] private int _maxHP = 30;
        [Tooltip("Flat damage absorbed by armor per hit (minimum 1 damage always goes through).")]
        [SerializeField, Min(0)] private int _defense = 0;
        [Tooltip("Percentage damage resistance (0..0.8, max 80% per GDD).")]
        [SerializeField, Range(0f, 0.8f)] private float _damageResistance = 0f;
        [SerializeField, Min(0f)] private float _hpGrowthPerWave = 0f;

        [Title("Movement")]
        [SerializeField, Min(0.1f)] private float _moveSpeed = 2f;
        [Tooltip("Minimum speed percentage when slowed. Per GDD, regular enemies cannot drop below 80% (0.8).")]
        [SerializeField, Range(0.05f, 1f)] private float _minSpeedRatio = 0.8f;

        [Title("Combat")]
        [SerializeField, Min(1)] private int _damage = 5;
        [SerializeField, Min(0.1f)] private float _attackInterval = 1.5f;
        [SerializeField, Min(0.1f)] private float _attackRange = 0.8f;
        [SerializeField] private bool _isRanged = false;

        [Title("Loot & Rewards")]
        [SerializeField, Min(1)] private int _honorReward = 10;
        [SerializeField] private LootTable _lootTable = new();

        public string Id => _id;
        public string DisplayName => _displayName;
        public bool IsBoss => _isBoss;
        public Sprite Sprite => _sprite;
        public Color TintColor => _tintColor;
        public Vector3 VisualScale => _visualScale;
        public int HonorReward => _honorReward;
        public int MaxHP => _maxHP;
        public int Defense => _defense;
        public float DamageResistance => _damageResistance;
        public float HpGrowthPerWave => _hpGrowthPerWave;
        public float MoveSpeed => _moveSpeed;
        public float MinSpeedRatio => _minSpeedRatio;
        public int Damage => _damage;
        public float AttackInterval => _attackInterval;
        public float AttackRange => _attackRange;
        public bool IsRanged => _isRanged;
        public LootTable LootTable => _lootTable;

        private void OnValidate()
        {
            if (_maxHP < 1) _maxHP = 1;
            if (_defense < 0) _defense = 0;
            _damageResistance = Mathf.Clamp(_damageResistance, 0f, 0.8f);
            if (_hpGrowthPerWave < 0f) _hpGrowthPerWave = 0f;
            if (_moveSpeed < 0.1f) _moveSpeed = 0.1f;
            if (_minSpeedRatio < 0.05f) _minSpeedRatio = 0.05f;
            if (_damage < 1) _damage = 1;
            if (_attackInterval < 0.1f) _attackInterval = 0.1f;
            if (_attackRange < 0.1f) _attackRange = 0.1f;
            if (_visualScale.x <= 0.01f || _visualScale.y <= 0.01f) _visualScale = Vector3.one;
        }

        public void SetDefenseStats(int defense, float damageResistance, float hpGrowthPerWave = 0f)
        {
            _defense = Mathf.Max(0, defense);
            _damageResistance = Mathf.Clamp(damageResistance, 0f, 0.8f);
            _hpGrowthPerWave = Mathf.Max(0f, hpGrowthPerWave);
        }

        public void SetVisuals(Sprite sprite, Color tintColor, Vector3? visualScale = null)
        {
            if (sprite != null) _sprite = sprite;
            _tintColor = tintColor;
            if (visualScale.HasValue && visualScale.Value.sqrMagnitude > 0.001f)
            {
                _visualScale = visualScale.Value;
            }
        }
    }
}
