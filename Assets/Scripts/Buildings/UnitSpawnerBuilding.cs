using System;
using System.Collections.Generic;
using UnityEngine;
using Alchemy.Inspector;
using Game.Core;
using Game.Units;
using Game.WaveSystem;

namespace Game.Buildings
{
    public class UnitSpawnerBuilding : Building
    {
        [Serializable]
        public struct LevelUnitConfig
        {
            public int unitCount;
            public int unitMaxHP;
            public int unitDamage;
            public float attackInterval;
            public float attackRange;
            public int aggroLimit;
            public float patrolRadius;
            public float respawnSeconds;
            public float knockbackCells;
            public int pierceCount;
            public int meleeDamage;
            public float meleeInterval;
            public float bleedDps;
            public float bleedDuration;
        }

        [Title("Unit Spawner Config")]
        [SerializeField] private Sprite _unitSprite;
        [SerializeField] private bool _isRanged;
        [SerializeField] private bool _isArcherUnit;
        [SerializeField] private bool _isBerserker;
        [SerializeField] private bool _usesStealth;
        [SerializeField] private float _stealthSeconds = 5f;
        [SerializeField] private Color _unitTint = new(0.3f, 0.78f, 0.95f, 1f);
        [SerializeField] private LevelUnitConfig[] _levels = new LevelUnitConfig[4];

        private readonly List<AllyUnit> _spawnedUnits = new(8);
        private float _respawnTimer;

        public LevelUnitConfig CurrentUnitConfig => _levels != null && _levels.Length > 0
            ? _levels[Mathf.Clamp(_currentLevel - 1, 0, _levels.Length - 1)]
            : default;

        public float EffectiveAttackInterval
        {
            get
            {
                float interval = Mathf.Max(0.25f, CurrentUnitConfig.attackInterval);
                float speedMult = RunProgressionManager.Instance != null
                    ? RunProgressionManager.Instance.GetEffectiveAttackSpeedMultiplier()
                    : 1f;
                return interval / Mathf.Max(0.2f, speedMult);
            }
        }

        public IReadOnlyList<AllyUnit> SpawnedUnits => _spawnedUnits;

        public void ConfigureSpawner(
            bool isRanged,
            bool isArcherUnit,
            bool isBerserker,
            bool usesStealth,
            float stealthSeconds,
            Color tint,
            LevelUnitConfig[] levels,
            Sprite unitSprite = null)
        {
            _isRanged = isRanged;
            _isArcherUnit = isArcherUnit;
            _isBerserker = isBerserker;
            _usesStealth = usesStealth;
            _stealthSeconds = stealthSeconds;
            _unitTint = tint;
            _levels = levels;
            if (unitSprite != null) _unitSprite = unitSprite;
        }

        protected override void OnPlaced()
        {
            base.OnPlaced();
            SpawnMissingUnitsToCount(CurrentUnitConfig.unitCount);
        }

        protected override void OnLevelUp(int newLevel)
        {
            base.OnLevelUp(newLevel);
            // GDD: Upgrading does NOT heal already-alive units; only spawns new slot count
            SpawnMissingUnitsToCount(CurrentUnitConfig.unitCount);
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            ClearSpawnedUnits();
        }

        private void Update()
        {
            if (IsDead) return;
            PruneDeadUnits();

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            bool combatActive = WaveController.Instance == null ||
                                WaveController.Instance.CurrentState == WaveState.WaveActive ||
                                WaveController.Instance.CurrentState == WaveState.WaveClearing;

            int targetCount = CurrentUnitConfig.unitCount;
            if (_spawnedUnits.Count >= targetCount)
            {
                _respawnTimer = 0f;
                return;
            }

            if (!combatActive)
            {
                // Keep units topped up in building phase cleanly? GDD: respawn during wave; pause auto-tops or respawns? Wait - respawn timer runs in combat, and between waves spawn missing cleanly on new wave start or fast refill!
                return;
            }

            if (_respawnTimer <= 0f)
            {
                float respawnMult = RunProgressionManager.Instance != null
                    ? RunProgressionManager.Instance.GetEffectiveUnitRespawnMultiplier()
                    : 1f;
                _respawnTimer = Mathf.Max(1f, CurrentUnitConfig.respawnSeconds * respawnMult);
            }

            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0f)
            {
                SpawnSingleUnit(_spawnedUnits.Count);
                _respawnTimer = 0f;
            }
        }

        private void PruneDeadUnits()
        {
            for (int i = _spawnedUnits.Count - 1; i >= 0; i--)
            {
                AllyUnit unit = _spawnedUnits[i];
                if (unit == null || unit.IsDead || !unit.gameObject.activeInHierarchy || unit.SpawnerBuilding != this)
                {
                    _spawnedUnits.RemoveAt(i);
                }
            }
        }

        private void OnSpawnedUnitDied(AllyUnit unit)
        {
            _spawnedUnits.Remove(unit);
        }

        private void SpawnMissingUnitsToCount(int targetCount)
        {
            PruneDeadUnits();
            while (_spawnedUnits.Count < targetCount)
            {
                SpawnSingleUnit(_spawnedUnits.Count);
            }
        }

        private void SpawnSingleUnit(int index)
        {
            LevelUnitConfig cfg = CurrentUnitConfig;
            float angle = index * 1.4f;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * 0.6f, Mathf.Sin(angle) * 0.6f, 0f);
            Vector3 spawnPos = transform.position + offset;

            string unitName = $"{(Data != null ? Data.Id : "Unit")}_{index}";
            AllyUnit unit = AllyUnitPool.GetOrCreate().Spawn(spawnPos, unitName);
            unit.Died += OnSpawnedUnitDied;
            unit.Initialize(
                this,
                transform.position,
                Mathf.Max(1, cfg.unitMaxHP),
                Mathf.Max(1, cfg.unitDamage),
                Mathf.Max(0.25f, cfg.attackInterval),
                Mathf.Max(0.75f, cfg.attackRange),
                Mathf.Max(1, cfg.aggroLimit),
                Mathf.Max(1f, cfg.patrolRadius),
                _isRanged,
                _isArcherUnit,
                _unitTint,
                cfg.knockbackCells,
                Mathf.Max(1, cfg.pierceCount),
                _usesStealth,
                _stealthSeconds,
                _isBerserker,
                Mathf.Max(1, cfg.meleeDamage),
                Mathf.Max(0.25f, cfg.meleeInterval),
                cfg.bleedDps,
                cfg.bleedDuration,
                false,
                0f,
                _unitSprite);

            _spawnedUnits.Add(unit);
        }

        private void ClearSpawnedUnits()
        {
            for (int i = _spawnedUnits.Count - 1; i >= 0; i--)
            {
                AllyUnit unit = _spawnedUnits[i];
                if (unit != null && unit.gameObject.activeInHierarchy && unit.SpawnerBuilding == this)
                {
                    unit.Died -= OnSpawnedUnitDied;
                    unit.Despawn();
                }
            }
            _spawnedUnits.Clear();
        }
    }
}
