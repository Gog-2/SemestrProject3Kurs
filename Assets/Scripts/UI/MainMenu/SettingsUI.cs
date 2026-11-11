using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Alchemy.Inspector;
using Game.Core;

namespace Game.UI.MainMenu
{
    public class SettingsUI : MonoBehaviour
    {
        [Title("Audio Sliders")]
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private TMP_Text _musicValueText;
        [SerializeField] private TMP_Text _sfxValueText;

        [Title("Navigation")]
        [SerializeField] private Button _backButton;
        [SerializeField] private MainMenuManager _mainMenuManager;

        private bool _isBound;

        private void Awake()
        {
            BindAudioControls(_musicSlider, _sfxSlider, _musicValueText, _sfxValueText, ref _isBound);

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(OnBackClicked);
                _backButton.onClick.AddListener(OnBackClicked);
            }
        }

        private void OnEnable()
        {
            SyncAudioControls(_musicSlider, _sfxSlider, _musicValueText, _sfxValueText);
            MetaSaveManager.ApplyAudioVolumes();
        }

        private void OnBackClicked()
        {
            if (_mainMenuManager == null)
            {
                _mainMenuManager = FindAnyObjectByType<MainMenuManager>();
            }
            _mainMenuManager?.OpenTitle();
        }

        public void ConfigureReferences(Slider musicSlider, Slider sfxSlider, TMP_Text musicText, TMP_Text sfxText, Button backButton, MainMenuManager manager)
        {
            _musicSlider = musicSlider;
            _sfxSlider = sfxSlider;
            _musicValueText = musicText;
            _sfxValueText = sfxText;
            _backButton = backButton;
            _mainMenuManager = manager;
        }

        public static void BindAudioControls(Slider musicSlider, Slider sfxSlider, TMP_Text musicText, TMP_Text sfxText, ref bool isBound)
        {
            if (isBound) return;
            isBound = true;

            if (musicSlider != null)
            {
                musicSlider.minValue = 0f;
                musicSlider.maxValue = 1f;
                musicSlider.onValueChanged.AddListener(val =>
                {
                    MetaSaveManager.MusicVolume = val;
                    if (musicText != null) musicText.text = $"{Mathf.RoundToInt(val * 100f)}%";
                });
            }

            if (sfxSlider != null)
            {
                sfxSlider.minValue = 0f;
                sfxSlider.maxValue = 1f;
                sfxSlider.onValueChanged.AddListener(val =>
                {
                    MetaSaveManager.SfxVolume = val;
                    if (sfxText != null) sfxText.text = $"{Mathf.RoundToInt(val * 100f)}%";
                });
            }

            SyncAudioControls(musicSlider, sfxSlider, musicText, sfxText);
        }

        public static void SyncAudioControls(Slider musicSlider, Slider sfxSlider, TMP_Text musicText, TMP_Text sfxText)
        {
            float m = MetaSaveManager.MusicVolume;
            float s = MetaSaveManager.SfxVolume;

            if (musicSlider != null) musicSlider.SetValueWithoutNotify(m);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(s);
            if (musicText != null) musicText.text = $"{Mathf.RoundToInt(m * 100f)}%";
            if (sfxText != null) sfxText.text = $"{Mathf.RoundToInt(s * 100f)}%";
        }
    }
}
