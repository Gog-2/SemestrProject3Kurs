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
            Debug.Assert(_titlePanel != null, "[MainMenuManager] _titlePanel is not assigned!", this);
            Debug.Assert(_knightSelectionPanel != null, "[MainMenuManager] _knightSelectionPanel is not assigned!", this);
            Debug.Assert(_arenaSelectionPanel != null, "[MainMenuManager] _arenaSelectionPanel is not assigned!", this);
            Debug.Assert(_weaponSelectionPanel != null, "[MainMenuManager] _weaponSelectionPanel is not assigned!", this);
            Debug.Assert(_shopPanel != null, "[MainMenuManager] _shopPanel is not assigned!", this);
            Debug.Assert(_diamondBalanceText != null, "[MainMenuManager] _diamondBalanceText is not assigned!", this);

            Debug.Assert(_playButton != null, "[MainMenuManager] _playButton is not assigned!", this);
            Debug.Assert(_shopButton != null, "[MainMenuManager] _shopButton is not assigned!", this);
            Debug.Assert(_quitButton != null, "[MainMenuManager] _quitButton is not assigned!", this);
            Debug.Assert(_knightToArenaButton != null, "[MainMenuManager] _knightToArenaButton is not assigned!", this);
            Debug.Assert(_arenaToWeaponButton != null, "[MainMenuManager] _arenaToWeaponButton is not assigned!", this);
            Debug.Assert(_startRunButton != null, "[MainMenuManager] _startRunButton is not assigned!", this);
            Debug.Assert(_backToTitleButton != null, "[MainMenuManager] _backToTitleButton is not assigned!", this);

            _playButton.onClick.AddListener(OpenKnightSelection);
            _shopButton.onClick.AddListener(OpenShop);
            _quitButton.onClick.AddListener(OnQuitClicked);

            _knightToArenaButton.onClick.AddListener(OpenArenaSelection);
            _arenaToWeaponButton.onClick.AddListener(OpenWeaponSelection);
            _startRunButton.onClick.AddListener(StartRun);
            _backToTitleButton.onClick.AddListener(OpenTitle);

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
            _diamondBalanceText.text = $"Алмазы: {MetaSaveManager.Data.Diamonds}";
        }

        public void OpenTitle()
        {
            HideAllPanels();
            _titlePanel.SetActive(true);
        }

        public void OpenKnightSelection()
        {
            HideAllPanels();
            _knightSelectionPanel.SetActive(true);
        }

        public void OpenArenaSelection()
        {
            HideAllPanels();
            _arenaSelectionPanel.SetActive(true);
        }

        public void OpenWeaponSelection()
        {
            HideAllPanels();
            _weaponSelectionPanel.SetActive(true);
        }

        public void OpenShop()
        {
            HideAllPanels();
            _shopPanel.SetActive(true);
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
            _titlePanel.SetActive(false);
            _knightSelectionPanel.SetActive(false);
            _arenaSelectionPanel.SetActive(false);
            _weaponSelectionPanel.SetActive(false);
            _shopPanel.SetActive(false);
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
