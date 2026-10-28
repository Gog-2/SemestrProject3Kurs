using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Alchemy.Inspector;
using Game.Core;

namespace Game.UI.MainMenu
{
    public class MainMenuManager : MonoBehaviour
    {
        [Title("Panels")]
        [SerializeField] private GameObject _titlePanel;
        [SerializeField] private GameObject _knightSelectionPanel;
        [SerializeField] private GameObject _arenaSelectionPanel;
        [SerializeField] private GameObject _weaponSelectionPanel;
        [SerializeField] private GameObject _shopPanel;

        [Title("Header")]
        [SerializeField] private TMP_Text _diamondBalanceText;

        [Title("Title Screen Buttons")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _shopButton;
        [SerializeField] private Button _quitButton;

        [Title("Wizard Flow Buttons")]
        [SerializeField] private Button _knightToArenaButton;
        [SerializeField] private Button _arenaToWeaponButton;
        [SerializeField] private Button _startRunButton;
        [SerializeField] private Button _backToTitleButton;

        private void Awake()
        {
            // Ensure RunConfig exists
            if (RunConfig.Instance == null)
            {
                var runConfigGo = new GameObject("RunConfig");
                runConfigGo.AddComponent<RunConfig>();
            }

            // Bind buttons
            if (_playButton != null) _playButton.onClick.AddListener(OpenKnightSelection);
            if (_shopButton != null) _shopButton.onClick.AddListener(OpenShop);
            if (_quitButton != null) _quitButton.onClick.AddListener(OnQuitClicked);

            if (_knightToArenaButton != null) _knightToArenaButton.onClick.AddListener(OpenArenaSelection);
            if (_arenaToWeaponButton != null) _arenaToWeaponButton.onClick.AddListener(OpenWeaponSelection);
            if (_startRunButton != null) _startRunButton.onClick.AddListener(StartRun);
            if (_backToTitleButton != null) _backToTitleButton.onClick.AddListener(OpenTitle);

            OpenTitle();
        }

        private void OnEnable()
        {
            MetaSaveManager.DiamondsChanged += UpdateBalance;
            UpdateBalance();
        }

        private void OnDisable()
        {
            MetaSaveManager.DiamondsChanged -= UpdateBalance;
        }

        private void UpdateBalance()
        {
            if (_diamondBalanceText != null)
            {
                _diamondBalanceText.text = $"Алмазы: {MetaSaveManager.Data.Diamonds}";
            }
        }

        public void OpenTitle()
        {
            HideAllPanels();
            if (_titlePanel != null) _titlePanel.SetActive(true);
        }

        public void OpenKnightSelection()
        {
            HideAllPanels();
            if (_knightSelectionPanel != null) _knightSelectionPanel.SetActive(true);
        }

        public void OpenArenaSelection()
        {
            HideAllPanels();
            if (_arenaSelectionPanel != null) _arenaSelectionPanel.SetActive(true);
        }

        public void OpenWeaponSelection()
        {
            HideAllPanels();
            if (_weaponSelectionPanel != null) _weaponSelectionPanel.SetActive(true);
        }

        public void OpenShop()
        {
            HideAllPanels();
            if (_shopPanel != null) _shopPanel.SetActive(true);
        }

        public void StartRun()
        {
            string sceneToLoad = "SampleScene";

            if (RunConfig.Instance != null && RunConfig.Instance.SelectedArena != null && !string.IsNullOrEmpty(RunConfig.Instance.SelectedArena.SceneName))
            {
                sceneToLoad = RunConfig.Instance.SelectedArena.SceneName;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneToLoad);
        }

        private void HideAllPanels()
        {
            if (_titlePanel != null) _titlePanel.SetActive(false);
            if (_knightSelectionPanel != null) _knightSelectionPanel.SetActive(false);
            if (_arenaSelectionPanel != null) _arenaSelectionPanel.SetActive(false);
            if (_weaponSelectionPanel != null) _weaponSelectionPanel.SetActive(false);
            if (_shopPanel != null) _shopPanel.SetActive(false);
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
