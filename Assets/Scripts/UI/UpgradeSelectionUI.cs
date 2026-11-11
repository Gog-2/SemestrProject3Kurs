using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Alchemy.Inspector;
using Game.Core;
using Game.Upgrades;

namespace Game.UI
{
    public class UpgradeSelectionUI : MonoBehaviour
    {
        private enum SelectionStage
        {
            Closed,
            ChestWaitingClick,
            RouletteSpinning,
            CardSelection
        }

        [Title("UI Elements")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private UpgradeCardUI[] _cards = new UpgradeCardUI[3];
        [SerializeField] private Button _skipButton;

        [Title("Chest & Roulette Stage (GIG-23, GIG-81)")]
        [SerializeField] private GameObject _chestStageRoot;
        [SerializeField] private Button _chestButton;
        [SerializeField] private TMP_Text _chestPromptText;
        [SerializeField] private GameObject _cardsStageRoot;
        [SerializeField] private float _rouletteDuration = 1.0f;

        [Title("Animation")]
        [SerializeField] private float _fadeDuration = 0.25f;

        private Tween _fadeTween;
        private Coroutine _rouletteCoroutine;
        private SelectionStage _stage = SelectionStage.Closed;
        private List<UpgradeData> _pendingChoices;

        public bool IsOpen => _stage != SelectionStage.Closed;

        private void Awake()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            if (_skipButton != null)
            {
                _skipButton.onClick.RemoveListener(OnSkipClicked);
                _skipButton.onClick.AddListener(OnSkipClicked);
            }
        }

        private void OnEnable()
        {
            SubscribeProgression();
            RegisterInput();
        }

        private void Start()
        {
            SubscribeProgression();
        }

        private void SubscribeProgression()
        {
            if (RunProgressionManager.Instance == null) return;
            RunProgressionManager.Instance.UpgradeSelectionRequested -= ShowSelection;
            RunProgressionManager.Instance.UpgradeSelectionRequested += ShowSelection;
            RunProgressionManager.Instance.ChestSelectionRequested -= ShowChestSelection;
            RunProgressionManager.Instance.ChestSelectionRequested += ShowChestSelection;
        }

        private void OnDisable()
        {
            if (RunProgressionManager.Instance != null)
            {
                RunProgressionManager.Instance.UpgradeSelectionRequested -= ShowSelection;
                RunProgressionManager.Instance.ChestSelectionRequested -= ShowChestSelection;
            }

            UnregisterInput();
            StopRoulette();
            _fadeTween?.Kill();
        }

        private void OnDestroy()
        {
            UnregisterInput();
        }

        private void RegisterInput()
        {
            UnregisterInput();
            if (InputManager.Instance == null) return;

            InputManager.Instance.SkipPressed += OnSkipPressed;
        }

        private void UnregisterInput()
        {
            if (InputManager.Instance == null) return;

            InputManager.Instance.SkipPressed -= OnSkipPressed;
        }

        private void OnSkipPressed()
        {
            if (_stage == SelectionStage.CardSelection)
            {
                OnSkipClicked();
            }
            else if (_stage == SelectionStage.RouletteSpinning)
            {
                CompleteRouletteImmediately();
            }
        }

        public void ShowChestSelection(List<UpgradeData> upgrades)
        {
            if (upgrades == null || upgrades.Count == 0) return;

            _pendingChoices = upgrades;
            EnsureChestStageExists();
            OpenModalContainer();

            _stage = SelectionStage.ChestWaitingClick;
            SetStageVisibility(showChest: true, showCards: false);
            if (_chestPromptText != null)
            {
                _chestPromptText.text = "НАГРАДА ЗАМКА!\n<size=70%>Нажмите на сундук, чтобы открыть</size>";
            }
        }

        public void ShowSelection(List<UpgradeData> upgrades)
        {
            if (upgrades == null || upgrades.Count == 0) return;

            _pendingChoices = upgrades;
            OpenModalContainer();
            RevealFinalCards();
        }

        private void OpenModalContainer()
        {
            if (_panelRoot != null) _panelRoot.SetActive(true);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.UpgradeSelection);
            }

