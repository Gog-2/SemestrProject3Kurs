using System;
using UnityEngine;
using Game.Core;
using Game.Enemies;

namespace Game.Projectiles
{
    public class Projectile_Lobbed : MonoBehaviour
    {
        private Vector3 _startPos;
        private Vector3 _targetPos;
        private float _duration;
        private float _elapsed;
        private float _arcHeight;
        private int _impactDamage;
        private float _splashRadius;
        private float _edgeDamageMultiplier;
        private float _knockbackCells;
        private bool _singleTargetImpact;
        private Action<Vector3> _onImpactCallback;
        private bool _isAlive;

        private SpriteRenderer _renderer;

        public static Projectile_Lobbed Spawn(
            Vector3 origin,
            Vector3 targetWorldPos,
            float flightDuration,
            float arcHeight,
            Color tint,
            Sprite sprite,
            Vector3 visualScale,
            int impactDamage,
            float splashRadius,
            float edgeDamageMultiplier = 1f,
            float knockbackCells = 0f,
            bool singleTargetImpact = false,
            Action<Vector3> onImpact = null)
        {
            var go = new GameObject("Projectile_Lobbed");
            go.transform.position = origin;

            var proj = go.AddComponent<Projectile_Lobbed>();
            proj.Initialize(
                origin,
                targetWorldPos,
                flightDuration,
                arcHeight,
                tint,
                sprite,
                visualScale,
                impactDamage,
                splashRadius,
                edgeDamageMultiplier,
                knockbackCells,
                singleTargetImpact,
                onImpact);
            return proj;
        }

        private void Initialize(
            Vector3 origin,
            Vector3 targetPos,
            float flightDuration,
            float arcHeight,
            Color tint,
            Sprite sprite,
            Vector3 visualScale,
            int impactDamage,
            float splashRadius,
            float edgeDamageMultiplier,
            float knockbackCells,
            bool singleTargetImpact,
            Action<Vector3> onImpact)
        {
            _startPos = new Vector3(origin.x, origin.y, 0f);
            _targetPos = new Vector3(targetPos.x, targetPos.y, 0f);
            _duration = Mathf.Max(0.05f, flightDuration);
            _elapsed = 0f;
            _arcHeight = Mathf.Max(0f, arcHeight);
            _impactDamage = Mathf.Max(0, impactDamage);
            _splashRadius = Mathf.Max(0f, splashRadius);
            _edgeDamageMultiplier = Mathf.Clamp01(edgeDamageMultiplier);
            _knockbackCells = Mathf.Max(0f, knockbackCells);
            _singleTargetImpact = singleTargetImpact;
            _onImpactCallback = onImpact;

            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = sprite != null ? sprite : HazardZone.GetOrCreateCircleSprite();
            _renderer.color = tint;
            _renderer.sortingLayerName = "Projectiles";
            _renderer.sortingOrder = 15;
            transform.localScale = visualScale.sqrMagnitude > 0.001f ? visualScale : new Vector3(0.35f, 0.35f, 1f);

            _isAlive = true;
        }

        private void Update()
        {
            if (!_isAlive) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            Vector3 flat = Vector3.Lerp(_startPos, _targetPos, t);
            float arcOffset = Mathf.Sin(t * Mathf.PI) * _arcHeight;
            Vector3 nextPos = flat + new Vector3(0f, arcOffset, 0f);

            Vector3 delta = nextPos - transform.position;
            if (delta.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            transform.position = nextPos;

            if (t >= 1f)
            {
                Detonate();
            }
        }

        public void Detonate()
        {
            if (!_isAlive) return;
            _isAlive = false;

            if (_impactDamage > 0 && _splashRadius > 0.01f)
            {
                ApplyImpactDamage();
            }

            _onImpactCallback?.Invoke(_targetPos);
            Destroy(gameObject);
        }

        private void ApplyImpactDamage()
        {
            float sqrRadius = _splashRadius * _splashRadius;
            float innerSqr = (_splashRadius * 0.5f) * (_splashRadius * 0.5f);
            var enemies = Enemy.ActiveEnemies;

            if (_singleTargetImpact)
            {
                Enemy closest = null;
                float bestSqr = sqrRadius;
                for (int i = 0; i < enemies.Count; i++)
                {
                    Enemy e = enemies[i];
                    if (e == null || e.IsDead) continue;
                    float sqrDist = (e.transform.position - _targetPos).sqrMagnitude;
                    if (sqrDist <= bestSqr)
                    {
                        bestSqr = sqrDist;
                        closest = e;
                    }
                }

                if (closest != null)
                {
                    closest.TakeDamage(_impactDamage);
                }
                return;
            }

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy e = enemies[i];
                if (e == null || e.IsDead) continue;

                Vector3 diff = e.transform.position - _targetPos;
                float sqrDist = diff.sqrMagnitude;
                if (sqrDist > sqrRadius) continue;

                int dmg = sqrDist <= innerSqr
                    ? _impactDamage
                    : Mathf.Max(1, Mathf.RoundToInt(_impactDamage * _edgeDamageMultiplier));

                e.TakeDamage(dmg);

                if (_knockbackCells > 0f && !e.IsDead)
                {
                    Vector2 dir = diff.sqrMagnitude > 0.001f
                        ? new Vector2(diff.x, diff.y).normalized
                        : Vector2.up;
                    e.ApplyKnockback(dir, _knockbackCells);
                }
            }
        }
    }
}
