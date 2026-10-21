using System;

namespace Game.WaveSystem
{
    public interface IWaveTimer
    {
        float RemainingTime { get; }
        float TotalDuration { get; }
        float Progress { get; }
        bool IsRunning { get; }

        event Action<float> TimeChanged;
        event Action TimerExpired;
    }
}
