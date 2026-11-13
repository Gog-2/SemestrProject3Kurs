using UnityEngine;
using Game.Projectiles;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "SpellEffect_Meteor", menuName = "Game/Spells/Effects/Meteor")]
    public class MeteorSpellEffect : SpellEffect
    {
        public override void Cast(SpellLevelStats stats, float dmgMult, Vector3 targetPos)
        {
            int impactDmg = Mathf.Max(1, Mathf.RoundToInt(stats.primaryDamage * dmgMult));
            float delay = Mathf.Max(0.5f, stats.delaySeconds);
            Vector3 skyOrigin = targetPos + new Vector3(-3f, 8f, 0f);

            HazardZone.Spawn(
                targetPos,
                HazardKind.FireBurn,
                stats.areaRadius,
                delay,
                dps: 0f,
                slowPercent: 0f,
                color: new Color(1f, 0.22f, 0.1f, 0.22f));

            Projectile_Lobbed.Spawn(
                skyOrigin,
                targetPos,
                flightDuration: delay,
                arcHeight: 1.5f,
                tint: new Color(1f, 0.45f, 0.1f, 1f),
                sprite: null,
                visualScale: new Vector3(0.85f, 0.85f, 1f),
                impactDamage: impactDmg,
                splashRadius: stats.areaRadius,
                edgeDamageMultiplier: 1f,
                knockbackCells: 0.75f,
                singleTargetImpact: false,
                onImpact: pos =>
                {
                    HazardZone.Spawn(
                        pos,
                        HazardKind.FireBurn,
                        stats.areaRadius,
                        stats.durationSeconds,
                        stats.secondaryDps * dmgMult,
                        slowPercent: 0f,
                        color: new Color(1f, 0.36f, 0.08f, 0.45f));
                });
        }
    }
}
