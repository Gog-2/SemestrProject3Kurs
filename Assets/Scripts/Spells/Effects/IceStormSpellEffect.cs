using UnityEngine;
using Game.Projectiles;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "SpellEffect_IceStorm", menuName = "Game/Spells/Effects/Ice Storm")]
    public class IceStormSpellEffect : SpellEffect
    {
        public override void Cast(SpellLevelStats stats, float dmgMult, Vector3 targetPos)
        {
            HazardZone.Spawn(
                targetPos,
                HazardKind.IceStorm,
                stats.areaRadius,
                stats.durationSeconds,
                stats.secondaryDps * dmgMult,
                slowPercent: 0.2f,
                color: new Color(0.4f, 0.85f, 1f, 0.42f),
                freezeThreshold: Mathf.Max(1f, stats.freezeThreshold));
        }
    }
}
