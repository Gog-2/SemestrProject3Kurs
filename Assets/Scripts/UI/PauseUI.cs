using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Alchemy.Inspector;
using DG.Tweening;
using Game.Core;
using Game.UI.MainMenu;

namespace Game.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PauseUI : MonoBehaviour
    {
        [Title("UI Elements")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _mainMenuButton;

        [Title("Audio Settings (GIG-28)")]
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private TMP_Text _musicValueText;
        [SerializeField] private TMP_Text _sfxValueText;

        [Title("Animation")]
        [SerializeField] private float _fadeDuration = 0.2f;

        private Tween _fadeTween;
        private bool _audioBound;

        private void Awake()
        {
            Debug.Assert(_canvasGroup != null, "[PauseUI] _canvasGroup is not assigned!", this);
            Debug.Assert(_resumeButton != null, "[PauseUI] _resumeButton is not assigned!", this);
            Debug.Assert(_restartButton != null, "[PauseUI] _restartButton is not assigned!", this);
            Debug.Assert(_mainMenuButton != null, "[PauseUI] _mainMenuButton is not assigned!", this);

            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            _resumeButton.onClick.RemoveListener(OnResumeClicked);
            _resumeButton.onClick.AddListener(OnResumeClicked);

            _restartButton.onClick.RemoveListener(OnRestartClicked);
            _restartButton.onClick.AddListener(OnRestartClicked);

            _mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);

            SettingsUI.BindAudioControls(_musicSlider, _sfxSlider, _musicValueText, _sfxValueText, ref _audioBound);
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PauseToggled -= OnPauseToggled;
                GameManager.Instance.PauseToggled += OnPauseToggled;
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PauseToggled -= OnPauseToggled;
                GameManager.Instance.PauseToggled += OnPauseToggled;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PauseToggled -= OnPauseToggled;
            }

            _fadeTween?.Kill();
        }

        private void OnPauseToggled(bool isPaused)
        {
            SetVisible(isPaused);
        }

        public void SetVisible(bool visible)
        {
            _fadeTween?.Kill();

            if (visible)
            {
                SettingsUI.SyncAudioControls(_musicSlider, _sfxSlider, _musicValueText, _sfxValueText);
                if (_panelRoot != null && _panelRoot != gameObject)
                {
                    _panelRoot.SetActive(true);
                }

                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                _fadeTween = _canvasGroup.DOFade(1f, _fadeDuration)
                    .SetUpdate(true)
                    .SetLink(gameObject);
            }
            else
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
                _fadeTween = _canvasGroup.DOFade(0f, _fadeDuration)
                    .SetUpdate(true)
                    .SetLink(gameObject)
                    .OnComplete(() =>
                    {
                        if (_panelRoot != null && _panelRoot != gameObject)
                        {
                            _panelRoot.SetActive(false);
                        }
                    });
            }
        }

        private void OnResumeClicked()
        {
            GameManager.Instance.ResumeGame();
        }

        private void OnRestartClicked()
        {
            GameManager.Instance.RestartCurrentScene();
        }

        private void OnMainMenuClicked()
        {
            GameManager.Instance.LoadMainMenu();
        }
    }
}
