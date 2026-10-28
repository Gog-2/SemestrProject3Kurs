using System;
using UnityEngine;

namespace Game.Core
{
    public static class MetaSaveManager
    {
        private const string MetaKey = "GIGA_MetaProgression_Save";
        private static MetaProgressionData _data;

        public static MetaProgressionData Data
        {
            get
            {
                if (_data == null)
                {
                    Load();
                }
                return _data;
            }
        }

        public static event Action DiamondsChanged;
        public static event Action UnlocksChanged;

        public static void Load()
        {
            if (PlayerPrefs.HasKey(MetaKey))
            {
                try
                {
                    string json = PlayerPrefs.GetString(MetaKey);
                    _data = JsonUtility.FromJson<MetaProgressionData>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MetaSaveManager] Failed to parse meta save: {e.Message}");
                    _data = new MetaProgressionData();
                }
            }
            else
            {
                _data = new MetaProgressionData();
            }

            // Ensure default unlocks
            if (!_data.UnlockedKnightIds.Contains("hador_oplot")) _data.UnlockedKnightIds.Add("hador_oplot");
            if (!_data.UnlockedArenaIds.Contains("arena_plains")) _data.UnlockedArenaIds.Add("arena_plains");
            if (!_data.UnlockedWeaponIds.Contains("heavy_ballista")) _data.UnlockedWeaponIds.Add("heavy_ballista");
        }

        public static void Save()
        {
            if (_data == null) return;
            string json = JsonUtility.ToJson(_data);
            PlayerPrefs.SetString(MetaKey, json);
            PlayerPrefs.Save();
        }

        public static void AddDiamonds(int amount)
        {
            if (amount <= 0) return;
            Data.Diamonds += amount;
            Save();
            DiamondsChanged?.Invoke();
        }

        public static bool TrySpendDiamonds(int amount)
        {
            if (amount <= 0) return true;
            if (Data.Diamonds < amount) return false;

            Data.Diamonds -= amount;
            Save();
            DiamondsChanged?.Invoke();
            return true;
        }

        public static bool IsKnightUnlocked(string knightId) => Data.UnlockedKnightIds.Contains(knightId);
        public static bool IsArenaUnlocked(string arenaId) => Data.UnlockedArenaIds.Contains(arenaId);
        public static bool IsWeaponUnlocked(string weaponId) => Data.UnlockedWeaponIds.Contains(weaponId);

        public static bool TryUnlockKnight(string knightId, int cost)
        {
            if (IsKnightUnlocked(knightId)) return true;
            if (!TrySpendDiamonds(cost)) return false;

            Data.UnlockedKnightIds.Add(knightId);
            Save();
            UnlocksChanged?.Invoke();
            return true;
        }

        public static bool TryUnlockArena(string arenaId, int cost)
        {
            if (IsArenaUnlocked(arenaId)) return true;
            if (!TrySpendDiamonds(cost)) return false;

            Data.UnlockedArenaIds.Add(arenaId);
            Save();
            UnlocksChanged?.Invoke();
            return true;
        }

        public static bool TryUnlockWeapon(string weaponId, int cost)
        {
            if (IsWeaponUnlocked(weaponId)) return true;
            if (!TrySpendDiamonds(cost)) return false;

            Data.UnlockedWeaponIds.Add(weaponId);
            Save();
            UnlocksChanged?.Invoke();
            return true;
        }

        public static void SetSelected(string knightId, string arenaId, string weaponId)
        {
            if (!string.IsNullOrEmpty(knightId)) Data.SelectedKnightId = knightId;
            if (!string.IsNullOrEmpty(arenaId)) Data.SelectedArenaId = arenaId;
            if (!string.IsNullOrEmpty(weaponId)) Data.SelectedWeaponId = weaponId;
            Save();
        }
    }
}
