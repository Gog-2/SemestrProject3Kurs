using UnityEngine;
using Game.Core;
using Game.Enemies;
using Game.WaveSystem;

namespace Game.Buildings
{
    public class BombBuilding : Building
    {
        private static readonly int[] s_centerDamage = { 75, 150, 250, 500 };
        private static readonly int[] s_edgeDamage = { 75, 150, 125, 250 };
        private static readonly float[] s_baseRadius = { 0.85f, 0.85f, 1.6f, 1.6f };

        // GDD: Enemies do not target bombs and walk directly onto them
        public override float PathBreakCost => 0f;
        public override bool CanBeTargetedByEnemies => false;

        private bool _detonated;

        private void Update()
        {
            if (IsDead || _detonated) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (WaveController.Instance != null)
            {
                WaveState ws = WaveController.Instance.CurrentState;
                if (ws != WaveState.WaveActive && ws != WaveState.WaveClearing) return;
            }

            const float TriggerRadiusSqr = 0.72f * 0.72f;
            Vector3 center = transform.position;
            var enemies = Enemy.ActiveEnemies;

            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy e = enemies[i];
                if (e == null || e.IsDead || e.IsRetreating) continue;
                if ((e.transform.position - center).sqrMagnitude <= TriggerRadiusSqr)
                {
                    Detonate();
                    return;
                }
            }
        }

        public void Detonate()
        {
            if (IsDead || _detonated) return;
            _detonated = true;

            int idx = Mathf.Clamp(_currentLevel - 1, 0, 3);
            float dmgMult = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveExplosiveDamageMultiplier()
                : 1f;
            float radiusBonus = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetExplosiveRadiusBonus() * 0.5f
                : 0f;

            float radius = s_baseRadius[idx] + radiusBonus;
            float sqrRadius = radius * radius;
            float innerSqr = 0.85f * 0.85f;
            int centerDmg = Mathf.Max(1, Mathf.RoundToInt(s_centerDamage[idx] * dmgMult));
            int edgeDmg = Mathf.Max(1, Mathf.RoundToInt(s_edgeDamage[idx] * dmgMult));

            float slowPct = 0f;
            float slowDur = 0f;
            RunProgressionManager.Instance?.GetControlDamageSlow(out slowPct, out slowDur);

            Vector3 center = transform.position;
            var enemies = Enemy.ActiveEnemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy e = enemies[i];
                if (e == null || e.IsDead) continue;

                float sqrDist = (e.transform.position - center).sqrMagnitude;
                if (sqrDist > sqrRadius) continue;

                int dmg = sqrDist <= innerSqr ? centerDmg : edgeDmg;
                e.TakeDamage(dmg);
                if (slowPct > 0f && slowDur > 0f && !e.IsDead)
                {
                    e.ApplySlow(slowPct, slowDur);
                }
            }

            Die();
        }
    }
}
