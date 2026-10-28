using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Enemies;
using Game.Buildings;

namespace Game.Projectiles
{
    public class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Collider2D _collider;

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

        private void Update()
        {
            if (!_isAlive) return;

            float dt = Time.deltaTime;
            transform.position += (Vector3)(_direction * (_speed * dt));

            _lifetime -= dt;
            if (_lifetime <= 0f)
            {
                Despawn();
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
            _damage = damageOverride > 0 ? damageOverride : (data != null ? data.Damage : 15);
            _speed = data != null ? data.Speed : 15f;
            _pierceRemaining = data != null ? data.PierceCount : 999;
            _knockbackCells = data != null ? data.KnockbackCells : 1f;
            _lifetime = data != null ? data.MaxLifetime : 5f;
            _onDespawn = onDespawn;

            transform.position = spawnPosition;

            // Rotate towards travel direction
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            if (_spriteRenderer != null && data != null)
            {
                if (data.Sprite != null) _spriteRenderer.sprite = data.Sprite;
                _spriteRenderer.color = data.Color;
                transform.localScale = new Vector3(data.Scale.x, data.Scale.y, 1f);
            }

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
