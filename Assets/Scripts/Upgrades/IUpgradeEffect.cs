namespace Game.Upgrades
{
    /// <summary>
    /// Command abstraction for a single upgrade effect applied to a run's modifier state.
    /// </summary>
    public interface IUpgradeEffect
    {
        UpgradeEffectType EffectType { get; }
        void Apply(float value, RunModifierState state);
    }
}
