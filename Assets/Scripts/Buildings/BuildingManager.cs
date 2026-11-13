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

        [Title("Dependencies")]
        [SerializeField] private GridMap _gridMap;
        [SerializeField] private BuildingGhostView _ghostView;
        [SerializeField] private PlayerWallet _wallet;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private Game.Weapons.CastleWeapon _castleWeapon;

        [Title("UI References")]
        [SerializeField] private BuildingContextMenuUI _contextMenuUI;

        [Title("Inventory")]
        [SerializeField] private List<BuildingData> _slots = new();
        [SerializeField, Min(1)] private int _maxSlots = 5;

        [Title("Runtime State")]
        [ShowInInspector, ReadOnly] private ActionMode _currentMode = ActionMode.None;
        [ShowInInspector, ReadOnly] private BuildingData PlacingData => _placement?.PlacingData;
        [ShowInInspector, ReadOnly] private Building MovingBuilding => _placement?.MovingBuilding;
        [ShowInInspector, ReadOnly] private Building SelectedBuildingInspect => _selection?.SelectedBuilding;

        private BuildingSlotInventory _inventory;
        private BuildingPlacementController _placement;
        private BuildingSelectionController _selection;

        public ActionMode CurrentMode => _currentMode;
        public IReadOnlyList<BuildingData> Slots => _slots;
        public IReadOnlyList<BuildingSlotEntry> SlotEntries => Inventory.Entries;
        public int MaxSlots => GetMaxSlotsLimit();
        public Building SelectedBuilding => Selection.SelectedBuilding;
        public bool CanManagePlacedBuildings => IsBuildingManagementAllowed();

        private PlayerWallet Wallet => _wallet != null ? _wallet : PlayerWallet.Instance;
        private IRunModifiers RunModifiers => RunProgressionManager.Instance;

        private BuildingSlotInventory Inventory => _inventory ??= new BuildingSlotInventory(
            _slots,
            _maxSlots,
            () => RunModifiers,
            () => SlotsChanged?.Invoke());

        private BuildingPlacementController Placement => _placement ??= new BuildingPlacementController(_gridMap, _ghostView);

        private BuildingSelectionController Selection => _selection ??= new BuildingSelectionController(
            _contextMenuUI,
            b => SelectedBuildingChanged?.Invoke(b));

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

            if (_gridMap == null) Debug.LogError($"[{nameof(BuildingManager)}] '_gridMap' is not assigned in Inspector!", this);
            if (_ghostView == null) Debug.LogError($"[{nameof(BuildingManager)}] '_ghostView' is not assigned in Inspector!", this);
            if (_wallet == null)
            {
                _wallet = PlayerWallet.Instance;
                if (_wallet == null) Debug.LogError($"[{nameof(BuildingManager)}] '_wallet' is not assigned and PlayerWallet.Instance is null!", this);
            }
            if (_contextMenuUI == null) Debug.LogWarning($"[{nameof(BuildingManager)}] '_contextMenuUI' is not assigned in Inspector!", this);
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) Debug.LogError($"[{nameof(BuildingManager)}] '_mainCamera' is not assigned and Camera.main is null!", this);
            }

            Inventory.InitializeStartingSlots();
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

        private void Update()
        {
            if (!IsPlayingState()) return;

            if (InputManager.Instance == null || _mainCamera == null) return;
            Vector3 pointerWorld = InputManager.Instance.GetPointerWorldPosition(_mainCamera);
            Placement.UpdateGhostHover(_currentMode, pointerWorld, Wallet, RunModifiers);
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

            if (RunModifiers != null)
            {
                RunModifiers.BuildSlotsCountChanged -= OnExtraBuildSlotsChanged;
                RunModifiers.BuildSlotsCountChanged += OnExtraBuildSlotsChanged;
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

            if (RunModifiers != null)
            {
                RunModifiers.BuildSlotsCountChanged -= OnExtraBuildSlotsChanged;
            }
        }

        private void RegisterInput()
        {
            UnregisterInput();

            if (InputManager.Instance == null)
            {
                Debug.LogWarning($"[{nameof(BuildingManager)}] InputManager.Instance is null; input actions were not registered.", this);
                return;
            }

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

        public void RegisterPlacementValidator(Func<BuildingData, Vector2Int, bool> validator)
            => Placement.RegisterPlacementValidator(validator);

        public void BindWeapon(Game.Weapons.CastleWeapon weapon)
            => _castleWeapon = weapon;

        public int GetMaxSlotsLimit() => Inventory.GetMaxSlotsLimit();
        public static int GetStackCap(BuildingData data) => BuildingSlotInventory.GetStackCap(data);
        public int GetSlotCount(int slotIndex) => Inventory.GetSlotCount(slotIndex);
        public int GetUnplacedSellRefund(int slotIndex) => Inventory.GetUnplacedSellRefund(slotIndex);
        public int GetPlacedSellRefund(Building building) => Selection.GetPlacedSellRefund(building);
        public bool CanAddBlueprint(BuildingData data) => Inventory.CanAddBlueprint(data);
        public bool TryAddBlueprint(BuildingData data, int count = 1) => Inventory.TryAddBlueprint(data, count, notify: true);
        public void SetSlots(IEnumerable<BuildingData> slots) => Inventory.SetSlots(slots);
        public void AddSlot(BuildingData data) => TryAddBlueprint(data, 1);

        public void SelectSlot(int slotIndex)
        {
            if (!IsPlayingState()) return;

            BuildingData data = Inventory.GetSlotData(slotIndex);
            if (data == null || Inventory.GetSlotCount(slotIndex) <= 0) return;

            if (_currentMode == ActionMode.Placing && Placement.PlacingData == data)
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

            SetMode(ActionMode.Placing);
            Placement.StartPlacing(data);
        }

        public void StartMoving(Building building)
        {
            if (!CanManageInPlayState() || building == null) return;

            CancelConflictingTools();
            SetMode(ActionMode.Moving);
            Placement.StartMoving(building);
            Selection.SetSelectedBuilding(null);
        }

        public void SelectBuilding(Building building)
        {
            if (!CanManageInPlayState()) return;
            if (Selection.SelectedBuilding == building) return;

            if (building != null)
            {
                CancelConflictingTools();
            }

            SetMode(building != null ? ActionMode.Selected : ActionMode.None);
            Selection.SetSelectedBuilding(building);
        }

        public void DeselectBuilding()
        {
            if (Selection.ClearSelection())
            {
                SetMode(ActionMode.None);
            }
        }

        public void SellBuilding(Building building)
        {
            if (!CanManageInPlayState() || building == null) return;

            Selection.SellBuilding(building, Wallet);
            if (_currentMode == ActionMode.Selected)
            {
                SetMode(ActionMode.None);
            }
        }

        public void SellUnplacedBlueprint(int slotIndex)
        {
            if (!CanManageInPlayState()) return;
            if (slotIndex < 0 || slotIndex >= Inventory.Count) return;

            int refund = Inventory.GetUnplacedSellRefund(slotIndex);
            if (!Inventory.TryConsumeSlotForSale(slotIndex, out BuildingData soldData, out bool slotEmptied))
            {
                return;
            }

            if (refund > 0)
            {
                // Intentionally does NOT grant castle XP (GIG-82, GIG-17)
                Wallet?.AddCoins(refund);
            }

            if (slotEmptied && _currentMode == ActionMode.Placing && Placement.PlacingData == soldData)
            {
                CancelAction();
            }
        }

        public void UpgradeBuilding(Building building)
        {
            if (!CanManageInPlayState() || building == null) return;
            Selection.UpgradeBuilding(building, Wallet, Placement.MovingBuilding, this);
        }

        public void UpgradeSlot(int slotIndex)
        {
            if (!CanManageInPlayState()) return;
            BuildingData data = Inventory.GetSlotData(slotIndex);
            Selection.UpgradeSlot(data, Wallet, RunModifiers, Placement.MovingBuilding);
        }

        public void CancelAction()
        {
            Placement.ClearPlacement();
            SetMode(ActionMode.None);
            DeselectBuilding();
        }

        private void OnPrimaryClicked(Vector2 screenPosition)
        {
            if (!IsPlayingState()) return;
            if (InputManager.Instance.IsPointerOverUI(screenPosition)) return;

            Vector3 pointerWorld = InputManager.Instance.GetPointerWorldPosition(_mainCamera);
            Vector3Int cellPos = _gridMap.WorldToCell(pointerWorld);
            var hoverCell = new Vector2Int(cellPos.x, cellPos.y);

            if (_currentMode == ActionMode.Placing && Placement.PlacingData != null)
            {
                if (Placement.TryExecutePlacement(
                        hoverCell,
                        Wallet,
                        RunModifiers,
                        Selection.GetStartingLevel,
                        Inventory.ConsumeOneBlueprint,
                        out bool shouldCancel) && shouldCancel)
                {
                    CancelAction();
                }
            }
            else if (_currentMode == ActionMode.Moving && Placement.MovingBuilding != null)
            {
                if (Placement.TryExecuteMove(hoverCell))
                {
                    CancelAction();
                }
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
            if (!IsPlayingState()) return;

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
            if (!IsPlayingState()) return;
            CancelAction();
        }

        private void OnSellPressed()
        {
            if (_currentMode == ActionMode.Selected && SelectedBuilding != null)
            {
                SellBuilding(SelectedBuilding);
            }
        }

        private void OnMoveBuildingPressed()
        {
            if (_currentMode == ActionMode.Selected && SelectedBuilding != null)
            {
                StartMoving(SelectedBuilding);
            }
        }

        private void OnUpgradePressed()
        {
            if (_currentMode == ActionMode.Selected && SelectedBuilding != null)
            {
                UpgradeBuilding(SelectedBuilding);
            }
        }

        private void OnExtraBuildSlotsChanged(int _)
        {
            SlotsChanged?.Invoke();
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
                if (_currentMode == ActionMode.Moving && Placement.RestoreMovingBuildingIfActive())
                {
                    SetMode(ActionMode.None);
                }

                DeselectBuilding();
            }
        }

        private void CancelConflictingTools()
        {
            if (_castleWeapon != null && _castleWeapon.IsAiming)
            {
                _castleWeapon.CancelAiming();
            }

            Game.Spells.SpellScrollManager.Instance?.CancelSelection();
        }

        private void SetMode(ActionMode newMode)
        {
            if (_currentMode == newMode) return;
            _currentMode = newMode;
            ModeChanged?.Invoke(_currentMode);
        }

        private static bool IsPlayingState()
            => GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing;

        private bool CanManageInPlayState()
            => IsPlayingState() && IsBuildingManagementAllowed();

        private static bool IsBuildingManagementAllowed()
        {
            if (WaveController.Instance == null) return true;
            WaveState waveState = WaveController.Instance.CurrentState;
            return waveState == WaveState.BuildingPhase || waveState == WaveState.NotStarted;
        }
    }
}
