using System.Collections.Generic;
using UnityEngine;
using Alchemy.Inspector;
using Game.Buildings;
using Game.Core;

namespace Game.UI.BuildingUI
{
    public class BuildingSlotBarUI : MonoBehaviour
    {
        [Title("Slots References")]
        [SerializeField] private BuildingSlotButton[] _slotButtons = new BuildingSlotButton[5];

        public void InitializeReferences(BuildingSlotButton[] slotButtons)
        {
            _slotButtons = slotButtons;
            RefreshSlots();
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshSlots();
        }

        private void Start()
        {
            Subscribe();
            RefreshSlots();
        }

        private void Subscribe()
        {
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.SlotsChanged -= RefreshSlots;
                BuildingManager.Instance.SlotsChanged += RefreshSlots;
            }

            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.CoinsChanged -= OnCoinsChanged;
                PlayerWallet.Instance.CoinsChanged += OnCoinsChanged;
            }
        }

        private void OnDisable()
        {
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.SlotsChanged -= RefreshSlots;
            }

            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.CoinsChanged -= OnCoinsChanged;
            }

            WalletUI.Instance?.HideSellPreview();
        }

        public void RefreshSlots()
        {
            if (BuildingManager.Instance == null) return;

            int maxSlots = BuildingManager.Instance.MaxSlots;
            EnsureSlotCapacity(maxSlots);

            IReadOnlyList<BuildingSlotEntry> entries = BuildingManager.Instance.SlotEntries;

            for (int i = 0; i < _slotButtons.Length; i++)
            {
                if (_slotButtons[i] == null) continue;

                bool visible = i < maxSlots;
                _slotButtons[i].gameObject.SetActive(visible);
                if (!visible) continue;

                BuildingData data = (entries != null && i < entries.Count) ? entries[i].Data : null;
                int count = (entries != null && i < entries.Count) ? entries[i].Count : 0;

                _slotButtons[i].Setup(
                    i,
                    data,
                    count,
                    OnSlotLeftClicked,
                    OnSlotDoubleLeftClicked,
                    OnSlotRightClicked,
                    OnSlotPointerEnter,
                    OnSlotPointerExit
                );
            }
        }

        private void EnsureSlotCapacity(int targetCapacity)
        {
            if (_slotButtons == null || _slotButtons.Length == 0 || _slotButtons.Length >= targetCapacity) return;

            BuildingSlotButton template = _slotButtons[0];
            if (template == null) return;

            Transform parent = template.transform.parent;
            var list = new List<BuildingSlotButton>(_slotButtons);
            while (list.Count < targetCapacity)
            {
                BuildingSlotButton clone = Instantiate(template, parent);
                clone.name = $"Slot_{list.Count + 1}";
                list.Add(clone);
            }
            _slotButtons = list.ToArray();
        }

        private void OnSlotLeftClicked(int slotIndex)
        {
            BuildingManager.Instance?.SelectSlot(slotIndex);
        }

        private void OnSlotDoubleLeftClicked(int slotIndex)
        {
            WalletUI.Instance?.HideSellPreview();
            BuildingManager.Instance?.SellUnplacedBlueprint(slotIndex);
        }

        private void OnSlotRightClicked(int slotIndex)
        {
            WalletUI.Instance?.HideSellPreview();
            BuildingManager.Instance?.SellUnplacedBlueprint(slotIndex);
        }

        private void OnSlotPointerEnter(int slotIndex)
        {
            if (BuildingManager.Instance == null || WalletUI.Instance == null) return;
            int refund = BuildingManager.Instance.GetUnplacedSellRefund(slotIndex);
            if (refund > 0)
            {
                WalletUI.Instance.ShowSellPreview(refund);
            }
        }

        private void OnSlotPointerExit()
        {
            WalletUI.Instance?.HideSellPreview();
        }

        private void OnCoinsChanged(int currentCoins)
        {
            for (int i = 0; i < _slotButtons.Length; i++)
            {
                if (_slotButtons[i] != null && _slotButtons[i].gameObject.activeSelf)
                {
                    _slotButtons[i].UpdateCostDisplay();
                }
            }
        }
    }
}
