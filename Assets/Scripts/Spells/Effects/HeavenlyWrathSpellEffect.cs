using System.Collections.Generic;
using UnityEngine;
using Game.Enemies;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "SpellEffect_HeavenlyWrath", menuName = "Game/Spells/Effects/Heavenly Wrath")]
    public class HeavenlyWrathSpellEffect : SpellEffect
    {
        public override void Cast(SpellLevelStats stats, float dmgMult, Vector3 targetPos)
        {
            int directDmg = Mathf.Max(1, Mathf.RoundToInt(stats.primaryDamage * dmgMult));
            int chainDmg = Mathf.Max(1, Mathf.RoundToInt(directDmg * 0.65f));
            float stunDur = Mathf.Max(1f, stats.durationSeconds);
            float shockDps = stats.secondaryDps * dmgMult;
            float sqrRadius = stats.areaRadius * stats.areaRadius;

            var hitEnemies = new List<Enemy>(8);
            var enemies = Enemy.ActiveEnemies;

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy e = enemies[i];
                if (e == null || e.IsDead || e.IsRetreating) continue;
                if ((e.transform.position - targetPos).sqrMagnitude <= sqrRadius)
                {
                    e.TakeDamage(directDmg);
                    if (!e.IsDead)
                    {
                        e.Stun(stunDur);
                        e.ApplyBurn(shockDps, stunDur);
                    }
                    hitEnemies.Add(e);
                }
            }

            // Chain lightning to up to 3 nearby enemies within 3 cells beyond zone radius
            int chains = 0;
            float chainSqr = (stats.areaRadius + 3f) * (stats.areaRadius + 3f);
            for (int i = enemies.Count - 1; i >= 0 && chains < 3; i--)
            {
                Enemy e = enemies[i];
                if (e == null || e.IsDead || e.IsRetreating || hitEnemies.Contains(e)) continue;
                if ((e.transform.position - targetPos).sqrMagnitude <= chainSqr)
                {
                    e.TakeDamage(chainDmg);
                    if (!e.IsDead)
                    {
                        e.Stun(stunDur * 0.7f);
                        e.ApplyBurn(shockDps, stunDur * 0.7f);
                    }
                    chains++;
                }
            }
        }
    }
}
