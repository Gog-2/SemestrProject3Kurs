using System;
using UnityEngine;
using Game.Core;

namespace Game.Buildings
{
    /// <summary>
    /// Handles building placement, relocation, grid validation, and ghost preview rendering.
    /// </summary>
    public sealed class BuildingPlacementController
    {
        private readonly GridMap _gridMap;
        private readonly BuildingGhostView _ghostView;
        private Func<BuildingData, Vector2Int, bool> _extraPlacementValidator;
        private Vector2Int _movingOriginalCell;

        public BuildingData PlacingData { get; private set; }
        public Building MovingBuilding { get; private set; }

        public BuildingPlacementController(GridMap gridMap, BuildingGhostView ghostView)
        {
            _gridMap = gridMap;
            _ghostView = ghostView;
        }

        public void RegisterPlacementValidator(Func<BuildingData, Vector2Int, bool> validator)
        {
            _extraPlacementValidator = validator;
        }

        public void UpdateGhostHover(
            BuildingManager.ActionMode mode,
            Vector3 pointerWorld,
            PlayerWallet wallet,
            IRunModifiers modifiers)
        {
            if (mode != BuildingManager.ActionMode.Placing && mode != BuildingManager.ActionMode.Moving) return;
            if (_gridMap == null || _ghostView == null) return;

            Vector3Int cellPos = _gridMap.WorldToCell(pointerWorld);
            var hoverCell = new Vector2Int(cellPos.x, cellPos.y);

            if (mode == BuildingManager.ActionMode.Placing && PlacingData != null)
            {
                bool canPlace = CanPlaceBuilding(PlacingData, hoverCell, wallet, modifiers, out _);
                _ghostView.UpdatePosition(hoverCell, canPlace);
            }
            else if (mode == BuildingManager.ActionMode.Moving && MovingBuilding != null)
            {
                bool canPlace = _gridMap.CanPlace(hoverCell, MovingBuilding.Size);
                _ghostView.UpdatePosition(hoverCell, canPlace);
            }
        }

        public void StartPlacing(BuildingData data)
        {
            PlacingData = data;
            _ghostView?.Show(PlacingData, _gridMap);
        }

        public void StartMoving(Building building)
        {
            MovingBuilding = building;
            _movingOriginalCell = building.Origin;

            _gridMap.Remove(MovingBuilding);
            MovingBuilding.gameObject.SetActive(false);
            _ghostView?.Show(MovingBuilding.Data, _gridMap);
        }

        public bool CanPlaceBuilding(
            BuildingData data,
            Vector2Int cell,
            PlayerWallet wallet,
            IRunModifiers modifiers,
            out int cost)
        {
            cost = 0;
            if (data == null || _gridMap == null) return false;

            float discountMult = modifiers != null
                ? modifiers.GetEffectiveUpgradeCostMultiplier()
                : 1f;
            cost = Mathf.Max(1, Mathf.RoundToInt(data.GetStats(1).cost * discountMult));

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

        public bool TryExecutePlacement(
            Vector2Int cell,
            PlayerWallet wallet,
            IRunModifiers modifiers,
            Func<BuildingData, int> getStartingLevel,
            Func<BuildingData, bool> consumeOneBlueprint,
            out bool shouldCancel)
        {
            shouldCancel = false;
            if (!CanPlaceBuilding(PlacingData, cell, wallet, modifiers, out int cost))
            {
                return false;
            }

            if (PlacingData.Prefab == null)
            {
                Debug.LogError($"BuildingData '{PlacingData.name}' has no prefab assigned.", PlacingData);
                return false;
            }

            if (wallet == null || !wallet.TrySpendCoins(cost))
            {
                return false;
            }

            BuildingData placingData = PlacingData;
            int startingLevel = getStartingLevel != null ? getStartingLevel(placingData) : 1;

            Building buildingInstance = UnityEngine.Object.Instantiate(placingData.Prefab);
            buildingInstance.Initialize(placingData, _gridMap, cell, startingLevel, modifiers);
            _gridMap.TryPlace(buildingInstance);

            bool hasMoreCopies = consumeOneBlueprint != null && consumeOneBlueprint(placingData);
            bool isShift = InputManager.Instance != null && InputManager.Instance.IsShiftHeld;
            shouldCancel = !isShift || !hasMoreCopies;
            return true;
        }

        public bool TryExecuteMove(Vector2Int cell)
        {
            if (MovingBuilding == null || _gridMap == null) return false;
            if (!_gridMap.CanPlace(cell, MovingBuilding.Size)) return false;

            MovingBuilding.gameObject.SetActive(true);
            MovingBuilding.SetOrigin(cell);
            _gridMap.TryPlace(MovingBuilding);

            MovingBuilding = null;
            return true;
        }

        public bool RestoreMovingBuildingIfActive()
        {
            if (MovingBuilding == null) return false;

            MovingBuilding.gameObject.SetActive(true);
            MovingBuilding.SetOrigin(_movingOriginalCell);
            _gridMap?.TryPlace(MovingBuilding);
            MovingBuilding = null;
            _ghostView?.Hide();
            return true;
        }

        public void ClearPlacement()
        {
            RestoreMovingBuildingIfActive();
            PlacingData = null;
            _ghostView?.Hide();
        }
    }
}
