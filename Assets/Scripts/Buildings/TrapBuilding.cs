using UnityEngine;
using Game.Core;
using Game.Enemies;
using Game.WaveSystem;

namespace Game.Buildings
{
    public class TrapBuilding : Building
    {
        private static readonly int[] s_damagePerSec = { 6, 10, 14, 20 };
        private static readonly int[] s_baseUses = { 35, 55, 80, 110 };

        private int _remainingUses = 30;
        private int _maxUses = 30;
        private float _tickTimer;

        // GDD: Enemies do not target traps and walk directly across them
        public override float PathBreakCost => 0f;
        public override bool CanBeTargetedByEnemies => false;

        public int RemainingUses => _remainingUses;
        public int MaxUses => _maxUses;

        protected override void OnPlaced()
        {
            base.OnPlaced();
            RefreshUsesCapacity(fillFull: true);
        }

        protected override void OnLevelUp(int newLevel)
        {
            base.OnLevelUp(newLevel);
            RefreshUsesCapacity(fillFull: true);
        }

        private void RefreshUsesCapacity(bool fillFull)
        {
            int idx = Mathf.Clamp(_currentLevel - 1, 0, 3);
            float usesMult = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveTrapUsesMultiplier()
                : 1f;
            _maxUses = Mathf.Max(1, Mathf.RoundToInt(s_baseUses[idx] * usesMult));
            if (fillFull)
            {
                _remainingUses = _maxUses;
            }
        }

        private void Update()
        {
            if (IsDead || _remainingUses <= 0) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (WaveController.Instance != null)
            {
                WaveState ws = WaveController.Instance.CurrentState;
                if (ws != WaveState.WaveActive && ws != WaveState.WaveClearing) return;
            }

            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0f) return;

            _tickTimer = 1.0f;
            TriggerTrapTick();
        }

        public void TriggerTrapTick()
        {
            if (IsDead || _remainingUses <= 0) return;

            int idx = Mathf.Clamp(_currentLevel - 1, 0, 3);
            float dmgMult = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveTrapDamageMultiplier()
                : 1f;
            int dmg = Mathf.Max(1, Mathf.RoundToInt(s_damagePerSec[idx] * dmgMult));

            float slowPct = 0f;
            float slowDur = 0f;
            RunProgressionManager.Instance?.GetControlDamageSlow(out slowPct, out slowDur);

            const float TriggerRadiusSqr = 0.72f * 0.72f;
            Vector3 center = transform.position;
            var enemies = Enemy.ActiveEnemies;

            for (int i = enemies.Count - 1; i >= 0 && _remainingUses > 0; i--)
            {
                Enemy e = enemies[i];
                if (e == null || e.IsDead || e.IsRetreating) continue;
                if ((e.transform.position - center).sqrMagnitude <= TriggerRadiusSqr)
                {
                    e.TakeDamage(dmg);
                    if (slowPct > 0f && slowDur > 0f && !e.IsDead)
                    {
                        e.ApplySlow(slowPct, slowDur);
                    }
                    AddCombatXP(dmg);
                    _remainingUses--;
                }
            }

            if (_remainingUses <= 0)
            {
                Die();
            }
        }
    }
}
