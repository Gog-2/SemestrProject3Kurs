using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Alchemy.Inspector;
using Game.Core;
using Game.Data;

namespace Game.UI.MainMenu
{
    public class WeaponSelectionUI : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private List<WeaponSelectionData> _allWeapons = new();
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descriptionText;

        [Title("Controls")]
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _actionButton;
        [SerializeField] private TMP_Text _actionButtonText;

        private int _currentIndex = 0;

        public WeaponSelectionData SelectedWeapon => _allWeapons.Count > 0 ? _allWeapons[_currentIndex] : null;

        private void Awake()
        {
            Debug.Assert(_prevButton != null, "[WeaponSelectionUI] _prevButton is not assigned!", this);
            Debug.Assert(_nextButton != null, "[WeaponSelectionUI] _nextButton is not assigned!", this);
            Debug.Assert(_actionButton != null, "[WeaponSelectionUI] _actionButton is not assigned!", this);
            Debug.Assert(_nameText != null, "[WeaponSelectionUI] _nameText is not assigned!", this);
            Debug.Assert(_descriptionText != null, "[WeaponSelectionUI] _descriptionText is not assigned!", this);
            Debug.Assert(_actionButtonText != null, "[WeaponSelectionUI] _actionButtonText is not assigned!", this);

            _prevButton.onClick.AddListener(OnPrevClicked);
            _nextButton.onClick.AddListener(OnNextClicked);
            _actionButton.onClick.AddListener(OnActionClicked);
        }

        private void OnEnable()
        {
            MetaSaveManager.UnlocksChanged += RefreshDisplay;
            SyncIndexToSavedWeapon();
            RefreshDisplay();
        }

        private void SyncIndexToSavedWeapon()
        {
            string selectedId = MetaSaveManager.Data?.SelectedWeaponId;
            if (string.IsNullOrEmpty(selectedId)) return;

            for (int i = 0; i < _allWeapons.Count; i++)
            {
                if (_allWeapons[i] != null && _allWeapons[i].Id == selectedId)
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
            if (_allWeapons.Count == 0) return;

            _currentIndex = Mathf.Clamp(_currentIndex, 0, _allWeapons.Count - 1);
            WeaponSelectionData weapon = _allWeapons[_currentIndex];
            if (weapon == null) return;

            _nameText.text = weapon.WeaponName;
            _descriptionText.text = weapon.Description;

            if (_iconImage != null)
            {
                _iconImage.sprite = weapon.Icon;
                _iconImage.enabled = weapon.Icon != null;
            }

            bool isUnlocked = MetaSaveManager.IsWeaponUnlocked(weapon.Id);

            if (isUnlocked)
            {
                bool isSelected = MetaSaveManager.Data.SelectedWeaponId == weapon.Id;
                _actionButtonText.text = isSelected ? "ВЫБРАНО" : "ВЫБРАТЬ";
            }
            else
            {
                _actionButtonText.text = $"ОТКРЫТЬ ({weapon.DiamondCost} алм.)";
            }

            if (RunConfig.Instance != null && isUnlocked)
            {
                RunConfig.Instance.SelectedWeapon = weapon;
            }
        }

        private void OnPrevClicked()
        {
            if (_allWeapons.Count == 0) return;
            _currentIndex = (_currentIndex - 1 + _allWeapons.Count) % _allWeapons.Count;
            RefreshDisplay();
        }

        private void OnNextClicked()
        {
            if (_allWeapons.Count == 0) return;
            _currentIndex = (_currentIndex + 1) % _allWeapons.Count;
            RefreshDisplay();
        }

        private void OnActionClicked()
        {
            if (_allWeapons.Count == 0) return;
            WeaponSelectionData weapon = _allWeapons[_currentIndex];
            if (weapon == null) return;

            if (!MetaSaveManager.IsWeaponUnlocked(weapon.Id))
            {
                if (MetaSaveManager.TryUnlockWeapon(weapon.Id, weapon.DiamondCost))
                {
                    SelectWeapon(weapon);
                }
                else
                {
                    Debug.Log("[WeaponSelectionUI] Недостаточно алмазов для открытия орудия!");
                }
            }
            else
            {
                SelectWeapon(weapon);
            }
        }

        private void SelectWeapon(WeaponSelectionData weapon)
        {
            MetaSaveManager.SetSelected(null, null, weapon.Id);
            if (RunConfig.Instance != null)
            {
                RunConfig.Instance.SelectedWeapon = weapon;
            }
            RefreshDisplay();
        }
    }
}
