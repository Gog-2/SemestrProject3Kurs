using System;
using System.Collections.Generic;

namespace Game.Core
{
    [Serializable]
    public class MetaBonusEntry
    {
        public string Id;
        public int Level;
    }

    [Serializable]
    public class MetaProgressionData
    {
        public int Diamonds = 0;
        public List<string> UnlockedKnightIds = new() { GameConstants.DefaultKnightId };
        public List<string> UnlockedArenaIds = new() { GameConstants.DefaultArenaId };
        public List<string> UnlockedWeaponIds = new() { GameConstants.DefaultWeaponId };
        public List<DifficultyMode> UnlockedDifficulties = new() { DifficultyMode.Base };
        public List<MetaBonusEntry> PurchasedBonuses = new();

        public string SelectedKnightId = GameConstants.DefaultKnightId;
        public string SelectedArenaId = GameConstants.DefaultArenaId;
        public string SelectedWeaponId = GameConstants.DefaultWeaponId;
        public DifficultyMode SelectedDifficulty = DifficultyMode.Base;
    }
}
