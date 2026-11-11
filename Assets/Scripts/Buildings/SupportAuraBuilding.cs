using System;
using System.Collections.Generic;
using UnityEngine;
using Alchemy.Inspector;
using Game.Core;

namespace Game.Buildings
{
    public enum SupportAuraMode
    {
        BannerMeleeAura = 0,
        LibraryOrthogonalBuff = 1
    }

    public class SupportAuraBuilding : Building
    {
        public static readonly List<SupportAuraBuilding> ActiveAuras = new(16);

        [Serializable]
        public struct AuraLevelConfig
        {
            public float damageBonusPercent;
            public float attackSpeedBonusPercent;
            public float auraRadius;
        }

        [Title("Aura Config")]
        [SerializeField] private SupportAuraMode _mode = SupportAuraMode.BannerMeleeAura;
        [SerializeField] private AuraLevelConfig[] _levels = new AuraLevelConfig[4];

        public SupportAuraMode Mode => _mode;

        public AuraLevelConfig CurrentAura => _levels != null && _levels.Length > 0
            ? _levels[Mathf.Clamp(_currentLevel - 1, 0, _levels.Length - 1)]
            : default;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!ActiveAuras.Contains(this))
            {
                ActiveAuras.Add(this);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ActiveAuras.Remove(this);
        }

        public void ConfigureAura(SupportAuraMode mode, AuraLevelConfig[] levels)
        {
            _mode = mode;
            _levels = levels;
        }

        public static void GetMeleeBuffsAt(Vector3 worldPos, out float dmgBonus, out float atkSpeedBonus)
        {
            dmgBonus = 0f;
            atkSpeedBonus = 0f;

            float supMult = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveSupportMultiplier()
                : 1f;

            for (int i = 0; i < ActiveAuras.Count; i++)
            {
                SupportAuraBuilding aura = ActiveAuras[i];
                if (aura == null || aura.IsDead || aura.Mode != SupportAuraMode.BannerMeleeAura) continue;

                AuraLevelConfig cfg = aura.CurrentAura;
                if (Vector2.Distance(worldPos, aura.transform.position) <= cfg.auraRadius)
                {
                    dmgBonus = Mathf.Max(dmgBonus, cfg.damageBonusPercent * supMult);
                    atkSpeedBonus = Mathf.Max(atkSpeedBonus, cfg.attackSpeedBonusPercent * supMult);
                }
            }
        }

        public static void GetLibraryBuffsFor(Building targetMageBuilding, out float dmgBonus, out float craftSpeedBonus)
        {
            dmgBonus = 0f;
            craftSpeedBonus = 0f;
            if (targetMageBuilding == null || targetMageBuilding.IsDead) return;

            float supMult = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveSupportMultiplier()
                : 1f;

            for (int i = 0; i < ActiveAuras.Count; i++)
            {
                SupportAuraBuilding aura = ActiveAuras[i];
                if (aura == null || aura.IsDead || aura.Mode != SupportAuraMode.LibraryOrthogonalBuff) continue;

                Vector2Int delta = targetMageBuilding.Origin - aura.Origin;
                if (!DoesLibraryAffectOffset(aura.CurrentLevel, delta)) continue;

                AuraLevelConfig cfg = aura.CurrentAura;
                dmgBonus += cfg.damageBonusPercent * supMult;
                craftSpeedBonus += cfg.attackSpeedBonusPercent * supMult;
            }
        }

        // L1: Left (-1,0); L2: Left+Right; L3: Left+Right+Up (0,+1); L4: Left+Right+Up+Down
        public static bool DoesLibraryAffectOffset(int libraryLevel, Vector2Int offset)
        {
            if (offset == new Vector2Int(-1, 0)) return libraryLevel >= 1;
            if (offset == new Vector2Int(1, 0)) return libraryLevel >= 2;
            if (offset == new Vector2Int(0, 1)) return libraryLevel >= 3;
            if (offset == new Vector2Int(0, -1)) return libraryLevel >= 4;
            return false;
        }
    }
}
