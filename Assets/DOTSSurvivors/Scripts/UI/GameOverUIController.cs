using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Collections;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// UI controller associated with the game over screen that is displayed after the player character is destroyed. 
    /// </summary>
    public class GameOverUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private Button _quitButton;
        [SerializeField] private ResultsPanelUIController _resultsPanel;
        [SerializeField] private Button _doneButton;
        [SerializeField] private float _gameOverDelayTime = 1f;

        private WaitForSeconds _gameOverDelay;
        private const int EVENT_SCHEDULE_FAIL_COUNT = 600;

        private void Start()
        {
            _gameOverDelay = new WaitForSeconds(_gameOverDelayTime);
            _gameOverPanel.SetActive(false);
            _resultsPanel.gameObject.SetActive(false);
        }
        
        private void OnEnable()
        {
            _quitButton.onClick.AddListener(OnButtonQuit);
            _doneButton.onClick.AddListener(OnButtonDone);
            StartCoroutine(DelayEventSubscription());
        }

        private void OnDisable()
        {
            _quitButton.onClick.RemoveAllListeners();
            _doneButton.onClick.RemoveAllListeners();
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// Setup listeners to events on the ECS side to tell this script when to begin displaying the game over UI.
        /// </summary>
        /// <remarks>
        /// When setup as a coroutine, this provides some protection in the unlikely event that the ECS world is not yet created when attempting to subscribe to these events.
        /// </remarks>
        private IEnumerator DelayEventSubscription()
        {
            var failCount = 0;
            var defaultWorld = World.DefaultGameObjectInjectionWorld;
            while (defaultWorld == null)
            {
                failCount++;
                if (failCount > EVENT_SCHEDULE_FAIL_COUNT)
                {
                    Debug.LogError($"Default World was null for {EVENT_SCHEDULE_FAIL_COUNT} frames. Check to ensure ECS world is being properly initialized.");
                    yield break;
                }
                yield return null;
                defaultWorld = World.DefaultGameObjectInjectionWorld;
            }

            var beginGameOverSystem = defaultWorld.GetExistingSystemManaged<BeginGameOverSystem>();
            beginGameOverSystem.OnGameOver += BeginShowGameOverUI;
        }

        private void UnsubscribeFromEvents()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;
            
            var beginGameOverSystem = world.GetExistingSystemManaged<BeginGameOverSystem>();
            beginGameOverSystem.OnGameOver -= BeginShowGameOverUI;
        }
        
        private void OnButtonQuit()
        {
            _resultsPanel.gameObject.SetActive(true);
            _resultsPanel.ShowResultsUI();
            _doneButton.Select();
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var allEntities = new EntityQueryBuilder(Allocator.Temp).WithPresent<DestroyEntityFlag>().Build(entityManager);
            entityManager.AddComponent<InstantDestroyTag>(allEntities);
            PauseManager.Instance.ResumeGame();
        }

        private void OnButtonDone()
        {
            SceneManager.LoadScene(0);
        }

        private void BeginShowGameOverUI()
        {
            StartCoroutine(BeginShowGameOverUICoroutine());
        }

        private IEnumerator BeginShowGameOverUICoroutine()
        {
            yield return _gameOverDelay;
            ShowGameOverUI();
        }

        private void ShowGameOverUI()
        {
            _gameOverPanel.SetActive(true);
            _quitButton.Select();
            PauseManager.Instance.PauseGame();
        }
    }
}