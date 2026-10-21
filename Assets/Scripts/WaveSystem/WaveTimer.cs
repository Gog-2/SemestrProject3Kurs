using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Game.WaveSystem
{
    public class WaveTimer : IWaveTimer
    {
        public float RemainingTime => _remainingTime;
        public float TotalDuration => _totalDuration;
        public float Progress => _totalDuration > 0f ? Mathf.Clamp01(1f - (_remainingTime / _totalDuration)) : 0f;
        public bool IsRunning => _isRunning;

        private float _totalDuration;
        private float _remainingTime;
        private bool _isRunning;

        public event Action<float> TimeChanged;
        public event Action TimerExpired;

        public async UniTask RunAsync(float duration, CancellationToken ct)
        {
            _totalDuration = Mathf.Max(0.1f, duration);
            _remainingTime = _totalDuration;
            _isRunning = true;

            TimeChanged?.Invoke(_remainingTime);

            try
            {
                while (_isRunning && _remainingTime > 0f)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);

                    if (!_isRunning) break;

                    _remainingTime -= Time.deltaTime;
                    if (_remainingTime < 0f)
                    {
                        _remainingTime = 0f;
                    }

                    TimeChanged?.Invoke(_remainingTime);
                }

                if (_isRunning && _remainingTime <= 0f)
                {
                    _isRunning = false;
                    TimerExpired?.Invoke();
                }
                else
                {
                    _isRunning = false;
                }
            }
            catch (OperationCanceledException)
            {
                _isRunning = false;
            }
        }

        public void Stop()
        {
            _isRunning = false;
        }
    }
}
