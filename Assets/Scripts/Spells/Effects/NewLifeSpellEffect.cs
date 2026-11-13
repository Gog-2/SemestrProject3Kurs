using System.Collections.Generic;
using UnityEngine;
using Game.Enemies;
using Game.Units;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "SpellEffect_NewLife", menuName = "Game/Spells/Effects/New Life")]
    public class NewLifeSpellEffect : SpellEffect
    {
        public override void Cast(SpellLevelStats stats, float dmgMult, Vector3 targetPos)
        {
            float sqrRadius = stats.areaRadius * stats.areaRadius;
            var enemies = Enemy.ActiveEnemies;
            var toPolymorph = new List<Enemy>(8);

            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy e = enemies[i];
                if (e == null || e.IsDead || e.IsRetreating) continue;
                if (e.Data != null && e.Data.IsBoss) continue; // Bosses immune to polymorph
                if ((e.transform.position - targetPos).sqrMagnitude <= sqrRadius)
                {
                    toPolymorph.Add(e);
                }
            }

            for (int i = 0; i < toPolymorph.Count; i++)
            {
                Enemy e = toPolymorph[i];
                Vector3 pos = e.transform.position;
                e.PolymorphOut();
                DecoySheep.Spawn(pos, hp: 40, tauntRadius: 3f);
            }
        }
    }
}
