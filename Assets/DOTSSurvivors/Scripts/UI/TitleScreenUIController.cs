using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// UI controller associated with the main menu in the title screen.
    /// </summary>
    /// <remarks>
    /// Handles showing and hiding UI panels when appropriate.
    /// </remarks>
    public class TitleScreenUIController : MonoBehaviour
    {
        [Header("Top Level")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _aboutButton;
        [SerializeField] private Button _optionsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private TextMeshProUGUI _coinCountText;

        [Header("About Panel")]
        [SerializeField] private GameObject _aboutPanel;
        [SerializeField] private Button _aboutCloseButton;
        [SerializeField] private Button _turboMakesGamesYouTubeButton;
        [SerializeField] private Button _penzillaDesignSiteButton;

        [Header("Options Panel")]
        [SerializeField] private GameObject _optionsPanel;
        [SerializeField] private Button _optionsCloseButton;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private Button _deleteDataButton;
        [SerializeField] private GameObject _confirmDeleteDataPanel;
        [SerializeField] private Button _confirmDeleteDataButton;
        [SerializeField] private Button _cancelDeleteDataButton;
        [SerializeField] private AudioClip _onSfxVolumeChangeAudioClip;

        [Header("Confirm Quit Panel")]
        [SerializeField] private GameObject _confirmQuitPanel;
        [SerializeField] private Button _confirmQuitButton;
        [SerializeField] private Button _cancelQuitButton;
        
        [Header("Character Select Panel")]
        [SerializeField] private GameObject _characterSelectPanel;
        [SerializeField] private Button _characterSelectNextButton;
        [SerializeField] private Button _characterSelectBackButton;
        [SerializeField] private CharacterSelectPanelUIController _characterSelectPanelUIController;
        
        [Header("Stage Select Panel")]
        [SerializeField] private GameObject _stageSelectPanel;
        [SerializeField] private Button _stageSelectGoButton;
        [SerializeField] private Button _stageSelectBackButton;
        [SerializeField] private Button _stageSelectArtTestSceneButton;
        [SerializeField] private StageSelectPanelUIController _stageSelectPanelUIController;

        private const int ART_TEST_SCENE_INDEX = 4;
        
        private void Start()
        {
            _aboutPanel.SetActive(false);
            _optionsPanel.SetActive(false);
            _confirmQuitPanel.SetActive(false);
            _characterSelectPanel.SetActive(false);
            _stageSelectPanel.SetActive(false);
            _playButton.Select();
            
            UpdateCoinCountText(PersistentDataManager.Instance.CurrentCoinCount);
        }
        
        private void OnEnable()
        {
            _playButton.onClick.AddListener(OnButtonPlay);
            _aboutButton.onClick.AddListener(OnButtonAbout);
            _optionsButton.onClick.AddListener(OnButtonOptions);
            _quitButton.onClick.AddListener(OnButtonQuit);

            _aboutCloseButton.onClick.AddListener(OnButtonAboutClose);
            _turboMakesGamesYouTubeButton.onClick.AddListener(OnButtonTurboMakesGamesYouTube);
            _penzillaDesignSiteButton.onClick.AddListener(OnButtonPenzillaDesignSite);

            _optionsCloseButton.onClick.AddListener(OnButtonOptionsClose);
            _musicVolumeSlider.onValueChanged.AddListener(OnSliderMusicVolume);
            _sfxVolumeSlider.onValueChanged.AddListener(OnSliderSfxVolume);
            _deleteDataButton.onClick.AddListener(OnButtonDeleteData);
            _confirmDeleteDataButton.onClick.AddListener(OnButtonConfirmDeleteData);
            _cancelDeleteDataButton.onClick.AddListener(OnButtonCancelDeleteData);
            
            _confirmQuitButton.onClick.AddListener(OnButtonConfirmQuit);
            _cancelQuitButton.onClick.AddListener(OnButtonCancelQuit);
            
            _characterSelectNextButton.onClick.AddListener(OnButtonCharacterSelectNext);
            _characterSelectBackButton.onClick.AddListener(OnButtonCharacterSelectBack);

            _stageSelectGoButton.onClick.AddListener(OnButtonStageSelectGo);
            _stageSelectBackButton.onClick.AddListener(OnButtonStageSelectBack);
            _stageSelectArtTestSceneButton.onClick.AddListener(OnButtonArtTestScene);
            
            PersistentDataManager.Instance.OnUpdateCoinCount += UpdateCoinCountText;
        }
        
        private void OnDisable()
        {
            _playButton.onClick.RemoveAllListeners();
            _aboutButton.onClick.RemoveAllListeners();
            _optionsButton.onClick.RemoveAllListeners();
            _quitButton.onClick.RemoveAllListeners();

            _aboutCloseButton.onClick.RemoveAllListeners();

            _optionsCloseButton.onClick.RemoveAllListeners();
            _musicVolumeSlider.onValueChanged.RemoveAllListeners();
            _sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            _deleteDataButton.onClick.RemoveAllListeners();
            _confirmDeleteDataButton.onClick.RemoveAllListeners();
            _cancelDeleteDataButton.onClick.RemoveAllListeners();
            
            _confirmQuitButton.onClick.RemoveAllListeners();
            _cancelQuitButton.onClick.RemoveAllListeners();
            
            _characterSelectNextButton.onClick.RemoveAllListeners();
            _characterSelectBackButton.onClick.RemoveAllListeners();

            _stageSelectGoButton.onClick.RemoveAllListeners();
            _stageSelectBackButton.onClick.RemoveAllListeners();
            _stageSelectArtTestSceneButton.onClick.RemoveAllListeners();
            
            PersistentDataManager.Instance.OnUpdateCoinCount -= UpdateCoinCountText;
        }

        private void OnButtonPlay()
        {
            _characterSelectPanel.SetActive(true);
            _quitButton.gameObject.SetActive(false);

            _characterSelectPanelUIController.ShowCharacterSelectPanel();
        }
        
        private void OnButtonAbout()
        {
            _aboutPanel.SetActive(true);
            _quitButton.gameObject.SetActive(false);
            _aboutCloseButton.Select();
        }

        private void OnButtonOptions()
        {
            _optionsPanel.SetActive(true);
            _musicVolumeSlider.value = MainMenuAudioController.Instance.GetNormalizedMusicLevel();
            _sfxVolumeSlider.value = MainMenuAudioController.Instance.GetNormalizedSfxLevel();
            _confirmDeleteDataPanel.SetActive(false);
            _quitButton.gameObject.SetActive(false);
            _musicVolumeSlider.Select();
        }

        private void OnButtonQuit()
        {
            _quitButton.gameObject.SetActive(false);
            _playButton.gameObject.SetActive(false);
            _aboutButton.gameObject.SetActive(false);
            _optionsButton.gameObject.SetActive(false);
            _confirmQuitPanel.SetActive(true);
            _cancelQuitButton.Select();
        }

        private void OnButtonConfirmQuit()
        {
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }

        private void OnButtonCancelQuit()
        {
            _quitButton.gameObject.SetActive(true);
            _playButton.gameObject.SetActive(true);
            _aboutButton.gameObject.SetActive(true);
            _optionsButton.gameObject.SetActive(true);
            _confirmQuitPanel.SetActive(false);
            _playButton.Select();
        }

        private void OnButtonAboutClose()
        {
            _aboutPanel.SetActive(false);
            _quitButton.gameObject.SetActive(true);
            _playButton.Select();
        }
        
        private void OnButtonTurboMakesGamesYouTube()
        {
            Application.OpenURL("https://www.youtube.com/@turbomakesgames");
        }
        
        private void OnButtonPenzillaDesignSite()
        {
            Application.OpenURL("https://www.penzilladesign.com");
        }
        
        private void OnButtonCharacterSelectNext()
        {
            _characterSelectPanel.SetActive(false);
            _stageSelectPanel.SetActive(true);
            _characterSelectPanelUIController.HideCharacterSelectPanel();
            _stageSelectPanelUIController.ShowStageSelectPanel();
        }

        private void OnButtonCharacterSelectBack()
        {
            _characterSelectPanel.SetActive(false);
            _quitButton.gameObject.SetActive(true);
            _characterSelectPanelUIController.HideCharacterSelectPanel();
            _playButton.Select();
        }

        private void OnButtonStageSelectGo()
        {
            LoadingScreenUIController.Instance.ShowLoadingScreen();
            var stageSceneIndex = _stageSelectPanelUIController.SelectedStageSceneIndex;
            SceneManager.LoadSceneAsync(stageSceneIndex);
        }

        public void OnButtonArtTestScene()
        {
            LoadingScreenUIController.Instance.ShowLoadingScreen();
            SceneManager.LoadSceneAsync(ART_TEST_SCENE_INDEX);
        }

        private void OnButtonStageSelectBack()
        {
            _stageSelectPanel.SetActive(false);
            _characterSelectPanel.SetActive(true);
            _characterSelectPanelUIController.ShowCharacterSelectPanel();
            _stageSelectPanelUIController.HideStageSelectPanel();
        }
        
        private void OnButtonOptionsClose()
        {
            _optionsPanel.SetActive(false);
            _quitButton.gameObject.SetActive(true);
            _playButton.Select();
        }

        private void OnSliderMusicVolume(float level)
        {
            MainMenuAudioController.Instance.SetMusicVolume(level);
        }

        private void OnSliderSfxVolume(float level)
        {
            MainMenuAudioController.Instance.SetSfxVolume(level);
            MainMenuAudioController.Instance.PlaySfxAudioClip(_onSfxVolumeChangeAudioClip, (int)AudioPriority.High);
        }

        private void OnButtonDeleteData()
        {
            _confirmDeleteDataPanel.SetActive(true);
            _cancelDeleteDataButton.Select();
        }

        private void OnButtonConfirmDeleteData()
        {
            PersistentDataManager.Instance.DeleteData();
            MainMenuAudioController.Instance.SetMusicVolume(1f);
            MainMenuAudioController.Instance.SetSfxVolume(1f);
            _optionsPanel.SetActive(false);
            _quitButton.gameObject.SetActive(true);
            _playButton.Select();
        }

        private void OnButtonCancelDeleteData()
        {
            _confirmDeleteDataPanel.SetActive(false);
            _musicVolumeSlider.Select();
        }

        private void UpdateCoinCountText(int coinCount)
        {
            _coinCountText.text = $"${coinCount:N0}";
        }
    }
}