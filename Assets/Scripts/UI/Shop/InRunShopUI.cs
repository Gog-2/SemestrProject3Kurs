using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Alchemy.Inspector;
using Game.Buildings;
using Game.Core;
using Game.Data;
using Game.WaveSystem;

namespace Game.UI.Shop
{
    public class InRunShopUI : MonoBehaviour
    {
        public static InRunShopUI Instance { get; private set; }
        public static bool IsAnyShopOpen => Instance != null && Instance._isOpen;

        [Serializable]
        public class OfferCardWidgets
        {
            public Button BuyButton;
            public TMP_Text TitleText;
            public TMP_Text DescriptionText;
            public TMP_Text PriceText;
            public Image IconImage;
        }

        [Title("Config & References")]
        [SerializeField] private BuildingPoolConfig _poolConfig;
        [SerializeField] private GameObject _shopPanelRoot;
        [SerializeField] private Button _openShopButton;
        [SerializeField] private TMP_Text _openShopButtonText;
        [SerializeField] private Button _closeButton;

        [Title("Top Section: 4 Building Plans")]
        [SerializeField] private OfferCardWidgets[] _buildingOfferCards = new OfferCardWidgets[4];

        [Title("Bottom Section: 2 Minor Bonuses")]
        [SerializeField] private OfferCardWidgets[] _bonusOfferCards = new OfferCardWidgets[2];

        private readonly BuildingPoolEntry[] _currentBuildingOffers = new BuildingPoolEntry[4];
        private readonly ShopMinorBonusEntry[] _currentBonusOffers = new ShopMinorBonusEntry[2];
        private readonly bool[] _bonusPurchased = new bool[2];
        private bool _isOpen;
        private bool _hasRolledAtLeastOnce;

        public BuildingPoolConfig PoolConfig => _poolConfig;

        public void InitializeReferences(
            BuildingPoolConfig poolConfig,
            GameObject shopPanelRoot,
            Button openShopButton,
            TMP_Text openShopButtonText,
            Button closeButton,
            OfferCardWidgets[] buildingCards,
            OfferCardWidgets[] bonusCards)
        {
            _poolConfig = poolConfig;
            _shopPanelRoot = shopPanelRoot;
            _openShopButton = openShopButton;
            _openShopButtonText = openShopButtonText;
            _closeButton = closeButton;
            _buildingOfferCards = buildingCards;
            _bonusOfferCards = bonusCards;
            BindStaticButtons();
            if (_shopPanelRoot != null) _shopPanelRoot.SetActive(false);
        }

        private void Awake()
        {
            if (Instance == null || Instance == this)
            {
                Instance = this;
            }

            BindStaticButtons();
            if (_shopPanelRoot != null) _shopPanelRoot.SetActive(false);
        }

        private void BindStaticButtons()
        {
            if (_openShopButton != null)
            {
                _openShopButton.onClick.RemoveListener(ToggleShop);
                _openShopButton.onClick.AddListener(ToggleShop);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(CloseShop);
                _closeButton.onClick.AddListener(CloseShop);
            }

            for (int i = 0; i < _buildingOfferCards.Length; i++)
            {
                int idx = i;
                var card = _buildingOfferCards[i];
                if (card?.BuyButton != null)
                {
                    card.BuyButton.onClick.RemoveAllListeners();
                    card.BuyButton.onClick.AddListener(() => BuyBuildingOffer(idx));
                }
            }

            for (int i = 0; i < _bonusOfferCards.Length; i++)
            {
                int idx = i;
                var card = _bonusOfferCards[i];
                if (card?.BuyButton != null)
                {
                    card.BuyButton.onClick.RemoveAllListeners();
                    card.BuyButton.onClick.AddListener(() => BuyBonusOffer(idx));
                }
            }
        }

        private void OnEnable()
        {
            Subscribe();
            UpdateOpenButtonAvailability();
        }

        private void Start()
        {
            Subscribe();
            if (!_hasRolledAtLeastOnce)
            {
                RollNewPauseStock();
            }
            UpdateOpenButtonAvailability();
        }

