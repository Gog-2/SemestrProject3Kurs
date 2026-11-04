using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Alchemy.Inspector;
using Game.Core;
using Game.Data;

namespace Game.UI.MainMenu
{
    public class ArenaSelectionUI : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private List<ArenaData> _allArenas = new();
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descriptionText;

        [Title("Controls")]
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _actionButton;
        [SerializeField] private TMP_Text _actionButtonText;

        private int _currentIndex = 0;

        public ArenaData SelectedArena => _allArenas.Count > 0 ? _allArenas[_currentIndex] : null;

        private void Awake()
        {
            Debug.Assert(_prevButton != null, "[ArenaSelectionUI] _prevButton is not assigned!", this);
            Debug.Assert(_nextButton != null, "[ArenaSelectionUI] _nextButton is not assigned!", this);
            Debug.Assert(_actionButton != null, "[ArenaSelectionUI] _actionButton is not assigned!", this);
            Debug.Assert(_nameText != null, "[ArenaSelectionUI] _nameText is not assigned!", this);
            Debug.Assert(_descriptionText != null, "[ArenaSelectionUI] _descriptionText is not assigned!", this);
            Debug.Assert(_actionButtonText != null, "[ArenaSelectionUI] _actionButtonText is not assigned!", this);

            _prevButton.onClick.AddListener(OnPrevClicked);
            _nextButton.onClick.AddListener(OnNextClicked);
            _actionButton.onClick.AddListener(OnActionClicked);
        }

        private void OnEnable()
        {
            MetaSaveManager.UnlocksChanged += RefreshDisplay;
            RefreshDisplay();
        }

        private void OnDisable()
        {
            MetaSaveManager.UnlocksChanged -= RefreshDisplay;
        }

        public void RefreshDisplay()
        {
            if (_allArenas.Count == 0) return;

            _currentIndex = Mathf.Clamp(_currentIndex, 0, _allArenas.Count - 1);
            ArenaData arena = _allArenas[_currentIndex];
            if (arena == null) return;

            _nameText.text = arena.ArenaName;
            _descriptionText.text = arena.Description;

            bool isUnlocked = MetaSaveManager.IsArenaUnlocked(arena.Id);

            if (isUnlocked)
            {
                bool isSelected = MetaSaveManager.Data.SelectedArenaId == arena.Id;
                _actionButtonText.text = isSelected ? "ВЫБРАНА" : "ВЫБРАТЬ";
            }
            else
            {
                _actionButtonText.text = $"ОТКРЫТЬ ({arena.DiamondCost} алм.)";
            }

            if (RunConfig.Instance != null && isUnlocked)
            {
                RunConfig.Instance.SelectedArena = arena;
            }
        }

        private void OnPrevClicked()
        {
            if (_allArenas.Count == 0) return;
            _currentIndex = (_currentIndex - 1 + _allArenas.Count) % _allArenas.Count;
            RefreshDisplay();
        }

        private void OnNextClicked()
        {
            if (_allArenas.Count == 0) return;
            _currentIndex = (_currentIndex + 1) % _allArenas.Count;
            RefreshDisplay();
        }

        private void OnActionClicked()
        {
            if (_allArenas.Count == 0) return;
            ArenaData arena = _allArenas[_currentIndex];
            if (arena == null) return;

            if (!MetaSaveManager.IsArenaUnlocked(arena.Id))
            {
                if (MetaSaveManager.TryUnlockArena(arena.Id, arena.DiamondCost))
                {
                    SelectArena(arena);
                }
                else
                {
                    Debug.Log("[ArenaSelectionUI] Недостаточно алмазов для открытия арены!");
                }
            }
            else
            {
                SelectArena(arena);
            }
        }

        private void SelectArena(ArenaData arena)
        {
            MetaSaveManager.SetSelected(null, arena.Id, null);
            if (RunConfig.Instance != null)
            {
                RunConfig.Instance.SelectedArena = arena;
            }
            RefreshDisplay();
        }
    }
}
