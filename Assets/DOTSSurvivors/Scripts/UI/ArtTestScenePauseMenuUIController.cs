using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// UI controller for the super secret art test scene.
    /// </summary>
    /// <remarks>
    /// Provides basic functionality to pause, resume, and quit the art test scene.
    /// </remarks>
    public class ArtTestScenePauseMenuUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _pauseMenuBackground;
        [SerializeField] private GameObject _pauseMenuOptions;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private GameObject _confirmQuitPanel;
        [SerializeField] private Button _confirmQuitButton;
        [SerializeField] private Button _cancelQuitButton;
        
        private DOTSSurvivorsInputActions _inputActions;
        private bool _showingPauseUI;

        private const int TITLE_SCENE_INDEX = 0;
        
        private void Awake()
        {
            _inputActions = new DOTSSurvivorsInputActions();
            _inputActions.Enable();
        }

        private void OnEnable()
        {
            _inputActions.UI.Pause.performed += AttemptToggleGamePause;
            _resumeButton.onClick.AddListener(()=>AttemptToggleGamePause(default));
            _quitButton.onClick.AddListener(OnButtonQuit);
            _confirmQuitButton.onClick.AddListener(OnButtonConfirmQuit);
            _cancelQuitButton.onClick.AddListener(OnButtonCancelQuit);
        }

        private void OnDisable()
        {
            _inputActions.UI.Pause.performed -= AttemptToggleGamePause;
            _resumeButton.onClick.RemoveAllListeners();
            _quitButton.onClick.RemoveAllListeners();
            _confirmQuitButton.onClick.RemoveAllListeners();
            _cancelQuitButton.onClick.RemoveAllListeners();
        }

        private void Start()
        {
            ShowHideUI(false);
        }
        
        private void AttemptToggleGamePause(InputAction.CallbackContext obj)
        {
            _showingPauseUI = !_showingPauseUI;
            ShowHideUI(_showingPauseUI);
        }

        private void ShowHideUI(bool shouldShow)
        {
            _pauseMenuBackground.SetActive(shouldShow);
            _pauseMenuOptions.SetActive(shouldShow);
            _confirmQuitPanel.SetActive(false);
            _resumeButton.gameObject.SetActive(shouldShow);
            _quitButton.gameObject.SetActive(shouldShow);
            if (shouldShow)
            {
                _resumeButton.Select();
                PauseManager.Instance.PauseGame();
            }
            else
            {
                SelectionIconUIController.Instance.SetPositionOffscreen();
                PauseManager.Instance.ResumeGame();
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
            SceneManager.LoadSceneAsync(TITLE_SCENE_INDEX);
        }

        private void OnButtonCancelQuit()
        {
            _confirmQuitPanel.SetActive(false);
            _resumeButton.gameObject.SetActive(true);
            _quitButton.gameObject.SetActive(true);
            _resumeButton.Select();
        }
    }
}