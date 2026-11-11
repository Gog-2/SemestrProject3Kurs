using System;
using System.Collections.Generic;
using UnityEngine;
using Alchemy.Inspector;

namespace Game.Spells
{
    [Serializable]
    public struct SpellScrollInstance
    {
        public SpellData Data;
        public int Level;

        public SpellScrollInstance(SpellData data, int level)
        {
            Data = data;
            Level = Mathf.Clamp(level, 1, 4);
        }
    }

    public class SpellScrollManager : MonoBehaviour
    {
        public static SpellScrollManager Instance { get; private set; }
        public const int MaxScrollSlots = 5;

        [Title("Config")]
        [SerializeField, Range(1, MaxScrollSlots)] private int _maxScrolls = MaxScrollSlots;

        private readonly List<SpellScrollInstance> _scrolls = new(MaxScrollSlots);
        private int _selectedIndex = -1;

        public int MaxScrolls => _maxScrolls;
        public IReadOnlyList<SpellScrollInstance> Scrolls => _scrolls;
        public int SelectedIndex => _selectedIndex;
        public bool HasSelection => _selectedIndex >= 0 && _selectedIndex < _scrolls.Count;
        public bool IsFull => _scrolls.Count >= _maxScrolls;

        public event Action ScrollsChanged;
        public event Action<int> SelectionChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool CanAddScroll() => _scrolls.Count < _maxScrolls;

        public bool TryAddScroll(SpellData data, int level)
        {
            if (data == null || IsFull) return false;

            _scrolls.Add(new SpellScrollInstance(data, level));
            ScrollsChanged?.Invoke();
            return true;
        }

        public void SelectScroll(int index)
        {
            if (index < 0 || index >= _scrolls.Count)
            {
                CancelSelection();
                return;
            }

            if (_selectedIndex == index)
            {
                CancelSelection();
                return;
            }

            _selectedIndex = index;
            SelectionChanged?.Invoke(_selectedIndex);
        }

        public void CancelSelection()
        {
            if (_selectedIndex == -1) return;
            _selectedIndex = -1;
            SelectionChanged?.Invoke(-1);
        }

        public bool TryConsumeSelectedScroll(out SpellScrollInstance consumed)
        {
            if (!HasSelection)
            {
                consumed = default;
                return false;
            }

            consumed = _scrolls[_selectedIndex];
            _scrolls.RemoveAt(_selectedIndex);
            _selectedIndex = -1;

            SelectionChanged?.Invoke(-1);
            ScrollsChanged?.Invoke();
            return true;
        }
    }
}
