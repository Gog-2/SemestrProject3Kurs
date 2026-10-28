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
        [SerializeField] private Image _previewImage;

        [Title("Controls")]
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _actionButton;
        [SerializeField] private TMP_Text _actionButtonText;

        private int _currentIndex = 0;

        public ArenaData SelectedArena => _allArenas.Count > 0 ? _allArenas[_currentIndex] : null;

        private void Awake()
        {
            if (_prevButton != null) _prevButton.onClick.AddListener(OnPrevClicked);
            if (_nextButton != null) _nextButton.onClick.AddListener(OnNextClicked);
            if (_actionButton != null) _actionButton.onClick.AddListener(OnActionClicked);

            if (_allArenas.Count == 0)
            {
                PopulateDefaultArenas();
            }
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

            if (_nameText != null) _nameText.text = arena.ArenaName;
            if (_descriptionText != null) _descriptionText.text = arena.Description;

            if (_previewImage != null && arena.Preview != null)
            {
                _previewImage.sprite = arena.Preview;
                _previewImage.gameObject.SetActive(true);
            }

            bool isUnlocked = MetaSaveManager.IsArenaUnlocked(arena.Id);

            if (_actionButtonText != null)
            {
                if (isUnlocked)
                {
                    bool isSelected = MetaSaveManager.Data.SelectedArenaId == arena.Id;
                    _actionButtonText.text = isSelected ? "ВЫБРАНА" : "ВЫБРАТЬ";
                }
                else
                {
                    _actionButtonText.text = $"ОТКРЫТЬ ({arena.DiamondCost} алм.)";
                }
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

        private void PopulateDefaultArenas()
        {
            ArenaData Create(string id, string name, string desc, string scene, bool unlocked, int cost)
            {
                var a = ScriptableObject.CreateInstance<ArenaData>();
                a.InitializeRuntime(id, name, desc, scene, unlocked, cost);
                return a;
            }

            _allArenas.Add(Create("arena_plains", "Зелёные равнины", "Базовая арена. Клетки равнин и воды, замедляющие передвижение врагов.", "SampleScene", true, 0));
            _allArenas.Add(Create("arena_lava", "Вулканический каньон", "Арена с реками лавы, наносящими периодический урон всем, кто ступает на них.", "SampleScene", false, 30));
            _allArenas.Add(Create("arena_forest", "Древний лес", "Густой лес с 50% шансом промаха дальнобойных атак.", "SampleScene", false, 30));
        }
    }
}
