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
        public List<string> UnlockedKnightIds = new() { "hador_oplot" };
        public List<string> UnlockedArenaIds = new() { "arena_plains" };
        public List<string> UnlockedWeaponIds = new() { "heavy_ballista" };
        public List<DifficultyMode> UnlockedDifficulties = new() { DifficultyMode.Base };
        public List<MetaBonusEntry> PurchasedBonuses = new();

        public string SelectedKnightId = "hador_oplot";
        public string SelectedArenaId = "arena_plains";
        public string SelectedWeaponId = "heavy_ballista";
        public DifficultyMode SelectedDifficulty = DifficultyMode.Base;
    }
}
