using UnityEngine;
using Alchemy.Inspector;

namespace Game.Projectiles
{
    public enum DamageType
    {
        Physical = 0,
        Fire = 1,
        Ice = 2,
        Magic = 3
    }

    public enum ProjectileClass
    {
        DirectPiercing = 0,
        LobbedAOE = 1,
        SpreadingVolley = 2,
        InstantZone = 3
    }

    [CreateAssetMenu(fileName = "NewProjectileData", menuName = "Game/Projectile Data", order = 20)]
    public class ProjectileData : ScriptableObject
    {
        [Title("Classification (GIG-86)")]
        [SerializeField] private ProjectileClass _projectileClass = ProjectileClass.DirectPiercing;
        [SerializeField] private DamageType _damageType = DamageType.Physical;

        [Title("Combat Stats")]
        [SerializeField, Min(1)] private int _damage = 15;
        [SerializeField, Min(0.1f)] private float _speed = 15f; // cells per sec (per GDD)
        [Tooltip("Max targets to pierce through before despawning. 0 or less = infinite.")]
        [SerializeField] private int _pierceCount = 999;
        [SerializeField, Min(0f)] private float _knockbackCells = 1f;

        [Title("AOE & Lobbed Flight")]
        [SerializeField, Min(0f)] private float _splashRadius = 0f;
        [SerializeField, Range(0f, 1f)] private float _edgeDamageMultiplier = 1f;
        [SerializeField, Min(0f)] private float _flightTimePerCell = 0.25f;
        [SerializeField, Min(0f)] private float _arcHeight = 1.5f;

        [Title("Hazard Zone")]
        [SerializeField, Min(0f)] private float _lingeringDuration = 0f;
        [SerializeField, Min(0f)] private float _lingeringDps = 0f;
        [SerializeField, Range(0f, 0.9f)] private float _lingeringSlowPercent = 0f;
        [SerializeField, Min(0f)] private float _lingeringRadius = 0f;
        [SerializeField] private Color _lingeringColor = new(1f, 0.45f, 0.1f, 0.35f);

        [Title("Lifecycle")]
        [SerializeField, Min(0.1f)] private float _maxLifetime = 5f;

        [Title("Visuals")]
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private Vector2 _scale = Vector2.one;

        public ProjectileClass ProjectileClass => _projectileClass;
        public DamageType DamageType => _damageType;
        public int Damage => _damage;
        public float Speed => _speed;
        public int PierceCount => _pierceCount;
        public float KnockbackCells => _knockbackCells;
        public float SplashRadius => _splashRadius;
        public float EdgeDamageMultiplier => _edgeDamageMultiplier;
        public float FlightTimePerCell => _flightTimePerCell;
        public float ArcHeight => _arcHeight;
        public float LingeringDuration => _lingeringDuration;
        public float LingeringDps => _lingeringDps;
        public float LingeringSlowPercent => _lingeringSlowPercent;
        public float LingeringRadius => _lingeringRadius;
        public Color LingeringColor => _lingeringColor;
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

        public void ConfigureClassAndAOE(
            ProjectileClass projectileClass,
            DamageType damageType,
            float splashRadius,
            float edgeDamageMultiplier,
            float flightTimePerCell,
            float arcHeight)
        {
            _projectileClass = projectileClass;
            _damageType = damageType;
            _splashRadius = Mathf.Max(0f, splashRadius);
            _edgeDamageMultiplier = Mathf.Clamp01(edgeDamageMultiplier);
            _flightTimePerCell = Mathf.Max(0f, flightTimePerCell);
            _arcHeight = Mathf.Max(0f, arcHeight);
        }

        public void ConfigureHazard(float duration, float dps, float slowPercent, float radius, Color color)
        {
            _lingeringDuration = Mathf.Max(0f, duration);
            _lingeringDps = Mathf.Max(0f, dps);
            _lingeringSlowPercent = Mathf.Clamp(slowPercent, 0f, 0.9f);
            _lingeringRadius = Mathf.Max(0f, radius);
            _lingeringColor = color;
        }
    }
}
