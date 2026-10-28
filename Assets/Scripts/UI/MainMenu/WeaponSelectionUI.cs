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
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private Image _iconImage;

        [Title("Controls")]
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _actionButton;
        [SerializeField] private TMP_Text _actionButtonText;

        private int _currentIndex = 0;

        public WeaponSelectionData SelectedWeapon => _allWeapons.Count > 0 ? _allWeapons[_currentIndex] : null;

        private void Awake()
        {
            if (_prevButton != null) _prevButton.onClick.AddListener(OnPrevClicked);
            if (_nextButton != null) _nextButton.onClick.AddListener(OnNextClicked);
            if (_actionButton != null) _actionButton.onClick.AddListener(OnActionClicked);

            if (_allWeapons.Count == 0)
            {
                PopulateDefaultWeapons();
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
            if (_allWeapons.Count == 0) return;

            _currentIndex = Mathf.Clamp(_currentIndex, 0, _allWeapons.Count - 1);
            WeaponSelectionData weapon = _allWeapons[_currentIndex];
            if (weapon == null) return;

            if (_nameText != null) _nameText.text = weapon.WeaponName;
            if (_descriptionText != null) _descriptionText.text = weapon.Description;

            if (_iconImage != null && weapon.Icon != null)
            {
                _iconImage.sprite = weapon.Icon;
                _iconImage.gameObject.SetActive(true);
            }

            bool isUnlocked = MetaSaveManager.IsWeaponUnlocked(weapon.Id);

            if (_actionButtonText != null)
            {
                if (isUnlocked)
                {
                    bool isSelected = MetaSaveManager.Data.SelectedWeaponId == weapon.Id;
                    _actionButtonText.text = isSelected ? "ВЫБРАНО" : "ВЫБРАТЬ";
                }
                else
                {
                    _actionButtonText.text = $"ОТКРЫТЬ ({weapon.DiamondCost} алм.)";
                }
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

        private void PopulateDefaultWeapons()
        {
            WeaponSelectionData Create(string id, string name, string desc, bool unlocked, int cost)
            {
                var w = ScriptableObject.CreateInstance<WeaponSelectionData>();
                w.InitializeRuntime(id, name, desc, unlocked, cost);
                return w;
            }

            _allWeapons.Add(Create("heavy_ballista", "Тяжёлая баллиста", "Выстреливает массивными болтами, пробивающими всех врагов насквозь и отталкивающими их на одну клетку.", true, 0));
            _allWeapons.Add(Create("alch_mortar", "Алхимическая мортира", "Выстреливает разрывными снарядами по площади 2х2 клетки, оставляя горящие лужи.", false, 40));
            _allWeapons.Add(Create("tar_cannon", "Смоляная пушка", "Создаёт поле смолы, замедляющее врагов на 30-80%.", false, 40));
            _allWeapons.Add(Create("orthunk_tower", "Ортхунк (Ураган)", "Смерч, наносящий периодический урон и стягивающий врагов к центру.", false, 40));
            _allWeapons.Add(Create("archers_salvo", "Обстрел лучников", "Град стрел лучников замка по указанной области.", false, 40));
        }
    }
}
