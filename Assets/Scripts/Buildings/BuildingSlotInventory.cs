using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Data;

namespace Game.Buildings
{
    /// <summary>
    /// Manages blueprint slot inventory, stack limits, and slot-level refunds.
    /// </summary>
    public sealed class BuildingSlotInventory
    {
        private readonly List<BuildingSlotEntry> _entries = new();
        private readonly List<BuildingData> _slotsView;
        private readonly int _defaultMaxSlots;
        private readonly Func<IRunModifiers> _getModifiers;
        private readonly Action _notifySlotsChanged;

        public IReadOnlyList<BuildingSlotEntry> Entries => _entries;
        public int Count => _entries.Count;

        public BuildingSlotInventory(
            List<BuildingData> serializedSlots,
            int defaultMaxSlots,
            Func<IRunModifiers> getModifiers,
            Action notifySlotsChanged)
        {
            _slotsView = serializedSlots ?? new List<BuildingData>();
            _defaultMaxSlots = Mathf.Max(1, defaultMaxSlots);
            _getModifiers = getModifiers;
            _notifySlotsChanged = notifySlotsChanged;
        }

        public void InitializeStartingSlots()
        {
            KnightData knight = RunConfig.Instance != null ? RunConfig.Instance.SelectedKnight : null;
            if (knight != null && knight.StartingBuildingAssets != null && knight.StartingBuildingAssets.Length > 0)
            {
                _entries.Clear();
                for (int i = 0; i < knight.StartingBuildingAssets.Length; i++)
                {
                    BuildingData bd = knight.StartingBuildingAssets[i];
                    if (bd != null)
                    {
                        TryAddBlueprint(bd, 1, notify: false);
                    }
                }
                SyncSlotsView();
                return;
            }

            if (_entries.Count == 0 && _slotsView.Count > 0)
            {
                var initialCopy = new List<BuildingData>(_slotsView);
                for (int i = 0; i < initialCopy.Count; i++)
                {
                    if (initialCopy[i] != null)
                    {
                        TryAddBlueprint(initialCopy[i], 1, notify: false);
                    }
                }
                SyncSlotsView();
            }
        }

        public int GetMaxSlotsLimit()
        {
            int baseSlots = RunConfig.Instance != null && RunConfig.Instance.SelectedKnight != null
                ? RunConfig.Instance.SelectedKnight.InitialBuildSlots
                : _defaultMaxSlots;
            IRunModifiers mods = _getModifiers?.Invoke();
            int bonusSlots = mods != null ? mods.ExtraBuildSlots : 0;
            return Mathf.Max(1, baseSlots + bonusSlots);
        }

        public static int GetStackCap(BuildingData data)
        {
            if (data == null) return 9;
            return (data.Size.x > 1 || data.Size.y > 1) ? 4 : 9;
        }

        public int GetSlotCount(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _entries.Count) return 0;
            return _entries[slotIndex].Count;
        }

        public BuildingData GetSlotData(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _entries.Count) return null;
            return _entries[slotIndex].Data;
        }

        public int GetUnplacedSellRefund(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _entries.Count) return 0;
            BuildingData data = _entries[slotIndex].Data;
            if (data == null) return 0;
            // Match half-price shop blueprint fee so selling cannot yield infinite gold.
            return Mathf.Max(1, Mathf.RoundToInt(data.GetStats(1).cost * 0.5f * data.UnplacedSaleRefundRatio));
        }

        public bool CanAddBlueprint(BuildingData data)
        {
            if (data == null) return false;
            int cap = GetStackCap(data);

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Data == data && _entries[i].Count < cap)
                {
                    return true;
                }
            }

            return _entries.Count < GetMaxSlotsLimit();
        }

        public bool TryAddBlueprint(BuildingData data, int count = 1, bool notify = true)
        {
            if (data == null || count <= 0) return false;

            int cap = GetStackCap(data);
            int remaining = count;
            bool addedAny = false;
            int maxSlots = GetMaxSlotsLimit();

            for (int i = 0; i < _entries.Count && remaining > 0; i++)
            {
                BuildingSlotEntry entry = _entries[i];
                if (entry.Data == data && entry.Count < cap)
                {
                    int room = cap - entry.Count;
                    int add = Mathf.Min(room, remaining);
                    entry.Count += add;
                    _entries[i] = entry;
                    remaining -= add;
                    addedAny = true;
                }
            }

            while (remaining > 0 && _entries.Count < maxSlots)
            {
                int add = Mathf.Min(cap, remaining);
                _entries.Add(new BuildingSlotEntry(data, add));
                remaining -= add;
                addedAny = true;
            }

            if (addedAny)
            {
                SyncSlotsView();
                if (notify)
                {
                    _notifySlotsChanged?.Invoke();
                }
            }

            return addedAny;
        }

        public bool ConsumeOneBlueprint(BuildingData data)
        {
            if (data == null) return false;

            int remainingForType = 0;
            bool consumed = false;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Data != data) continue;

                if (!consumed)
                {
                    BuildingSlotEntry entry = _entries[i];
                    entry.Count--;
                    consumed = true;

                    if (entry.Count <= 0)
                    {
                        _entries.RemoveAt(i);
                        i--;
                        continue;
                    }
                    _entries[i] = entry;
                }

                remainingForType += _entries[i].Count;
            }

            if (consumed)
            {
                SyncSlotsView();
                _notifySlotsChanged?.Invoke();
            }

            return remainingForType > 0;
        }

        public bool TryConsumeSlotForSale(int slotIndex, out BuildingData soldData, out bool slotEmptied)
        {
            soldData = null;
            slotEmptied = false;
            if (slotIndex < 0 || slotIndex >= _entries.Count) return false;

            BuildingSlotEntry entry = _entries[slotIndex];
            if (entry.Data == null || entry.Count <= 0) return false;

            soldData = entry.Data;
            entry.Count--;
            if (entry.Count <= 0)
            {
                _entries.RemoveAt(slotIndex);
                slotEmptied = true;
            }
            else
            {
                _entries[slotIndex] = entry;
            }

            SyncSlotsView();
            _notifySlotsChanged?.Invoke();
            return true;
        }

        public void SetSlots(IEnumerable<BuildingData> slots)
        {
            _entries.Clear();
            if (slots != null)
            {
                foreach (BuildingData data in slots)
                {
                    if (data != null)
                    {
                        TryAddBlueprint(data, 1, notify: false);
                    }
                }
            }
            SyncSlotsView();
            _notifySlotsChanged?.Invoke();
        }

        private void SyncSlotsView()
        {
            _slotsView.Clear();
            for (int i = 0; i < _entries.Count; i++)
            {
                _slotsView.Add(_entries[i].Data);
            }
        }
    }
}
