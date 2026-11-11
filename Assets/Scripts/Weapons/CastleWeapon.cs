using System;
using UnityEngine;
using Alchemy.Inspector;
using Game.Buildings;
using Game.Core;

namespace Game.Weapons
{
    public abstract class CastleWeapon : MonoBehaviour
    {
        [Title("Weapon Base")]
        [SerializeField] protected Castle _castle;
        [SerializeField] protected Transform _turretVisual;
        [SerializeField, ReadOnly] protected int _currentLevel = 1;
        [SerializeField, ReadOnly] protected float _cooldownTimer = 0f;

        public int CurrentLevel => _currentLevel;
        public float CooldownTimer => _cooldownTimer;
        public abstract float MaxCooldown { get; }
        public virtual float MaxRange => 0f; // 0 = unlimited range
        public virtual float AoERadius => 0.45f;
        public bool IsReady => _cooldownTimer <= 0f;
        public bool IsAiming { get; protected set; }
        public virtual Vector3 AimOrigin => transform.position;

        public event Action<float, float> CooldownChanged; // current, max
        public event Action<bool> AimingToggled;
        public event Action Fired;
        public event Action<int> LevelChanged;

        protected virtual void Awake()
        {
            if (_castle == null)
            {
                _castle = GetComponentInParent<Castle>();
            }
            Debug.Assert(_castle != null, $"[{name}] _castle is not assigned and not found in parents!", this);
        }

        protected virtual void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                if (IsAiming)
                {
                    CancelAiming();
                }
                return;
            }

            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
                if (_cooldownTimer < 0f) _cooldownTimer = 0f;
                CooldownChanged?.Invoke(_cooldownTimer, MaxCooldown);
            }
        }

        public bool IsTargetInRange(Vector3 targetWorldPosition)
        {
            if (MaxRange <= 0.01f) return true;
            return Vector2.Distance(AimOrigin, targetWorldPosition) <= MaxRange;
        }

        protected int ApplyWeaponDamageModifiers(int baseDamage, bool isExplosive = false)
        {
            if (RunProgressionManager.Instance == null) return baseDamage;

            float mult = RunProgressionManager.Instance.GetEffectiveWeaponDamageMultiplier();
            if (isExplosive && RunProgressionManager.Instance.ActiveKnight != null)
            {
                mult += RunProgressionManager.Instance.ActiveKnight.ExplosiveDamageBonus;
            }
            return Mathf.Max(1, Mathf.RoundToInt(baseDamage * mult));
        }

        protected float ApplyWeaponCooldownModifiers(float baseCooldown, bool isPiercing = false)
        {
            if (RunProgressionManager.Instance == null) return baseCooldown;

            float mult = RunProgressionManager.Instance.GetEffectiveWeaponCooldownMultiplier();
            if (isPiercing)
            {
                mult *= RunProgressionManager.Instance.GetEffectivePiercingCooldownMultiplier();
            }
            return Mathf.Max(0.25f, baseCooldown * mult);
        }

        public void RotateTowardsWorldPoint(Vector3 worldPos)
        {
            Transform rotTarget = _turretVisual;
            if (rotTarget == null)
            {
                if (_castle != null && gameObject != _castle.gameObject)
                {
                    rotTarget = transform;
                }
                else
                {
                    return;
                }
            }

            Vector2 dir = worldPos - rotTarget.position;
            if (dir.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                rotTarget.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        public virtual void ToggleAiming()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (IsAiming)
            {
                CancelAiming();
            }
            else
            {
                StartAiming();
            }
        }

        public virtual void StartAiming()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (!IsReady) return;

            IsAiming = true;
            AimingToggled?.Invoke(true);
        }

        public virtual void CancelAiming()
        {
            IsAiming = false;
            AimingToggled?.Invoke(false);
        }

        public abstract bool TryFire(Vector3 targetWorldPosition);

        public virtual void Upgrade()
        {
            if (_currentLevel < 4)
            {
                _currentLevel++;
                LevelChanged?.Invoke(_currentLevel);
            }
        }

        public virtual void SetLevel(int level)
        {
            _currentLevel = Mathf.Clamp(level, 1, 4);
            LevelChanged?.Invoke(_currentLevel);
        }

        protected Vector3 GetOriginPosition()
        {
            if (_castle != null)
            {
                return _castle.transform.position;
            }
            return transform.position;
        }

        protected void TriggerFired()
        {
            _cooldownTimer = MaxCooldown;
            CancelAiming();
            Fired?.Invoke();
            CooldownChanged?.Invoke(_cooldownTimer, MaxCooldown);
        }
    }
}
