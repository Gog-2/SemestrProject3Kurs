using System;
using System.Collections.Generic;
using UnityEngine;
using Alchemy.Inspector;
using Game.Core;
using Game.Data;
using Game.UI.BuildingUI;
using Game.WaveSystem;

namespace Game.Buildings
{
    [Serializable]
    public struct BuildingSlotEntry
    {
        public BuildingData Data;
        public int Count;

        public BuildingSlotEntry(BuildingData data, int count)
        {
            Data = data;
            Count = count;
        }
    }

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

        [Title("Available Slots")]
        [SerializeField] private List<BuildingData> _slots = new();
        [SerializeField, Min(1)] private int _maxSlots = 5;

        [Title("Debug State")]
        [ShowInInspector, ReadOnly] private ActionMode _currentMode = ActionMode.None;
        [ShowInInspector, ReadOnly] private BuildingData _placingData;
        [ShowInInspector, ReadOnly] private Building _movingBuilding;
        [ShowInInspector, ReadOnly] private Building _selectedBuilding;
        private Vector2Int _movingOriginalCell;

        private readonly List<BuildingSlotEntry> _slotEntries = new();
        private readonly Dictionary<string, int> _globalLevels = new();
        private Func<BuildingData, Vector2Int, bool> _extraPlacementValidator;

        public ActionMode CurrentMode => _currentMode;
        public IReadOnlyList<BuildingData> Slots => _slots;
        public IReadOnlyList<BuildingSlotEntry> SlotEntries => _slotEntries;
        public int MaxSlots => GetMaxSlotsLimit();
        public Building SelectedBuilding => _selectedBuilding;
        public bool CanManagePlacedBuildings => IsBuildingManagementAllowed();
        private PlayerWallet Wallet => _wallet != null ? _wallet : PlayerWallet.Instance;

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
            if (_ghostView == null) _ghostView = FindAnyObjectByType<BuildingGhostView>();
            if (_wallet == null) _wallet = PlayerWallet.Instance ?? FindAnyObjectByType<PlayerWallet>();
            if (_contextMenuUI == null) _contextMenuUI = FindAnyObjectByType<BuildingContextMenuUI>();
            if (_mainCamera == null) _mainCamera = Camera.main;