            Time.timeScale = 0f;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                _fadeTween?.Kill();
                _fadeTween = _canvasGroup.DOFade(1f, _fadeDuration)
                    .SetUpdate(true)
                    .SetLink(gameObject);
            }
        }

        private void EnsureChestStageExists()
        {
            if (_chestButton != null && _chestStageRoot != null)
            {
                _chestButton.onClick.RemoveListener(OnChestOrRouletteClicked);
                _chestButton.onClick.AddListener(OnChestOrRouletteClicked);
                return;
            }

            if (_panelRoot == null) return;

            var chestGo = new GameObject("ChestRewardStage", typeof(RectTransform), typeof(Image), typeof(Button));
            chestGo.transform.SetParent(_panelRoot.transform, false);
            var rt = chestGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(440f, 220f);
            rt.anchoredPosition = Vector2.zero;

            var bg = chestGo.GetComponent<Image>();
            bg.color = new Color(0.28f, 0.18f, 0.08f, 0.96f);

            _chestButton = chestGo.GetComponent<Button>();
            _chestButton.onClick.AddListener(OnChestOrRouletteClicked);

            var textGo = new GameObject("PromptText", typeof(RectTransform));
            textGo.transform.SetParent(chestGo.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(18f, 18f);
            textRt.offsetMax = new Vector2(-18f, -18f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 28f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(1f, 0.88f, 0.35f, 1f);
            if (_skipButton != null)
            {
                var skipTmp = _skipButton.GetComponentInChildren<TMP_Text>();
                if (skipTmp != null && skipTmp.font != null) tmp.font = skipTmp.font;
            }

            _chestPromptText = tmp;
            _chestStageRoot = chestGo;
        }

        private void SetStageVisibility(bool showChest, bool showCards)
        {
            if (_chestStageRoot != null) _chestStageRoot.SetActive(showChest);

            if (_cardsStageRoot != null)
            {
                _cardsStageRoot.SetActive(showCards);
            }
            else
            {
                for (int i = 0; i < _cards.Length; i++)
                {
                    if (_cards[i] != null) _cards[i].gameObject.SetActive(showCards);
                }
            }

            if (_skipButton != null)
            {
                _skipButton.gameObject.SetActive(showCards);
            }
        }

        private void OnChestOrRouletteClicked()
        {
            if (_stage == SelectionStage.ChestWaitingClick)
            {
                StartRouletteSpin();
            }
            else if (_stage == SelectionStage.RouletteSpinning)
            {
                CompleteRouletteImmediately();
            }
        }

        private void StartRouletteSpin()
        {
            _stage = SelectionStage.RouletteSpinning;
            StopRoulette();

            if (_chestStageRoot != null)
            {
                _chestStageRoot.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 6, 1f).SetUpdate(true);
            }

            _rouletteCoroutine = StartCoroutine(RouletteSpinRoutine());
        }

        private IEnumerator RouletteSpinRoutine()
        {
            IReadOnlyList<UpgradeData> pool = RunProgressionManager.Instance != null && RunProgressionManager.Instance.Database != null
                ? RunProgressionManager.Instance.Database.AllUpgrades
                : _pendingChoices;

            float elapsed = 0f;
            float tick = 0f;

            while (elapsed < _rouletteDuration && _stage == SelectionStage.RouletteSpinning)
            {
                elapsed += Time.unscaledDeltaTime;
                tick -= Time.unscaledDeltaTime;

                if (tick <= 0f && pool != null && pool.Count > 0 && _chestPromptText != null)
                {
                    tick = 0.08f;
                    UpgradeData sample = pool[UnityEngine.Random.Range(0, pool.Count)];
                    if (sample != null)
                    {
                        _chestPromptText.text = $"ПРОКРУТКА НАГРАД...\n<color=#FFF>{sample.Title}</color>\n<size=55%>Нажмите ещё раз, чтобы пропустить</size>";
                    }
                }

                yield return null;
            }

            if (_stage == SelectionStage.RouletteSpinning)
            {
                RevealFinalCards();
            }
        }

        public void CompleteRouletteImmediately()
        {
            StopRoulette();
            RevealFinalCards();
        }

        private void StopRoulette()
        {
            if (_rouletteCoroutine != null)
            {
                StopCoroutine(_rouletteCoroutine);
                _rouletteCoroutine = null;
            }
        }

        private void RevealFinalCards()
        {
            StopRoulette();
            _stage = SelectionStage.CardSelection;
            SetStageVisibility(showChest: false, showCards: true);

            if (_pendingChoices == null) return;

            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] != null)
                {
                    if (i < _pendingChoices.Count)
                    {
                        _cards[i].Setup(_pendingChoices[i], OnUpgradeChosen);
                    }
                    else
                    {
                        _cards[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        private void OnUpgradeChosen(UpgradeData upgrade)
        {
            if (_stage != SelectionStage.CardSelection) return;
            RunProgressionManager.Instance?.ApplyUpgrade(upgrade);
            Close();
        }

        private void OnSkipClicked()
        {
            if (_stage != SelectionStage.CardSelection) return;
            Close();
        }

        private void Close()
        {
            if (_stage == SelectionStage.Closed) return;
            _stage = SelectionStage.Closed;
            StopRoulette();

            Time.timeScale = 1f;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
                _fadeTween?.Kill();
                _fadeTween = _canvasGroup.DOFade(0f, _fadeDuration)
                    .SetUpdate(true)
                    .SetLink(gameObject)
                    .OnComplete(() =>
                    {
                        if (_panelRoot != null) _panelRoot.SetActive(false);
                        PresentQueuedIfAny();
                    });
            }
            else
            {
                if (_panelRoot != null) _panelRoot.SetActive(false);
                PresentQueuedIfAny();
            }
        }

        private static void PresentQueuedIfAny()
        {
            if (RunProgressionManager.Instance != null && RunProgressionManager.Instance.PendingUpgradeSelections > 0)
            {
                RunProgressionManager.Instance.TryPresentNextPendingUpgrade(viaChest: true);
            }
        }

        [Button]
        public void TestOpen()
        {
            RunProgressionManager.Instance?.RequestUpgradeSelection();
        }
    }
}
