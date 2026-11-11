using System;
using UnityEngine;
using Game.Core;
using Game.Buildings;
using Game.Projectiles;
using Game.WaveSystem;

namespace Game.Units
{
    public enum SupportUnitKind
    {
        Priest = 0,
        Builder = 1
    }

    public class SupportUnit : MonoBehaviour, IDamageable
    {
        private SupportUnitKind _kind;
        private Building _homeBuilding;
        private Vector3 _homePosition;
        private int _maxHP;
        private int _currentHP;
        private int _baseAmount;
        private float _interval;
        private float _actionRange;
        private float _roamRadius;
        private float _moveSpeed = 2.4f;
        private float _timer;

        private SpriteRenderer _renderer;

        public int CurrentHP => _currentHP;
        public int MaxHP => _maxHP;
        public bool IsDead => _currentHP <= 0;

        public event Action<IDamageable> HealthChanged;
        public event Action<IDamageable> Destroyed;
        public event Action<SupportUnit> Died;

        public void Initialize(
            SupportUnitKind kind,
            Building homeBuilding,
            Vector3 homePos,
            int maxHp,
            int amount,
            float interval,
            float actionRange,
            float roamRadius,
            Color tint)
        {
            _kind = kind;
            _homeBuilding = homeBuilding;
            _homePosition = homePos;

            float hpMult = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveUnitMaxHpMultiplier()
                : 1f;
            _maxHP = Mathf.Max(1, Mathf.RoundToInt(maxHp * hpMult));
            _currentHP = _maxHP;

            _baseAmount = Mathf.Max(1, amount);
            _interval = Mathf.Max(0.5f, interval);
            _actionRange = Mathf.Max(0.75f, actionRange);
            _roamRadius = Mathf.Max(1f, roamRadius);

            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = HazardZone.GetOrCreateCircleSprite();
            _renderer.color = tint;
            _renderer.sortingLayerName = "Enemies";
            _renderer.sortingOrder = 9;
            transform.localScale = new Vector3(0.4f, 0.4f, 1f);
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;
            _currentHP = Mathf.Max(0, _currentHP - amount);
            HealthChanged?.Invoke(this);
            if (_currentHP <= 0)
            {
                Destroyed?.Invoke(this);
                Died?.Invoke(this);
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (IsDead) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            float dt = Time.deltaTime;
            if (_timer > 0f) _timer -= dt;

            bool activePhase = WaveController.Instance == null ||
                               WaveController.Instance.CurrentState == WaveState.WaveActive ||
                               WaveController.Instance.CurrentState == WaveState.WaveClearing;

            if (!activePhase)
            {
                ReturnHome(dt);
                return;
            }

            if (_kind == SupportUnitKind.Priest)
            {
                TickPriest(dt);
            }
            else
            {
                TickBuilder(dt);
            }
        }

        private void TickPriest(float dt)
        {
            AllyUnit lowestUnit = null;
            float lowestRatio = 1f;
            float maxSearchDist = _roamRadius + _actionRange;

            for (int i = 0; i < AllyUnit.ActiveUnits.Count; i++)
            {
                AllyUnit u = AllyUnit.ActiveUnits[i];
                if (u == null || u.IsDead || u.MaxHP <= 0) continue;
                float ratio = u.CurrentHP / (float)u.MaxHP;
                if (ratio >= 0.999f) continue;
                if (Vector2.Distance(u.transform.position, _homePosition) > maxSearchDist) continue;

                if (ratio < lowestRatio)
                {
                    lowestRatio = ratio;
                    lowestUnit = u;
                }
            }

            if (lowestUnit == null)
            {
                ReturnHome(dt);
                return;
            }

            float dist = Vector2.Distance(transform.position, lowestUnit.transform.position);
            if (dist > _actionRange)
            {
                transform.position = Vector3.MoveTowards(transform.position, lowestUnit.transform.position, _moveSpeed * dt);
                return;
            }

            if (_timer <= 0f)
            {
                float mult = RunProgressionManager.Instance != null
                    ? RunProgressionManager.Instance.GetEffectiveSupportMultiplier()
                    : 1f;
                int healAmt = Mathf.Max(1, Mathf.RoundToInt(_baseAmount * mult));
                lowestUnit.Heal(healAmt);
                _homeBuilding?.AddCombatXP(healAmt);
                _timer = _interval;
            }
        }

        private void TickBuilder(float dt)
        {
            Building lowestBuilding = null;
            float lowestRatio = 1f;
            float effectiveRoam = _roamRadius + (RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetBuilderRangeBonus()
                : 0f);
            float maxSearchDist = effectiveRoam + _actionRange;

            var buildings = Building.ActiveBuildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                if (b == null || b.IsDead || b.MaxHP <= 0) continue;
                if (b.Data != null && !b.Data.IsDestructible) continue;

                float ratio = b.CurrentHP / (float)b.MaxHP;
                if (ratio >= 0.999f) continue;
                if (Vector2.Distance(b.transform.position, _homePosition) > maxSearchDist) continue;

                if (ratio < lowestRatio)
                {
                    lowestRatio = ratio;
                    lowestBuilding = b;
                }
            }

            if (lowestBuilding == null)
            {
                ReturnHome(dt);
                return;
            }

            float dist = Vector2.Distance(transform.position, lowestBuilding.transform.position);
            if (dist > _actionRange + 0.5f)
            {
                transform.position = Vector3.MoveTowards(transform.position, lowestBuilding.transform.position, _moveSpeed * dt);
                return;
            }

            if (_timer <= 0f)
            {
                float mult = RunProgressionManager.Instance != null
                    ? RunProgressionManager.Instance.GetBuilderRepairSpeedMultiplier()
                    : 1f;
                int repairAmt = Mathf.Max(1, Mathf.RoundToInt(_baseAmount * mult));
                lowestBuilding.Repair(repairAmt);
                _homeBuilding?.AddCombatXP(repairAmt);
                _timer = _interval;
            }
        }

        private void ReturnHome(float dt)
        {
            if (Vector2.Distance(transform.position, _homePosition) <= 0.35f) return;
            transform.position = Vector3.MoveTowards(transform.position, _homePosition, _moveSpeed * dt);
        }
    }
}
