namespace Game.Upgrades
{
    public enum UpgradeRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public enum UpgradeEffectType
    {
        // Castle
        CastleMaxHealthPercent,
        CastleMaxHealthFlat,
        CastleHealPercent,
        BuildSlotAdd,

        // Combat
        RangedDamagePercent,
        MeleeDamagePercent,
        AttackSpeedPercent,
        WeaponDamagePercent,
        WeaponCooldownPercent,

        // Defense
        UnitHealthPercent,
        UnitRespawnPercent,
        BuildingHealthPercent,

        // Economy
        CoinPerKill,
        DiamondDropChanceAdd,
        EconomicSpeedPercent,

        // Control
        SlowOnHitPercent,
        TrapDamagePercent,
        BombDamagePercent
    }
}
