using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Projectiles;

namespace Game.Units
{
    public class DecoySheep : MonoBehaviour, IDamageable
    {
        public static readonly List<DecoySheep> ActiveSheep = new(16);

        private int _maxHP = 40;
        private int _currentHP = 40;
        private float _tauntRadius = 3f;
        private float _moveSpeed = 1.1f;
        private Vector3 _origin;
        private Vector3 _wanderTarget;
        private float _wanderTimer;
        private SpriteRenderer _renderer;

        public int CurrentHP => _currentHP;
        public int MaxHP => _maxHP;
        public bool IsDead => _currentHP <= 0;
        public float TauntRadius => _tauntRadius;

        public event Action<IDamageable> HealthChanged;
        public event Action<IDamageable> Destroyed;

        private void OnEnable()
        {
            if (!ActiveSheep.Contains(this))
            {
                ActiveSheep.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveSheep.Remove(this);
        }

        public static DecoySheep Spawn(Vector3 worldPos, int hp = 40, float tauntRadius = 3f)
        {
            var go = new GameObject("Polymorphed_Sheep");
            go.transform.position = worldPos;
            var sheep = go.AddComponent<DecoySheep>();
            sheep.Initialize(worldPos, hp, tauntRadius);
            return sheep;
        }

        private void Initialize(Vector3 origin, int hp, float tauntRadius)
        {
            _origin = origin;
            _maxHP = Mathf.Max(1, hp);
            _currentHP = _maxHP;
            _tauntRadius = Mathf.Max(1f, tauntRadius);
            _wanderTarget = origin;

            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = HazardZone.GetOrCreateCircleSprite();
            _renderer.color = new Color(0.98f, 0.96f, 0.9f, 1f);
            _renderer.sortingLayerName = "Default";
            _renderer.sortingOrder = 9;
            transform.localScale = new Vector3(0.38f, 0.32f, 1f);
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;
            _currentHP = Mathf.Max(0, _currentHP - amount);
            HealthChanged?.Invoke(this);
            if (_currentHP <= 0)
            {
                Destroyed?.Invoke(this);
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (IsDead) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            _wanderTimer -= Time.deltaTime;
            if (_wanderTimer <= 0f)
            {
                _wanderTimer = UnityEngine.Random.Range(1.5f, 3f);
                Vector2 offset = UnityEngine.Random.insideUnitCircle * 1.2f;
                _wanderTarget = _origin + new Vector3(offset.x, offset.y, 0f);
            }

            if (Vector2.Distance(transform.position, _wanderTarget) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, _wanderTarget, _moveSpeed * Time.deltaTime);
            }
        }
    }
}
