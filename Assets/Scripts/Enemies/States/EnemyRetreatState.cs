using UnityEngine;

namespace Game.Enemies.States
{
    public sealed class EnemyRetreatState : IEnemyState
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

            if (enemy.HasExitedRetreatBounds())
            {
                enemy.CompleteRetreat();
                return;
            }

            Vector2 retreatDir = enemy.GetRetreatDirection();
            if (retreatDir.sqrMagnitude > 0.0001f)
            {
                enemy.MoveByDirection(retreatDir, enemy.CurrentSpeed * 1.15f, dt);
            }
        }

        public void Exit(Enemy enemy)
        {
        }
    }
}
