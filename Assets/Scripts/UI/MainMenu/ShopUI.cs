using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Alchemy.Inspector;
using Game.Core;
using Game.Data;

namespace Game.UI.MainMenu
{
    public class ShopUI : MonoBehaviour
    {
        public enum ShopTab
        {
            Bonuses = 0,
            Knights = 1,
            Weapons = 2
        }

        [Title("UI Elements")]
        [SerializeField] private TMP_Text _diamondBalanceText;
        [SerializeField] private Button _backButton;
        [SerializeField, HideInInspector] private Button _addTestDiamondsButton;
        [SerializeField] private MainMenuManager _mainMenuManager;

        [Title("3-Tab Navigation (GIG-29)")]
        [SerializeField] private Button _bonusesTabButton;
        [SerializeField] private Button _knightsTabButton;
        [SerializeField] private Button _weaponsTabButton;
        [SerializeField] private Transform _itemsContainer;

        [Title("Catalogs")]
        [SerializeField] private List<KnightData> _allKnights = new();
        [SerializeField] private List<WeaponSelectionData> _allWeapons = new();

        private ShopTab _currentTab = ShopTab.Bonuses;
        private readonly List<GameObject> _spawnedRows = new();

        public ShopTab CurrentTab => _currentTab;
        public int RenderedItemCount => _spawnedRows.Count;

        private void Awake()
        {
            Debug.Assert(_diamondBalanceText != null, "[ShopUI] _diamondBalanceText is not assigned!", this);

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(OnBackClicked);
                _backButton.onClick.AddListener(OnBackClicked);
            }

            // Hide legacy prototype test diamonds button in production UI (GIG-29)
            if (_addTestDiamondsButton != null)
            {
                _addTestDiamondsButton.gameObject.SetActive(false);
            }

            if (_bonusesTabButton != null)
            {
                _bonusesTabButton.onClick.AddListener(() => SwitchTab(ShopTab.Bonuses));
            }
            if (_knightsTabButton != null)
            {
                _knightsTabButton.onClick.AddListener(() => SwitchTab(ShopTab.Knights));
            }
            if (_weaponsTabButton != null)
            {
                _weaponsTabButton.onClick.AddListener(() => SwitchTab(ShopTab.Weapons));
            }
        }

        private void OnEnable()
        {
            MetaSaveManager.DiamondsChanged += RefreshAll;
            MetaSaveManager.UnlocksChanged += RefreshAll;
            MetaSaveManager.BonusesChanged += RefreshAll;
            RefreshAll();
        }

        private void OnDisable()
        {
            MetaSaveManager.DiamondsChanged -= RefreshAll;
            MetaSaveManager.UnlocksChanged -= RefreshAll;
            MetaSaveManager.BonusesChanged -= RefreshAll;
        }

        public void SetCatalogs(List<KnightData> knights, List<WeaponSelectionData> weapons)
        {
            _allKnights = knights ?? new List<KnightData>();
            _allWeapons = weapons ?? new List<WeaponSelectionData>();
        }

        public void SwitchTab(ShopTab tab)
        {
            _currentTab = tab;
            RefreshItems();
        }

        public void UpdateBalance()
        {
            if (_diamondBalanceText != null)
            {
                _diamondBalanceText.text = $"Баланс: {MetaSaveManager.Data.Diamonds} алмазов";
            }
        }

        public void RefreshAll()
        {
            UpdateBalance();
            RefreshItems();
        }

        private void ClearSpawnedRows()
        {
            for (int i = 0; i < _spawnedRows.Count; i++)
            {
                if (_spawnedRows[i] != null)
                {
                    if (Application.isPlaying) Destroy(_spawnedRows[i]);
                    else DestroyImmediate(_spawnedRows[i]);
                }
            }
            _spawnedRows.Clear();
        }

