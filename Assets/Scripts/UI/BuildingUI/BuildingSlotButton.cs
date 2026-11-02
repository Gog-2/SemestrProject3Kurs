using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Alchemy.Inspector;
using Game.Buildings;
using Game.Core;

namespace Game.UI.BuildingUI
{
    public class BuildingSlotButton : MonoBehaviour, IPointerClickHandler
    {
        [Title("UI Elements")]
        [SerializeField] private TMP_Text _hotkeyText;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Image _background;

        [Title("Colors")]
        [SerializeField] private Color _availableColor = new(0.2f, 0.35f, 0.5f, 1f);
        [SerializeField] private Color _insufficientCoinsColor = new(0.4f, 0.2f, 0.2f, 0.8f);

        private int _slotIndex;
        private BuildingData _data;
        private Action<int> _onLeftClicked;
        private Action<int> _onRightClicked;

        public void InitializeReferences(
            Image background,
            TMP_Text hotkeyText,
            TMP_Text nameText,
            TMP_Text costText)
        {
            _background = background;
            _hotkeyText = hotkeyText;
            _nameText = nameText;
            _costText = costText;
        }

        public void Setup(
            int slotIndex,
            BuildingData data,
            Action<int> onLeftClicked,
            Action<int> onRightClicked)
        {
            _slotIndex = slotIndex;
            _data = data;
            _onLeftClicked = onLeftClicked;
            _onRightClicked = onRightClicked;

            if (_hotkeyText != null)
            {
                _hotkeyText.text = (slotIndex + 1).ToString();
            }

            if (_data == null)
            {
                if (_costText != null) _costText.text = "-";
                if (_nameText != null) _nameText.text = "Пусто";
                return;
            }

            if (_nameText != null) _nameText.text = _data.DisplayName;

            UpdateCostDisplay();
        }

        public void UpdateCostDisplay()
        {
            if (_data == null) return;

            int cost = _data.GetStats(1).cost;
            if (_costText != null)
            {
                _costText.text = $"{cost} м.";
            }

            bool hasCoins = PlayerWallet.Instance.Coins >= cost;
            if (_background != null)
            {
                _background.color = hasCoins ? _availableColor : _insufficientCoinsColor;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_data == null) return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _onLeftClicked?.Invoke(_slotIndex);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                _onRightClicked?.Invoke(_slotIndex);
            }
        }
    }
}
