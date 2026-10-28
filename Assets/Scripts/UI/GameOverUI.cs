using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Alchemy.Inspector;
using Game.Core;

namespace Game.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [Title("UI Elements")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _killsText;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _wavesText;
        [SerializeField] private TMP_Text _diamondsText;
        [SerializeField] private TMP_Text _coinsText;

        [Title("Buttons")]
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _mainMenuButton;

        [Title("Visual Themes")]
        [SerializeField] private Color _victoryColor = new(0.2f, 0.9f, 0.4f);
        [SerializeField] private Color _defeatColor = new(0.9f, 0.2f, 0.2f);
        [SerializeField] private float _fadeDuration = 0.4f;

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
                GameManager.Instance.StateChanged -= OnGameStateChanged;
                GameManager.Instance.StateChanged += OnGameStateChanged;
            }
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnGameStateChanged;
                GameManager.Instance.StateChanged += OnGameStateChanged;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnGameStateChanged;
            }

            _fadeTween?.Kill();
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                Show(isVictory: false);
            }
            else if (state == GameState.Victory)
            {
                Show(isVictory: true);
            }
        }

        public void Show(bool isVictory)
        {
            if (_panelRoot == null) return;

            RunStatistics stats = RunStatistics.Instance;
            if (stats != null)
            {
                stats.SetOutcome(isVictory);

                if (_killsText != null) _killsText.text = $"Убито монстров: {stats.TotalKills}";
                if (_timeText != null) _timeText.text = $"Время в игре: {stats.FormattedTime}";
                if (_wavesText != null) _wavesText.text = $"Пройдено волн: {stats.WavesCompleted}";
                if (_diamondsText != null) _diamondsText.text = $"Добыто алмазов: {stats.DiamondsEarned}";
                if (_coinsText != null) _coinsText.text = $"Собрано монет: {stats.CoinsEarned}";
            }

            if (_titleText != null)
            {
                _titleText.text = isVictory ? "ПОБЕДА!" : "ЗАМОК ПАЛ";
                _titleText.color = isVictory ? _victoryColor : _defeatColor;
            }

            if (_panelRoot != null && _panelRoot != gameObject)
            {
                _panelRoot.SetActive(true);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;

                _fadeTween?.Kill();
                _fadeTween = _canvasGroup.DOFade(1f, _fadeDuration)
                    .SetUpdate(true)
                    .SetLink(gameObject);
            }
            else if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
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

        [Button]
        public void TestDefeat()
        {
            Show(false);
        }

        [Button]
        public void TestVictory()
        {
            Show(true);
        }
    }
}
