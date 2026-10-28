using UnityEngine;
using UnityEngine.UI;
using Alchemy.Inspector;
using DG.Tweening;
using Game.Core;

namespace Game.UI
{
    public class PauseUI : MonoBehaviour
    {
        [Title("UI Elements")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _mainMenuButton;

        [Title("Animation")]
        [SerializeField] private float _fadeDuration = 0.2f;

        private Tween _fadeTween;

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            else if (_panelRoot != null && _panelRoot != gameObject)
            {
                _panelRoot.SetActive(false);
            }

            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveListener(OnResumeClicked);
                _resumeButton.onClick.AddListener(OnResumeClicked);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(OnRestartClicked);
                _restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
                _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
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

        private void OnEnable()
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
                if (_panelRoot != null && _panelRoot != gameObject)
                {
                    _panelRoot.SetActive(true);
                }

                if (_canvasGroup != null)
                {
                    _canvasGroup.interactable = true;
                    _canvasGroup.blocksRaycasts = true;
                    _fadeTween = _canvasGroup.DOFade(1f, _fadeDuration)
                        .SetUpdate(true)
                        .SetLink(gameObject);
                }
            }
            else
            {
                if (_canvasGroup != null)
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
                else if (_panelRoot != null && _panelRoot != gameObject)
                {
                    _panelRoot.SetActive(false);
                }
            }
        }

        private void OnResumeClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
            else
            {
                SetVisible(false);
            }
        }

        private void OnRestartClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartCurrentScene();
            }
        }

        private void OnMainMenuClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadMainMenu();
            }
        }
    }
}
