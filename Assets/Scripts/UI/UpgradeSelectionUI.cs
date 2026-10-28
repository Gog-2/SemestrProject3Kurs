using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;
using Alchemy.Inspector;
using Game.Core;
using Game.Upgrades;

namespace Game.UI
{
    public class UpgradeSelectionUI : MonoBehaviour
    {
        [Title("UI Elements")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private UpgradeCardUI[] _cards = new UpgradeCardUI[3];
        [SerializeField] private Button _skipButton;

        [Title("Animation")]
        [SerializeField] private float _fadeDuration = 0.25f;

        private Tween _fadeTween;
        private bool _isOpen;

        private void Awake()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            if (_skipButton != null)
            {
                _skipButton.onClick.AddListener(OnSkipClicked);
            }
        }

        private void OnEnable()
        {
            if (RunProgressionManager.Instance != null)
            {
                RunProgressionManager.Instance.UpgradeSelectionRequested += ShowSelection;
            }
        }

        private void OnDisable()
        {
            if (RunProgressionManager.Instance != null)
            {
                RunProgressionManager.Instance.UpgradeSelectionRequested -= ShowSelection;
            }

            _fadeTween?.Kill();
        }

        private void Update()
        {
            if (!_isOpen) return;

            // Spacebar skips card selection per GDD
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                OnSkipClicked();
            }
        }

        public void ShowSelection(List<UpgradeData> upgrades)
        {
            if (_panelRoot == null || upgrades == null || upgrades.Count == 0) return;

            _isOpen = true;
            _panelRoot.SetActive(true);

            // Populate cards
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] != null)
                {
                    if (i < upgrades.Count)
                    {
                        _cards[i].Setup(upgrades[i], OnUpgradeChosen);
                    }
                    else
                    {
                        _cards[i].gameObject.SetActive(false);
                    }
                }
            }

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

        private void OnUpgradeChosen(UpgradeData upgrade)
        {
            if (RunProgressionManager.Instance != null)
            {
                RunProgressionManager.Instance.ApplyUpgrade(upgrade);
            }

            Close();
        }

        private void OnSkipClicked()
        {
            Close();
        }

        private void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;

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
                    });
            }
            else
            {
                if (_panelRoot != null) _panelRoot.SetActive(false);
            }
        }

        [Button]
        public void TestOpen()
        {
            if (RunProgressionManager.Instance != null)
            {
                RunProgressionManager.Instance.RequestUpgradeSelection();
            }
        }
    }
}
