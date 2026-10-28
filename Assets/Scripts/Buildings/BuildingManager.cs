using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Alchemy.Inspector;
using Game.Core;
using Game.UI.BuildingUI;

namespace Game.Buildings
{
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

            if (_gridMap == null) _gridMap = FindAnyObjectByType<GridMap>();
            if (_wallet == null) _wallet = FindAnyObjectByType<PlayerWallet>();
            if (_mainCamera == null) _mainCamera = Camera.main;

            if (_ghostView == null)
            {
                _ghostView = FindAnyObjectByType<BuildingGhostView>();
                if (_ghostView == null)
                {
                    GameObject ghostGo = new GameObject("BuildingGhost", typeof(BuildingGhostView));
                    _ghostView = ghostGo.GetComponent<BuildingGhostView>();
                }
            }

            if (_contextMenuUI == null)
            {
                _contextMenuUI = FindAnyObjectByType<BuildingContextMenuUI>(FindObjectsInactive.Include);
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnGameStateChanged;
                GameManager.Instance.StateChanged += OnGameStateChanged;
            }
            SlotsChanged?.Invoke();
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnGameStateChanged;
                GameManager.Instance.StateChanged += OnGameStateChanged;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnGameStateChanged;
            }
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

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnGameStateChanged;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            // Do not process building interactions if game is paused or ended
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            HandleHotkeys();
            HandleMouseInteraction();
        }

        private void HandleHotkeys()
        {
            if (Keyboard.current == null) return;

            // Slots 1 to 5
            if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectSlot(0);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectSlot(1);
            else if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectSlot(2);
            else if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectSlot(3);
            else if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectSlot(4);

            // Context actions when a building is selected
            if (_currentMode == ActionMode.Selected && _selectedBuilding != null)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame)
                {
                    SellBuilding(_selectedBuilding);
                }
                else if (Keyboard.current.fKey.wasPressedThisFrame)
                {
                    StartMoving(_selectedBuilding);
                }
                else if (Keyboard.current.cKey.wasPressedThisFrame)
                {
                    UpgradeBuilding(_selectedBuilding);
                }
            }

            // Cancel action
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelAction();
            }
        }

        private void HandleMouseInteraction()
        {
            if (_mainCamera == null || _gridMap == null || Mouse.current == null) return;

            Vector3 mouseWorld = GetMouseWorldPosition();
            Vector3Int cellPos = _gridMap.WorldToCell(mouseWorld);
            Vector2Int hoverCell = new Vector2Int(cellPos.x, cellPos.y);

            // Update Ghost View
            if (_currentMode == ActionMode.Placing && _placingData != null)
            {
                bool canPlace = CanPlaceBuilding(_placingData, hoverCell, out _);
                _ghostView.UpdatePosition(hoverCell, canPlace);

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    TryExecutePlacement(hoverCell);
                }
                else if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    CancelAction();
                }
            }
            else if (_currentMode == ActionMode.Moving && _movingBuilding != null)
            {
                bool canPlace = _gridMap.CanPlace(hoverCell, _movingBuilding.Size);
                _ghostView.UpdatePosition(hoverCell, canPlace);

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    TryExecuteMove(hoverCell);
                }
                else if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    CancelAction();
                }
            }
            else if (_currentMode == ActionMode.None || _currentMode == ActionMode.Selected)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
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
                else if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    DeselectBuilding();
                }
            }
        }

        public void SelectSlot(int slotIndex)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (slotIndex < 0 || slotIndex >= _slots.Count) return;

            BuildingData data = _slots[slotIndex];
            if (data == null) return;

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

            int level = GetStartingLevel(data);
            cost = data.GetStats(level).cost;

            bool terrainValid = _gridMap.CanPlace(cell, data.Size);
            bool hasCoins = _wallet == null || _wallet.Coins >= cost;

            return terrainValid && hasCoins;
        }

        private void TryExecutePlacement(Vector2Int cell)
        {
            if (!CanPlaceBuilding(_placingData, cell, out int cost))
            {
                return;
            }

            if (_wallet != null && !_wallet.TrySpendCoins(cost))
            {
                return;
            }

            int startingLevel = GetStartingLevel(_placingData);

            Building buildingInstance;
            if (_placingData.Prefab != null)
            {
                buildingInstance = Instantiate(_placingData.Prefab);
            }
            else
            {
                // Fallback runtime building creation if prefab not yet assigned
                GameObject go = new GameObject(_placingData.DisplayName);
                int bLayer = LayerMask.NameToLayer("Building");
                if (bLayer != -1) go.layer = bLayer;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = "Buildings";
                sr.sortingOrder = 5;

                if (_placingData.Icon != null)
                {
                    sr.sprite = _placingData.Icon;
                }

                if (_placingData.Category == BuildingCategory.Control)
                {
                    buildingInstance = go.AddComponent<WallBuilding>();
                }
                else if (_placingData.Category == BuildingCategory.Siege)
                {
                    buildingInstance = go.AddComponent<TurretBuilding>();
                }
                else if (_placingData.Category == BuildingCategory.Economy)
                {
                    buildingInstance = go.AddComponent<EconomyBuilding>();
                }
                else
                {
                    buildingInstance = go.AddComponent<Building>();
                }
            }

            buildingInstance.Initialize(_placingData, _gridMap, cell, startingLevel);
            _gridMap.TryPlace(buildingInstance);

            // If Shift is not held, exit placement mode
            if (Keyboard.current == null || !Keyboard.current.shiftKey.isPressed)
            {
                CancelAction();
            }
        }

        public void StartMoving(Building building)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (building == null) return;

            _movingBuilding = building;
            _movingOriginalCell = building.Origin;

            _gridMap.Remove(_movingBuilding);

            SetMode(ActionMode.Moving);
            _ghostView.Show(_movingBuilding.Data, _gridMap);
        }

        private void TryExecuteMove(Vector2Int cell)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                CancelAction();
                return;
            }

            if (_movingBuilding == null) return;

            if (!_gridMap.CanPlace(cell, _movingBuilding.Size))
            {
                return;
            }

            _movingBuilding.SetOrigin(cell);
            _gridMap.TryPlace(_movingBuilding);

            _movingBuilding = null;
            CancelAction();
        }

        public void SelectBuilding(Building building)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (_selectedBuilding == building) return;

            _selectedBuilding = building;
            SetMode(ActionMode.Selected);
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
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (building == null) return;

            BuildingData data = building.Data;
            int refund = 0;

            if (data != null)
            {
                int totalCost = 0;
                for (int lvl = 1; lvl <= building.CurrentLevel; lvl++)
                {
                    totalCost += data.GetStats(lvl).cost;
                }
                refund = Mathf.RoundToInt(totalCost * data.PlacedSaleRefundRatio);
            }

            if (refund > 0 && _wallet != null)
            {
                _wallet.AddCoins(refund);
            }

            DeselectBuilding();
            building.DestroyBuilding();
        }

        public void UpgradeBuilding(Building building)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (building == null) return;

            if (!building.CanUpgrade(out int cost)) return;

            if (_wallet != null && !_wallet.TrySpendCoins(cost))
            {
                Debug.Log("[BuildingManager] Недостаточно монет для улучшения!");
                return;
            }

            building.Upgrade();

            // Global upgrade handling for Siege and Control
            if (building.Data != null && building.Data.UpgradeType == BuildingUpgradeType.Global)
            {
                string id = building.Data.Id;
                _globalLevels[id] = building.CurrentLevel;

                // Sync all existing buildings of this type
                var allBuildings = FindObjectsByType<Building>();
                for (int i = 0; i < allBuildings.Length; i++)
                {
                    if (allBuildings[i] != building && allBuildings[i].Data != null && allBuildings[i].Data.Id == id)
                    {
                        allBuildings[i].SetLevel(building.CurrentLevel);
                    }
                }
            }

            SelectedBuildingChanged?.Invoke(_selectedBuilding);
        }

        public void SellUnplacedBlueprint(int slotIndex)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (slotIndex < 0 || slotIndex >= _slots.Count) return;

            BuildingData data = _slots[slotIndex];
            if (data == null) return;

            int refund = Mathf.RoundToInt(data.GetStats(1).cost * data.UnplacedSaleRefundRatio);
            if (refund > 0 && _wallet != null)
            {
                _wallet.AddCoins(refund);
            }

            _slots.RemoveAt(slotIndex);
            SlotsChanged?.Invoke();
        }

        public void AddSlot(BuildingData data)
        {
            if (data == null || _slots.Count >= _maxSlots) return;
            _slots.Add(data);
            SlotsChanged?.Invoke();
        }

        public void CancelAction()
        {
            if (_currentMode == ActionMode.Moving && _movingBuilding != null)
            {
                // Restore to original cell
                _movingBuilding.SetOrigin(_movingOriginalCell);
                _gridMap.TryPlace(_movingBuilding);
                _movingBuilding = null;
            }

            _placingData = null;
            _ghostView.Hide();
            DeselectBuilding();
            SetMode(ActionMode.None);
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
            if (_mainCamera == null || Mouse.current == null) return Vector3.zero;

            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -_mainCamera.transform.position.z));
            worldPos.z = 0f;
            return worldPos;
        }
    }
}
