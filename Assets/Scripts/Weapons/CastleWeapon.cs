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
        [SerializeField, ReadOnly] protected int _currentLevel = 1;
        [SerializeField, ReadOnly] protected float _cooldownTimer = 0f;

        public int CurrentLevel => _currentLevel;
        public float CooldownTimer => _cooldownTimer;
        public abstract float MaxCooldown { get; }
        public bool IsReady => _cooldownTimer <= 0f;
        public bool IsAiming { get; protected set; }

        public event Action<float, float> CooldownChanged; // current, max
        public event Action<bool> AimingToggled;
        public event Action Fired;
        public event Action<int> LevelChanged;

        protected virtual void Awake()
        {
            if (_castle == null)
            {
                _castle = GetComponentInParent<Castle>();
                if (_castle == null)
                {
                    _castle = FindAnyObjectByType<Castle>();
                }
            }
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
