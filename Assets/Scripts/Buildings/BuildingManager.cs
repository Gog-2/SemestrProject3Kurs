using System;
using System.Collections.Generic;
using UnityEngine;
using Alchemy.Inspector;
using Game.Core;
using Game.UI.BuildingUI;

namespace Game.Buildings
{
    [DefaultExecutionOrder(-50)]
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance { get; private set; }

        public enum ActionMode
        {
            None,
            Placing,
            Moving,
            Selected
        }

        [Title("References")]
        [SerializeField] private GridMap _gridMap;
        [SerializeField] private BuildingGhostView _ghostView;
        [SerializeField] private PlayerWallet _wallet;
        [SerializeField] private Camera _mainCamera;

        [Title("UI Connections")]
        [SerializeField] private BuildingContextMenuUI _contextMenuUI;

        [Title("Available Slots (1 to 5)")]
        [SerializeField] private List<BuildingData> _slots = new();
        [SerializeField, Min(1)] private int _maxSlots = 5;

        [Title("Debug State")]
        [ShowInInspector, ReadOnly] private ActionMode _currentMode = ActionMode.None;
        [ShowInInspector, ReadOnly] private BuildingData _placingData;
        [ShowInInspector, ReadOnly] private Building _movingBuilding;
        [ShowInInspector, ReadOnly] private Building _selectedBuilding;
        private Vector2Int _movingOriginalCell;

        private readonly Dictionary<string, int> _globalLevels = new();

        public ActionMode CurrentMode => _currentMode;
        public IReadOnlyList<BuildingData> Slots => _slots;
        public Building SelectedBuilding => _selectedBuilding;

