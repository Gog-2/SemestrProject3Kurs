using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Alchemy.Inspector;
using Game.Core;
using Game.WaveSystem;

namespace Game.UI
{
    public class WaveTimerUI : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private WaveController _waveController;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private Button _startWaveButton;

        [Title("Animation Settings")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _warningColor = Color.red;
        [SerializeField] private Color _buildingColor = new(0.95f, 0.85f, 0.35f);
        [SerializeField] private Vector3 _pulseStrength = new(0.12f, 0.12f, 0f);

        private Tweener _pulseTween;
        private int _lastDisplayedSecond = -1;
        private int _lastDisplayedEnemies = -1;

        private void Awake()
        {
            Debug.Assert(_waveController != null, "[WaveTimerUI] _waveController is not assigned!", this);
            Debug.Assert(_timerText != null, "[WaveTimerUI] _timerText is not assigned!", this);

            if (_startWaveButton != null)
            {
                _startWaveButton.onClick.AddListener(OnStartWaveButtonClicked);
                _startWaveButton.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (_waveController != null)
            {
                _waveController.Timer.TimeChanged += OnTimeChanged;
                _waveController.Timer.TimerExpired += OnTimerExpired;
                _waveController.StateChanged += OnStateChanged;
                _waveController.BuildingPhaseTimeChanged += OnBuildingPhaseTimeChanged;

                if (_waveController.Spawner != null)
                {
                    _waveController.Spawner.ActiveEnemyCountChanged += OnEnemyCountChanged;
                }

                OnStateChanged(_waveController.CurrentState);
            }

            if (InputManager.Instance != null)
            {
                InputManager.Instance.SkipPressed += OnSkipPressed;
            }
        }

        private void OnDisable()
        {
            if (_waveController != null)
            {
                _waveController.Timer.TimeChanged -= OnTimeChanged;
                _waveController.Timer.TimerExpired -= OnTimerExpired;
                _waveController.StateChanged -= OnStateChanged;
                _waveController.BuildingPhaseTimeChanged -= OnBuildingPhaseTimeChanged;

                if (_waveController.Spawner != null)
                {
                    _waveController.Spawner.ActiveEnemyCountChanged -= OnEnemyCountChanged;
                }
            }

            if (InputManager.Instance != null)
            {
                InputManager.Instance.SkipPressed -= OnSkipPressed;
            }

            KillPulseTween();
        }

        private void OnSkipPressed()
        {
            if (_waveController != null && _waveController.CurrentState == WaveState.BuildingPhase)
            {
                _waveController.SkipBuildingPhaseAndStartWave();
            }
        }

        private void OnStartWaveButtonClicked()
        {
            if (_waveController != null)
            {
                _waveController.SkipBuildingPhaseAndStartWave();
            }
        }

        private void OnTimeChanged(float remainingTime)
        {
            if (_waveController.CurrentState == WaveState.WaveActive)
            {
                UpdateDisplay(remainingTime);
            }
        }

        private void OnBuildingPhaseTimeChanged(float remainingTime)
        {
            if (_waveController.CurrentState == WaveState.BuildingPhase)
            {
                UpdateBuildingPhaseDisplay(remainingTime);
            }
        }

        private void OnEnemyCountChanged(int activeCount)
        {
            if (_waveController.CurrentState == WaveState.WaveActive)
            {
                UpdateDisplay(_waveController.Timer.RemainingTime, forceUpdate: true);
            }
            else if (_waveController.CurrentState == WaveState.WaveClearing)
            {
                _timerText.text = $"Волна {_waveController.CurrentWaveNumber}: Зачистка ({activeCount})";
                _timerText.color = _normalColor;
            }
        }

        private void OnTimerExpired()
        {
            if (_waveController.CurrentState == WaveState.WaveClearing)
            {
                int count = _waveController.Spawner != null ? _waveController.Spawner.ActiveEnemyCount : 0;
                _timerText.text = $"Волна {_waveController.CurrentWaveNumber}: Зачистка ({count})";
                _timerText.color = _normalColor;
            }
            KillPulseTween();
        }

        private void OnStateChanged(WaveState state)
        {
            KillPulseTween();
            _lastDisplayedSecond = -1;
            _lastDisplayedEnemies = -1;

            if (_startWaveButton != null)
            {
                _startWaveButton.gameObject.SetActive(state == WaveState.BuildingPhase);
            }

            switch (state)
            {
                case WaveState.WaveActive:
                    UpdateDisplay(_waveController.Timer.RemainingTime, forceUpdate: true);
                    break;
                case WaveState.WaveClearing:
                    int count = _waveController.Spawner != null ? _waveController.Spawner.ActiveEnemyCount : 0;
                    _timerText.text = $"Волна {_waveController.CurrentWaveNumber}: Зачистка ({count})";
                    _timerText.color = _normalColor;
                    break;
                case WaveState.BuildingPhase:
                    UpdateBuildingPhaseDisplay(_waveController.BuildingPhaseRemainingTime, forceUpdate: true);
                    break;
                case WaveState.GameOver:
                    _timerText.text = "<color=red>ЗАМОК ПАЛ</color>";
                    _timerText.color = _warningColor;
                    break;
                default:
                    _timerText.text = "Подготовка...";
                    _timerText.color = _normalColor;
                    break;
            }
        }

        private void UpdateDisplay(float remainingSeconds, bool forceUpdate = false)
        {
            int totalSec = Mathf.CeilToInt(remainingSeconds);
            int activeEnemies = _waveController.Spawner != null ? _waveController.Spawner.ActiveEnemyCount : 0;

            if (!forceUpdate && totalSec == _lastDisplayedSecond && activeEnemies == _lastDisplayedEnemies)
            {
                return;
            }

            _lastDisplayedSecond = totalSec;
            _lastDisplayedEnemies = activeEnemies;

            int minutes = totalSec / 60;
            int seconds = totalSec % 60;

            string enemyInfo = activeEnemies > 0 ? $" | Врагов: {activeEnemies}" : "";
            _timerText.text = $"Волна {_waveController.CurrentWaveNumber}: {minutes:00}:{seconds:00}{enemyInfo}";

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

        private void UpdateBuildingPhaseDisplay(float remainingSeconds, bool forceUpdate = false)
        {
            int totalSec = Mathf.CeilToInt(remainingSeconds);
            if (!forceUpdate && totalSec == _lastDisplayedSecond)
            {
                return;
            }

            _lastDisplayedSecond = totalSec;
            _timerText.text = $"Фаза строительства: {totalSec}с\n<size=65%><color=#FFE066>[Пробел] Начать волну</color></size>";
            _timerText.color = _buildingColor;
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
