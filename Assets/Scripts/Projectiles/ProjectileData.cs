using UnityEngine;
using Alchemy.Inspector;

namespace Game.Projectiles
{
    [CreateAssetMenu(fileName = "NewProjectileData", menuName = "Game/Projectile Data", order = 20)]
    public class ProjectileData : ScriptableObject
    {
        [Title("Combat Stats")]
        [SerializeField, Min(1)] private int _damage = 15;
        [SerializeField, Min(0.1f)] private float _speed = 15f; // cells per sec (per GDD)
        [Tooltip("Max targets to pierce through before despawning. 0 or less = infinite.")]
        [SerializeField] private int _pierceCount = 999;
        [SerializeField, Min(0f)] private float _knockbackCells = 1f;

        [Title("Lifecycle")]
        [SerializeField, Min(0.1f)] private float _maxLifetime = 5f;

        [Title("Visuals")]
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private Vector2 _scale = Vector2.one;

        public int Damage => _damage;
        public float Speed => _speed;
        public int PierceCount => _pierceCount;
        public float KnockbackCells => _knockbackCells;
        public float MaxLifetime => _maxLifetime;
        public Sprite Sprite => _sprite;
        public Color Color => _color;
        public Vector2 Scale => _scale;

        public void SetRuntimeStats(int damage, float speed, int pierceCount, float knockbackCells)
        {
            _damage = damage;
            _speed = speed;
            _pierceCount = pierceCount;
            _knockbackCells = knockbackCells;
        }
    }
}
