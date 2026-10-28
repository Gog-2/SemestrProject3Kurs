using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Alchemy.Inspector;
using Game.Buildings;

namespace Game.UI.BuildingUI
{
    public class BuildingContextMenuUI : MonoBehaviour
    {
        [Title("UI Elements")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _menuRect;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _hpText;
        [SerializeField] private TMP_Text _combatXpText;

        [Title("Buttons")]
        [SerializeField] private Button _destroyButton;
        [SerializeField] private TMP_Text _destroyButtonText;
        [SerializeField] private Button _moveButton;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private TMP_Text _upgradeButtonText;

        [Title("Screen Positioning")]
        [SerializeField] private Vector2 _screenOffset = new(0, 80);

        private Camera _mainCamera;
        private Building _currentBuilding;

        public void InitializeReferences(
            GameObject panelRoot,
            RectTransform menuRect,
            TMP_Text titleText,
            TMP_Text hpText,
            TMP_Text combatXpText,
            Button upgradeButton,
            TMP_Text upgradeButtonText,
            Button moveButton,
            Button destroyButton,
            TMP_Text destroyButtonText)
        {
            _panelRoot = panelRoot;
            _menuRect = menuRect;
            _titleText = titleText;
            _hpText = hpText;
            _combatXpText = combatXpText;
            _upgradeButton = upgradeButton;
            _upgradeButtonText = upgradeButtonText;
            _moveButton = moveButton;
            _destroyButton = destroyButton;
            _destroyButtonText = destroyButtonText;

            if (_destroyButton != null)
            {
                _destroyButton.onClick.RemoveListener(OnDestroyClicked);
                _destroyButton.onClick.AddListener(OnDestroyClicked);
            }
            if (_moveButton != null)
            {
                _moveButton.onClick.RemoveListener(OnMoveClicked);
                _moveButton.onClick.AddListener(OnMoveClicked);
            }
            if (_upgradeButton != null)
            {
                _upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
                _upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }

            if (_panelRoot != null) _panelRoot.SetActive(false);
        }

        private void Awake()
        {
            _mainCamera = Camera.main;

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            else if (_panelRoot != null && _panelRoot != gameObject)
            {
                _panelRoot.SetActive(false);
            }

            if (_destroyButton != null)
            {
                _destroyButton.onClick.RemoveListener(OnDestroyClicked);
                _destroyButton.onClick.AddListener(OnDestroyClicked);
            }
            if (_moveButton != null)
            {
                _moveButton.onClick.RemoveListener(OnMoveClicked);
                _moveButton.onClick.AddListener(OnMoveClicked);
            }
            if (_upgradeButton != null)
            {
                _upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
                _upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }
        }

        private void Start()
        {
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.SelectedBuildingChanged -= OnBuildingSelected;
                BuildingManager.Instance.SelectedBuildingChanged += OnBuildingSelected;
            }
        }

        private void OnEnable()
        {
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.SelectedBuildingChanged -= OnBuildingSelected;
                BuildingManager.Instance.SelectedBuildingChanged += OnBuildingSelected;
            }
        }

        private void OnDisable()
        {
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.SelectedBuildingChanged -= OnBuildingSelected;
            }
        }

        private void LateUpdate()
        {
            if (_currentBuilding == null) return;
            if (_canvasGroup != null && _canvasGroup.alpha <= 0.01f) return;
            if (_panelRoot != null && !_panelRoot.activeSelf) return;

            UpdateScreenPosition();
        }

        public void UpdateScreenPosition()
        {
            if (_currentBuilding == null) return;

            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            Vector3 screenPos = _mainCamera.WorldToScreenPoint(_currentBuilding.transform.position);
            screenPos += (Vector3)_screenOffset;

            // Clamp so menu doesn't go off-screen
            float halfWidth = _menuRect != null ? _menuRect.sizeDelta.x * 0.5f : 120f;
            float halfHeight = _menuRect != null ? _menuRect.sizeDelta.y * 0.5f : 75f;
            screenPos.x = Mathf.Clamp(screenPos.x, halfWidth + 10f, Screen.width - halfWidth - 10f);
            screenPos.y = Mathf.Clamp(screenPos.y, halfHeight + 10f, Screen.height - halfHeight - 10f);

            if (_menuRect != null)
            {
                _menuRect.position = screenPos;
            }
        }

        public void OnBuildingSelected(Building building)
        {
            _currentBuilding = building;

            if (_currentBuilding == null)
            {
                Hide();
                return;
            }

            RefreshInfo();
            Show();
        }

        public void Show()
        {
            if (_panelRoot != null && _panelRoot != gameObject)
            {
                _panelRoot.SetActive(true);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
            else if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }

            UpdateScreenPosition();
        }

        public void Hide()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            else if (_panelRoot != null && _panelRoot != gameObject)
            {
                _panelRoot.SetActive(false);
            }
        }

        private void RefreshInfo()
        {
            if (_currentBuilding == null) return;

            string name = _currentBuilding.Data != null ? _currentBuilding.Data.DisplayName : "Постройка";
            if (_titleText != null)
            {
                _titleText.text = $"{name} (Ур. {_currentBuilding.CurrentLevel})";
            }

            if (_hpText != null)
            {
                _hpText.text = $"HP: {_currentBuilding.CurrentHP}/{_currentBuilding.MaxHP}";
            }

            if (_combatXpText != null)
            {
                _combatXpText.text = $"Боевой опыт: {_currentBuilding.CombatXP}";
            }

            // Destroy refund (50%)
            if (_destroyButtonText != null)
            {
                int refund = 0;
                if (_currentBuilding.Data != null)
                {
                    int totalCost = 0;
                    for (int i = 1; i <= _currentBuilding.CurrentLevel; i++)
                    {
                        totalCost += _currentBuilding.Data.GetStats(i).cost;
                    }
                    refund = Mathf.RoundToInt(totalCost * _currentBuilding.Data.PlacedSaleRefundRatio);
                }
                _destroyButtonText.text = $"Снести [Q] (+{refund} м.)";
            }

            // Upgrade info
            if (_upgradeButton != null && _upgradeButtonText != null)
            {
                if (_currentBuilding.CanUpgrade(out int upgradeCost))
                {
                    _upgradeButton.interactable = true;
                    _upgradeButtonText.text = $"Улучшить [C] ({upgradeCost} м.)";
                }
                else
                {
                    _upgradeButton.interactable = false;
                    _upgradeButtonText.text = "МАКС. УРОВЕНЬ";
                }
            }
        }

        private void OnDestroyClicked()
        {
            if (_currentBuilding != null && BuildingManager.Instance != null)
            {
                BuildingManager.Instance.SellBuilding(_currentBuilding);
            }
        }

        private void OnMoveClicked()
        {
            if (_currentBuilding != null && BuildingManager.Instance != null)
            {
                BuildingManager.Instance.StartMoving(_currentBuilding);
            }
        }

        private void OnUpgradeClicked()
        {
            if (_currentBuilding != null && BuildingManager.Instance != null)
            {
                BuildingManager.Instance.UpgradeBuilding(_currentBuilding);
                RefreshInfo();
            }
        }
    }
}
