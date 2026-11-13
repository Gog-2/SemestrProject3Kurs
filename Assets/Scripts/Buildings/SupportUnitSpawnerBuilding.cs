using System;
using UnityEngine;
using Alchemy.Inspector;
using Game.Core;
using Game.Units;
using Game.WaveSystem;

namespace Game.Buildings
{
    public class SupportUnitSpawnerBuilding : Building
    {
        [Serializable]
        public struct SupportLevelConfig
        {
            public int unitMaxHP;
            public int effectAmount;
            public float actionInterval;
            public float actionRange;
            public float roamRadius;
            public float respawnSeconds;
        }

        [Title("Support Spawner Config")]
        [SerializeField] private SupportUnitKind _unitKind = SupportUnitKind.Priest;
        [SerializeField] private Sprite _unitSprite;
        [SerializeField] private Color _unitTint = new(0.95f, 0.9f, 0.45f, 1f);
        [SerializeField] private SupportLevelConfig[] _levels = new SupportLevelConfig[4];

        private SupportUnit _activeUnit;
        private float _respawnTimer;

        public SupportLevelConfig CurrentSupportStats => _levels != null && _levels.Length > 0
            ? _levels[Mathf.Clamp(_currentLevel - 1, 0, _levels.Length - 1)]
            : default;

        public SupportUnit ActiveUnit => _activeUnit;

        public void ConfigureSupportSpawner(SupportUnitKind kind, Color tint, SupportLevelConfig[] levels, Sprite unitSprite = null)
        {
            _unitKind = kind;
            _unitTint = tint;
            _levels = levels;
            if (unitSprite != null) _unitSprite = unitSprite;
        }

        protected override void OnPlaced()
        {
            base.OnPlaced();
            SpawnUnit();
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (_activeUnit != null)
            {
                Destroy(_activeUnit.gameObject);
                _activeUnit = null;
            }
        }

        private void Update()
        {
            if (IsDead) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (_activeUnit != null && !_activeUnit.IsDead)
            {
                _respawnTimer = 0f;
                return;
            }

            if (WaveController.Instance != null)
            {
                WaveState ws = WaveController.Instance.CurrentState;
                if (ws != WaveState.WaveActive && ws != WaveState.WaveClearing) return;
            }

            if (_respawnTimer <= 0f)
            {
                float mult = RunProgressionManager.Instance != null
                    ? RunProgressionManager.Instance.GetEffectiveUnitRespawnMultiplier()
                    : 1f;
                _respawnTimer = Mathf.Max(1f, CurrentSupportStats.respawnSeconds * mult);
            }

            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0f)
            {
                SpawnUnit();
                _respawnTimer = 0f;
            }
        }

        private void SpawnUnit()
        {
            if (_activeUnit != null && !_activeUnit.IsDead) return;

            SupportLevelConfig cfg = CurrentSupportStats;
            var go = new GameObject($"{_unitKind}_{name}");
            go.transform.position = transform.position + new Vector3(0.5f, 0f, 0f);
            _activeUnit = go.AddComponent<SupportUnit>();
            _activeUnit.Initialize(
                _unitKind,
                this,
                transform.position,
                Mathf.Max(1, cfg.unitMaxHP),
                Mathf.Max(1, cfg.effectAmount),
                Mathf.Max(0.5f, cfg.actionInterval),
                Mathf.Max(0.75f, cfg.actionRange),
                Mathf.Max(1f, cfg.roamRadius),
                _unitTint,
                _unitSprite);
        }
    }
}
