using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Enemies;
using Game.Buildings;

namespace Game.Projectiles
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Collider2D _collider;
        [SerializeField] private Rigidbody2D _rigidbody;

        private static readonly RaycastHit2D[] s_hitBuffer = new RaycastHit2D[16];

        private ProjectileData _data;
        private Vector2 _direction;
        private int _damage;
        private float _speed;
        private int _pierceRemaining;
        private float _knockbackCells;
        private float _lifetime;
        private Action<Projectile> _onDespawn;
        private bool _isAlive;

        private readonly HashSet<IDamageable> _hitTargets = new();

        private void Awake()
        {
            Debug.Assert(_spriteRenderer != null, "[Projectile] _spriteRenderer is not assigned!", this);
            Debug.Assert(_collider != null, "[Projectile] _collider is not assigned!", this);
            Debug.Assert(_rigidbody != null, "[Projectile] _rigidbody is not assigned!", this);

            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody.useFullKinematicContacts = true;
        }

        private void Update()
        {
            if (!_isAlive) return;

            float dt = Time.deltaTime;
            float stepDistance = _speed * dt;

            if (stepDistance > 0.0001f)
            {
                SweepCheck(stepDistance);
                if (!_isAlive) return;
            }

            transform.position += (Vector3)(_direction * stepDistance);

            _lifetime -= dt;
            if (_lifetime <= 0f)
            {
                Despawn();
            }
        }

        private void SweepCheck(float distance)
        {
            float radius = 0.2f;
            if (_collider is CircleCollider2D circle)
            {
                radius = circle.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
            }

            int hitCount = Physics2D.CircleCastNonAlloc(
                transform.position,
                radius,
                _direction,
                s_hitBuffer,
                distance,
                ~0
            );

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D col = s_hitBuffer[i].collider;
                if (col == null || col == _collider) continue;

                if (col.TryGetComponent<Enemy>(out var enemy))
                {
                    HandleHit(enemy);
                    if (!_isAlive) return;
                }
                else if (col.TryGetComponent<IDamageable>(out var damageable))
                {
                    if (damageable is Castle || damageable is Building) continue;

                    HandleHit(damageable);
                    if (!_isAlive) return;
                }
            }
        }

        public void Initialize(
            Vector3 spawnPosition,
            Vector2 direction,
            ProjectileData data,
            int damageOverride = -1,
            Action<Projectile> onDespawn = null)
        {
            _data = data;
            _direction = direction.normalized;
            _onDespawn = onDespawn;

            _damage = damageOverride >= 0 ? damageOverride : data.Damage;
            _speed = data.Speed;
            _pierceRemaining = data.PierceCount;
            _knockbackCells = data.KnockbackCells;
            _lifetime = data.MaxLifetime;

            transform.position = spawnPosition;

            // Rotate towards travel direction
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            if (data.Sprite != null) _spriteRenderer.sprite = data.Sprite;
            _spriteRenderer.color = data.Color;
            transform.localScale = new Vector3(data.Scale.x, data.Scale.y, 1f);

            _hitTargets.Clear();
            _isAlive = true;
            gameObject.SetActive(true);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!_isAlive) return;

            // Check if hit enemy
            if (collision.TryGetComponent<Enemy>(out var enemy))
            {
                HandleHit(enemy);
            }
            else if (collision.TryGetComponent<IDamageable>(out var damageable))
            {
                // Never hit player's own castle or buildings
                if (damageable is Castle || damageable is Building) return;

                HandleHit(damageable);
            }
        }

        private void HandleHit(IDamageable target)
        {
            if (target == null || target.IsDead) return;
            if (_hitTargets.Contains(target)) return;

            _hitTargets.Add(target);
            target.TakeDamage(_damage);

            if (target is Enemy enemy)
            {
                enemy.ApplyKnockback(_direction, _knockbackCells);
            }

            if (_pierceRemaining > 0)
            {
                _pierceRemaining--;
                if (_pierceRemaining <= 0)
                {
                    Despawn();
                }
            }
        }

        public void Despawn()
        {
            if (!_isAlive) return;
            _isAlive = false;

            _onDespawn?.Invoke(this);
        }

        public void OnSpawnFromPool()
        {
            _hitTargets.Clear();
            _isAlive = false;
        }

        public void OnReturnToPool()
        {
            _hitTargets.Clear();
            _isAlive = false;
            gameObject.SetActive(false);
        }
    }
}
