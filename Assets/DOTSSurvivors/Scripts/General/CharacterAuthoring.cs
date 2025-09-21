using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Physics.GraphicsIntegration;
using UnityEngine;
using Unity.Transforms;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Flag component to signify this character entity needs to be initialized via the <see cref="CharacterInitializationSystem"/>.
    /// </summary>
    /// <remarks>
    /// Characters spawn with this flag enabled, once initialization for the character is complete, this component will be disabled.
    /// Initialization should happen very shortly after the character is spawned.
    /// </remarks>
    public struct InitializeCharacterFlag : IComponentData, IEnableableComponent {}

    /// <summary>
    /// Component to hold the character's base movement speed in units per second.
    /// </summary>
    /// <remarks>
    /// Movement speed can be temporarily or permanently modified via stat modification (see <see cref="CharacterStatModificationState.MoveSpeed"/>)
    /// </remarks>
    public struct CharacterBaseMoveSpeed : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// Component to hold the character's hit point accumulator.
    /// When component is enabled, health regeneration will take place in the <see cref="CharacterHealthRegenerationSystem"/>.
    /// </summary>
    public struct CharacterHealthRegenerationState : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// Holds fractional values of hit points to recover. Once value is >= 1, 1 hit point will be added the character's <see cref="CurrentHitPoints"/> until it reaches the player's maximum hit point value.
        /// </summary>
        /// <remarks>
        /// This is required because hit points are stored as integers, but health regeneration recovers fractions of a hit point per second and occurs continuously when the player is not at max health.
        /// </remarks>
        public float HitPointAccumulator;
    }

    /// <summary>
    /// Component defining the current move direction of a character.
    /// </summary>
    /// <remarks>
    /// Character move direction is set in various systems depending on the type of character. Player entities get their value set in <see cref="GetPlayerInputSystem"/>, while enemies get their values set in systems like <see cref="EnemyMoveToPlayerJob"/> or <see cref="EnemySineWaveMovementSystem"/>.
    /// Non-zero values should be normalized.
    /// </remarks>
    public struct CharacterMoveDirection : IComponentData
    {
        public float2 Value;
    }

    /// <summary>
    /// Component for various entities to reference their associated character entity.
    /// </summary>
    public struct CharacterEntity : IComponentData
    {
        public Entity Value;
    }
    
    /// <summary>
    /// Dynamic buffer to hold the currently active stat modifier entities.
    /// </summary>
    /// <remarks>
    /// Stat modifier entities each have their own <see cref="StatModifier"/> dynamic buffer which contains the specific stats that entity will be modifying. The purpose of having stat modifier entities is to enable the ability to have temporary stat modifications so that for example, a stat modifier entity can be destroyed after a set amount of time and all other stat modifications will remain in effect.
    /// Internal buffer capacity is set to 0, meaning all elements of the dynamic buffer will live outside the chunk. This is acceptable for this case as the buffer is only walked occasionally when a character's stats need to be recalculated in the <see cref="RecalculateStatsSystem"/>.
    /// </remarks>
    /// <seeaslo cref="CharacterStatModificationState"/>
    /// <seeaslo cref="StatModifier"/>
    /// <seeaslo cref="RecalculateStatsSystem"/>
    /// <seeaslo cref="StatModifierProperties"/>
    [InternalBufferCapacity(0)]
    public struct ActiveStatModifierEntity : IBufferElementData
    {
        public Entity Value;
    }

    /// <summary>
    /// Tag component to inform the <see cref="DestroyEntitySystem"/> this character has a destruction animation that should play when the character is destroyed.
    /// </summary>
    public struct GraphicsEntityPlayDestroyEffectTag : IComponentData {}
    
    /// <summary>
    /// Enableable component to signify this entity needs their <see cref="CharacterStatModificationState"/> need to be recalculated in the <see cref="RecalculateStatsSystem"/>.
    /// </summary>
    /// <seealso cref="CapabilityUpgradeController.UpgradePassive"/>
    public struct RecalculateStatsFlag : IComponentData, IEnableableComponent {}
    
    /// <summary>
    /// Data component to store the current stat modification values for a character.
    /// Several systems reference this component to calculate effective stat data (i.e. take the base value of a stat, and add or multiply the relevant stat modification value as required).
    /// </summary>
    public struct CharacterStatModificationState : IComponentData
    {
        /// <summary>
        /// Move speed modification. This value is multiplied by the character's <see cref="CharacterBaseMoveSpeed"/> in the <see cref="CharacterMoveSystem"/> to calculate the current effective move speed.
        /// </summary>
        public float MoveSpeed;
        /// <summary>
        /// Damage dealing modification. This value is multiplied by the base damage value for an attack in various attack systems to calculate the current effective damage to be assigned to that attack entity.
        /// </summary>
        public float DamageDealt;
        /// <summary>
        /// Additional hit points to increase the character's maximum hit points.
        /// </summary>
        public int AdditionalHitPoints;
        /// <summary>
        /// Reduce incoming damage points by this integer value.
        /// </summary>
        public int DamageReceived;
        /// <summary>
        /// Health regeneration modification. This value represents how many hit points per second are restored in the <see cref="CharacterHealthRegenerationSystem"/>.
        /// </summary>
        public float HealthRegeneration;
        /// <summary>
        /// Attack cooldown modification. This value is multiplied by the base cooldown for an attack in various attack systems to calculate the current effective cooldown for that weapon.
        /// </summary>
        public float AttackCooldown;
        /// <summary>
        /// Attack projectile speed modification. This value is multiplied by the base projectile speed for an attack in various attack systems to calculate the current effective projectile speed of attack entities.
        /// </summary>
        public float AttackProjectileSpeed;
        /// <summary>
        /// Attack duration modification. This value is multiplied by the base attack duration for an attack in various attack systems to calculate the current effective duration for how long the attack entity exists in the world.
        /// </summary>
        public float AttackDuration;
        /// <summary>
        /// Number of additional attack projectiles to spawn for each applicable attack group.
        /// </summary>
        public int AdditionalAttackProjectiles;
        /// <summary>
        /// Attack area modification. This value is multiplied by the base attack area for an attack in various attack systems to calculate the current scale of the attack.
        /// </summary>
        public float AttackArea;
        /// <summary>
        /// Item attraction radius modifier. This value is multiplied by the player's <see cref="PlayerAttractionAreaData.Radius"/> in the <see cref="DetectItemAttractionSystem"/> to calculate the effective attraction area for the player.
        /// </summary>
        public float ItemAttractionRadius;
        /// <summary>
        /// Experience point gain modifier. This value is multiplied by the value of experience points collected by the player in the <see cref="HandlePlayerExperienceThisFrameSystem"/> to calculate the effective experience point gain for the collected gem.
        /// </summary>
        public float ExperiencePointGain;

        /// <summary>
        /// Constructor for easy creation of initial CharacterStatModificationState using <see cref="CharacterDefaultModificationValues"/>.
        /// </summary>
        /// <remarks>
        /// Used during <see cref="CharacterInitializationSystem"/> and <see cref="RecalculateStatsSystem"/> to get a baseline of stat values before applying modifications stored in the <see cref="StatModifier"/> buffers of stat modifier entities stored in <see cref="ActiveStatModifierEntity"/>.
        /// </remarks>
        /// <param name="defaultValues">Default values for each modification state.</param>
        public CharacterStatModificationState(CharacterDefaultModificationValues defaultValues)
        {
            MoveSpeed = defaultValues.MoveSpeed;
            DamageDealt = defaultValues.DamageDealt;
            AdditionalHitPoints = defaultValues.AdditionalHitPoints;
            DamageReceived = defaultValues.DamageReceived;
            HealthRegeneration = defaultValues.HealthRegeneration;
            AttackCooldown = defaultValues.AttackCooldown;
            AttackProjectileSpeed = defaultValues.AttackProjectileSpeed;
            AttackDuration = defaultValues.AttackDuration;
            AdditionalAttackProjectiles = defaultValues.AdditionalAttackProjectiles;
            AttackArea = defaultValues.AttackArea;
            ItemAttractionRadius = defaultValues.ItemAttractionRadius;
            ExperiencePointGain = defaultValues.ExperiencePointGain;
        }

        /// <summary>
        /// Method to add stat modifiers to the character's current set of stat modifications. This method will also clamp the stat modification values to ensure they stay within an acceptable range to facilitate balanced gameplay.
        /// </summary>
        /// <remarks>
        /// Certain values are stored as integers and must be casted to an integer after clamping.
        /// Minimum and maximum stat modification values are authored in <see cref="StatModifierProperties"/>, stored in <see cref="StatModifierController._statModifierPropertiesLookup"/>, and clamped in <see cref="StatModifierHelper.ClampModificationValue"/>.
        /// </remarks>
        /// <param name="statModifier">Stat modifier to add to the character's current stat modifications.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if an invalid <see cref="StatModifierType"/> is set on this <see cref="StatModifier"/> element.</exception>
        public void AddStat(StatModifier statModifier)
        {
            switch (statModifier.Type)
            {
                case StatModifierType.DamageDealt:
                    DamageDealt += statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref DamageDealt);
                    break;
                case StatModifierType.DamageReceived:
                    var modifiedDamageReceived = DamageReceived + statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref modifiedDamageReceived);
                    DamageReceived = (int)modifiedDamageReceived;
                    break;
                case StatModifierType.AdditionalHitPoints:
                    var modifiedMaxHealth = AdditionalHitPoints + statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref modifiedMaxHealth);
                    AdditionalHitPoints = (int)modifiedMaxHealth;
                    break;
                case StatModifierType.HealthRegeneration:
                    HealthRegeneration += statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref HealthRegeneration);
                    break;
                case StatModifierType.AttackCooldown:
                    AttackCooldown += statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref AttackCooldown);
                    break;
                case StatModifierType.AttackArea:
                    AttackArea += statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref AttackArea);
                    break;
                case StatModifierType.AttackProjectileSpeed:
                    AttackProjectileSpeed += statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref AttackProjectileSpeed);
                    break;
                case StatModifierType.MoveSpeed:
                    MoveSpeed += statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref MoveSpeed);
                    break;
                case StatModifierType.AttackDuration:
                    AttackDuration += statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref AttackDuration);
                    break;
                case StatModifierType.AdditionalAttackProjectiles:
                    var modifiedAdditionalProjectiles = AdditionalAttackProjectiles + statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref modifiedAdditionalProjectiles);
                    AdditionalAttackProjectiles = (int)modifiedAdditionalProjectiles;
                    break;
                case StatModifierType.ItemAttractionRadius:
                    ItemAttractionRadius += statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref ItemAttractionRadius);
                    break;
                case StatModifierType.ExperiencePointGain:
                    ExperiencePointGain += statModifier.Value;
                    statModifier.Type.ClampModificationValue(ref ExperiencePointGain);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        /// <summary>
        /// Helper function to get the current modification value for a given <see cref="StatModifierType"/>.
        /// </summary>
        /// <param name="type">Type of stat modifier to be returned.</param>
        /// <returns>The current value of the requested stat modifier. Note that this always returns a float although certain stats are stored as integers.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Throws if an invalid <see cref="StatModifierType"/> is passed into this function.</exception>
        public float GetCurrentValue(StatModifierType type)
        {
            return type switch
            {
                StatModifierType.DamageDealt => DamageDealt,
                StatModifierType.DamageReceived => DamageReceived,
                StatModifierType.AdditionalHitPoints => AdditionalHitPoints,
                StatModifierType.HealthRegeneration => HealthRegeneration,
                StatModifierType.AttackCooldown => AttackCooldown,
                StatModifierType.AttackArea => AttackArea,
                StatModifierType.AttackProjectileSpeed => AttackProjectileSpeed,
                StatModifierType.MoveSpeed => MoveSpeed,
                StatModifierType.AttackDuration => AttackDuration,
                StatModifierType.AdditionalAttackProjectiles => AdditionalAttackProjectiles,
                StatModifierType.ItemAttractionRadius => ItemAttractionRadius,
                StatModifierType.ExperiencePointGain => ExperiencePointGain,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }

    /// <summary>
    /// Authoring script to add necessary components to character entities.
    /// </summary>
    /// <remarks>
    /// Requiring the <see cref="DamageableEntityAuthoring"/> and <see cref="GraphicsEntityAuthoring"/> to ensure all necessary components are added.
    /// </remarks>
    [RequireComponent(typeof(DamageableEntityAuthoring))]
    [RequireComponent(typeof(GraphicsEntityAuthoring))]
    public class CharacterAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Base movement speed of the character in units per second.
        /// </summary>
        public float MoveSpeed;
        
        private class Baker : Baker<CharacterAuthoring>
        {
            public override void Bake(CharacterAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CharacterBaseMoveSpeed { Value = authoring.MoveSpeed });
                AddComponent<InitializeCharacterFlag>(entity);
                AddComponent<CharacterHealthRegenerationState>(entity);
                SetComponentEnabled<CharacterHealthRegenerationState>(entity, false);
                AddComponent<CharacterMoveDirection>(entity);
                AddComponent<CharacterStatModificationState>(entity);
                AddComponent<RecalculateStatsFlag>(entity);
                SetComponentEnabled<RecalculateStatsFlag>(entity, true);
                AddBuffer<ActiveStatModifierEntity>(entity);
                AddComponent(entity, new PhysicsGraphicalSmoothing { ApplySmoothing = 1 });
            }
        }
    }

    /// <summary>
    /// System to move characters in the game world based off input from their <see cref="CharacterMoveDirection"/>. Uses PhysicsVelocity to apply movement so collisions with other entities are properly handled.
    /// </summary>
    /// <remarks>
    /// This system initially moved characters by adding to the LocalTransform.Position of a character. However, this lead to some undesired behavior where entities could overlap each other and move through other physical colliders. Converting this system to modify the PhysicsVelocity component resolved this issue as collisions are properly accounted for and resolved.
    /// As this system modifies physics components, it updates in the FixedStepSimulationSystemGroup before the PhysicsSystemGroup.
    /// </remarks>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct CharacterMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (velocity, moveDirection, moveSpeed, characterStats, entity) in SystemAPI.Query<RefRW<PhysicsVelocity>, CharacterMoveDirection, CharacterBaseMoveSpeed, CharacterStatModificationState>().WithNone<KnockbackState>().WithEntityAccess())
            {
                var currentMovement = moveDirection.Value * moveSpeed.Value * characterStats.MoveSpeed;

                velocity.ValueRW.Linear = new float3(currentMovement.x, 0f, currentMovement.y);
                if (SystemAPI.HasComponent<GraphicsEntity>(entity) && math.abs(moveDirection.Value.x) > 0.15f)
                {
                    var graphicsEntity = SystemAPI.GetComponent<GraphicsEntity>(entity).Value;
                    if (SystemAPI.HasComponent<FacingDirectionOverride>(graphicsEntity))
                    {
                        var facingDirectionOverride = SystemAPI.GetComponentRW<FacingDirectionOverride>(graphicsEntity);
                        facingDirectionOverride.ValueRW.Value = math.sign(currentMovement.x);
                    }
                }
            }
        }
    }

    /// <summary>
    /// This system fixes the character's position to 0 in the y-axis as this game takes place on the x and z 2D axes.
    /// </summary>
    /// <remarks>
    /// Although no game systems move characters along the y-axis, characters can slightly fall off the y-axis during collisions. This can lead to behavior where entities overlap and move over/under one another.
    /// </remarks>
    [UpdateInGroup(typeof(DS_PhysicsSystemGroup), OrderLast = true)]
    public partial struct FixCharacterPositionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var transform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<CharacterBaseMoveSpeed>())
            {
                transform.ValueRW.Position.y = 0f;
            }
        }
    }

    /// <summary>
    /// System to initialize values pertaining to game characters.
    /// </summary>
    /// <remarks>
    /// Important that this updates "before" the GameStartSystem as that is where the player spawns and a 1 frame delay is required before calling UpgradeCapability on the EntityUpgradeControllerManaged otherwise some exceptions are thrown when subscene is open.
    /// </remarks>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup))]
    [UpdateBefore(typeof(GameStartSystem))]
    public partial struct CharacterInitializationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CharacterDefaultModificationValues>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var defaultModificationValues = SystemAPI.GetSingleton<CharacterDefaultModificationValues>();
            
            foreach (var (physicsMass, characterStats, statModifierEntities, entity) in SystemAPI.Query<RefRW<PhysicsMass>, RefRW<CharacterStatModificationState>, DynamicBuffer<ActiveStatModifierEntity>>().WithAll<InitializeCharacterFlag>().WithEntityAccess())
            {
                // Setting InverseInertia to zero ensures that the character entity will not rotate due to physics collisions or other external forces. Character can still rotate by directly setting LocalTransform.Rotation if needed.
                physicsMass.ValueRW.InverseInertia = float3.zero;

                // Set initial stat modifications.
                var currentModificationValues = new CharacterStatModificationState(defaultModificationValues);

                foreach (var statModifierEntity in statModifierEntities)
                {
                    var statModifiers = SystemAPI.GetBuffer<StatModifier>(statModifierEntity.Value);
                    foreach (var statModifier in statModifiers)
                    {
                        currentModificationValues.AddStat(statModifier);
                    }
                }

                characterStats.ValueRW = currentModificationValues;

                if (characterStats.ValueRO.AdditionalHitPoints > 0)
                {
                    var currentHitPoints = SystemAPI.GetComponentRW<CurrentHitPoints>(entity);
                    currentHitPoints.ValueRW.Value += characterStats.ValueRO.AdditionalHitPoints;
                }
                
                SystemAPI.SetComponentEnabled<InitializeCharacterFlag>(entity, false);
            }
        }
    }
    
    /// <summary>
    /// System to handle regenerating character health.
    /// </summary>
    /// <remarks>
    /// System will only run on entities with the <see cref="CharacterHealthRegenerationState"/> component enabled.
    /// CharacterHealthRegenerationState holds fractions of a hit point that are constantly added to while system is active. Once a whole number is reached, the whole hit point integer will be added to the character's current hit points.
    /// Updates before the <see cref="ProcessDamageThisFrameSystem"/> to ensure regenerated hit points are added to the <see cref="DamageThisFrame"/> buffer before processing damage for the current frame.
    /// </remarks>
    /// <seeaslo cref="DamageThisFrame"/>
    /// <seeaslo cref="ProcessDamageThisFrameSystem"/>
    [UpdateBefore(typeof(ProcessDamageThisFrameSystem))]
    public partial struct CharacterHealthRegenerationSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (healthRegeneration, damageThisFrame, characterStats) in SystemAPI.Query<RefRW<CharacterHealthRegenerationState>, DynamicBuffer<DamageThisFrame>, CharacterStatModificationState>())
            {
                healthRegeneration.ValueRW.HitPointAccumulator += characterStats.HealthRegeneration * deltaTime;

                if (healthRegeneration.ValueRO.HitPointAccumulator >= 1f)
                {
                    var regeneratedHitPoints = (int)math.floor(healthRegeneration.ValueRO.HitPointAccumulator);
                    // Add negative damage points to DamageThisFrame dynamic buffer to add health to character's current hit points.
                    damageThisFrame.Add(new DamageThisFrame { Value = -1 * regeneratedHitPoints });
                    healthRegeneration.ValueRW.HitPointAccumulator -= regeneratedHitPoints;
                }
            }
        }
    }

    /// <summary>
    /// System to recalculate the character's current stat modifications stored in <see cref="CharacterStatModificationState"/>.
    /// </summary>
    /// <remarks>
    /// Stat modification is implemented by iterating stat modification entities associated with a given character stored in the <see cref="ActiveStatModifierEntity"/> dynamic buffer on each character. Each active stat modifier entity has a <see cref="StatModifier"/> dynamic buffer that contains the individual stat modifications; this buffer will be iterated in an inner foreach loop and stats will be added to default stat modification values stored in <see cref="CharacterDefaultModificationValues"/> using the helper method <see cref="CharacterStatModificationState.AddStat"/>.
    /// Updates after the <see cref="DS_InteractionSystemGroup"/> to all ensure stat modifications are accounted for before recalculating stats.
    /// </remarks>
    [UpdateAfter(typeof(DS_InteractionSystemGroup))]
    public partial struct RecalculateStatsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CharacterDefaultModificationValues>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var statModifierLookup = SystemAPI.GetBufferLookup<StatModifier>();
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var defaultStats = SystemAPI.GetSingleton<CharacterDefaultModificationValues>();
            
            // Ensure character is initialized before recalculating stats otherwise move speed and max health may not be properly set in the base character stats.
            foreach (var (statModifierEntities, characterStats, currentHitPoints, baseHitPoints, recalculateStats, characterEntity) in SystemAPI.Query<DynamicBuffer<ActiveStatModifierEntity>, RefRW<CharacterStatModificationState>, RefRW<CurrentHitPoints>, BaseHitPoints, EnabledRefRW<RecalculateStatsFlag>>().WithNone<InitializeCharacterFlag>().WithEntityAccess())
            {
                var currentStats = new CharacterStatModificationState(defaultStats);
                
                // Reverse for loop so removing non-existent stat modifier entities doesn't affect calculating stats for subsequent stat modifier entities.
                for (var i = statModifierEntities.Length - 1; i >= 0; i--)
                {
                    var statModifierEntity = statModifierEntities[i].Value;
                    if (!SystemAPI.Exists(statModifierEntity))
                    {
                        statModifierEntities.RemoveAtSwapBack(i);
                        continue;
                    }
                    
                    var currentStatModifiers = statModifierLookup[statModifierEntity];
                    foreach (var currentStatModifier in currentStatModifiers)
                    {
                        currentStats.AddStat(currentStatModifier);
                        if (currentStatModifier.Type == StatModifierType.AdditionalHitPoints && SystemAPI.HasComponent<PlayerTag>(characterEntity))
                        {
                            SystemAPI.SetComponentEnabled<UpdatePlayerHealthUIFlag>(characterEntity, true);
                        }
                    }
                }

                var maxHitPoints = baseHitPoints.Value + currentStats.AdditionalHitPoints;
                currentHitPoints.ValueRW.Value = math.min(currentHitPoints.ValueRO.Value, maxHitPoints);
                
                var enableHealthRegeneration = currentStats.HealthRegeneration > 0f && currentHitPoints.ValueRO.Value < maxHitPoints;
                SystemAPI.SetComponentEnabled<CharacterHealthRegenerationState>(characterEntity, enableHealthRegeneration);

                characterStats.ValueRW = currentStats;
                recalculateStats.ValueRW = false;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}