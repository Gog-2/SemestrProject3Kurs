using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Alchemy.Inspector;
using Game.Core;

namespace Game.UI.MainMenu
{
    public class ShopUI : MonoBehaviour
    {
        [Title("UI Elements")]
        [SerializeField] private TMP_Text _diamondBalanceText;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _addTestDiamondsButton;

        private void Awake()
        {
            if (_addTestDiamondsButton != null)
            {
                _addTestDiamondsButton.onClick.AddListener(OnAddTestDiamondsClicked);
            }
        }

        private void OnEnable()
        {
            MetaSaveManager.DiamondsChanged += UpdateBalance;
            UpdateBalance();
        }

        private void OnDisable()
        {
            MetaSaveManager.DiamondsChanged -= UpdateBalance;
        }

        public void UpdateBalance()
        {
            if (_diamondBalanceText != null)
            {
                _diamondBalanceText.text = $"Баланс: {MetaSaveManager.Data.Diamonds} алмазов";
            }
        }

        private void OnAddTestDiamondsClicked()
        {
            MetaSaveManager.AddDiamonds(50);
        }

        [Button]
        public void Add50Diamonds()
        {
            MetaSaveManager.AddDiamonds(50);
        }
    }
}
