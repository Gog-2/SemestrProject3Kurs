namespace Game.Enemies.States
{
    public sealed class EnemyDisabledState : IEnemyState
    {
        public void Enter(Enemy enemy)
        {
        }

        public void Tick(Enemy enemy, float dt)
        {
            if (enemy.IsFrozen || enemy.IsStunned)
            {
                return;
            }

            if (enemy.IsRetreating)
            {
                enemy.TransitionTo(enemy.RetreatState);
            }
            else
            {
                enemy.TransitionTo(enemy.AdvanceState);
            }
        }

        public void Exit(Enemy enemy)
        {
        }
    }
}
