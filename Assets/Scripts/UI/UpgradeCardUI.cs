using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Alchemy.Inspector;
using Game.Upgrades;

namespace Game.UI
{
    public class UpgradeCardUI : MonoBehaviour
    {
        [Title("UI Elements")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private TMP_Text _rarityText;
        [SerializeField] private Image _rarityBackground;
        [SerializeField] private Button _selectButton;

        [Title("Visuals")]
        [SerializeField] private Color _commonColor = new(0.8f, 0.8f, 0.8f);
        [SerializeField] private Color _rareColor = new(0.2f, 0.6f, 1f);
        [SerializeField] private Color _epicColor = new(0.7f, 0.2f, 0.9f);
        [SerializeField] private Color _legendaryColor = new(1f, 0.8f, 0.1f);

        private UpgradeData _data;
        private Action<UpgradeData> _onSelectedCallback;

        private void Awake()
        {
            Debug.Assert(_selectButton != null, "[UpgradeCardUI] _selectButton is not assigned!", this);
            Debug.Assert(_titleText != null, "[UpgradeCardUI] _titleText is not assigned!", this);
            Debug.Assert(_descriptionText != null, "[UpgradeCardUI] _descriptionText is not assigned!", this);
            Debug.Assert(_rarityText != null, "[UpgradeCardUI] _rarityText is not assigned!", this);
            Debug.Assert(_rarityBackground != null, "[UpgradeCardUI] _rarityBackground is not assigned!", this);

            _selectButton.onClick.AddListener(OnButtonClicked);
        }

        public void Setup(UpgradeData data, Action<UpgradeData> onSelected)
        {
            _data = data;
            _onSelectedCallback = onSelected;

            if (_data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            _titleText.text = _data.Title;
            _descriptionText.text = _data.Description;

            Color rarityColor = GetRarityColor(_data.Rarity);

            _rarityText.text = _data.Rarity.ToString().ToUpper();
            _rarityText.color = rarityColor;
            _rarityBackground.color = rarityColor;
        }

        private Color GetRarityColor(UpgradeRarity rarity)
        {
            return rarity switch
            {
                UpgradeRarity.Rare => _rareColor,
                UpgradeRarity.Epic => _epicColor,
                UpgradeRarity.Legendary => _legendaryColor,
                _ => _commonColor,
            };
        }

        private void OnButtonClicked()
        {
            if (_data == null) return;

            transform.DOPunchScale(Vector3.one * 0.1f, 0.15f, 10, 1f)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _onSelectedCallback?.Invoke(_data);
                });
        }
    }
}
