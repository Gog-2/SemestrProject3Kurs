using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Enemies;
using Game.Projectiles;
using Game.Units;
using Game.WaveSystem;

namespace Game.Spells
{
    public class SpellCaster : MonoBehaviour
    {
        [SerializeField] private Camera _mainCamera;

        private LineRenderer _previewCircle;
        private Material _previewMaterial;

        private void Awake()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.PrimaryClicked += OnPrimaryClicked;
                InputManager.Instance.SecondaryClicked += OnCancelInput;
                InputManager.Instance.CancelPressed += OnCancelInput;
            }
        }

        private void OnDisable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.PrimaryClicked -= OnPrimaryClicked;
                InputManager.Instance.SecondaryClicked -= OnCancelInput;
                InputManager.Instance.CancelPressed -= OnCancelInput;
            }
            SetPreviewVisible(false);
        }

        private void OnDestroy()
        {
            if (_previewMaterial != null) Destroy(_previewMaterial);
        }

        private void Update()
        {
            SpellScrollManager mgr = SpellScrollManager.Instance;
            if (mgr == null || !mgr.HasSelection ||
                (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing))
            {
                SetPreviewVisible(false);
                return;
            }

            if (InputManager.Instance == null) return;
            Vector3 worldPos = InputManager.Instance.GetPointerWorldPosition(_mainCamera);
            worldPos.z = 0f;

            SpellScrollInstance scroll = mgr.Scrolls[mgr.SelectedIndex];
            float radius = Mathf.Max(0.75f, scroll.Data.GetStats(scroll.Level).areaRadius);
            UpdatePreviewCircle(worldPos, radius, scroll.Data.AccentColor);
        }

        private void OnCancelInput()
        {
            SpellScrollManager.Instance?.CancelSelection();
        }

        private void OnPrimaryClicked(Vector2 screenPos)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            SpellScrollManager mgr = SpellScrollManager.Instance;
            if (mgr == null || !mgr.HasSelection) return;
            if (InputManager.Instance != null && InputManager.Instance.IsPointerOverUI(screenPos)) return;

            if (WaveController.Instance != null)
            {
                WaveState ws = WaveController.Instance.CurrentState;
                if (ws != WaveState.WaveActive && ws != WaveState.WaveClearing)
                {
                    return;
                }
            }

            Vector3 targetPos = InputManager.Instance != null
                ? InputManager.Instance.GetPointerWorldPosition(_mainCamera)
                : Vector3.zero;
            targetPos.z = 0f;

            if (mgr.TryConsumeSelectedScroll(out SpellScrollInstance scroll))
            {
                CastSpell(scroll.Data, scroll.Level, targetPos);
            }
        }

        public void CastSpell(SpellData data, int level, Vector3 targetWorldPos)
        {
            if (data == null) return;
            SpellLevelStats stats = data.GetStats(level);
            float dmgMult = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveSpellDamageMultiplier()
                : 1f;

            switch (data.Kind)
            {
                case SpellKind.Meteor:
                    CastMeteor(stats, dmgMult, targetWorldPos);
                    break;

                case SpellKind.IceStorm:
                    HazardZone.Spawn(
                        targetWorldPos,
                        HazardKind.IceStorm,
                        stats.areaRadius,
                        stats.durationSeconds,
                        stats.secondaryDps * dmgMult,
                        slowPercent: 0.2f,
                        color: new Color(0.4f, 0.85f, 1f, 0.42f),
                        freezeThreshold: Mathf.Max(1f, stats.freezeThreshold));
                    break;

                case SpellKind.HeavenlyWrath:
                    CastHeavenlyWrath(stats, dmgMult, targetWorldPos);
                    break;

                case SpellKind.DeadLegion:
                    HazardZone.Spawn(
                        targetWorldPos,
                        HazardKind.DeadLegion,
                        stats.areaRadius,
                        stats.durationSeconds,
                        stats.secondaryDps * dmgMult,
                        slowPercent: 0f,
                        color: new Color(0.28f, 0.85f, 0.45f, 0.42f),
                        freezeThreshold: 2f,
                        pullSpeed: 0f,
                        spawnRatePerSec: stats.spawnRatePerSec,
                        onSpawnSkeleton: pos => AllyUnit.SpawnTemporarySkeleton(pos, 20, 50, 3f),
                        onEnemyKilledInside: dead => AllyUnit.SpawnTemporarySkeleton(dead.transform.position, 20, 50, 3f));
                    break;

                case SpellKind.NewLife:
                    CastPolymorph(stats.areaRadius, targetWorldPos);
                    break;
            }
        }

        private void CastMeteor(SpellLevelStats stats, float dmgMult, Vector3 targetPos)
        {
            int impactDmg = Mathf.Max(1, Mathf.RoundToInt(stats.primaryDamage * dmgMult));
            float delay = Mathf.Max(0.5f, stats.delaySeconds);
            Vector3 skyOrigin = targetPos + new Vector3(-3f, 8f, 0f);

            // Warning telegraph ring while falling
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

        private void CastHeavenlyWrath(SpellLevelStats stats, float dmgMult, Vector3 targetPos)
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

            // Chain lightning to up to 3 nearby enemies within 3.5 cells of zone center
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

        private static void CastPolymorph(float radius, Vector3 targetPos)
        {
            float sqrRadius = radius * radius;
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

        private void EnsurePreviewCircle()
        {
            if (_previewCircle != null) return;

            var go = new GameObject("SpellPreviewCircle");
            go.transform.SetParent(transform, false);
            _previewCircle = go.AddComponent<LineRenderer>();
            _previewCircle.useWorldSpace = false;
            _previewCircle.loop = true;
            _previewCircle.positionCount = 36;
            _previewCircle.startWidth = 0.06f;
            _previewCircle.endWidth = 0.06f;
            _previewCircle.sortingLayerName = "Effects";
            _previewCircle.sortingOrder = 102;

            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader != null)
            {
                _previewMaterial = new Material(shader);
                _previewCircle.material = _previewMaterial;
            }
        }

        private void UpdatePreviewCircle(Vector3 center, float radius, Color color)
        {
            EnsurePreviewCircle();
            _previewCircle.enabled = true;
            _previewCircle.transform.position = center;
            _previewCircle.startColor = color;
            _previewCircle.endColor = color;

            int count = _previewCircle.positionCount;
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                _previewCircle.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        private void SetPreviewVisible(bool visible)
        {
            if (_previewCircle != null)
            {
                _previewCircle.enabled = visible;
            }
        }
    }
}
