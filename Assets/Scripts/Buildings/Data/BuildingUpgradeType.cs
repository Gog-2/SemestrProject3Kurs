namespace Game.Buildings
{
    public enum BuildingUpgradeType
    {
        Local,  // Individual upgrade per building instance
        Global  // Upgrading one upgrades all existing and future buildings of this type (Siege, Control)
    }
}
