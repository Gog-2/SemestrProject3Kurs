using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Alchemy.Inspector;
using Game.UI;

namespace Game.Core
{
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Title("State")]
        [ShowInInspector, ReadOnly]
        private GameState _currentState = GameState.Playing;

        [Title("UI Connections")]
        [SerializeField] private PauseUI _pauseUI;
        [SerializeField] private GameOverUI _gameOverUI;

        [Title("Pause Settings")]
        [SerializeField] private bool _allowPauseKey = true;

        private GameState _previousStateBeforePause = GameState.Playing;

        public GameState CurrentState => _currentState;
        public bool IsPaused => _currentState == GameState.Paused;
        public bool CanTogglePause => _currentState == GameState.Playing || _currentState == GameState.Paused;

        public event Action<GameState> StateChanged;
        public event Action<bool> PauseToggled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Time.timeScale = 1f;

            if (_pauseUI == null)
            {
                _pauseUI = FindAnyObjectByType<PauseUI>(FindObjectsInactive.Include);
            }

            if (_gameOverUI == null)
            {
                _gameOverUI = FindAnyObjectByType<GameOverUI>(FindObjectsInactive.Include);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Time.timeScale = 1f;
                Instance = null;
            }
        }

        private void Update()
        {
            if (_allowPauseKey && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (CanTogglePause)
                {
                    TogglePause();
                }
            }
        }

        public void SetState(GameState newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;

            if (_currentState == GameState.Paused)
            {
                Time.timeScale = 0f;
                _pauseUI?.SetVisible(true);
                PauseToggled?.Invoke(true);
            }
            else if (_currentState == GameState.GameOver || _currentState == GameState.Victory)
            {
                Time.timeScale = 0f;
                _pauseUI?.SetVisible(false);
                PauseToggled?.Invoke(false);

                if (_currentState == GameState.GameOver)
                {
                    _gameOverUI?.Show(isVictory: false);
                }
                else
                {
                    _gameOverUI?.Show(isVictory: true);
                }
            }
            else if (_currentState == GameState.Playing)
            {
                Time.timeScale = 1f;
                _pauseUI?.SetVisible(false);
                PauseToggled?.Invoke(false);
            }

            StateChanged?.Invoke(_currentState);
        }

        [Button]
        public void TogglePause()
        {
            if (_currentState == GameState.Paused)
            {
                ResumeGame();
            }
            else if (_currentState == GameState.Playing)
            {
                PauseGame();
            }
        }

        [Button]
        public void PauseGame()
        {
            if (_currentState == GameState.Paused || _currentState == GameState.GameOver || _currentState == GameState.Victory)
            {
                return;
            }

            _previousStateBeforePause = _currentState;
            SetState(GameState.Paused);
        }

        [Button]
        public void ResumeGame()
        {
            if (_currentState != GameState.Paused) return;

            SetState(_previousStateBeforePause == GameState.Paused ? GameState.Playing : _previousStateBeforePause);
        }

        [Button]
        public void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.buildIndex);
        }

        [Button]
        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            // Checks if scene "MainMenu" exists in BuildSettings, otherwise load index 0 or log
            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            {
                SceneManager.LoadScene("MainMenu");
            }
            else
            {
                Debug.LogWarning("[GameManager] 'MainMenu' scene not found in Build Settings! Loading scene index 0.");
                SceneManager.LoadScene(0);
            }
        }
    }
}
