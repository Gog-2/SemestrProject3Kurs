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

        [Title("Health & Defense")]
        [SerializeField, Min(1)] private int _maxHP = 30;

        [Title("Movement")]
        [SerializeField, Min(0.1f)] private float _moveSpeed = 2f;
        [Tooltip("Minimum speed percentage when slowed. Per GDD, regular enemies cannot drop below 80% (0.8).")]
        [SerializeField, Range(0.05f, 1f)] private float _minSpeedRatio = 0.8f;

        [Title("Combat")]
        [SerializeField, Min(1)] private int _damage = 5;
        [SerializeField, Min(0.1f)] private float _attackInterval = 1.5f;
        [SerializeField, Min(0.1f)] private float _attackRange = 0.8f;

        [Title("Loot")]
        [SerializeField] private LootTable _lootTable = new();

        public string Id => _id;
        public string DisplayName => _displayName;
        public int MaxHP => _maxHP;
        public float MoveSpeed => _moveSpeed;
        public float MinSpeedRatio => _minSpeedRatio;
        public int Damage => _damage;
        public float AttackInterval => _attackInterval;
        public float AttackRange => _attackRange;
        public LootTable LootTable => _lootTable;

        private void OnValidate()
        {
            if (_maxHP < 1) _maxHP = 1;
            if (_moveSpeed < 0.1f) _moveSpeed = 0.1f;
            if (_minSpeedRatio < 0.05f) _minSpeedRatio = 0.05f;
            if (_damage < 1) _damage = 1;
            if (_attackInterval < 0.1f) _attackInterval = 0.1f;
            if (_attackRange < 0.1f) _attackRange = 0.1f;
        }
    }
}
