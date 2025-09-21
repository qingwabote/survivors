using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;
using Unity.Physics.GraphicsIntegration;
using Object = UnityEngine.Object;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Tag component to identify this as the player entity.
    /// </summary>
    /// <remarks>
    /// Several systems and MonoBehaviours assume there is only one player entity in the game world at a time.
    /// </remarks>
    public struct PlayerTag : IComponentData {}

    /// <summary>
    /// Timestamp for when the associated entity is no longer invincible to incoming damage. This component will be enabled when the entity is invincible to incoming damage and disabled for normal behavior where the entity can receive damage.
    /// </summary>
    /// <remarks>
    /// Should the entity be considered invincible, incoming damage buffered in a <see cref="DamageThisFrame"/> dynamic buffer will simply be ignored in the <see cref="ProcessDamageThisFrameSystem"/>.
    /// </remarks>
    public struct InvincibilityExpirationTimestamp : IComponentData, IEnableableComponent
    {
        public double Value;
    }

    /// <summary>
    /// Data component to hold Unity object references to the transform and slider for the player's health bar which exists on a world space canvas.
    /// </summary>
    public struct PlayerWorldUI : ICleanupComponentData
    {
        /// <summary>
        /// Transform of the world UI canvas that contains the player health bar.
        /// </summary>
        public UnityObjectRef<Transform> UICanvas;
        /// <summary>
        /// UnityEngine.UI.Slider component of the player's health bar.
        /// </summary>
        public UnityObjectRef<Slider> HealthBarSlider;
    }
    
    /// <summary>
    /// Flag component to inform the <see cref="PlayerWorldUISystem"/> to update the health slider.
    /// </summary>
    /// <remarks>
    /// This is set to enabled when the player current or max health is changed due to being damaged or picking up an item.
    /// </remarks>
    public struct UpdatePlayerHealthUIFlag : IComponentData, IEnableableComponent {}

    /// <summary>
    /// Current state of the player's experience.
    /// </summary>
    /// <remarks>
    /// Experience points are granted in integers and the number of points required to reach the next level will always be an integer. Total experience points are stored as a float so that fractions of experience points can be accounted for. Player can achieve fractions of experience points through experience point modification (see <see cref="CharacterStatModificationState.ExperiencePointGain"/>
    /// </remarks>
    public struct PlayerExperienceState : IComponentData
    {
        /// <summary>
        /// Total experience points the player has achieved in the current game run.
        /// </summary>
        public float TotalPoints;
        /// <summary>
        /// Level the player is on.
        /// </summary>
        /// <remarks>
        /// Player starts the game at level 1 and level is incremented once <see cref="TotalPoints"/> is greater than or equal to <see cref="PointsToNextLevel"/>.
        /// </remarks>
        public int Level;
        /// <summary>
        /// Total experience points required for the player to level up.
        /// </summary>
        public int PointsToNextLevel;
    }

    /// <summary>
    /// Dynamic buffer to queue experience points to add to the player in a given frame.
    /// </summary>
    /// <remarks>
    /// Purpose of the dynamic buffer is so that experience points can potentially be granted from multiple threads and all be properly accounted for. This also leads to a single system (in this case <see cref="HandlePlayerExperienceThisFrameSystem"/>) for handling the addition of player experience to the player and performing any additional logic i.e. leveling up.
    /// </remarks>
    /// <seealso cref="HandlePlayerExperienceThisFrameSystem"/>
    [InternalBufferCapacity(1)]
    public struct PlayerExperienceThisFrame : IBufferElementData
    {
        public int Value;
    }

    /// <summary>
    /// Data component to store the player's previous input.
    /// </summary>
    public struct PreviousPlayerInput : IComponentData
    {
        /// <summary>
        /// Player's input from the previous frame.
        /// </summary>
        /// <remarks>
        /// This is required for the game to know when the player switches between moving and not moving states to change player's animation.
        /// </remarks>
        public float2 PreviousInput;
        /// <summary>
        /// Player's last non-zero input.
        /// </summary>
        /// <remarks>
        /// Used for firing attacks in the direction of the player's most recent input when not moving.
        /// </remarks>
        /// <seealso cref="ScrewdriverAttackSystem"/>
        public float2 LastPositiveInput;
        /// <summary>
        /// Sign of player's last non-zero input on the x-axis.
        /// </summary>
        /// <remarks>
        /// Used for attack and animation systems that need to know the player's facing direction. Only updates when x-input is non-zero. Will be -1 for left and +1 for right.
        /// </remarks>
        public float LastFacingDirection;
    }

    /// <summary>
    /// Helper struct to pair an <see cref="UpgradeProperties"/> ScriptableObject with the index of its next level.
    /// </summary>
    public struct CapabilityUpgradeLevel
    {
        public UpgradeProperties UpgradeProperties;
        public int NextLevelIndex;
    }
    
    /// <summary>
    /// Authoring script to add components specific to the player entity.
    /// </summary>
    [RequireComponent(typeof(CharacterAuthoring))]
    public class PlayerAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerTag>(entity);
                AddComponent<PlayerExperienceState>(entity);
                AddBuffer<PlayerExperienceThisFrame>(entity);
                AddComponent(entity, new PreviousPlayerInput
                {
                    PreviousInput = float2.zero,
                    LastPositiveInput = math.right().xz, 
                    LastFacingDirection = 1f
                });
                AddComponent<UpdatePlayerHealthUIFlag>(entity);
                SetComponentEnabled<UpdatePlayerHealthUIFlag>(entity, true);
                AddComponent<InvincibilityExpirationTimestamp>(entity);
                SetComponentEnabled<InvincibilityExpirationTimestamp>(entity, false);
                AddComponent<PhysicsGraphicalInterpolationBuffer>(entity);
            }
        }
    }

    /// <summary>
    /// System to initialize values pertaining to player entities.
    /// </summary>
    /// <remarks>
    /// System is a SystemBase system so it can have System.Action events as member variables that can be invoked when UI elements need to be updated.
    /// </remarks>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup))]
    [UpdateBefore(typeof(CharacterInitializationSystem))]
    public partial class PlayerInitializationSystem : SystemBase
    {
        /// <summary>
        /// Event to be invoked to initialize UI elements corresponding to the player's starting level.
        /// Arguments passed:
        /// 1. int - level the player is upgrading to
        /// 2. int - minimum experience points for the level the player is upgrading to
        /// 3. int - maximum experience points for the level the player is upgrading to
        /// </summary>
        /// <seealso cref="HUDUIController.UpdatePlayerLevel"/>
        public Action<int, int, int> OnInitializePlayerLevel;

        protected override void OnCreate()
        {
            RequireForUpdate<ExperiencePointTable>();
        }
        
        protected override void OnUpdate()
        {
            var didInitializePlayer = false;
            
            foreach (var playerExperienceState in SystemAPI.Query<RefRW<PlayerExperienceState>>().WithAll<PlayerTag, InitializeCharacterFlag>())
            {
                var level2ExperiencePoints = SystemAPI.GetSingleton<ExperiencePointTable>()[1];
                playerExperienceState.ValueRW = new PlayerExperienceState
                {
                    TotalPoints = 0,
                    Level = 1,
                    PointsToNextLevel = level2ExperiencePoints
                };
                OnInitializePlayerLevel?.Invoke(1, 0, level2ExperiencePoints);
                didInitializePlayer = true;
            }
            
            // This must be called outside the above foreach loop otherwise exceptions will be thrown as EntityManager structural changes run in EntityUpgradeController.UpgradeCapability().
            if (didInitializePlayer)
            {
                var selectedCharacter = SystemAPI.GetSingleton<SelectedCharacterReference>().Value.Value;
                CapabilityUpgradeController.Instance.UpgradeCapability(selectedCharacter.StartingWeapon);
            }
        }
    }

    /// <summary>
    /// System for reading input from the input system package and assigning it to the player's <see cref="CharacterMoveDirection"/>.
    /// </summary>
    /// <remarks>
    /// System is a SystemBase type so it can have a managed member variable for the input actions.
    /// Updates in the <see cref="DS_InitializationSystemGroup"/> so input is read early in the frame.
    /// </remarks>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup))]
    public partial class GetPlayerInputSystem : SystemBase
    {
        /// <summary>
        /// Input actions the system will be reading from.
        /// </summary>
        private DOTSSurvivorsInputActions _inputActions;

        protected override void OnCreate()
        {
            _inputActions = new DOTSSurvivorsInputActions();
            
            RequireForUpdate<PlayerTag>();
        }

        protected override void OnStartRunning()
        {
            _inputActions.Enable();
        }

        protected override void OnUpdate()
        {
            var curMovement = (float2)_inputActions.Player.Move.ReadValue<Vector2>();
            foreach (var (moveDirection, previousPlayerInput) in SystemAPI.Query<RefRW<CharacterMoveDirection>, RefRW<PreviousPlayerInput>>().WithAll<PlayerTag>())
            {
                previousPlayerInput.ValueRW.PreviousInput = moveDirection.ValueRO.Value;
                moveDirection.ValueRW.Value = curMovement;

                if (math.lengthsq(curMovement) > float.Epsilon)
                {
                    previousPlayerInput.ValueRW.LastPositiveInput = curMovement;
                }

                if (math.abs(curMovement.x) > float.Epsilon)
                {
                    previousPlayerInput.ValueRW.LastFacingDirection = math.sign(curMovement.x);
                }
            }
        }
        
        protected override void OnStopRunning()
        {
            _inputActions.Disable();
        }
    }
    
    /// <summary>
    /// System to take the player off invincibility once the invincibility expiration timestamp is reached. As <see cref="InvincibilityExpirationTimestamp"/> is an enableable component, entities are only considered invincible when the component is enabled; foreach loop will also only iterate over entities with the component enabled.
    /// </summary>
    public partial struct InvincibilityExpirationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (invincibilityExpirationTimestamp, isInvincible) in SystemAPI.Query<InvincibilityExpirationTimestamp, EnabledRefRW<InvincibilityExpirationTimestamp>>())
            {
                if (invincibilityExpirationTimestamp.Value >= SystemAPI.Time.ElapsedTime) continue;
                isInvincible.ValueRW = false;
            }
        }
    }

    /// <summary>
    /// System to initialize, update, and cleanup the player UI - used for player's in-world health bar.
    /// </summary>
    /// <remarks>
    /// This system contains multiple foreach that do the following:
    /// 1. Instantiates an instance of the player world UI and sets up UnityObjectRef references in the <see cref="PlayerWorldUI"/> component.
    /// 2. Updates the position of the world UI instance to match that of the player, so the health bar appears to follow the player entity.
    /// 3. When the <see cref="UpdatePlayerHealthUIFlag"/> is enabled, update the current and max health values on the health bar slider UI element.
    /// 4. When the player is destroyed, destroy the player world UI instance so it no longer exists in the game world.
    /// </remarks>
    /// <seealso cref="PlayerWorldUI"/>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct PlayerWorldUISystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameEntityTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            
            // Instantiates an instance of the player world UI and sets up UnityObjectRef references in the PlayerWorldUI component.
            foreach (var (localToWorld, entity) in SystemAPI.Query<LocalToWorld>().WithAll<PlayerTag>().WithNone<PlayerWorldUI>().WithEntityAccess())
            {
                var gameControllerEntity = SystemAPI.GetSingletonEntity<GameEntityTag>();
                var playerWorldUIPrefab = state.EntityManager.GetComponentObject<ManagedGameData>(gameControllerEntity).PlayerWorldUICanvasPrefab;
                var newPlayerWorldUI = Object.Instantiate(playerWorldUIPrefab, localToWorld.Position, Quaternion.Euler(90f, 0f, 0f));
                var uiTransform = newPlayerWorldUI.transform;
                var uiSlider = newPlayerWorldUI.GetComponentInChildren<Slider>();
                uiSlider.value = 1f;
                
                ecb.AddComponent(entity, new PlayerWorldUI
                {
                    UICanvas = new UnityObjectRef<Transform> { Value = uiTransform },
                    HealthBarSlider = new UnityObjectRef<Slider> { Value = uiSlider }
                });
            }

            // Updates the position of the world UI instance to match that of the player, so the health bar appears to follow the player entity.
            foreach (var (localToWorld, worldUIReference) in SystemAPI.Query<LocalToWorld, PlayerWorldUI>())
            {
                worldUIReference.UICanvas.Value.position = localToWorld.Position;
            }

            // When the UpdatePlayerHealthUIFlag is enabled, update the current and max health values on the health bar slider UI element.
            foreach (var (currentHitPoints, baseHitPoints, characterStats, playerWorldUIReference, forceUpdatePlayerHealthUIFlag) in SystemAPI.Query<CurrentHitPoints, BaseHitPoints, CharacterStatModificationState, PlayerWorldUI, EnabledRefRW<UpdatePlayerHealthUIFlag>>().WithAll<PlayerTag>())
            {
                forceUpdatePlayerHealthUIFlag.ValueRW = false;
                var maxHitPoints = baseHitPoints.Value + characterStats.AdditionalHitPoints;
                var hitPointSliderValue = (float)currentHitPoints.Value / maxHitPoints;
                playerWorldUIReference.HealthBarSlider.Value.value = hitPointSliderValue;
            }

            // When the player is destroyed, destroy the player world UI instance so it no longer exists in the game world.
            foreach (var (playerWorldUIReference, entity) in SystemAPI.Query<PlayerWorldUI>().WithNone<LocalToWorld>().WithEntityAccess())
            {
                if (playerWorldUIReference.UICanvas.Value != null)
                {
                    Object.Destroy(playerWorldUIReference.UICanvas.Value.gameObject);
                }
                ecb.RemoveComponent<PlayerWorldUI>(entity);
            }
            
            ecb.Playback(state.EntityManager);
        }
    }

    /// <summary>
    /// System to add player experience from the player's <see cref="PlayerExperienceThisFrame"/> dynamic buffer to the player's <see cref="PlayerExperienceState.TotalPoints"/>. This system also determines if the player should level up. If the player should level up, then this system will also determine a weapon or passive ability to upgrade.
    /// </summary>
    /// <remarks>
    /// System is a SystemBase system so it can have System.Action events as member variables that can be invoked when UI elements need to be updated.
    /// </remarks>
    [UpdateAfter(typeof(DS_InteractionSystemGroup))]
    public partial class HandlePlayerExperienceThisFrameSystem : SystemBase
    {
        /// <summary>
        /// When determining a random upgrade, the system will try to find an upgrade this many times. If it still fails after this amount, then the system can infer that the player has no further upgrades available and will thus display health and money bonuses.
        /// </summary>
        private const int FAILED_RANDOM_COUNT = 500;
        /// <summary>
        /// Duration in seconds for how long the player will become invincible after leveling up.
        /// </summary>
        private const double POST_LEVEL_UP_INVINCIBILITY_DURATION = 0.15;
        /// <summary>
        /// Event to be invoked when the player levels up.
        /// Arguments passed:
        /// 1. int - level the player is upgrading to
        /// 2. int - minimum experience points for the level the player is upgrading to
        /// 3. int - maximum experience points for the level the player is upgrading to
        /// </summary>
        /// <seealso cref="HUDUIController.UpdatePlayerLevel"/>
        public Action<int, int, int> OnUpdatePlayerLevel;
        /// <summary>
        /// Event to be invoked when the player gains experience points. Used for UI to update the HUD experience slider at the top of the screen.
        /// Argument passed:
        /// 1. float - Player's new total experience point value.
        /// </summary>
        public Action<float> OnUpdatePlayerExperience;
        /// <summary>
        /// Event to be invoked when the player levels up.
        /// <see cref="CapabilityUpgradeLevel"/> array passed containing the selected <see cref="UpgradeProperties"/> and next level indices for the selected upgrades.
        /// </summary>
        /// <seealso cref="LevelUpUIController.ShowLevelUpUI"/>
        public Action<CapabilityUpgradeLevel[]> OnBeginLevelUp;
        
        protected override void OnCreate()
        {
            RequireForUpdate<ExperiencePointTable>();
            RequireForUpdate<GameEntityTag>();
            RequireForUpdate<PlayerTag>();
        }

        protected override void OnUpdate()
        {
            foreach (var (playerExperienceThisFrame, playerExperienceState, characterStats, entity) in SystemAPI.Query<DynamicBuffer<PlayerExperienceThisFrame>, RefRW<PlayerExperienceState>, CharacterStatModificationState>().WithNone<InitializeCharacterFlag>().WithEntityAccess())
            {
                var playerExperienceUpdated = false;
                foreach (var experiencePoint in playerExperienceThisFrame)
                {
                    playerExperienceState.ValueRW.TotalPoints += experiencePoint.Value * characterStats.ExperiencePointGain;
                    playerExperienceUpdated = true;
                }

                // Check for leveling up
                if (playerExperienceState.ValueRO.TotalPoints >= playerExperienceState.ValueRO.PointsToNextLevel)
                {
                    var experiencePointsForPrevLevel = playerExperienceState.ValueRO.PointsToNextLevel;
                    var nextLevel = playerExperienceState.ValueRW.Level += 1;
                    playerExperienceState.ValueRW.PointsToNextLevel = SystemAPI.GetSingleton<ExperiencePointTable>()[nextLevel] + experiencePointsForPrevLevel;
                    var minExperiencePointsForLevel = experiencePointsForPrevLevel;
                    var maxExperiencePointsForLevel = playerExperienceState.ValueRO.PointsToNextLevel;
                    OnUpdatePlayerLevel?.Invoke(nextLevel, minExperiencePointsForLevel, maxExperiencePointsForLevel);

                    var gameEntity = SystemAPI.GetSingletonEntity<GameEntityTag>();
                    var upgradeData = EntityManager.GetComponentObject<PlayerUpgradeData>(gameEntity);
                    var random = SystemAPI.GetComponentRW<EntityRandom>(gameEntity);
                    var randomUpgrades = new List<CapabilityUpgradeLevel>();

                    for (var i = 0; i < 3; i++)
                    {
                        UpgradeProperties upgradePropertiesToTest;
                        int nextLevelIndex;
                        bool getNewRandomProperties;
                        var failCounter = 0;

                        do
                        {
                            failCounter++;
                            getNewRandomProperties = false;
                            upgradePropertiesToTest = upgradeData.GetRandomUpgradeProperties(ref random.ValueRW.Value);
                            nextLevelIndex = 0;

                            // Check to ensure the current upgrade isn't already selected
                            foreach (var previouslySelectedUpgrade in randomUpgrades)
                            {
                                if (previouslySelectedUpgrade.UpgradeProperties == upgradePropertiesToTest)
                                {
                                    getNewRandomProperties = true;
                                    break;
                                }
                            }

                            if (!getNewRandomProperties)
                            {
                                if (upgradePropertiesToTest is WeaponUpgradeProperties weaponPropertiesToTest)
                                {
                                    // If the player already has this weapon, select it if it is not at the max level
                                    if (upgradeData.ActiveWeaponEntityLookup.TryGetValue(weaponPropertiesToTest, out var weaponEntity))
                                    {
                                        var curWeaponLevelIndex = SystemAPI.GetComponent<WeaponState>(weaponEntity).LevelIndex;
                                        var maxLevelIndex = SystemAPI.GetComponent<WeaponUpgradePropertiesReference>(weaponEntity).Value.Value.MaxLevelIndex;
                                        if (curWeaponLevelIndex >=  maxLevelIndex)
                                        {
                                            getNewRandomProperties = true;
                                        }
                                        else
                                        {
                                            // Store the next level to be displayed in Level Up UI
                                            nextLevelIndex = curWeaponLevelIndex + 1;
                                        }
                                    }
                                    // If the player does not have the weapon and no free slots, try again
                                    else if (!upgradeData.HasFreeWeaponSlots)
                                    {
                                        getNewRandomProperties = true;
                                    }
                                    // Else this is a new weapon the player can select
                                }
                                else if (upgradePropertiesToTest is PassiveUpgradeProperties passivePropertiesToTest)
                                {
                                    // If the player already has this passive ability, select it if it is not at the max level
                                    if (upgradeData.ActivePassiveEntityLookup.TryGetValue(passivePropertiesToTest, out var passiveEntity))
                                    {
                                        var curPassiveLevelIndex = SystemAPI.GetComponent<PassiveLevelIndex>(passiveEntity).Value;
                                        var passiveMaxLevelIndex = SystemAPI.GetComponent<PassiveUpgradePropertiesReference>(passiveEntity).Value.Value.MaxLevelIndex;
                                        if (curPassiveLevelIndex >= passiveMaxLevelIndex)
                                        {
                                            getNewRandomProperties = true;
                                        }
                                        else
                                        {
                                            // Store the next level to be displayed in Level Up UI
                                            nextLevelIndex = curPassiveLevelIndex + 1;
                                        }
                                    }
                                    // If the player does not have the passive and no free slots, try again
                                    else if (!upgradeData.HasFreePassiveSlots)
                                    {
                                        getNewRandomProperties = true;
                                    }
                                    // Else this is a new passive the player can select
                                }
                                else
                                {
                                    Debug.LogError("Error: undefined upgrade type");
                                    getNewRandomProperties = true;
                                }
                            }

                            if (failCounter >= FAILED_RANDOM_COUNT) upgradePropertiesToTest = null;
                        } while (getNewRandomProperties && failCounter < FAILED_RANDOM_COUNT);

                        if (upgradePropertiesToTest != null)
                        {
                            randomUpgrades.Add(new CapabilityUpgradeLevel
                            {
                                UpgradeProperties = upgradePropertiesToTest,
                                NextLevelIndex = nextLevelIndex
                            });
                        }
                    }
                    
                    OnBeginLevelUp?.Invoke(randomUpgrades.ToArray());
                    SystemAPI.SetComponent(entity, new InvincibilityExpirationTimestamp { Value = SystemAPI.Time.ElapsedTime + POST_LEVEL_UP_INVINCIBILITY_DURATION });
                    SystemAPI.SetComponentEnabled<InvincibilityExpirationTimestamp>(entity, true);
                }

                // Update player experience slider at top of screen if the player gained experience this frame.
                if (playerExperienceUpdated)
                {
                    OnUpdatePlayerExperience?.Invoke(playerExperienceState.ValueRO.TotalPoints);
                }
                
                playerExperienceThisFrame.Clear();
            }
        }
    }
}