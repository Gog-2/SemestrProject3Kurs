using UnityEngine;

namespace Game.Spells
{
    public abstract class SpellEffect : ScriptableObject
    {
        public abstract void Cast(SpellLevelStats stats, float dmgMult, Vector3 targetPos);
    }
}
