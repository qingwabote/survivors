using UnityEngine;
using Unity.Entities;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// UI controller associated the UI panel that is displayed when the player picks up a loot crate dropped by a boss enemy.
    /// </summary>
    /// <remarks>
    /// Handles displaying different UI elements and invoking timelines and particle systems for that sweet, sweet dopamine rush.
    /// </remarks>
    public class CrateUIController : MonoBehaviour
    {
        private const int EVENT_SCHEDULE_FAIL_COUNT = 600;

        [SerializeField] private GameObject _crateUIPanel;
        [SerializeField] private CapabilityUpgradeUIController _capabilityUpgradeUIController;
        [SerializeField] private Button _closePanelButton;
        [SerializeField] private Button _openCrateButton;
        [SerializeField] private float _sequenceTime;
        [SerializeField] private TextMeshProUGUI _goldCounterText;
        [SerializeField] private PlayableDirector _crateTimelinePlayer;
        [SerializeField] private TimelineAsset _crateDiscoveredSequence;
        [SerializeField] private TimelineAsset _crateOpenSequence;
        [SerializeField] private TimelineAsset _crateFinalSequence;
        [SerializeField] private TimelineAsset _defaultTimelineState;
        [SerializeField] private Image _selectedItemIconImage;
        [SerializeField] private ParticleSystem[] _particleSystems;
        [SerializeField] private AudioClip _crateDiscoveredAudioClip;
        [SerializeField] private AudioClip _crateOpeningMusic;
        
        private CapabilityUpgradeLevel _upgradeToShow;
        private BonusItemProperties _bonusItemProperties;
        private int _upgradeLevel;
        private EntityManager _entityManager;
        private EntityQuery _gameControllerQuery;
        
        private void Start()
        {
            _crateUIPanel.SetActive(false);
            _capabilityUpgradeUIController.gameObject.SetActive(false);
            _capabilityUpgradeUIController.DisableInteraction();
        }
        
        private void OnEnable()
        {
            StartCoroutine(DelayEventSubscription());
            _closePanelButton.onClick.AddListener(OnButtonClosePanel);
            _openCrateButton.onClick.AddListener(OnButtonOpenCrate);
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            _closePanelButton.onClick.RemoveAllListeners();
            _openCrateButton.onClick.RemoveAllListeners();
        }
        
        /// <summary>
        /// Setup listeners to events on the ECS side to tell this script when to begin displaying the crate UI.
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
            
            _entityManager = defaultWorld.EntityManager;
            _gameControllerQuery = _entityManager.CreateEntityQuery(typeof(GameEntityTag));

            var crateInteractionSystem = defaultWorld.GetExistingSystemManaged<HandleCrateItemInteractionSystem>();
            crateInteractionSystem.OnBeginOpenCrate += OpenCrateUIPanel;
        }

        private void UnsubscribeFromEvents()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;
            
            var crateInteractionSystem = world.GetExistingSystemManaged<HandleCrateItemInteractionSystem>();
            crateInteractionSystem.OnBeginOpenCrate -= OpenCrateUIPanel;
        }

        private void OnButtonClosePanel()
        {
            SelectionIconUIController.Instance.SetPositionOffscreen();
            GameAudioController.Instance.PlayPauseBackgroundMusic();
            
            _crateUIPanel.SetActive(false);
            _capabilityUpgradeUIController.gameObject.SetActive(false);
            _crateTimelinePlayer.Stop();
            _crateTimelinePlayer.playableAsset = _defaultTimelineState;
            _crateTimelinePlayer.extrapolationMode = DirectorWrapMode.Hold;
            _crateTimelinePlayer.Play();

            if (_upgradeToShow.UpgradeProperties != null)
            {
                CapabilityUpgradeController.Instance.UpgradeCapability(_upgradeToShow.UpgradeProperties);
            }
            else if(_bonusItemProperties != null)
            {
                CapabilityUpgradeController.Instance.CollectBonusItem(_bonusItemProperties.ItemType, _bonusItemProperties.ItemValue);
            }
            else
            {
                Debug.LogError("Error: did not apply upgrade or bonus item as both were null.");
            }

            PauseManager.Instance.ResumeGame();
        }

        private void OpenCrateUIPanel(CapabilityUpgradeLevel upgradeToShow)
        {
            GameAudioController.Instance.PlayPauseBackgroundMusic();
            GameAudioController.Instance.PlayPauseResistantAudioClip(_crateDiscoveredAudioClip);
            
            _crateUIPanel.SetActive(true);
            _openCrateButton.Select();
            _crateTimelinePlayer.playableAsset = _crateDiscoveredSequence;
            _crateTimelinePlayer.extrapolationMode = DirectorWrapMode.Loop;
            _crateTimelinePlayer.Play();
            
            _upgradeToShow = upgradeToShow;
            
            PauseManager.Instance.PauseGame();
        }

        private void OnButtonOpenCrate()
        {
            SelectionIconUIController.Instance.SetPositionOffscreen();
            StartCoroutine(OpenCrateSequence());
        }

        private IEnumerator OpenCrateSequence()
        {
            GameAudioController.Instance.PlayPauseResistantAudioClip(_crateOpeningMusic);
            
            _crateTimelinePlayer.playableAsset = _crateOpenSequence;
            _crateTimelinePlayer.extrapolationMode = DirectorWrapMode.Hold;
            _crateTimelinePlayer.Play();
            
            if (_upgradeToShow.UpgradeProperties != null)
            {
                _capabilityUpgradeUIController.SetLevelUpUI(_upgradeToShow);
                _selectedItemIconImage.sprite = _upgradeToShow.UpgradeProperties.Icon;
            }
            else
            {
                var gameControllerEntity = _gameControllerQuery.GetSingletonEntity();
                _bonusItemProperties = _entityManager.GetComponentObject<ManagedGameData>(gameControllerEntity).MoneyBonusItem;
                _capabilityUpgradeUIController.SetLevelUpUI(_bonusItemProperties);
                _selectedItemIconImage.sprite = _bonusItemProperties.ItemIcon;
            }
            
            var finalGoldCount = Random.Range(125, 176);

            var elapsedTime = 0f;

            while (elapsedTime < _sequenceTime)
            {
                var curGoldCount = elapsedTime / _sequenceTime * finalGoldCount;
                _goldCounterText.text = $"{curGoldCount:N2}";
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            _goldCounterText.text = $"<color=yellow>{finalGoldCount:N2}</color>";

            CapabilityUpgradeController.Instance.CollectBonusItem(BonusItemType.Money, finalGoldCount);
        }
        
        public void BeginFinalSequence()
        {
            _closePanelButton.Select();
            _crateTimelinePlayer.playableAsset = _crateFinalSequence;
            _crateTimelinePlayer.extrapolationMode = DirectorWrapMode.Loop;
            _crateTimelinePlayer.Play();
        }

        public void StopParticleSystems()
        {
            foreach (var curParticleSystem in _particleSystems)
            {
                curParticleSystem.Stop();
            }           
        }
    }
}