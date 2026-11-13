using Game.Core;
using UnityEngine;

namespace Game.Enemies.States
{
    public sealed class EnemyAdvanceState : IEnemyState
    {
        public void Enter(Enemy enemy)
        {
            enemy.ClearTarget();
        }

        public void Tick(Enemy enemy, float dt)
        {
            if (enemy.IsFrozen || enemy.IsStunned)
            {
                enemy.TransitionTo(enemy.DisabledState);
                return;
            }

            if (enemy.TryResolveCombatTarget(out IDamageable target, out Vector3 targetPoint, out bool isPointTarget))
            {
                if (!isPointTarget || enemy.IsInAttackRange(targetPoint))
                {
                    enemy.SetCombatTarget(target, targetPoint, isPointTarget);
                    enemy.TransitionTo(enemy.AttackState);
                    enemy.TickCombat(dt, target);
                    return;
                }

                enemy.MoveTowardsPoint(targetPoint, dt);
                return;
            }

            Vector2 flowDir = enemy.GetFlowDirection();
            if (flowDir.sqrMagnitude > 0.0001f)
            {
                enemy.MoveByDirection(flowDir, enemy.CurrentSpeed, dt);
            }
        }

        public void Exit(Enemy enemy)
        {
        }
    }
}
