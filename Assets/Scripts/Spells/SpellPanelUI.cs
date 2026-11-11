using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Alchemy.Inspector;

namespace Game.Spells
{
    public class SpellPanelUI : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private SpellScrollManager _scrollManager;
        [SerializeField] private Transform _slotsContainer;
        [SerializeField] private TMP_FontAsset _fontOverride;

        private readonly List<Button> _slotButtons = new(SpellScrollManager.MaxScrollSlots);
        private readonly List<TMP_Text> _slotLabels = new(SpellScrollManager.MaxScrollSlots);
        private readonly List<Image> _slotBackgrounds = new(SpellScrollManager.MaxScrollSlots);

        private void Start()
        {
            Bind(_scrollManager != null ? _scrollManager : SpellScrollManager.Instance);
        }

        private void OnEnable()
        {
            Bind(_scrollManager != null ? _scrollManager : SpellScrollManager.Instance);
        }

        private void OnDisable()
        {
            if (_scrollManager != null)
            {
                _scrollManager.ScrollsChanged -= Refresh;
                _scrollManager.SelectionChanged -= OnSelectionChanged;
            }
        }

        public void Bind(SpellScrollManager manager)
        {
            if (_scrollManager != null)
            {
                _scrollManager.ScrollsChanged -= Refresh;
                _scrollManager.SelectionChanged -= OnSelectionChanged;
            }

            _scrollManager = manager;

            if (_scrollManager != null)
            {
                _scrollManager.ScrollsChanged += Refresh;
                _scrollManager.SelectionChanged += OnSelectionChanged;
            }

            EnsureSlotsCreated();
            Refresh();
        }

        private void EnsureSlotsCreated()
        {
            if (_slotsContainer == null) _slotsContainer = transform;
            if (_slotButtons.Count >= SpellScrollManager.MaxScrollSlots) return;

            for (int i = _slotButtons.Count; i < SpellScrollManager.MaxScrollSlots; i++)
            {
                int idx = i;
                var go = new GameObject($"ScrollSlot_{i}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                go.transform.SetParent(_slotsContainer, false);

                var le = go.GetComponent<LayoutElement>();
                le.preferredWidth = 104f;
                le.preferredHeight = 46f;

                var img = go.GetComponent<Image>();
                img.color = new Color(0.18f, 0.2f, 0.28f, 0.9f);

                var btn = go.GetComponent<Button>();
                btn.onClick.AddListener(() => _scrollManager?.SelectScroll(idx));

                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(go.transform, false);
                var tmp = labelGo.GetComponent<TextMeshProUGUI>();
                if (_fontOverride != null) tmp.font = _fontOverride;
                tmp.fontSize = 12f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.raycastTarget = false;

                var rt = tmp.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(4f, 2f);
                rt.offsetMax = new Vector2(-4f, -2f);

                _slotButtons.Add(btn);
                _slotLabels.Add(tmp);
                _slotBackgrounds.Add(img);
            }
        }

        private void OnSelectionChanged(int _)
        {
            Refresh();
        }

        public void Refresh()
        {
            EnsureSlotsCreated();

            var scrolls = _scrollManager != null ? _scrollManager.Scrolls : null;
            int selected = _scrollManager != null ? _scrollManager.SelectedIndex : -1;

            for (int i = 0; i < _slotButtons.Count; i++)
            {
                bool hasScroll = scrolls != null && i < scrolls.Count && scrolls[i].Data != null;
                _slotButtons[i].interactable = hasScroll;

                if (hasScroll)
                {
                    SpellScrollInstance scroll = scrolls[i];
                    _slotLabels[i].text = $"{scroll.Data.SpellName}\nУр. {scroll.Level}";
                    Color accent = scroll.Data.AccentColor;
                    _slotBackgrounds[i].color = (selected == i)
                        ? new Color(1f, 0.85f, 0.2f, 1f)
                        : new Color(accent.r * 0.45f, accent.g * 0.45f, accent.b * 0.45f, 0.92f);
                }
                else
                {
                    _slotLabels[i].text = "[пусто]";
                    _slotBackgrounds[i].color = new Color(0.14f, 0.15f, 0.18f, 0.6f);
                }
            }
        }
    }
}