        public void RefreshItems()
        {
            UpdateTabButtonVisuals();
            ClearSpawnedRows();

            if (_itemsContainer == null) return;

            switch (_currentTab)
            {
                case ShopTab.Bonuses:
                    BuildBonusRows();
                    break;
                case ShopTab.Knights:
                    BuildKnightRows();
                    break;
                case ShopTab.Weapons:
                    BuildWeaponRows();
                    break;
            }
        }

        private void UpdateTabButtonVisuals()
        {
            SetTabColor(_bonusesTabButton, _currentTab == ShopTab.Bonuses);
            SetTabColor(_knightsTabButton, _currentTab == ShopTab.Knights);
            SetTabColor(_weaponsTabButton, _currentTab == ShopTab.Weapons);
        }

        private static void SetTabColor(Button btn, bool active)
        {
            if (btn == null || btn.targetGraphic == null) return;
            btn.targetGraphic.color = active
                ? new Color(0.95f, 0.72f, 0.18f, 1f)
                : new Color(0.24f, 0.27f, 0.34f, 1f);
        }

        private void BuildBonusRows()
        {
            var defs = MetaBonusCatalog.Definitions;
            for (int i = 0; i < defs.Count; i++)
            {
                MetaBonusDefinition def = defs[i];
                int lvl = MetaSaveManager.GetBonusLevel(def.Id);
                bool isMax = lvl >= def.MaxLevel;
                int cost = def.GetCostForNextLevel(lvl);
                bool canAfford = !isMax && cost >= 0 && MetaSaveManager.Data.Diamonds >= cost;

                string title = $"{def.Title} (Ур. {lvl}/{def.MaxLevel})";
                string desc = MetaBonusCatalog.FormatDescription(def, lvl);
                string btnLabel = isMax ? "МАКС." : $"{cost} алм.";

                string capturedId = def.Id;
                CreateRowWidget(title, desc, btnLabel, !isMax && canAfford, () =>
                {
                    if (MetaSaveManager.TryUpgradeBonus(capturedId))
                    {
                        RefreshAll();
                    }
                });
            }
        }

        private void BuildKnightRows()
        {
            for (int i = 0; i < _allKnights.Count; i++)
            {
                KnightData k = _allKnights[i];
                if (k == null) continue;

                bool unlocked = MetaSaveManager.IsKnightUnlocked(k.Id);
                bool selected = unlocked && MetaSaveManager.Data.SelectedKnightId == k.Id;
                bool canBuy = !unlocked && MetaSaveManager.Data.Diamonds >= k.DiamondCost;

                string title = $"{k.KnightName} — {k.Title}";
                string skills = k.StartingSkills != null ? string.Join(" | ", k.StartingSkills) : k.Quote;
                string btnLabel = selected ? "ВЫБРАН" : unlocked ? "ВЫБРАТЬ" : $"{k.DiamondCost} алм.";
                bool interactable = unlocked ? !selected : canBuy;

                KnightData captured = k;
                CreateRowWidget(title, skills, btnLabel, interactable, () =>
                {
                    if (!MetaSaveManager.IsKnightUnlocked(captured.Id))
                    {
                        if (!MetaSaveManager.TryUnlockKnight(captured.Id, captured.DiamondCost)) return;
                    }

                    MetaSaveManager.SetSelected(captured.Id, null, null);
                    if (RunConfig.Instance != null) RunConfig.Instance.SelectedKnight = captured;
                    RefreshAll();
                });
            }
        }

