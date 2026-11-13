using Game.Core;
using UnityEngine;

namespace Game.Enemies.States
{
    public sealed class EnemyAttackState : IEnemyState
    {
        public void Enter(Enemy enemy)
        {
        }

        public void Tick(Enemy enemy, float dt)
        {
            if (enemy.IsFrozen || enemy.IsStunned)
            {
                enemy.TransitionTo(enemy.DisabledState);
                return;
            }

            if (!enemy.TryResolveCombatTarget(out IDamageable target, out Vector3 targetPoint, out bool isPointTarget))
            {
                enemy.TransitionTo(enemy.AdvanceState);
                return;
            }

            if (isPointTarget && !enemy.IsInAttackRange(targetPoint))
            {
                enemy.ClearTarget();
                enemy.TransitionTo(enemy.AdvanceState);
                return;
            }

            enemy.SetCombatTarget(target, targetPoint, isPointTarget);
            enemy.TickCombat(dt, target);
        }

        public void Exit(Enemy enemy)
        {
        }
    }
}