            InitializeStartingSlots();
        }

        private void InitializeStartingSlots()
        {
            KnightData knight = RunConfig.Instance != null ? RunConfig.Instance.SelectedKnight : null;
            if (knight != null && knight.StartingBuildingAssets != null && knight.StartingBuildingAssets.Length > 0)
            {
                _slotEntries.Clear();
                for (int i = 0; i < knight.StartingBuildingAssets.Length; i++)
                {
                    BuildingData bd = knight.StartingBuildingAssets[i];
                    if (bd != null)
                    {
                        TryAddBlueprintInternal(bd, 1, notify: false);
                    }
                }
                SyncSlotsView();
                return;
            }

            if (_slotEntries.Count == 0 && _slots != null && _slots.Count > 0)
            {
                var initialCopy = new List<BuildingData>(_slots);
                for (int i = 0; i < initialCopy.Count; i++)
                {
                    if (initialCopy[i] != null)
                    {
                        TryAddBlueprintInternal(initialCopy[i], 1, notify: false);
                    }
                }
                SyncSlotsView();
            }
        }

        private void Start()
        {
            SubscribeToExternalEvents();
            RegisterInput();
            SlotsChanged?.Invoke();
        }

        private void OnEnable()
        {
            SubscribeToExternalEvents();
            RegisterInput();
        }

        private void SubscribeToExternalEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnGameStateChanged;
                GameManager.Instance.StateChanged += OnGameStateChanged;
            }

            if (WaveController.Instance != null)
            {
                WaveController.Instance.StateChanged -= OnWaveStateChanged;
                WaveController.Instance.StateChanged += OnWaveStateChanged;
            }

            if (RunProgressionManager.Instance != null)
            {
                RunProgressionManager.Instance.BuildSlotsCountChanged -= OnExtraBuildSlotsChanged;
                RunProgressionManager.Instance.BuildSlotsCountChanged += OnExtraBuildSlotsChanged;
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromExternalEvents();
            UnregisterInput();
        }

        private void OnDestroy()
        {
            UnsubscribeFromExternalEvents();
            UnregisterInput();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void UnsubscribeFromExternalEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnGameStateChanged;
            }

            if (WaveController.Instance != null)
            {
                WaveController.Instance.StateChanged -= OnWaveStateChanged;
            }

            if (RunProgressionManager.Instance != null)
            {
                RunProgressionManager.Instance.BuildSlotsCountChanged -= OnExtraBuildSlotsChanged;
            }
        }

        private void OnExtraBuildSlotsChanged(int _)
        {
            SlotsChanged?.Invoke();
        }

        public void RegisterPlacementValidator(Func<BuildingData, Vector2Int, bool> validator)
        {
            _extraPlacementValidator = validator;
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

        private void OnWaveStateChanged(WaveState waveState)
        {
            if (!IsBuildingManagementAllowed())
            {
                CancelMovingAndSelection();
            }
        }

        private static bool IsBuildingManagementAllowed()
        {
            if (WaveController.Instance == null) return true;
            WaveState waveState = WaveController.Instance.CurrentState;
            return waveState == WaveState.BuildingPhase || waveState == WaveState.NotStarted;
        }

        private void CancelMovingAndSelection()
        {
            if (_currentMode == ActionMode.Moving && _movingBuilding != null)
            {
                _movingBuilding.gameObject.SetActive(true);
                _movingBuilding.SetOrigin(_movingOriginalCell);
                _gridMap.TryPlace(_movingBuilding);
                _movingBuilding = null;
                _ghostView.Hide();
                SetMode(ActionMode.None);
            }

            DeselectBuilding();
        }

        public int GetMaxSlotsLimit()
        {
            int baseSlots = RunConfig.Instance != null && RunConfig.Instance.SelectedKnight != null
                ? RunConfig.Instance.SelectedKnight.InitialBuildSlots
                : _maxSlots;
            int bonusSlots = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.ExtraBuildSlots
                : 0;
            return Mathf.Max(1, baseSlots + bonusSlots);
        }

        public static int GetStackCap(BuildingData data)
        {
            if (data == null) return 9;
            return (data.Size.x > 1 || data.Size.y > 1) ? 4 : 9;
        }

        public int GetSlotCount(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slotEntries.Count) return 0;
            return _slotEntries[slotIndex].Count;
        }

        public int GetUnplacedSellRefund(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slotEntries.Count) return 0;
            BuildingData data = _slotEntries[slotIndex].Data;
            if (data == null) return 0;
            return Mathf.RoundToInt(data.GetStats(1).cost * data.UnplacedSaleRefundRatio);
        }

        public int GetPlacedSellRefund(Building building)
        {
            if (building == null || building.Data == null) return 0;
            int totalCost = 0;
            for (int lvl = 1; lvl <= building.CurrentLevel; lvl++)
            {
                totalCost += building.Data.GetStats(lvl).cost;
            }
            return Mathf.RoundToInt(totalCost * building.Data.PlacedSaleRefundRatio);
        }

        public bool CanAddBlueprint(BuildingData data)
        {
            if (data == null) return false;
            int cap = GetStackCap(data);

            for (int i = 0; i < _slotEntries.Count; i++)
            {
                if (_slotEntries[i].Data == data && _slotEntries[i].Count < cap)
                {
                    return true;
                }
            }

            return _slotEntries.Count < MaxSlots;
        }

        public bool TryAddBlueprint(BuildingData data, int count = 1)
        {
            return TryAddBlueprintInternal(data, count, notify: true);
        }

        private bool TryAddBlueprintInternal(BuildingData data, int count, bool notify)
        {
            if (data == null || count <= 0) return false;

            int cap = GetStackCap(data);
            int remaining = count;
            bool addedAny = false;

            for (int i = 0; i < _slotEntries.Count && remaining > 0; i++)
            {
                BuildingSlotEntry entry = _slotEntries[i];
                if (entry.Data == data && entry.Count < cap)
                {
                    int room = cap - entry.Count;
                    int add = Mathf.Min(room, remaining);
                    entry.Count += add;
                    _slotEntries[i] = entry;
                    remaining -= add;
                    addedAny = true;
                }
            }

            while (remaining > 0 && _slotEntries.Count < MaxSlots)
            {
                int add = Mathf.Min(cap, remaining);
                _slotEntries.Add(new BuildingSlotEntry(data, add));
                remaining -= add;
                addedAny = true;
            }

            if (addedAny)
            {
                SyncSlotsView();
                if (notify)
                {
                    SlotsChanged?.Invoke();
                }
            }

            return addedAny;
        }

        public void SetSlots(IEnumerable<BuildingData> slots)
        {
            _slotEntries.Clear();
            if (slots != null)
            {
                foreach (var data in slots)
                {
                    if (data != null)
                    {
                        TryAddBlueprintInternal(data, 1, notify: false);
                    }
                }
            }
            SyncSlotsView();
            SlotsChanged?.Invoke();
        }

        private void SyncSlotsView()
        {
            _slots.Clear();
            for (int i = 0; i < _slotEntries.Count; i++)
            {
                _slots.Add(_slotEntries[i].Data);
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
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
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

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
                if (occupant is Building building && !(occupant is Castle) && IsBuildingManagementAllowed())
                {
                    SelectBuilding(building);
                }
                else
                {
                    DeselectBuilding();
                }
            }
        }

        private void OnSecondaryClicked()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

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
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            CancelAction();
        }

        private void OnSellPressed()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (!IsBuildingManagementAllowed()) return;
            if (_currentMode == ActionMode.Selected && _selectedBuilding != null)
            {
                SellBuilding(_selectedBuilding);
            }
        }

        private void OnMoveBuildingPressed()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (!IsBuildingManagementAllowed()) return;
            if (_currentMode == ActionMode.Selected && _selectedBuilding != null)
            {
                StartMoving(_selectedBuilding);
            }
        }

        private void OnUpgradePressed()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (!IsBuildingManagementAllowed()) return;
            if (_currentMode == ActionMode.Selected && _selectedBuilding != null)
            {
                UpgradeBuilding(_selectedBuilding);
            }
        }

        public void SelectSlot(int slotIndex)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (slotIndex < 0 || slotIndex >= _slotEntries.Count) return;

            BuildingData data = _slotEntries[slotIndex].Data;
            if (data == null || _slotEntries[slotIndex].Count <= 0) return;

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

            CancelConflictingTools();
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

            var wallet = Wallet;
            if (wallet == null || wallet.Coins < cost)
            {
                return false;
            }

            if (_extraPlacementValidator != null && !_extraPlacementValidator(data, cell))
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

            var wallet = Wallet;
            if (wallet == null || !wallet.TrySpendCoins(cost))
            {
                return;
            }

            BuildingData placingData = _placingData;
            int startingLevel = GetStartingLevel(placingData);

            Building buildingInstance = Instantiate(placingData.Prefab);
            buildingInstance.Initialize(placingData, _gridMap, cell, startingLevel);
            _gridMap.TryPlace(buildingInstance);

            bool hasMoreCopies = ConsumeOneBlueprint(placingData);

            bool isShift = InputManager.Instance != null && InputManager.Instance.IsShiftHeld;
            if (!isShift || !hasMoreCopies)
            {
                CancelAction();
            }
        }

        private bool ConsumeOneBlueprint(BuildingData data)
        {
            if (data == null) return false;

            int remainingForType = 0;
            bool consumed = false;

            for (int i = 0; i < _slotEntries.Count; i++)
            {
                if (_slotEntries[i].Data != data) continue;

                if (!consumed)
                {
                    BuildingSlotEntry entry = _slotEntries[i];
                    entry.Count--;
                    consumed = true;

                    if (entry.Count <= 0)
                    {
                        _slotEntries.RemoveAt(i);
                        i--;
                        continue;
                    }
                    _slotEntries[i] = entry;
                }

                remainingForType += _slotEntries[i].Count;
            }

            if (consumed)
            {
                SyncSlotsView();
                SlotsChanged?.Invoke();
            }

            return remainingForType > 0;
        }

        public void StartMoving(Building building)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (!IsBuildingManagementAllowed()) return;
            if (building == null) return;

            CancelConflictingTools();

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
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (!IsBuildingManagementAllowed()) return;
            if (_selectedBuilding == building) return;

            if (building != null)
            {
                CancelConflictingTools();
            }

            _selectedBuilding = building;
            SetMode(_selectedBuilding != null ? ActionMode.Selected : ActionMode.None);
            _contextMenuUI?.OnBuildingSelected(_selectedBuilding);
            SelectedBuildingChanged?.Invoke(_selectedBuilding);
        }

        private void CancelConflictingTools()
        {
            var weapon = FindAnyObjectByType<Game.Weapons.CastleWeapon>();
            if (weapon != null && weapon.IsAiming)
            {
                weapon.CancelAiming();
            }

            Game.Spells.SpellScrollManager.Instance?.CancelSelection();
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
            if (!IsBuildingManagementAllowed()) return;
            if (building == null) return;

            int refund = GetPlacedSellRefund(building);
            if (refund > 0)
            {
                // Intentionally does NOT grant castle XP (GIG-17)
                Wallet?.AddCoins(refund);
            }

            DeselectBuilding();
            building.DestroyBuilding();
        }

        public void SellUnplacedBlueprint(int slotIndex)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (!IsBuildingManagementAllowed()) return;
            if (slotIndex < 0 || slotIndex >= _slotEntries.Count) return;

            BuildingSlotEntry entry = _slotEntries[slotIndex];
            if (entry.Data == null || entry.Count <= 0) return;

            int refund = Mathf.RoundToInt(entry.Data.GetStats(1).cost * entry.Data.UnplacedSaleRefundRatio);
            if (refund > 0)
            {
                // Intentionally does NOT grant castle XP (GIG-82, GIG-17)
                Wallet?.AddCoins(refund);
            }

            BuildingData soldData = entry.Data;
            entry.Count--;
            if (entry.Count <= 0)
            {
                _slotEntries.RemoveAt(slotIndex);
                if (_currentMode == ActionMode.Placing && _placingData == soldData)
                {
                    CancelAction();
                }
            }
            else
            {
                _slotEntries[slotIndex] = entry;
            }

            SyncSlotsView();
            SlotsChanged?.Invoke();
        }

        public void UpgradeBuilding(Building building)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (!IsBuildingManagementAllowed()) return;
            if (building == null) return;

            if (!building.CanUpgrade(out int cost)) return;

            var wallet = Wallet;
            if (wallet == null || !wallet.TrySpendCoins(cost))
            {
                Debug.Log("[BuildingManager] Недостаточно монет для улучшения!");
                return;
            }

            building.Upgrade();

            ApplyGlobalUpgrade(building.Data, building.CurrentLevel);

            _contextMenuUI?.OnBuildingSelected(_selectedBuilding);
            SelectedBuildingChanged?.Invoke(_selectedBuilding);
        }

        public void UpgradeSlot(int slotIndex)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (!IsBuildingManagementAllowed()) return;
            if (slotIndex < 0 || slotIndex >= _slotEntries.Count) return;

            BuildingData data = _slotEntries[slotIndex].Data;
            if (data == null || data.UpgradeType != BuildingUpgradeType.Global) return;

            int currentLvl = GetStartingLevel(data);
            if (currentLvl >= 4) return;
            BuildingLevelStats nextStats = data.GetStats(currentLvl + 1);

            float discountMult = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveUpgradeCostMultiplier()
                : 1f;
            int cost = Mathf.RoundToInt(nextStats.cost * discountMult);

            var wallet = Wallet;
            if (wallet == null || !wallet.TrySpendCoins(cost)) return;

            ApplyGlobalUpgrade(data, currentLvl + 1);

            if (_selectedBuilding != null && _selectedBuilding.Data == data)
            {
                _contextMenuUI?.OnBuildingSelected(_selectedBuilding);
                SelectedBuildingChanged?.Invoke(_selectedBuilding);
            }
        }

        private void ApplyGlobalUpgrade(BuildingData data, int newLevel)
        {
            if (data == null || data.UpgradeType != BuildingUpgradeType.Global) return;

            string id = data.Id;
            _globalLevels[id] = newLevel;

            var activeBuildings = Building.ActiveBuildings;
            for (int i = activeBuildings.Count - 1; i >= 0; i--)
            {
                Building active = activeBuildings[i];
                if (active != null && !active.IsDead && active.Data != null && active.Data.Id == id)
                {
                    active.SyncToLevel(newLevel);
                }
            }

            if (_movingBuilding != null && !_movingBuilding.IsDead &&
                _movingBuilding.Data != null && _movingBuilding.Data.Id == id)
            {
                _movingBuilding.SyncToLevel(newLevel);
            }
        }

        public void AddSlot(BuildingData data)
        {
            TryAddBlueprint(data, 1);
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
            if (_ghostView != null) _ghostView.Hide();
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
