using System;
using UnityEngine;
using TMPro;
using DG.Tweening;
using Alchemy.Inspector;
using Game.WaveSystem;

namespace Game.UI
{
    public class WaveTimerUI : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private WaveController _waveController;
        [SerializeField] private TextMeshProUGUI _timerText;

        [Title("Animation Settings")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _warningColor = Color.red;
        [SerializeField] private Vector3 _pulseStrength = new(0.12f, 0.12f, 0f);

        private Tweener _pulseTween;
        private int _lastDisplayedSecond = -1;

        private void OnEnable()
        {
            _waveController.Timer.TimeChanged += OnTimeChanged;
            _waveController.Timer.TimerExpired += OnTimerExpired;
            _waveController.StateChanged += OnStateChanged;

            OnStateChanged(_waveController.CurrentState);
        }

        private void OnDisable()
        {
            _waveController.Timer.TimeChanged -= OnTimeChanged;
            _waveController.Timer.TimerExpired -= OnTimerExpired;
            _waveController.StateChanged -= OnStateChanged;

            KillPulseTween();
        }

        private void OnTimeChanged(float remainingTime)
        {
            if (_waveController.CurrentState == WaveState.WaveActive)
            {
                UpdateDisplay(remainingTime);
            }
        }

        private void OnTimerExpired()
        {
            if (_timerText == null) return;

            if (_waveController.CurrentState == WaveState.WaveClearing)
            {
                _timerText.text = $"Wave {_waveController.CurrentWaveNumber}: Clearing...";
                _timerText.color = _normalColor;
            }
            KillPulseTween();
        }

        private void OnStateChanged(WaveState state)
        {
            KillPulseTween();
            _lastDisplayedSecond = -1;

            if (_timerText == null) return;

            switch (state)
            {
                case WaveState.WaveActive:
                    UpdateDisplay(_waveController.Timer.RemainingTime, forceUpdate: true);
                    break;
                case WaveState.WaveClearing:
                    _timerText.text = $"Wave {_waveController.CurrentWaveNumber}: Clearing...";
                    _timerText.color = _normalColor;
                    break;
                case WaveState.BuildingPhase:
                    _timerText.text = "Building Phase";
                    _timerText.color = _normalColor;
                    break;
                case WaveState.GameOver:
                    _timerText.text = "<color=red>GAME OVER</color>";
                    _timerText.color = _warningColor;
                    break;
                default:
                    _timerText.text = "Preparing...";
                    _timerText.color = _normalColor;
                    break;
            }
        }

        private void UpdateDisplay(float remainingSeconds, bool forceUpdate = false)
        {
            int totalSec = Mathf.CeilToInt(remainingSeconds);

            if (!forceUpdate && totalSec == _lastDisplayedSecond)
            {
                return;
            }

            _lastDisplayedSecond = totalSec;

            int minutes = totalSec / 60;
            int seconds = totalSec % 60;

            _timerText.text = $"Wave {_waveController.CurrentWaveNumber}: {minutes:00}:{seconds:00}";

            if (totalSec <= 5 && totalSec > 0)
            {
                _timerText.color = _warningColor;
                TriggerPulse();
            }
            else
            {
                _timerText.color = _normalColor;
            }
        }

        private void TriggerPulse()
        {
            KillPulseTween();

            transform.localScale = Vector3.one;
            _pulseTween = transform.DOPunchScale(_pulseStrength, 0.2f, 8, 1f)
                .SetLink(gameObject)
                .SetUpdate(true);
        }

        private void KillPulseTween()
        {
            if (_pulseTween.IsActive())
            {
                _pulseTween.Kill();
                transform.localScale = Vector3.one;
            }
        }
    }
}
