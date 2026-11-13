using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Alchemy.Inspector;
using Game.Core;
using Game.Data;

namespace Game.UI.MainMenu
{
    public class KnightSelectionUI : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private List<KnightData> _allKnights = new();
        [SerializeField] private Image _portraitImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _quoteText;
        [SerializeField] private TMP_Text _statsText;
        [SerializeField] private TMP_Text _skillsText;

        [Title("Controls")]
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _actionButton;
        [SerializeField] private TMP_Text _actionButtonText;

        private int _currentIndex = 0;

        public KnightData SelectedKnight => _allKnights.Count > 0 ? _allKnights[_currentIndex] : null;

        private void Awake()
        {
            Debug.Assert(_prevButton != null, "[KnightSelectionUI] _prevButton is not assigned!", this);
            Debug.Assert(_nextButton != null, "[KnightSelectionUI] _nextButton is not assigned!", this);
            Debug.Assert(_actionButton != null, "[KnightSelectionUI] _actionButton is not assigned!", this);
            Debug.Assert(_nameText != null, "[KnightSelectionUI] _nameText is not assigned!", this);
            Debug.Assert(_titleText != null, "[KnightSelectionUI] _titleText is not assigned!", this);
            Debug.Assert(_quoteText != null, "[KnightSelectionUI] _quoteText is not assigned!", this);
            Debug.Assert(_statsText != null, "[KnightSelectionUI] _statsText is not assigned!", this);
            Debug.Assert(_skillsText != null, "[KnightSelectionUI] _skillsText is not assigned!", this);
            Debug.Assert(_actionButtonText != null, "[KnightSelectionUI] _actionButtonText is not assigned!", this);

            _prevButton.onClick.AddListener(OnPrevClicked);
            _nextButton.onClick.AddListener(OnNextClicked);
            _actionButton.onClick.AddListener(OnActionClicked);
        }

        private void OnEnable()
        {
            MetaSaveManager.UnlocksChanged += RefreshDisplay;
            SyncIndexToSavedKnight();
            RefreshDisplay();
        }

        private void SyncIndexToSavedKnight()
        {
            string selectedId = MetaSaveManager.Data?.SelectedKnightId;
            if (string.IsNullOrEmpty(selectedId)) return;

            for (int i = 0; i < _allKnights.Count; i++)
            {
                if (_allKnights[i] != null && _allKnights[i].Id == selectedId)
                {
                    _currentIndex = i;
                    return;
                }
            }
        }

        private void OnDisable()
        {
            MetaSaveManager.UnlocksChanged -= RefreshDisplay;
        }

        public void RefreshDisplay()
        {
            if (_allKnights.Count == 0) return;

            _currentIndex = Mathf.Clamp(_currentIndex, 0, _allKnights.Count - 1);
            KnightData knight = _allKnights[_currentIndex];
            if (knight == null) return;

            _nameText.text = knight.KnightName;
            _titleText.text = knight.Title;
            _quoteText.text = knight.Quote;

            if (_portraitImage != null)
            {
                _portraitImage.sprite = knight.Portrait;
                _portraitImage.enabled = knight.Portrait != null;
            }

            _statsText.text = $"Прочность замка: {knight.CastleMaxHP} (+{knight.HPPerLevel}/ур.)\nСлотов для построек: {knight.InitialBuildSlots}";

            string bld = knight.StartingBuildings != null ? string.Join(", ", knight.StartingBuildings) : "-";
            string skl = knight.StartingSkills != null ? string.Join("\n- ", knight.StartingSkills) : "-";
            _skillsText.text = $"Стартовые постройки: {bld}\nНавыки:\n- {skl}";

            bool isUnlocked = MetaSaveManager.IsKnightUnlocked(knight.Id);

            if (isUnlocked)
            {
                bool isSelected = MetaSaveManager.Data.SelectedKnightId == knight.Id;
                _actionButtonText.text = isSelected ? "ВЫБРАН" : "ВЫБРАТЬ";
            }
            else
            {
                _actionButtonText.text = $"ОТКРЫТЬ ({knight.DiamondCost} алм.)";
            }

            if (RunConfig.Instance != null && isUnlocked)
            {
                RunConfig.Instance.SelectedKnight = knight;
            }
        }

        private void OnPrevClicked()
        {
            if (_allKnights.Count == 0) return;
            _currentIndex = (_currentIndex - 1 + _allKnights.Count) % _allKnights.Count;
            RefreshDisplay();
        }

        private void OnNextClicked()
        {
            if (_allKnights.Count == 0) return;
            _currentIndex = (_currentIndex + 1) % _allKnights.Count;
            RefreshDisplay();
        }

        private void OnActionClicked()
        {
            if (_allKnights.Count == 0) return;
            KnightData knight = _allKnights[_currentIndex];
            if (knight == null) return;

            if (!MetaSaveManager.IsKnightUnlocked(knight.Id))
            {
                if (MetaSaveManager.TryUnlockKnight(knight.Id, knight.DiamondCost))
                {
                    SelectKnight(knight);
                }
                else
                {
                    Debug.LogWarning("[KnightSelectionUI] Недостаточно алмазов для открытия!", this);
                }
            }
            else
            {
                SelectKnight(knight);
            }
        }

        private void SelectKnight(KnightData knight)
        {
            MetaSaveManager.SetSelected(knight.Id, null, null);
            if (RunConfig.Instance != null)
            {
                RunConfig.Instance.SelectedKnight = knight;
            }
            RefreshDisplay();
        }
    }
}
