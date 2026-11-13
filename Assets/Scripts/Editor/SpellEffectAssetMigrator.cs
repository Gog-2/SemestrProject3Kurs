using UnityEditor;
using Game.Spells;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static class SpellEffectAssetMigrator
    {
        static SpellEffectAssetMigrator()
        {
            EditorApplication.delayCall += EnsureAllSpellEffectsLinked;
        }

        public static void EnsureAllSpellEffectsLinked()
        {
            bool changed = false;
            changed |= LinkEffect<MeteorSpellEffect>("Assets/ScriptbleObjects/Spells/Spell_Meteor.asset", "SpellEffect_Meteor");
            changed |= LinkEffect<IceStormSpellEffect>("Assets/ScriptbleObjects/Spells/Spell_IceStorm.asset", "SpellEffect_IceStorm");
            changed |= LinkEffect<HeavenlyWrathSpellEffect>("Assets/ScriptbleObjects/Spells/Spell_HeavenlyWrath.asset", "SpellEffect_HeavenlyWrath");
            changed |= LinkEffect<DeadLegionSpellEffect>("Assets/ScriptbleObjects/Spells/Spell_DeadLegion.asset", "SpellEffect_DeadLegion");
            changed |= LinkEffect<NewLifeSpellEffect>("Assets/ScriptbleObjects/Spells/Spell_NewLife.asset", "SpellEffect_NewLife");

            if (changed)
            {
                AssetDatabase.SaveAssets();
            }
        }

        private static bool LinkEffect<TEffect>(string spellAssetPath, string effectAssetName)
            where TEffect : SpellEffect
        {
            SpellData spell = AssetDatabase.LoadAssetAtPath<SpellData>(spellAssetPath);
            if (spell == null || spell.Effect != null)
            {
                return false;
            }

            TEffect effect = GigaAssetUtility.GetOrCreateSO<TEffect>($"Assets/ScriptbleObjects/Spells/Effects/{effectAssetName}.asset");
            var so = new SerializedObject(spell);
            SerializedProperty prop = so.FindProperty("_effect");
            if (prop == null)
            {
                return false;
            }

            prop.objectReferenceValue = effect;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(spell);
            return true;
        }
    }
}