        private void BuildWeaponRows()
        {
            for (int i = 0; i < _allWeapons.Count; i++)
            {
                WeaponSelectionData w = _allWeapons[i];
                if (w == null) continue;

                bool unlocked = MetaSaveManager.IsWeaponUnlocked(w.Id);
                bool selected = unlocked && MetaSaveManager.Data.SelectedWeaponId == w.Id;
                bool canBuy = !unlocked && MetaSaveManager.Data.Diamonds >= w.DiamondCost;

                string title = w.WeaponName;
                string desc = w.Description;
                string btnLabel = selected ? "ВЫБРАНО" : unlocked ? "ВЫБРАТЬ" : $"{w.DiamondCost} алм.";
                bool interactable = unlocked ? !selected : canBuy;

                WeaponSelectionData captured = w;
                CreateRowWidget(title, desc, btnLabel, interactable, () =>
                {
                    if (!MetaSaveManager.IsWeaponUnlocked(captured.Id))
                    {
                        if (!MetaSaveManager.TryUnlockWeapon(captured.Id, captured.DiamondCost)) return;
                    }

                    MetaSaveManager.SetSelected(null, null, captured.Id);
                    if (RunConfig.Instance != null) RunConfig.Instance.SelectedWeapon = captured;
                    RefreshAll();
                });
            }
        }

        private void CreateRowWidget(string title, string description, string actionText, bool interactable, Action onClick)
        {
            GameObject rowGo = new GameObject("ShopItemRow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            rowGo.transform.SetParent(_itemsContainer, false);

            var bg = rowGo.GetComponent<Image>();
            bg.color = new Color(0.15f, 0.17f, 0.22f, 0.92f);

            var le = rowGo.GetComponent<LayoutElement>();
            le.preferredHeight = 76f;
            le.minHeight = 68f;
            le.flexibleWidth = 1f;

            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(14, 14, 8, 8);
            hlg.spacing = 12f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            GameObject textCol = new GameObject("InfoCol", typeof(RectTransform), typeof(LayoutElement));
            textCol.transform.SetParent(rowGo.transform, false);
            var colLe = textCol.GetComponent<LayoutElement>();
            colLe.flexibleWidth = 1f;
            var vlg = textCol.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;

            TMP_Text titleTmp = CreateRowText(textCol.transform, "Title", title, 18, FontStyles.Bold, new Color(1f, 0.9f, 0.55f));
            titleTmp.textWrappingMode = TextWrappingModes.Normal;

            TMP_Text descTmp = CreateRowText(textCol.transform, "Desc", description, 13, FontStyles.Normal, new Color(0.85f, 0.87f, 0.92f));
            descTmp.textWrappingMode = TextWrappingModes.Normal;

            GameObject btnGo = new GameObject("ActionBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnGo.transform.SetParent(rowGo.transform, false);
            var btnLe = btnGo.GetComponent<LayoutElement>();
            btnLe.preferredWidth = 135f;
            btnLe.minWidth = 120f;

            var btnImg = btnGo.GetComponent<Image>();
            btnImg.color = interactable
                ? new Color(0.22f, 0.62f, 0.3f, 1f)
                : new Color(0.32f, 0.34f, 0.38f, 1f);

            var btn = btnGo.GetComponent<Button>();
            btn.interactable = interactable;
            btn.onClick.AddListener(() => onClick?.Invoke());

            TMP_Text btnTmp = CreateRowText(btnGo.transform, "Label", actionText, 15, FontStyles.Bold, Color.white);
            btnTmp.alignment = TextAlignmentOptions.Center;
            RectTransform btnTextRt = btnTmp.rectTransform;
            btnTextRt.anchorMin = Vector2.zero;
            btnTextRt.anchorMax = Vector2.one;
            btnTextRt.offsetMin = Vector2.zero;
            btnTextRt.offsetMax = Vector2.zero;

            _spawnedRows.Add(rowGo);
        }

        private TMP_Text CreateRowText(Transform parent, string goName, string content, float fontSize, FontStyles style, Color color)
        {
            GameObject go = new GameObject(goName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (_diamondBalanceText != null && _diamondBalanceText.font != null)
            {
                tmp.font = _diamondBalanceText.font;
            }
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void OnBackClicked()
        {
            if (_mainMenuManager == null)
            {
                _mainMenuManager = FindAnyObjectByType<MainMenuManager>();
            }
            _mainMenuManager?.OpenTitle();
        }

        [Button, ContextMenu("Debug Add 50 Diamonds")]
        public void Add50Diamonds()
        {
            MetaSaveManager.AddDiamonds(50);
        }
    }
}
