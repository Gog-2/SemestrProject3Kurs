using UnityEngine;
using Game.Projectiles;
using Game.Units;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "SpellEffect_DeadLegion", menuName = "Game/Spells/Effects/Dead Legion")]
    public class DeadLegionSpellEffect : SpellEffect
    {
        public override void Cast(SpellLevelStats stats, float dmgMult, Vector3 targetPos)
        {
            HazardZone.Spawn(
                targetPos,
                HazardKind.DeadLegion,
                stats.areaRadius,
                stats.durationSeconds,
                stats.secondaryDps * dmgMult,
                slowPercent: 0f,
                color: new Color(0.28f, 0.85f, 0.45f, 0.42f),
                freezeThreshold: 2f,
                pullSpeed: 0f,
                spawnRatePerSec: stats.spawnRatePerSec,
                onSpawnSkeleton: pos => AllyUnit.SpawnTemporarySkeleton(pos, 20, 50, 3f),
                onEnemyKilledInside: dead => AllyUnit.SpawnTemporarySkeleton(dead.transform.position, 20, 50, 3f));
        }
    }
}
