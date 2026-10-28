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

        private void Start()
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

            RefreshSlots();
        }

        private void OnEnable()
        {
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.SlotsChanged += RefreshSlots;
            }

            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.CoinsChanged += OnCoinsChanged;
            }

            RefreshSlots();
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
        }

        public void RefreshSlots()
        {
            if (BuildingManager.Instance == null) return;

            IReadOnlyList<BuildingData> slots = BuildingManager.Instance.Slots;

            for (int i = 0; i < _slotButtons.Length; i++)
            {
                if (_slotButtons[i] == null) continue;

                BuildingData data = (slots != null && i < slots.Count) ? slots[i] : null;

                _slotButtons[i].Setup(
                    i,
                    data,
                    OnSlotLeftClicked,
                    OnSlotRightClicked
                );
            }
        }

        private void OnSlotLeftClicked(int slotIndex)
        {
            BuildingManager.Instance?.SelectSlot(slotIndex);
        }

        private void OnSlotRightClicked(int slotIndex)
        {
            BuildingManager.Instance?.SellUnplacedBlueprint(slotIndex);
        }

        private void OnCoinsChanged(int currentCoins)
        {
            for (int i = 0; i < _slotButtons.Length; i++)
            {
                if (_slotButtons[i] != null)
                {
                    _slotButtons[i].UpdateCostDisplay();
                }
            }
        }
    }
}
