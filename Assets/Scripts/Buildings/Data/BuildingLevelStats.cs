using System;
using UnityEngine;

namespace Game.Buildings
{
    [Serializable]
    public struct BuildingLevelStats
    {
        [Tooltip("Coins cost to place or upgrade to this level")]
        public int cost;

        [Tooltip("Health points of the building")]
        public int maxHP;

        [Tooltip("Damage dealt per attack")]
        public int damage;

        [Tooltip("Attack range in grid cells")]
        public float attackRange;

        [Tooltip("Cooldown between attacks in seconds")]
        public float attackInterval;

        [Tooltip("Seconds between generating currency (Economy)")]
        public float productionInterval;

        [Tooltip("Amount of currency generated")]
        public int productionAmount;

        [Tooltip("Seconds to respawn unit after death")]
        public float spawnInterval;

        [Tooltip("Max active units")]
        public int unitCount;

        [Tooltip("Combat XP needed for discount")]
        public int combatXpRequirement;
    }
}
