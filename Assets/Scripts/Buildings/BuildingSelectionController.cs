using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.UI.BuildingUI;

namespace Game.Buildings
{
    /// <summary>
    /// Handles building selection, context menu sync, upgrades, and placed building refunds.
    /// </summary>
    public sealed class BuildingSelectionController
    {
        private readonly Dictionary<string, int> _globalLevels = new();
        private BuildingContextMenuUI _contextMenuUI;
        private readonly Action<Building> _onSelectionChanged;

        public Building SelectedBuilding { get; private set; }

        public BuildingSelectionController(BuildingContextMenuUI contextMenuUI, Action<Building> onSelectionChanged)
        {
            _contextMenuUI = contextMenuUI;
            _onSelectionChanged = onSelectionChanged;
        }

        public void BindContextMenu(BuildingContextMenuUI contextMenuUI)
        {
            _contextMenuUI = contextMenuUI;
        }

        public void SetSelectedBuilding(Building building)
        {
            SelectedBuilding = building;
            _contextMenuUI?.OnBuildingSelected(SelectedBuilding);
            _onSelectionChanged?.Invoke(SelectedBuilding);
        }

        public bool ClearSelection()
        {
            if (SelectedBuilding == null) return false;
            SetSelectedBuilding(null);
            return true;
        }

        public int GetStartingLevel(BuildingData data)
        {
            if (data != null && data.UpgradeType == BuildingUpgradeType.Global &&
                _globalLevels.TryGetValue(data.Id, out int lvl))
            {
                return lvl;
            }
            return 1;
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

        public void SellBuilding(Building building, PlayerWallet wallet)
        {
            if (building == null) return;

            int refund = GetPlacedSellRefund(building);
            if (refund > 0)
            {
                // Intentionally does NOT grant castle XP (GIG-17)
                wallet?.AddCoins(refund);
            }

            ClearSelection();
            building.DestroyBuilding();
        }

        public void UpgradeBuilding(Building building, PlayerWallet wallet, Building movingBuilding, UnityEngine.Object logContext)
        {
            if (building == null) return;
            if (!building.CanUpgrade(out int cost)) return;

            if (wallet == null || !wallet.TrySpendCoins(cost))
            {
                Debug.LogWarning("[BuildingManager] Недостаточно монет для улучшения!", logContext);
                return;
            }

            building.Upgrade();
            ApplyGlobalUpgrade(building.Data, building.CurrentLevel, movingBuilding);

            _contextMenuUI?.OnBuildingSelected(SelectedBuilding);
            _onSelectionChanged?.Invoke(SelectedBuilding);
        }

        public void UpgradeSlot(BuildingData data, PlayerWallet wallet, IRunModifiers modifiers, Building movingBuilding)
        {
            if (data == null || data.UpgradeType != BuildingUpgradeType.Global) return;

            int currentLvl = GetStartingLevel(data);
            if (currentLvl >= 4) return;

            BuildingLevelStats nextStats = data.GetStats(currentLvl + 1);
            float discountMult = modifiers != null
                ? modifiers.GetEffectiveUpgradeCostMultiplier()
                : 1f;
            int cost = Mathf.RoundToInt(nextStats.cost * discountMult);

            if (wallet == null || !wallet.TrySpendCoins(cost)) return;

            ApplyGlobalUpgrade(data, currentLvl + 1, movingBuilding);

            if (SelectedBuilding != null && SelectedBuilding.Data == data)
            {
                _contextMenuUI?.OnBuildingSelected(SelectedBuilding);
                _onSelectionChanged?.Invoke(SelectedBuilding);
            }
        }

        public void ApplyGlobalUpgrade(BuildingData data, int newLevel, Building movingBuilding)
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

            if (movingBuilding != null && !movingBuilding.IsDead &&
                movingBuilding.Data != null && movingBuilding.Data.Id == id)
            {
                movingBuilding.SyncToLevel(newLevel);
            }
        }
    }
}
