using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Alchemy.Inspector;
using Game.Core;
using Game.Data;

namespace Game.UI.MainMenu
{
    public class KnightSelectionUI : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private List<KnightData> _allKnights = new();
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _quoteText;
        [SerializeField] private TMP_Text _statsText;
        [SerializeField] private TMP_Text _skillsText;
        [SerializeField] private Image _portraitImage;

        [Title("Controls")]
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _actionButton;
        [SerializeField] private TMP_Text _actionButtonText;

        private int _currentIndex = 0;

        public KnightData SelectedKnight => _allKnights.Count > 0 ? _allKnights[_currentIndex] : null;

        private void Awake()
        {
            if (_prevButton != null) _prevButton.onClick.AddListener(OnPrevClicked);
            if (_nextButton != null) _nextButton.onClick.AddListener(OnNextClicked);
            if (_actionButton != null) _actionButton.onClick.AddListener(OnActionClicked);

            if (_allKnights.Count == 0)
            {
                PopulateDefaultKnights();
            }
        }

        private void OnEnable()
        {
            MetaSaveManager.UnlocksChanged += RefreshDisplay;
            RefreshDisplay();
        }

        private void OnDisable()
        {
            MetaSaveManager.UnlocksChanged -= RefreshDisplay;
        }

        public void RefreshDisplay()
        {
            if (_allKnights.Count == 0) return;

            _currentIndex = Mathf.Clamp(_currentIndex, 0, _allKnights.Count - 1);
            KnightData knight = _allKnights[_currentIndex];
            if (knight == null) return;

            if (_nameText != null) _nameText.text = knight.KnightName;
            if (_titleText != null) _titleText.text = knight.Title;
            if (_quoteText != null) _quoteText.text = knight.Quote;

            if (_statsText != null)
            {
                _statsText.text = $"Прочность замка: {knight.CastleMaxHP} (+{knight.HPPerLevel}/ур.)\nСлотов для построек: {knight.InitialBuildSlots}";
            }

            if (_skillsText != null)
            {
                string bld = knight.StartingBuildings != null ? string.Join(", ", knight.StartingBuildings) : "-";
                string skl = knight.StartingSkills != null ? string.Join("\n- ", knight.StartingSkills) : "-";
                _skillsText.text = $"Стартовые постройки: {bld}\nНавыки:\n- {skl}";
            }

            if (_portraitImage != null && knight.Portrait != null)
            {
                _portraitImage.sprite = knight.Portrait;
                _portraitImage.gameObject.SetActive(true);
            }

            bool isUnlocked = MetaSaveManager.IsKnightUnlocked(knight.Id);

            if (_actionButtonText != null)
            {
                if (isUnlocked)
                {
                    bool isSelected = MetaSaveManager.Data.SelectedKnightId == knight.Id;
                    _actionButtonText.text = isSelected ? "ВЫБРАН" : "ВЫБРАТЬ";
                }
                else
                {
                    _actionButtonText.text = $"ОТКРЫТЬ ({knight.DiamondCost} алм.)";
                }
            }

            if (RunConfig.Instance != null && isUnlocked)
            {
                RunConfig.Instance.SelectedKnight = knight;
            }
        }

        private void OnPrevClicked()
        {
            if (_allKnights.Count == 0) return;
            _currentIndex = (_currentIndex - 1 + _allKnights.Count) % _allKnights.Count;
            RefreshDisplay();
        }

        private void OnNextClicked()
        {
            if (_allKnights.Count == 0) return;
            _currentIndex = (_currentIndex + 1) % _allKnights.Count;
            RefreshDisplay();
        }

        private void OnActionClicked()
        {
            if (_allKnights.Count == 0) return;
            KnightData knight = _allKnights[_currentIndex];
            if (knight == null) return;

            if (!MetaSaveManager.IsKnightUnlocked(knight.Id))
            {
                if (MetaSaveManager.TryUnlockKnight(knight.Id, knight.DiamondCost))
                {
                    SelectKnight(knight);
                }
                else
                {
                    Debug.Log("[KnightSelectionUI] Недостаточно алмазов для открытия!");
                }
            }
            else
            {
                SelectKnight(knight);
            }
        }

        private void SelectKnight(KnightData knight)
        {
            MetaSaveManager.SetSelected(knight.Id, null, null);
            if (RunConfig.Instance != null)
            {
                RunConfig.Instance.SelectedKnight = knight;
            }
            RefreshDisplay();
        }

        private void PopulateDefaultKnights()
        {
            KnightData Create(string id, string name, string title, string quote, int hp, int perLvl, int slots, string[] bld, string[] skl, bool unlocked, int cost)
            {
                var k = ScriptableObject.CreateInstance<KnightData>();
                k.InitializeRuntime(id, name, title, quote, hp, perLvl, slots, bld, skl, unlocked, cost);
                return k;
            }

            _allKnights.Add(Create("hador_oplot", "Хадор Оплот", "Рыцарь-Бастион", "«Камень не молит о пощаде, он просто стоит.»", 2000, 125, 4,
                new[] { "Стены", "Казарма щитовиков" },
                new[] { "Несокрушимый фундамент (+30% HP)", "Стена щитов (+1 враг, +20% HP)" }, true, 0));

            _allKnights.Add(Create("finrod_storm", "Финрод Буревестник", "Рыцарь-Маг", "«Моя магия не знает преград, а сознание не имеет границ.»", 1500, 75, 5,
                new[] { "Башня огня", "Библиотека" },
                new[] { "Поток маны (свитки на 15% быстрее)", "Мудрость веков (+10% поддержка)" }, false, 50));

            _allKnights.Add(Create("borin_fire", "Борин Огнеплав", "Рыцарь-Инженер", "«Хорошая взрывчатка и капля кислоты эффективнее, чем тысяча клинков.»", 1300, 150, 6,
                new[] { "Мортира", "Строители" },
                new[] { "Эврика (+20% урон взрывов)", "Золотые руки (строители на 25% быстрее)" }, false, 50));

            _allKnights.Add(Create("aradan_shadow", "Арадан Тенеход", "Рыцарь-Следопыт", "«Они думают, что бегут к победе, но еще не знают в какую западню попали.»", 1400, 100, 4,
                new[] { "Охотник", "Бомба" },
                new[] { "Смертельная западня (+10% ловушки)", "Тень леса (+25% лучники)" }, false, 50));

            _allKnights.Add(Create("baron_gold", "Барон Златолиз", "Рыцарь-Казначей", "«Золото — лучшая броня.»", 1650, 90, 6,
                new[] { "Золотая шахта", "Строители" },
                new[] { "Кровь и золото (+1 монета за врага)", "Ростовщичество (+30% добыча)" }, false, 50));
        }
    }
}
