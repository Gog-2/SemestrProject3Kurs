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
    public class BuildingSlotButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const float DoubleClickMaxInterval = 0.38f;

        [Title("UI Elements")]
        [SerializeField] private TMP_Text _hotkeyText;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private Image _background;
        [SerializeField] private Image _iconImage;

        [Title("Colors")]
        [SerializeField] private Color _availableColor = new(0.2f, 0.35f, 0.5f, 1f);
        [SerializeField] private Color _insufficientCoinsColor = new(0.4f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color _emptyColor = new(0.14f, 0.16f, 0.2f, 0.6f);

        private int _slotIndex;
        private BuildingData _data;
        private int _count;
        private Action<int> _onLeftClicked;
        private Action<int> _onDoubleLeftClicked;
        private Action<int> _onRightClicked;
        private Action<int> _onPointerEnter;
        private Action _onPointerExit;
        private float _lastLeftClickTime = -10f;

        private void Awake()
        {
            Debug.Assert(_hotkeyText != null, "[BuildingSlotButton] _hotkeyText is not assigned!", this);
            Debug.Assert(_costText != null, "[BuildingSlotButton] _costText is not assigned!", this);
            Debug.Assert(_nameText != null, "[BuildingSlotButton] _nameText is not assigned!", this);
            Debug.Assert(_background != null, "[BuildingSlotButton] _background is not assigned!", this);
            EnsureCountText();
        }

        public void InitializeReferences(
            Image background,
            TMP_Text hotkeyText,
            TMP_Text nameText,
            TMP_Text costText,
            TMP_Text countText = null,
            Image iconImage = null)
        {
            _background = background;
            _hotkeyText = hotkeyText;
            _nameText = nameText;
            _costText = costText;
            _countText = countText;
            _iconImage = iconImage;
            EnsureCountText();
        }

        private void EnsureCountText()
        {
            if (_countText != null) return;
            Transform existing = transform.Find("Count");
            if (existing != null)
            {
                _countText = existing.GetComponent<TMP_Text>();
                if (_countText != null) return;
            }

            var go = new GameObject("Count", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-4f, -4f);
            rt.sizeDelta = new Vector2(44f, 22f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.TopRight;
            tmp.fontSize = 15f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(1f, 0.9f, 0.35f, 1f);
            if (_costText != null && _costText.font != null)
            {
                tmp.font = _costText.font;
            }
            _countText = tmp;
        }

        public void Setup(
            int slotIndex,
            BuildingData data,
            Action<int> onLeftClicked,
            Action<int> onRightClicked)
        {
            Setup(slotIndex, data, data != null ? 1 : 0, onLeftClicked, onRightClicked, onRightClicked, null, null);
        }

        public void Setup(
            int slotIndex,
            BuildingData data,
            int count,
            Action<int> onLeftClicked,
            Action<int> onDoubleLeftClicked,
            Action<int> onRightClicked,
            Action<int> onPointerEnter,
            Action onPointerExit)
        {
            _slotIndex = slotIndex;
            _data = data;
            _count = count;
            _onLeftClicked = onLeftClicked;
            _onDoubleLeftClicked = onDoubleLeftClicked;
            _onRightClicked = onRightClicked;
            _onPointerEnter = onPointerEnter;
            _onPointerExit = onPointerExit;

            _hotkeyText.text = (slotIndex + 1).ToString();
            EnsureCountText();

            if (_data == null || _count <= 0)
            {
                _costText.text = "-";
                _nameText.text = "Пусто";
                if (_countText != null) _countText.text = string.Empty;
                if (_iconImage != null)
                {
                    _iconImage.sprite = null;
                    _iconImage.enabled = false;
                }
                if (_background != null) _background.color = _emptyColor;
                return;
            }

            _nameText.text = _data.DisplayName;
            if (_countText != null)
            {
                _countText.text = $"x{_count}";
            }

            if (_iconImage != null)
            {
                _iconImage.sprite = _data.Icon;
                _iconImage.enabled = _data.Icon != null;
            }

            UpdateCostDisplay();
        }

        public void UpdateCostDisplay()
        {
            if (_data == null || _count <= 0) return;

            int cost = _data.GetStats(1).cost;
            _costText.text = $"{cost} м.";

            var wallet = PlayerWallet.Instance;
            bool hasCoins = wallet != null && wallet.Coins >= cost;
            if (_background != null)
            {
                _background.color = hasCoins ? _availableColor : _insufficientCoinsColor;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_data == null || _count <= 0) return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                float now = Time.unscaledTime;
                bool isDoubleClick = eventData.clickCount >= 2 || (now - _lastLeftClickTime <= DoubleClickMaxInterval);
                if (isDoubleClick)
                {
                    _lastLeftClickTime = -10f;
                    _onDoubleLeftClicked?.Invoke(_slotIndex);
                    return;
                }

                _lastLeftClickTime = now;
                _onLeftClicked?.Invoke(_slotIndex);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                _onRightClicked?.Invoke(_slotIndex);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_data != null && _count > 0)
            {
                _onPointerEnter?.Invoke(_slotIndex);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _onPointerExit?.Invoke();
        }
    }
}
