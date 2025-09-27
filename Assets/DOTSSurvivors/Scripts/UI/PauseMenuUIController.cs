using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// UI controller associated with the pause menu that is displayed when the player pauses the game.
    /// </summary>
    /// <remarks>
    /// Displays more detailed information about the player's current game run, has some basic settings and ability to quit the game.
    /// </remarks>
    public class PauseMenuUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _pauseMenuItems;
        [SerializeField] private CurrentCapabilitiesUIController _currentCapabilitiesUIController;
        [SerializeField] private CurrentStatsUIController _currentStatsUIController;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private GameObject _confirmQuitPanel;
        [SerializeField] private Button _confirmQuitButton;
        [SerializeField] private Button _cancelQuitButton;

        [Header("Audio Controls")]
        [SerializeField] private GameObject _audioControlPanel;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private AudioClip _onSfxVolumeChangeAudioClip;

        private DOTSSurvivorsInputActions _inputActions;
        private bool _showingPauseUI;

        private void Awake()
        {
            _inputActions = new DOTSSurvivorsInputActions();
            _inputActions.Enable();
        }

        private void OnEnable()
        {
            _inputActions.UI.Pause.performed += AttemptToggleGamePause;
            _resumeButton.onClick.AddListener(() => AttemptToggleGamePause(default));
            _quitButton.onClick.AddListener(OnButtonQuit);
            _confirmQuitButton.onClick.AddListener(OnButtonConfirmQuit);
            _cancelQuitButton.onClick.AddListener(OnButtonCancelQuit);
            _musicVolumeSlider.onValueChanged.AddListener(UpdateMusicVolume);
            _sfxVolumeSlider.onValueChanged.AddListener(UpdateSfxVolume);
            InputSystem.onDeviceChange += DeviceChangeEvent;
        }

        private void OnDisable()
        {
            _inputActions.UI.Pause.performed -= AttemptToggleGamePause;
            _resumeButton.onClick.RemoveAllListeners();
            _quitButton.onClick.RemoveAllListeners();
            _confirmQuitButton.onClick.RemoveAllListeners();
            _cancelQuitButton.onClick.RemoveAllListeners();
            _musicVolumeSlider.onValueChanged.RemoveAllListeners();
            _sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            InputSystem.onDeviceChange -= DeviceChangeEvent;
        }

        private void Start()
        {
            ShowHideUI(false);
        }

        /// <summary>
        /// Attempt to pause or unpause the game.
        /// </summary>
        /// <remarks>
        /// Pausing is not allowed if the player doesn't exist (i.e. when the player has died but game over screen has not yet been displayed) or if the game is already paused due to level up, chest, or otherwise.
        /// </remarks>
        private void AttemptToggleGamePause(InputAction.CallbackContext obj)
        {
            if (World.DefaultGameObjectInjectionWorld == null) return;

            var playerQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(PlayerTag));
            if (playerQuery.IsEmpty) return;

            if (_showingPauseUI)
            {
                ShowHideUI(false);
                PauseManager.Instance.ResumeGame();
                _showingPauseUI = false;
            }
            else
            {
                if (PauseManager.Instance.IsPaused) return;
                PauseManager.Instance.PauseGame();
                ShowHideUI(true);
                _showingPauseUI = true;
            }
        }

        private void ShowHideUI(bool shouldShow)
        {
            _pauseMenuItems.SetActive(shouldShow);
            _currentCapabilitiesUIController.gameObject.SetActive(shouldShow);
            _currentStatsUIController.gameObject.SetActive(shouldShow);
            _confirmQuitPanel.SetActive(false);
            _resumeButton.gameObject.SetActive(shouldShow);
            _quitButton.gameObject.SetActive(shouldShow);
            _audioControlPanel.SetActive(shouldShow);
            if (shouldShow)
            {
                _currentCapabilitiesUIController.ShowCapabilitiesUI();
                _currentStatsUIController.ShowStatsUI();

                var musicVolumeLevel = GameAudioController.Instance.GetNormalizedMusicLevel();
                _musicVolumeSlider.value = musicVolumeLevel;

                var sfxVolumeLevel = GameAudioController.Instance.GetNormalizedSfxLevel();
                _sfxVolumeSlider.value = sfxVolumeLevel;
                _resumeButton.Select();
            }
            else
            {
                SelectionIconUIController.Instance.SetPositionOffscreen();
                _currentCapabilitiesUIController.HideCapabilitiesUI();
                _currentStatsUIController.HideStatsUI();
            }
        }

        private void OnDestroy()
        {
            _inputActions.Disable();
        }

        private void OnButtonQuit()
        {
            _confirmQuitPanel.SetActive(true);
            _resumeButton.gameObject.SetActive(false);
            _quitButton.gameObject.SetActive(false);
            _cancelQuitButton.Select();
        }

        private void OnButtonConfirmQuit()
        {
            SelectionIconUIController.Instance.SetPositionOffscreen();
            ShowHideUI(false);
            var playerEntity = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(PlayerTag)).GetSingletonEntity();
            World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentEnabled<DestroyEntityFlag>(playerEntity, true);
            PauseManager.Instance.ResumeGame();
            _showingPauseUI = false;
        }

        private void OnButtonCancelQuit()
        {
            _confirmQuitPanel.SetActive(false);
            _resumeButton.gameObject.SetActive(true);
            _quitButton.gameObject.SetActive(true);
            _resumeButton.Select();
        }

        private void UpdateMusicVolume(float level)
        {
            GameAudioController.Instance.SetMusicVolume(level);
        }

        private void UpdateSfxVolume(float level)
        {
            GameAudioController.Instance.SetSfxVolume(level);
            GameAudioController.Instance.PlaySfxAudioClip(_onSfxVolumeChangeAudioClip, (int)AudioPriority.High);
        }

        private void DeviceChangeEvent(InputDevice device, InputDeviceChange deviceChangeEvent)
        {
            if (deviceChangeEvent == InputDeviceChange.Removed)
            {
                if (_showingPauseUI) return;
                AttemptToggleGamePause(default);
            }
        }
    }
}