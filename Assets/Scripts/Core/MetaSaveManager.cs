using System;
using UnityEngine;

namespace Game.Core
{
    public static class MetaSaveManager
    {
        private const string MetaKey = "GIGA_MetaProgression_Save";
        private const string MusicVolKey = "GIGA_Audio_MusicVolume";
        private const string SfxVolKey = "GIGA_Audio_SFXVolume";

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

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicVolKey, 0.7f);
            set
            {
                PlayerPrefs.SetFloat(MusicVolKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
                ApplyAudioVolumes();
                AudioSettingsChanged?.Invoke();
            }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxVolKey, 0.8f);
            set
            {
                PlayerPrefs.SetFloat(SfxVolKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
                ApplyAudioVolumes();
                AudioSettingsChanged?.Invoke();
            }
        }

        public static void ApplyAudioVolumes()
        {
            AudioListener.volume = Mathf.Clamp01(Mathf.Max(MusicVolume, SfxVolume));
        }

        public static event Action DiamondsChanged;
        public static event Action UnlocksChanged;
        public static event Action BonusesChanged;
        public static event Action AudioSettingsChanged;
        public static event Action<DifficultyMode> SelectedDifficultyChanged;

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
            _data.UnlockedKnightIds ??= new System.Collections.Generic.List<string>();
            _data.UnlockedArenaIds ??= new System.Collections.Generic.List<string>();
            _data.UnlockedWeaponIds ??= new System.Collections.Generic.List<string>();
            _data.UnlockedDifficulties ??= new System.Collections.Generic.List<DifficultyMode>();
            _data.PurchasedBonuses ??= new System.Collections.Generic.List<MetaBonusEntry>();

            if (!_data.UnlockedKnightIds.Contains("hador_oplot")) _data.UnlockedKnightIds.Add("hador_oplot");
            if (!_data.UnlockedArenaIds.Contains("arena_plains")) _data.UnlockedArenaIds.Add("arena_plains");
            if (!_data.UnlockedWeaponIds.Contains("heavy_ballista")) _data.UnlockedWeaponIds.Add("heavy_ballista");
            if (!_data.UnlockedDifficulties.Contains(DifficultyMode.Base)) _data.UnlockedDifficulties.Add(DifficultyMode.Base);

            if (!_data.UnlockedDifficulties.Contains(_data.SelectedDifficulty))
            {
                _data.SelectedDifficulty = DifficultyMode.Base;
            }
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

        public static bool IsDifficultyUnlocked(DifficultyMode mode)
        {
            return mode == DifficultyMode.Base ||
                   (Data.UnlockedDifficulties != null && Data.UnlockedDifficulties.Contains(mode));
        }

        public static void SetSelectedDifficulty(DifficultyMode mode)
        {
            if (!IsDifficultyUnlocked(mode)) return;
            if (Data.SelectedDifficulty == mode) return;

            Data.SelectedDifficulty = mode;
            Save();
            SelectedDifficultyChanged?.Invoke(mode);
        }

        public static bool UnlockNextDifficultyAfterVictory(DifficultyMode clearedMode)
        {
            DifficultyMode? nextMode = clearedMode switch
            {
                DifficultyMode.Base => DifficultyMode.Hard,
                DifficultyMode.Hard => DifficultyMode.Legendary,
                _ => null
            };

            if (!nextMode.HasValue || IsDifficultyUnlocked(nextMode.Value))
            {
                return false;
            }

            Data.UnlockedDifficulties.Add(nextMode.Value);
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

        public static int GetBonusLevel(string bonusId)
        {
            if (string.IsNullOrEmpty(bonusId) || Data.PurchasedBonuses == null) return 0;

            for (int i = 0; i < Data.PurchasedBonuses.Count; i++)
            {
                if (Data.PurchasedBonuses[i] != null && Data.PurchasedBonuses[i].Id == bonusId)
                {
                    return Data.PurchasedBonuses[i].Level;
                }
            }

            return 0;
        }

        public static bool TryUpgradeBonus(string bonusId)
        {
            MetaBonusDefinition def = MetaBonusCatalog.FindById(bonusId);
            if (def == null) return false;

            int currentLevel = GetBonusLevel(bonusId);
            if (currentLevel >= def.MaxLevel) return false;

            int cost = def.GetCostForNextLevel(currentLevel);
            if (cost < 0 || !TrySpendDiamonds(cost)) return false;

            Data.PurchasedBonuses ??= new System.Collections.Generic.List<MetaBonusEntry>();
            MetaBonusEntry existing = null;
            for (int i = 0; i < Data.PurchasedBonuses.Count; i++)
            {
                if (Data.PurchasedBonuses[i] != null && Data.PurchasedBonuses[i].Id == bonusId)
                {
                    existing = Data.PurchasedBonuses[i];
                    break;
                }
            }

            if (existing != null)
            {
                existing.Level = currentLevel + 1;
            }
            else
            {
                Data.PurchasedBonuses.Add(new MetaBonusEntry { Id = bonusId, Level = 1 });
            }

            Save();
            BonusesChanged?.Invoke();
            return true;
        }
    }
}