        private void Subscribe()
        {
            if (WaveController.Instance != null)
            {
                WaveController.Instance.OnBuildingPhaseStarted -= OnBuildingPhaseStarted;
                WaveController.Instance.OnBuildingPhaseStarted += OnBuildingPhaseStarted;
                WaveController.Instance.OnBuildingPhaseEnded -= OnBuildingPhaseEnded;
                WaveController.Instance.OnBuildingPhaseEnded += OnBuildingPhaseEnded;
                WaveController.Instance.StateChanged -= OnWaveStateChanged;
                WaveController.Instance.StateChanged += OnWaveStateChanged;
            }

            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.CoinsChanged -= OnCoinsChanged;
                PlayerWallet.Instance.CoinsChanged += OnCoinsChanged;
            }

            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.SlotsChanged -= RefreshUI;
                BuildingManager.Instance.SlotsChanged += RefreshUI;
            }
        }

        private void OnDisable()
        {
            if (WaveController.Instance != null)
            {
                WaveController.Instance.OnBuildingPhaseStarted -= OnBuildingPhaseStarted;
                WaveController.Instance.OnBuildingPhaseEnded -= OnBuildingPhaseEnded;
                WaveController.Instance.StateChanged -= OnWaveStateChanged;
            }

            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.CoinsChanged -= OnCoinsChanged;
            }

            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.SlotsChanged -= RefreshUI;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnBuildingPhaseStarted()
        {
            RollNewPauseStock();
            UpdateOpenButtonAvailability();
        }

        private void OnBuildingPhaseEnded()
        {
            CloseShop();
            UpdateOpenButtonAvailability();
        }

        private void OnWaveStateChanged(WaveState state)
        {
            if (state != WaveState.BuildingPhase)
            {
                CloseShop();
            }
            UpdateOpenButtonAvailability();
        }

        private void OnCoinsChanged(int _)
        {
            if (_isOpen)
            {
                RefreshUI();
            }
        }

        public bool IsPausePhaseActive()
        {
            if (WaveController.Instance == null) return true;
            return WaveController.Instance.CurrentState == WaveState.BuildingPhase;
        }

        private void UpdateOpenButtonAvailability()
        {
            if (_openShopButton == null) return;
            bool available = IsPausePhaseActive() &&
                             (GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing);
            _openShopButton.interactable = available;
            if (_openShopButtonText != null)
            {
                _openShopButtonText.text = available ? "Магазин" : "Магазин (пауза)";
            }
        }

        public void ToggleShop()
        {
            if (_isOpen) CloseShop();
            else OpenShop();
        }

        public void OpenShop()
        {
            if (!IsPausePhaseActive()) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (!_hasRolledAtLeastOnce)
            {
                RollNewPauseStock();
            }

            _isOpen = true;
            if (_shopPanelRoot != null) _shopPanelRoot.SetActive(true);
            RefreshUI();
        }

        public void CloseShop()
        {
            if (!_isOpen && (_shopPanelRoot == null || !_shopPanelRoot.activeSelf)) return;
            _isOpen = false;
            if (_shopPanelRoot != null) _shopPanelRoot.SetActive(false);
        }

        [Button]
        public void RollNewPauseStock()
        {
            _hasRolledAtLeastOnce = true;

            int castleLevel = RunProgressionManager.Instance != null ? RunProgressionManager.Instance.CurrentLevel : 1;
            KnightData knight = RunConfig.Instance != null ? RunConfig.Instance.SelectedKnight : null;

            Array.Clear(_currentBuildingOffers, 0, _currentBuildingOffers.Length);
            Array.Clear(_currentBonusOffers, 0, _currentBonusOffers.Length);
            Array.Clear(_bonusPurchased, 0, _bonusPurchased.Length);

            if (_poolConfig != null)
            {
                List<BuildingPoolEntry> bOffers = _poolConfig.RollBuildingOffers(4, castleLevel, knight);
                for (int i = 0; i < _currentBuildingOffers.Length && i < bOffers.Count; i++)
                {
                    _currentBuildingOffers[i] = bOffers[i];
                }

                List<ShopMinorBonusEntry> sOffers = _poolConfig.RollMinorBonusOffers(2);
                for (int i = 0; i < _currentBonusOffers.Length && i < sOffers.Count; i++)
                {
                    _currentBonusOffers[i] = sOffers[i];
                }
            }

            if (_isOpen)
            {
                RefreshUI();
            }
        }

        public bool BuyBuildingOffer(int index)
        {
            if (!IsPausePhaseActive()) return false;
            if (index < 0 || index >= _currentBuildingOffers.Length) return false;

            BuildingPoolEntry entry = _currentBuildingOffers[index];
            if (entry == null || entry.Data == null) return false;

            if (BuildingManager.Instance == null || !BuildingManager.Instance.CanAddBlueprint(entry.Data))
            {
                return false;
            }

            float discountMult = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveUpgradeCostMultiplier()
                : 1f;
            int price = entry.GetPrice(discountMult);

            if (PlayerWallet.Instance == null || !PlayerWallet.Instance.TrySpendCoins(price))
            {
                return false;
            }

            BuildingManager.Instance.TryAddBlueprint(entry.Data, 1);

            // Immediately replace purchased plan with a fresh unique blueprint (GIG-70)
            ReplaceBuildingOfferSlot(index);
            RefreshUI();
            return true;
        }

        private void ReplaceBuildingOfferSlot(int slotIndex)
        {
            if (_poolConfig == null)
            {
                _currentBuildingOffers[slotIndex] = null;
                return;
            }

            int castleLevel = RunProgressionManager.Instance != null ? RunProgressionManager.Instance.CurrentLevel : 1;
            KnightData knight = RunConfig.Instance != null ? RunConfig.Instance.SelectedKnight : null;

            var activeSet = new HashSet<BuildingData>();
            for (int i = 0; i < _currentBuildingOffers.Length; i++)
            {
                if (i != slotIndex && _currentBuildingOffers[i]?.Data != null)
                {
                    activeSet.Add(_currentBuildingOffers[i].Data);
                }
            }
            if (_currentBuildingOffers[slotIndex]?.Data != null)
            {
                activeSet.Add(_currentBuildingOffers[slotIndex].Data);
            }

            _currentBuildingOffers[slotIndex] = _poolConfig.RollSingleReplacement(castleLevel, knight, activeSet);
        }

        public bool BuyBonusOffer(int index)
        {
            if (!IsPausePhaseActive()) return false;
            if (index < 0 || index >= _currentBonusOffers.Length) return false;
            if (_bonusPurchased[index]) return false;

            ShopMinorBonusEntry bonus = _currentBonusOffers[index];
            if (bonus == null) return false;

            if (PlayerWallet.Instance == null || !PlayerWallet.Instance.TrySpendCoins(bonus.Cost))
            {
                return false;
            }

            _bonusPurchased[index] = true;
            RunProgressionManager.Instance?.ApplyShopBonusEffect(bonus.EffectType, bonus.Value, bonus.Title);
            RefreshUI();
            return true;
        }

        public void RefreshUI()
        {
            int coins = PlayerWallet.Instance != null ? PlayerWallet.Instance.Coins : 0;
            float discountMult = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveUpgradeCostMultiplier()
                : 1f;

            for (int i = 0; i < _buildingOfferCards.Length; i++)
            {
                OfferCardWidgets card = _buildingOfferCards[i];
                if (card == null) continue;

                BuildingPoolEntry offer = i < _currentBuildingOffers.Length ? _currentBuildingOffers[i] : null;
                if (offer == null || offer.Data == null)
                {
                    if (card.TitleText != null) card.TitleText.text = "Нет в наличии";
                    if (card.DescriptionText != null) card.DescriptionText.text = string.Empty;
                    if (card.PriceText != null) card.PriceText.text = "-";
                    if (card.IconImage != null) card.IconImage.enabled = false;
                    if (card.BuyButton != null) card.BuyButton.interactable = false;
                    continue;
                }

                int price = offer.GetPrice(discountMult);
                bool canFit = BuildingManager.Instance == null || BuildingManager.Instance.CanAddBlueprint(offer.Data);
                bool canAfford = coins >= price && canFit;

                if (card.TitleText != null) card.TitleText.text = $"{offer.Data.DisplayName} ({offer.Data.Size.x}x{offer.Data.Size.y})";
                if (card.DescriptionText != null) card.DescriptionText.text = offer.Data.Description;
                if (card.PriceText != null) card.PriceText.text = canFit ? $"{price} м." : "Слоты полны";
                if (card.IconImage != null)
                {
                    card.IconImage.sprite = offer.Data.Icon;
                    card.IconImage.enabled = offer.Data.Icon != null;
                }
                if (card.BuyButton != null) card.BuyButton.interactable = canAfford;
            }

            for (int i = 0; i < _bonusOfferCards.Length; i++)
            {
                OfferCardWidgets card = _bonusOfferCards[i];
                if (card == null) continue;

                ShopMinorBonusEntry offer = i < _currentBonusOffers.Length ? _currentBonusOffers[i] : null;
                if (offer == null)
                {
                    if (card.TitleText != null) card.TitleText.text = "-";
                    if (card.DescriptionText != null) card.DescriptionText.text = string.Empty;
                    if (card.PriceText != null) card.PriceText.text = "-";
                    if (card.BuyButton != null) card.BuyButton.interactable = false;
                    continue;
                }

                bool sold = _bonusPurchased[i];
                bool canAfford = !sold && coins >= offer.Cost;
                if (card.TitleText != null) card.TitleText.text = offer.Title;
                if (card.DescriptionText != null) card.DescriptionText.text = offer.Description;
                if (card.PriceText != null) card.PriceText.text = sold ? "КУПЛЕНО" : $"{offer.Cost} м.";
                if (card.BuyButton != null) card.BuyButton.interactable = canAfford;
            }
        }
    }
}
