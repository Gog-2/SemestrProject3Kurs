namespace Game.Enemies.States
{
    public interface IEnemyState
    {
        void Enter(Enemy enemy);
        void Tick(Enemy enemy, float dt);
        void Exit(Enemy enemy);
    }
}