        public event Action<ActionMode> ModeChanged;
        public event Action<Building> SelectedBuildingChanged;
        public event Action SlotsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Debug.Assert(_gridMap != null, "[BuildingManager] _gridMap is not assigned!", this);
            Debug.Assert(_ghostView != null, "[BuildingManager] _ghostView is not assigned!", this);
            Debug.Assert(_wallet != null, "[BuildingManager] _wallet is not assigned!", this);
            Debug.Assert(_contextMenuUI != null, "[BuildingManager] _contextMenuUI is not assigned!", this);
        }

        private void Start()
        {
            SubscribeToGameState();
            RegisterInput();
            SlotsChanged?.Invoke();
        }

        private void OnEnable()
        {
            SubscribeToGameState();
            RegisterInput();
        }

        private void SubscribeToGameState()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.StateChanged -= OnGameStateChanged;
            GameManager.Instance.StateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnGameStateChanged;
            }

            UnregisterInput();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnGameStateChanged;
            }

            UnregisterInput();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void RegisterInput()
        {
            UnregisterInput();

            if (InputManager.Instance == null) return;

            InputManager.Instance.PrimaryClicked += OnPrimaryClicked;
            InputManager.Instance.SecondaryClicked += OnSecondaryClicked;
            InputManager.Instance.CancelPressed += OnCancelPressed;
            InputManager.Instance.SlotSelected += SelectSlot;
            InputManager.Instance.SellPressed += OnSellPressed;
            InputManager.Instance.MoveBuildingPressed += OnMoveBuildingPressed;
            InputManager.Instance.UpgradePressed += OnUpgradePressed;
        }

        private void UnregisterInput()
        {
            if (InputManager.Instance == null) return;

            InputManager.Instance.PrimaryClicked -= OnPrimaryClicked;
            InputManager.Instance.SecondaryClicked -= OnSecondaryClicked;
            InputManager.Instance.CancelPressed -= OnCancelPressed;
            InputManager.Instance.SlotSelected -= SelectSlot;
            InputManager.Instance.SellPressed -= OnSellPressed;
            InputManager.Instance.MoveBuildingPressed -= OnMoveBuildingPressed;
            InputManager.Instance.UpgradePressed -= OnUpgradePressed;
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state != GameState.Playing)
            {
                CancelAction();
            }
        }

        public void SetSlots(IEnumerable<BuildingData> slots)
        {
            _slots.Clear();
            if (slots != null)
            {
                _slots.AddRange(slots);
            }
            SlotsChanged?.Invoke();
        }

        private void Update()
        {
            // Do not process building interactions if game is paused or ended
            if (GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            UpdateGhostHover();
        }

        private void UpdateGhostHover()
        {
            if (_currentMode != ActionMode.Placing && _currentMode != ActionMode.Moving) return;

            Vector3 pointerWorld = InputManager.Instance.GetPointerWorldPosition(_mainCamera);
            Vector3Int cellPos = _gridMap.WorldToCell(pointerWorld);
            Vector2Int hoverCell = new Vector2Int(cellPos.x, cellPos.y);

            if (_currentMode == ActionMode.Placing && _placingData != null)
            {
                bool canPlace = CanPlaceBuilding(_placingData, hoverCell, out _);
                _ghostView.UpdatePosition(hoverCell, canPlace);
            }
            else if (_currentMode == ActionMode.Moving && _movingBuilding != null)
            {
                bool canPlace = _gridMap.CanPlace(hoverCell, _movingBuilding.Size);
                _ghostView.UpdatePosition(hoverCell, canPlace);
            }
        }

        private void OnPrimaryClicked(Vector2 screenPosition)
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;

            if (InputManager.Instance.IsPointerOverUI(screenPosition)) return;

            Vector3 pointerWorld = InputManager.Instance.GetPointerWorldPosition(_mainCamera);
            Vector3Int cellPos = _gridMap.WorldToCell(pointerWorld);
            Vector2Int hoverCell = new Vector2Int(cellPos.x, cellPos.y);

            if (_currentMode == ActionMode.Placing && _placingData != null)
            {
                TryExecutePlacement(hoverCell);
            }
            else if (_currentMode == ActionMode.Moving && _movingBuilding != null)
            {
                TryExecuteMove(hoverCell);
            }
            else if (_currentMode == ActionMode.None || _currentMode == ActionMode.Selected)
            {
                IBuilding occupant = _gridMap.GetOccupant(hoverCell.x, hoverCell.y);
                if (occupant is Building building && !(occupant is Castle))
                {
                    SelectBuilding(building);
                }
                else if (occupant == null)
                {
                    DeselectBuilding();
                }
            }
        }

        private void OnSecondaryClicked()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;

            if (_currentMode == ActionMode.Placing || _currentMode == ActionMode.Moving)
            {
                CancelAction();
            }
            else if (_currentMode == ActionMode.Selected)
            {
                DeselectBuilding();
            }
        }

        private void OnCancelPressed()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;
            CancelAction();
        }

        private void OnSellPressed()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;
            if (_currentMode == ActionMode.Selected && _selectedBuilding != null)
            {
                SellBuilding(_selectedBuilding);
            }
        }

        private void OnMoveBuildingPressed()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;
            if (_currentMode == ActionMode.Selected && _selectedBuilding != null)
            {
                StartMoving(_selectedBuilding);
            }
        }

        private void OnUpgradePressed()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;
            if (_currentMode == ActionMode.Selected && _selectedBuilding != null)
            {
                UpgradeBuilding(_selectedBuilding);
            }
        }

        public void SelectSlot(int slotIndex)
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;
            if (slotIndex < 0 || slotIndex >= _slots.Count) return;

            BuildingData data = _slots[slotIndex];
            if (data == null) return;

            // Toggle off if already placing this building
            if (_currentMode == ActionMode.Placing && _placingData == data)
            {
                CancelAction();
                return;
            }

            StartPlacing(data);
        }

        public void StartPlacing(BuildingData data)
        {
            if (data == null) return;

            DeselectBuilding();

            _placingData = data;
            SetMode(ActionMode.Placing);
            _ghostView.Show(_placingData, _gridMap);
        }

        private bool CanPlaceBuilding(BuildingData data, Vector2Int cell, out int cost)
        {
            cost = 0;
            if (data == null || _gridMap == null) return false;

            cost = data.GetStats(1).cost;

            if (_wallet != null && _wallet.Coins < cost)
            {
                return false;
            }

            return _gridMap.CanPlace(cell, data.Size);
        }

        private void TryExecutePlacement(Vector2Int cell)
        {
            if (!CanPlaceBuilding(_placingData, cell, out int cost))
            {
                return;
            }

            if (_placingData.Prefab == null)
            {
                Debug.LogError($"BuildingData '{_placingData.name}' has no prefab assigned.", _placingData);
                return;
            }

            _wallet.TrySpendCoins(cost);

            int startingLevel = GetStartingLevel(_placingData);

            Building buildingInstance = Instantiate(_placingData.Prefab);
            buildingInstance.Initialize(_placingData, _gridMap, cell, startingLevel);
            _gridMap.TryPlace(buildingInstance);

            // If Shift is not held, exit placement mode
            bool isShift = InputManager.Instance.IsShiftHeld;
            if (!isShift)
            {
                CancelAction();
            }
        }

        public void StartMoving(Building building)
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;
            if (building == null) return;

            _movingBuilding = building;
            _movingOriginalCell = building.Origin;
            SetMode(ActionMode.Moving);

            _gridMap.Remove(_movingBuilding);
            _movingBuilding.gameObject.SetActive(false);

            _ghostView.Show(_movingBuilding.Data, _gridMap);
            _contextMenuUI?.OnBuildingSelected(null);
            SelectedBuildingChanged?.Invoke(null);
        }

        private void TryExecuteMove(Vector2Int cell)
        {
            if (_movingBuilding == null) return;

            if (!_gridMap.CanPlace(cell, _movingBuilding.Size))
            {
                return;
            }

            _movingBuilding.gameObject.SetActive(true);
            _movingBuilding.SetOrigin(cell);
            _gridMap.TryPlace(_movingBuilding);

            _movingBuilding = null;
            CancelAction();
        }

        public void SelectBuilding(Building building)
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;
            if (_selectedBuilding == building) return;

            _selectedBuilding = building;
            SetMode(_selectedBuilding != null ? ActionMode.Selected : ActionMode.None);
            _contextMenuUI?.OnBuildingSelected(_selectedBuilding);
            SelectedBuildingChanged?.Invoke(_selectedBuilding);
        }

        public void DeselectBuilding()
        {
            if (_selectedBuilding != null)
            {
                _selectedBuilding = null;
                SetMode(ActionMode.None);
                _contextMenuUI?.OnBuildingSelected(null);
                SelectedBuildingChanged?.Invoke(null);
            }
        }

        public void SellBuilding(Building building)
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;
            if (building == null) return;

            BuildingData data = building.Data;
            int totalCost = 0;
            if (data != null)
            {
                totalCost = data.GetStats(1).cost;
                // Add upgrade costs if applicable
                for (int lvl = 1; lvl <= building.CurrentLevel; lvl++)
                {
                    BuildingLevelStats stats = data.GetStats(lvl);
                    totalCost += stats.cost;
                }
            }

            int refund = Mathf.RoundToInt(totalCost * 0.5f);
            if (refund > 0)
            {
                _wallet.AddCoins(refund);
            }

            DeselectBuilding();
            building.DestroyBuilding();
        }

        public void SellUnplacedBlueprint(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count) return;
            BuildingData data = _slots[slotIndex];
            if (data == null) return;

            int refund = Mathf.RoundToInt(data.GetStats(1).cost * data.UnplacedSaleRefundRatio);
            if (refund > 0)
            {
                _wallet.AddCoins(refund);
            }
            _slots.RemoveAt(slotIndex);
            SlotsChanged?.Invoke();
        }

        public void UpgradeBuilding(Building building)
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;
            if (building == null) return;

            if (!building.CanUpgrade(out int cost)) return;

            if (!_wallet.TrySpendCoins(cost))
            {
                Debug.Log("[BuildingManager] Недостаточно монет для улучшения!");
                return;
            }

            building.Upgrade();

            // Handle global upgrade sync
            if (building.Data != null && building.Data.UpgradeType == BuildingUpgradeType.Global)
            {
                string id = building.Data.Id;
                _globalLevels[id] = building.CurrentLevel;
            }

            // Refresh menu display
            _contextMenuUI?.OnBuildingSelected(_selectedBuilding);
            SelectedBuildingChanged?.Invoke(_selectedBuilding);
        }

        public void UpgradeSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count) return;

            BuildingData data = _slots[slotIndex];
            if (data == null || data.UpgradeType != BuildingUpgradeType.Global) return;

            int currentLvl = GetStartingLevel(data);
            if (currentLvl >= 4) return;
            BuildingLevelStats nextStats = data.GetStats(currentLvl + 1);

            if (!_wallet.TrySpendCoins(nextStats.cost)) return;

            _globalLevels[data.Id] = currentLvl + 1;
        }

        public void AddSlot(BuildingData data)
        {
            if (data == null) return;
            if (_slots.Count >= _maxSlots) return;

            _slots.Add(data);
            SlotsChanged?.Invoke();
        }

        public void CancelAction()
        {
            if (_currentMode == ActionMode.Moving && _movingBuilding != null)
            {
                _movingBuilding.gameObject.SetActive(true);
                _movingBuilding.SetOrigin(_movingOriginalCell);
                _gridMap.TryPlace(_movingBuilding);
                _movingBuilding = null;
            }

            _placingData = null;
            _ghostView.Hide();
            SetMode(ActionMode.None);
            DeselectBuilding();
        }

        private int GetStartingLevel(BuildingData data)
        {
            if (data != null && data.UpgradeType == BuildingUpgradeType.Global && _globalLevels.TryGetValue(data.Id, out int lvl))
            {
                return lvl;
            }
            return 1;
        }

        private void SetMode(ActionMode newMode)
        {
            if (_currentMode == newMode) return;
            _currentMode = newMode;
            ModeChanged?.Invoke(_currentMode);
        }

        private Vector3 GetMouseWorldPosition()
        {
            return InputManager.Instance.GetPointerWorldPosition(_mainCamera);
        }
    }
}
