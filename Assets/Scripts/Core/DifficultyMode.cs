namespace Game.Core
{
    public enum DifficultyMode
    {
        Base = 0,
        Hard = 1,
        Legendary = 2
    }

    public static class DifficultyModifiers
    {
        public static float GetEnemyDamageMultiplier(DifficultyMode mode)
        {
            return mode switch
            {
                DifficultyMode.Hard => 1.25f,
                DifficultyMode.Legendary => 1.35f,
                _ => 1f
            };
        }

        public static float GetEnemySpeedMultiplier(DifficultyMode mode)
        {
            return mode switch
            {
                DifficultyMode.Hard => 1.25f,
                DifficultyMode.Legendary => 1.35f,
                _ => 1f
            };
        }

        public static float GetCoinDropMultiplier(DifficultyMode mode)
        {
            return mode switch
            {
                DifficultyMode.Legendary => 0.75f,
                _ => 1f
            };
        }

        public static string GetDisplayName(DifficultyMode mode)
        {
            return mode switch
            {
                DifficultyMode.Hard => "Усложнённая",
                DifficultyMode.Legendary => "Легендарная",
                _ => "Базовая"
            };
        }

        public static string GetDescription(DifficultyMode mode)
        {
            return mode switch
            {
                DifficultyMode.Hard => "Урон врагов +25%, скорость врагов +25%",
                DifficultyMode.Legendary => "Урон врагов +35%, скорость врагов +35%, выпадение монет -25%",
                _ => "Стандартный баланс волны и наград"
            };
        }
    }
}
