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
            if (_prevButton != null) _prevButton.onClick.AddListener(OnPrevClicked);
            if (_nextButton != null) _nextButton.onClick.AddListener(OnNextClicked);
            if (_actionButton != null) _actionButton.onClick.AddListener(OnActionClicked);
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
            if (_allKnights.Count == 0) return;

            _currentIndex = Mathf.Clamp(_currentIndex, 0, _allKnights.Count - 1);
            KnightData knight = _allKnights[_currentIndex];
            if (knight == null) return;

            if (_nameText != null) _nameText.text = knight.KnightName;
            if (_titleText != null) _titleText.text = knight.Title;
            if (_quoteText != null) _quoteText.text = knight.Quote;

            if (_statsText != null)
            {
                _statsText.text = $"Прочность замка: {knight.CastleMaxHP} (+{knight.HPPerLevel}/ур.)\nСлотов для построек: {knight.InitialBuildSlots}";
            }

            if (_skillsText != null)
            {
                string bld = knight.StartingBuildings != null ? string.Join(", ", knight.StartingBuildings) : "-";
                string skl = knight.StartingSkills != null ? string.Join("\n- ", knight.StartingSkills) : "-";
                _skillsText.text = $"Стартовые постройки: {bld}\nНавыки:\n- {skl}";
            }

            bool isUnlocked = MetaSaveManager.IsKnightUnlocked(knight.Id);

            if (_actionButtonText != null)
            {
                if (isUnlocked)
                {
                    bool isSelected = MetaSaveManager.Data.SelectedKnightId == knight.Id;
                    _actionButtonText.text = isSelected ? "ВЫБРАН" : "ВЫБРАТЬ";
                }
                else
                {
                    _actionButtonText.text = $"ОТКРЫТЬ ({knight.DiamondCost} алм.)";
                }
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
                    Debug.Log("[KnightSelectionUI] Недостаточно алмазов для открытия!");
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
