using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Alchemy.Inspector;
using Game.Core;

namespace Game.UI.MainMenu
{
    public class DifficultySelectionUI : MonoBehaviour
    {
        [Title("Difficulty Buttons")]
        [SerializeField] private Button _baseButton;
        [SerializeField] private Button _hardButton;
        [SerializeField] private Button _legendaryButton;

        [Title("Labels & Description")]
        [SerializeField] private TMP_Text _baseButtonText;
        [SerializeField] private TMP_Text _hardButtonText;
        [SerializeField] private TMP_Text _legendaryButtonText;
        [SerializeField] private TMP_Text _descriptionText;

        [Title("Colors")]
        [SerializeField] private Color _selectedColor = new(0.22f, 0.58f, 0.32f, 1f);
        [SerializeField] private Color _unlockedColor = new(0.20f, 0.35f, 0.55f, 1f);
        [SerializeField] private Color _lockedColor = new(0.22f, 0.22f, 0.26f, 0.70f);

        private void Awake()
        {
            BindButtons();
        }

        private void OnEnable()
        {
            MetaSaveManager.UnlocksChanged += RefreshDisplay;
            MetaSaveManager.SelectedDifficultyChanged += OnSelectedDifficultyChanged;
            RefreshDisplay();
        }

        private void OnDisable()
        {
            MetaSaveManager.UnlocksChanged -= RefreshDisplay;
            MetaSaveManager.SelectedDifficultyChanged -= OnSelectedDifficultyChanged;
        }

        public void InitializeReferences(
            Button baseButton,
            Button hardButton,
            Button legendaryButton,
            TMP_Text baseText,
            TMP_Text hardText,
            TMP_Text legendaryText,
            TMP_Text descriptionText)
        {
            _baseButton = baseButton;
            _hardButton = hardButton;
            _legendaryButton = legendaryButton;
            _baseButtonText = baseText;
            _hardButtonText = hardText;
            _legendaryButtonText = legendaryText;
            _descriptionText = descriptionText;
            BindButtons();
        }

        private void BindButtons()
        {
            if (_baseButton != null)
            {
                _baseButton.onClick.RemoveListener(SelectBase);
                _baseButton.onClick.AddListener(SelectBase);
            }

            if (_hardButton != null)
            {
                _hardButton.onClick.RemoveListener(SelectHard);
                _hardButton.onClick.AddListener(SelectHard);
            }

            if (_legendaryButton != null)
            {
                _legendaryButton.onClick.RemoveListener(SelectLegendary);
                _legendaryButton.onClick.AddListener(SelectLegendary);
            }
        }

        public void SelectDifficulty(DifficultyMode mode)
        {
            if (!MetaSaveManager.IsDifficultyUnlocked(mode))
            {
                RefreshDisplay();
                return;
            }

            MetaSaveManager.SetSelectedDifficulty(mode);
            SyncRunConfig(mode);
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            DifficultyMode selected = MetaSaveManager.Data.SelectedDifficulty;
            if (!MetaSaveManager.IsDifficultyUnlocked(selected))
            {
                selected = DifficultyMode.Base;
                MetaSaveManager.SetSelectedDifficulty(selected);
            }

            SyncRunConfig(selected);

            UpdateButtonVisual(_baseButton, _baseButtonText, DifficultyMode.Base, selected);
            UpdateButtonVisual(_hardButton, _hardButtonText, DifficultyMode.Hard, selected);
            UpdateButtonVisual(_legendaryButton, _legendaryButtonText, DifficultyMode.Legendary, selected);

            if (_descriptionText != null)
            {
                string desc = DifficultyModifiers.GetDescription(selected);
                if (!MetaSaveManager.IsDifficultyUnlocked(DifficultyMode.Hard))
                {
                    desc += "\n[Усложнённая: победите на Базовой • Легендарная: победите на Усложнённой]";
                }
                else if (!MetaSaveManager.IsDifficultyUnlocked(DifficultyMode.Legendary))
                {
                    desc += "\n[Легендарная: победите на Усложнённой для открытия]";
                }
                _descriptionText.text = desc;
            }
        }

        private void UpdateButtonVisual(Button button, TMP_Text label, DifficultyMode mode, DifficultyMode selected)
        {
            bool unlocked = MetaSaveManager.IsDifficultyUnlocked(mode);
            bool isSelected = selected == mode;

            if (button != null)
            {
                button.interactable = unlocked;
                Image img = button.GetComponent<Image>();
                if (img != null)
                {
                    img.color = !unlocked
                        ? _lockedColor
                        : (isSelected ? _selectedColor : _unlockedColor);
                }
            }

            if (label != null)
            {
                string title = DifficultyModifiers.GetDisplayName(mode);
                label.text = unlocked ? title : $"{title} (Закрыто)";
            }
        }

        private void OnSelectedDifficultyChanged(DifficultyMode mode)
        {
            SyncRunConfig(mode);
            RefreshDisplay();
        }

        private static void SyncRunConfig(DifficultyMode mode)
        {
            if (RunConfig.Instance != null)
            {
                RunConfig.Instance.SelectedDifficulty = mode;
            }
        }

        private void SelectBase() => SelectDifficulty(DifficultyMode.Base);
        private void SelectHard() => SelectDifficulty(DifficultyMode.Hard);
        private void SelectLegendary() => SelectDifficulty(DifficultyMode.Legendary);
    }
}
