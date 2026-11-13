using UnityEngine;
using Alchemy.Inspector;
using Game.Core;
using Game.Spells;
using Game.WaveSystem;

namespace Game.Buildings
{
    public class MageTowerBuilding : Building
    {
        [Title("Mage Tower Config (GIG-94 / GIG-95)")]
        [SerializeField] private SpellData _spellData;
        [SerializeField] private float[] _craftCooldowns = new float[4] { 22f, 19f, 16f, 13f };

        private float _elapsed;
        private float _progress01;
        private bool _isFrozenAtCap;
        private bool _warnedMissingScrollManager;

        public SpellData SpellData => _spellData;
        public float Progress01 => _progress01;
        public bool IsFrozenAtCap => _isFrozenAtCap;

        public void ConfigureMageTower(SpellData spellData, float[] craftCooldowns = null)
        {
            _spellData = spellData;
            if (craftCooldowns != null && craftCooldowns.Length == 4)
            {
                _craftCooldowns = craftCooldowns;
            }
        }

        public float GetEffectiveCraftDuration()
        {
            int idx = Mathf.Clamp(_currentLevel - 1, 0, _craftCooldowns.Length - 1);
            float baseCd = Mathf.Max(5f, _craftCooldowns[idx]);

            float knightMult = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveMageCooldownMultiplier()
                : 1f;

            SupportAuraBuilding.GetLibraryBuffsFor(this, out _, out float librarySpeedBonus);
            return (baseCd * knightMult) / Mathf.Max(0.25f, 1f + librarySpeedBonus);
        }

        private void Update()
        {
            if (IsDead || _spellData == null) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (WaveController.Instance != null)
            {
                WaveState ws = WaveController.Instance.CurrentState;
                if (ws != WaveState.WaveActive && ws != WaveState.WaveClearing) return;
            }

            float duration = GetEffectiveCraftDuration();
            if (_progress01 < 1f)
            {
                _elapsed += Time.deltaTime;
                _progress01 = Mathf.Clamp01(_elapsed / duration);
            }

            if (_progress01 >= 1f)
            {
                SpellScrollManager mgr = SpellScrollManager.Instance;
                if (mgr == null)
                {
                    if (!_warnedMissingScrollManager)
                    {
                        _warnedMissingScrollManager = true;
                        Debug.LogWarning($"[{nameof(MageTowerBuilding)}] SpellScrollManager.Instance is null; crafted scroll cannot be added.", this);
                    }
                    return;
                }

                if (mgr.IsFull)
                {
                    // GIG-95: Freeze crafting progress at 100% until a scroll slot frees up
                    _isFrozenAtCap = true;
                    _progress01 = 1f;
                    return;
                }

                if (mgr.TryAddScroll(_spellData, _currentLevel))
                {
                    _isFrozenAtCap = false;
                    _elapsed = 0f;
                    _progress01 = 0f;
                    AddCombatXP(30);
                }
            }
        }

        // GIG-95: Cap placed Mage Towers to MaxScrollSlots (5)
        public static bool CanPlaceMageTower(BuildingData data, Vector2Int _)
        {
            if (data == null || data.Category != BuildingCategory.Mages) return true;

            int activeMageTowers = 0;
            var active = Building.ActiveBuildings;
            for (int i = 0; i < active.Count; i++)
            {
                Building b = active[i];
                if (b == null || b.IsDead || !b.gameObject.activeInHierarchy || b.Data == null) continue;
                if (b.Data.Category == BuildingCategory.Mages)
                {
                    activeMageTowers++;
                }
            }

            int maxSlots = SpellScrollManager.Instance != null
                ? SpellScrollManager.Instance.MaxScrolls
                : SpellScrollManager.MaxScrollSlots;
            return activeMageTowers < maxSlots;
        }
    }
}
