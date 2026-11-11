using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Enemies;

namespace Game.Projectiles
{
    public enum HazardKind
    {
        FireBurn = 0,
        TarSlow = 1,
        IceStorm = 2,
        DeadLegion = 3,
        OrthancVortex = 4
    }

    public class HazardZone : MonoBehaviour
    {
        private HazardKind _kind;
        private float _radius;
        private float _durationRemaining;
        private float _dps;
        private float _slowPercent;
        private float _freezeThreshold;
        private float _pullSpeed;
        private float _spawnRatePerSec;
        private float _tickTimer;
        private float _spawnAccum;
        private float _dotAccum;
        private bool _isActive;

        private SpriteRenderer _renderer;
        private Action<Vector3> _skeletonSpawnCallback;
        private Action<Enemy> _enemyKilledCallback;
        private readonly Dictionary<Enemy, float> _iceExposure = new();
        private readonly List<Enemy> _cleanupList = new();

        private static Sprite s_softCircleSprite;

        public HazardKind Kind => _kind;
        public float Radius => _radius;
        public float DurationRemaining => _durationRemaining;

        public static HazardZone Spawn(
            Vector3 worldPosition,
            HazardKind kind,
            float radius,
            float duration,
            float dps,
            float slowPercent,
            Color color,
            float freezeThreshold = 2f,
            float pullSpeed = 1.6f,
            float spawnRatePerSec = 0f,
            Action<Vector3> onSpawnSkeleton = null,
            Action<Enemy> onEnemyKilledInside = null)
        {
            var go = new GameObject($"HazardZone_{kind}");
            go.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);

            var zone = go.AddComponent<HazardZone>();
            zone.Initialize(
                kind,
                radius,
                duration,
                dps,
                slowPercent,
                color,
                freezeThreshold,
                pullSpeed,
                spawnRatePerSec,
                onSpawnSkeleton,
                onEnemyKilledInside);
            return zone;
        }

        private void Initialize(
            HazardKind kind,
            float radius,
            float duration,
            float dps,
            float slowPercent,
            Color color,
            float freezeThreshold,
            float pullSpeed,
            float spawnRatePerSec,
            Action<Vector3> onSpawnSkeleton,
            Action<Enemy> onEnemyKilledInside)
        {
            _kind = kind;
            _radius = Mathf.Max(0.25f, radius);
            _durationRemaining = Mathf.Max(0.1f, duration);
            _dps = Mathf.Max(0f, dps);
            _slowPercent = Mathf.Clamp01(slowPercent);
            _freezeThreshold = Mathf.Max(0.25f, freezeThreshold);
            _pullSpeed = Mathf.Max(0f, pullSpeed);
            _spawnRatePerSec = Mathf.Max(0f, spawnRatePerSec);
            _skeletonSpawnCallback = onSpawnSkeleton;
            _enemyKilledCallback = onEnemyKilledInside;
            _tickTimer = 0f;
            _spawnAccum = 0f;
            _dotAccum = 0f;
            _iceExposure.Clear();

            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = GetOrCreateCircleSprite();
            _renderer.color = color;
            _renderer.sortingLayerName = "Effects";
            _renderer.sortingOrder = 2;

            float diameter = _radius * 2f;
            transform.localScale = new Vector3(diameter, diameter, 1f);
            _isActive = true;
        }

        private void Update()
        {
            if (!_isActive) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            float dt = Time.deltaTime;
            _durationRemaining -= dt;
            if (_durationRemaining <= 0f)
            {
                _isActive = false;
                Destroy(gameObject);
                return;
            }

            if (_kind == HazardKind.OrthancVortex)
            {
                TickVortexPull(dt);
                _renderer.transform.Rotate(0f, 0f, -90f * dt);
            }

            if (_kind == HazardKind.IceStorm)
            {
                TickIceExposure(dt);
            }

            if (_kind == HazardKind.DeadLegion && _spawnRatePerSec > 0f && _skeletonSpawnCallback != null)
            {
                _spawnAccum += _spawnRatePerSec * dt;
                while (_spawnAccum >= 1f)
                {
                    _spawnAccum -= 1f;
                    Vector2 jitter = UnityEngine.Random.insideUnitCircle * (_radius * 0.55f);
                    _skeletonSpawnCallback.Invoke(transform.position + new Vector3(jitter.x, jitter.y, 0f));
                }
            }

            _tickTimer += dt;
            const float TickInterval = 0.25f;
            if (_tickTimer >= TickInterval)
            {
                float stepDt = _tickTimer;
                _tickTimer = 0f;
                ApplyPeriodicEffects(stepDt);
            }
        }

        private void TickVortexPull(float dt)
        {
            if (_pullSpeed <= 0.001f) return;

            float sqrRadius = _radius * _radius;
            Vector3 center = transform.position;
            var enemies = Enemy.ActiveEnemies;

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy e = enemies[i];
                if (e == null || e.IsDead || e.IsRetreating) continue;

                Vector3 offset = center - e.transform.position;
                float sqrDist = offset.sqrMagnitude;
                if (sqrDist <= sqrRadius && sqrDist > 0.02f)
                {
                    Vector3 pull = offset.normalized * (_pullSpeed * dt);
                    e.transform.position += pull;
                }
            }
        }

        private void TickIceExposure(float dt)
        {
            float sqrRadius = _radius * _radius;
            Vector3 center = transform.position;
            var enemies = Enemy.ActiveEnemies;

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy e = enemies[i];
                if (e == null || e.IsDead || e.IsRetreating) continue;

                bool inside = (e.transform.position - center).sqrMagnitude <= sqrRadius;
                if (inside)
                {
                    _iceExposure.TryGetValue(e, out float exp);
                    exp += dt;
                    _iceExposure[e] = exp;
                    if (exp >= _freezeThreshold)
                    {
                        e.Freeze(Mathf.Max(1.5f, _durationRemaining));
                    }
                }
            }

            _cleanupList.Clear();
            foreach (var kv in _iceExposure)
            {
                if (kv.Key == null || kv.Key.IsDead) _cleanupList.Add(kv.Key);
            }
            for (int i = 0; i < _cleanupList.Count; i++)
            {
                _iceExposure.Remove(_cleanupList[i]);
            }
        }

        private void ApplyPeriodicEffects(float stepDt)
        {
            float sqrRadius = _radius * _radius;
            Vector3 center = transform.position;
            var enemies = Enemy.ActiveEnemies;

            int tickDamage = 0;
            if (_dps > 0f)
            {
                _dotAccum += _dps * stepDt;
                tickDamage = Mathf.FloorToInt(_dotAccum);
                if (tickDamage > 0)
                {
                    _dotAccum -= tickDamage;
                }
            }

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy e = enemies[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - center).sqrMagnitude > sqrRadius) continue;

                if (_slowPercent > 0f)
                {
                    e.ApplySlow(_slowPercent, 0.5f);
                }

                if (tickDamage > 0)
                {
                    e.TakeDamage(tickDamage);
                    if (e.IsDead)
                    {
                        _enemyKilledCallback?.Invoke(e);
                    }
                }
            }
        }

        public static Sprite GetOrCreateCircleSprite()
        {
            if (s_softCircleSprite != null) return s_softCircleSprite;

            const int Size = 64;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "ProceduralCircleSprite",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float half = (Size - 1) * 0.5f;
            float invRadius = 1f / half;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = (x - half) * invRadius;
                    float dy = (y - half) * invRadius;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = d <= 1f ? Mathf.Clamp01((1f - d) * 4f) : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            s_softCircleSprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
            return s_softCircleSprite;
        }
    }
}
