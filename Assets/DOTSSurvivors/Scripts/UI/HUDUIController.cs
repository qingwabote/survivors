using System;
using System.Collections;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// UI controller associated with the in-game HUD UI.
    /// </summary>
    /// <remarks>
    /// Controls all UI elements the player can reference when in the game.
    /// </remarks>
    public class HUDUIController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _enemiesDefeatedText;
        [SerializeField] private TextMeshProUGUI _coinsText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private Slider _playerExperienceSlider;
        [SerializeField] private Image[] _currentWeaponIconImages;
        [SerializeField] private Image[] _currentPassiveIconImages;

        private int _currentWeaponCount;
        private int _currentPassiveCount;
        private CanvasGroup _canvasGroup;
        private bool showHud = true;
        
        private const int EVENT_SCHEDULE_FAIL_COUNT = 600;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }
        
        private void OnEnable()
        {
            StartCoroutine(DelayEventSubscription());
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }
        
        private void Start()
        {
            UpdateEnemiesDefeatedText(0);
            UpdateCoinsText(0);
            UpdatePlayerLevel(1, 0, 10);
            UpdatePlayerExperience(0);
            UpdateGameTime(0);
            
            CapabilityUpgradeController.Instance.OnAddNewWeapon += AddWeaponIcon;
            CapabilityUpgradeController.Instance.OnAddNewPassive += AddPassiveIcon;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                showHud = !showHud;
                _canvasGroup.alpha = showHud ? 1f : 0f;
            }
        }
        
        /// <summary>
        /// Setup listeners to events on the ECS side to tell this script when to update certain UI elements.
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

            SubscribeToEvents(defaultWorld);
        }

        private void SubscribeToEvents(World world)
        {
            var playerExperienceSystem = world.GetExistingSystemManaged<HandlePlayerExperienceThisFrameSystem>();
            playerExperienceSystem.OnUpdatePlayerLevel += UpdatePlayerLevel;
            playerExperienceSystem.OnUpdatePlayerExperience += UpdatePlayerExperience;

            var playerInitializationSystem = world.GetExistingSystemManaged<PlayerInitializationSystem>();
            playerInitializationSystem.OnInitializePlayerLevel += UpdatePlayerLevel;

            var updateUISystem = world.GetExistingSystemManaged<UpdateUISystem>();
            updateUISystem.OnUpdateEnemiesDefeatedCount += UpdateEnemiesDefeatedText;
            
            var handleCoinItemInteractionSystem = world.GetExistingSystemManaged<GrantMoneyOnInteractionSystem>();
            handleCoinItemInteractionSystem.OnUpdateCoinCount += UpdateCoinsText;

            var updateGameTimeSystem = world.GetExistingSystemManaged<UpdateGameTimeSystem>();
            updateGameTimeSystem.OnUpdateGameTime += UpdateGameTime;
        }

        private void UnsubscribeFromEvents()
        {
            CapabilityUpgradeController.Instance.OnAddNewWeapon -= AddWeaponIcon;
            CapabilityUpgradeController.Instance.OnAddNewPassive -= AddPassiveIcon;
            
            if (World.DefaultGameObjectInjectionWorld == null) return;
            var world = World.DefaultGameObjectInjectionWorld;
            
            var playerExperienceSystem = world.GetExistingSystemManaged<HandlePlayerExperienceThisFrameSystem>();
            playerExperienceSystem.OnUpdatePlayerLevel -= UpdatePlayerLevel;
            playerExperienceSystem.OnUpdatePlayerExperience -= UpdatePlayerExperience;

            var playerInitializationSystem = world.GetExistingSystemManaged<PlayerInitializationSystem>();
            playerInitializationSystem.OnInitializePlayerLevel -= UpdatePlayerLevel;

            var updateUISystem = world.GetExistingSystemManaged<UpdateUISystem>();
            updateUISystem.OnUpdateEnemiesDefeatedCount -= UpdateEnemiesDefeatedText;

            var handleCoinItemInteractionSystem = world.GetExistingSystemManaged<GrantMoneyOnInteractionSystem>();
            handleCoinItemInteractionSystem.OnUpdateCoinCount -= UpdateCoinsText;

            var updateGameTimeSystem = world.GetExistingSystemManaged<UpdateGameTimeSystem>();
            updateGameTimeSystem.OnUpdateGameTime -= UpdateGameTime;
        }
        
        private void UpdateEnemiesDefeatedText(int newScore)
        {
            _enemiesDefeatedText.text = $"{newScore:N0}";
        }

        private void UpdateCoinsText(int coins)
        {
            _coinsText.text = $"{coins:N0}";
        }

        private void UpdatePlayerExperience(float experiencePoints)
        {
            _playerExperienceSlider.value = experiencePoints;
        }

        private void UpdatePlayerLevel(int levelNumber, int minExperiencePoints, int maxExperiencePoints)
        {
            _levelText.text = $"LVL: {levelNumber:N0}";
            _playerExperienceSlider.maxValue = maxExperiencePoints;
            _playerExperienceSlider.minValue = minExperiencePoints;
        }

        private void UpdateGameTime(float secondsInGame)
        {
            var timeInGame = TimeSpan.FromSeconds(Mathf.FloorToInt(secondsInGame));
            _timeText.text = $"{timeInGame:m\\:ss}";
        }

        private void AddWeaponIcon(WeaponUpgradeProperties upgradeProperties)
        {
            _currentWeaponIconImages[_currentWeaponCount].sprite = upgradeProperties.Icon;
            _currentWeaponCount += 1;
        }
        
        private void AddPassiveIcon(PassiveUpgradeProperties upgradeProperties)
        {
            _currentPassiveIconImages[_currentPassiveCount].sprite = upgradeProperties.Icon;
            _currentPassiveCount += 1;
        }
    }
}