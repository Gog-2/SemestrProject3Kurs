using System;
using UnityEngine;
using DG.Tweening;
using Alchemy.Inspector;
using Game.Core;

namespace Game.Loot
{
    public class LootPickup : MonoBehaviour, IPoolable
    {
        [Title("Visuals")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Title("Loot Settings")]
        [SerializeField] private LootType _lootType = LootType.Coin;
        [SerializeField, Min(1)] private int _amount = 1;

        public LootType Type => _lootType;
        public int Amount => _amount;

        private Sequence _animSequence;
        private Action<LootPickup> _onCollectedCallback;

        public void Initialize(LootType type, int value, Action<LootPickup> onCollected)
        {
            _lootType = type;
            _amount = Mathf.Max(1, value);
            _onCollectedCallback = onCollected;

            PlaySpawnAnimation();
        }

        private void PlaySpawnAnimation()
        {
            KillActiveAnimation();

            transform.localScale = Vector3.zero;
            Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(-0.4f, 0.4f), 0f);
            Vector3 targetPos = transform.position + randomOffset;

            _animSequence = DOTween.Sequence()
                .Append(transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack))
                .Join(transform.DOJump(targetPos, 0.5f, 1, 0.4f))
                .AppendInterval(0.6f)
                .Append(transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack))
                .OnComplete(() =>
                {
                    _onCollectedCallback?.Invoke(this);
                })
                .SetLink(gameObject)
                .SetUpdate(true);
        }

        public void OnSpawnFromPool()
        {
            gameObject.SetActive(true);
        }

        public void OnReturnToPool()
        {
            KillActiveAnimation();
            gameObject.SetActive(false);
            _onCollectedCallback = null;
        }

        private void KillActiveAnimation()
        {
            if (_animSequence.IsActive())
            {
                _animSequence.Kill();
                _animSequence = null;
            }
        }

        private void OnDestroy()
        {
            KillActiveAnimation();
        }
    }
}
