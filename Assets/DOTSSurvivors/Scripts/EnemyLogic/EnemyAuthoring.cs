using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Enum used to identify unique alien types. This enum is set in the Unity Editor on the prefab for a given alien type. This enum will also serve as a key to map to the baked entity prefab. This is so <see cref="EnemySpawnWaveProperties"/> and <see cref="EnemySpawnEventProperties"/> ScriptableObjects can define which enemy types are present in the current wave or event and the <see cref="EnemySpawnSystem"/> can spawn the appropriate prefabs.
    /// </summary>
    public enum EnemyType : byte
    {
        None,
        
        BaseAlien_Green,
        BaseAlien_Blue,
        BaseAlien_Red,
        BaseAlien_Gray,
        
        PlantAlien_Red,
        PlantAlien_Green,
        PlantAlien_Blue,
        PlantAlien_Purple,
        
        FloatingHead_Green,
        FloatingHead_Red,
        FloatingHead_Blue,
        FloatingHead_Gray,
        
        CentaurAlien_Green,
        CentaurAlien_Purple,
        
        SpiderAlien_Black,
        SpiderAlien_White,
        
        UFOAlien_Green,
        UFOAlien_Blue,
        UFOAlien_Red,
        UFOAlien_Purple,
        
        MiniAlien_Green,
        MiniAlien_Blue,
        MiniAlien_Red,
        MiniAlien_Gray,
        
        BlobAlien_Green,
        
        ArmorAlien_Black,
        ArmorAlien_White,
        
        GhostAlien_Blue,
        GhostAlien_Green,
        
        SkeletonAlien_Bone,
        SkeletonAlien_Gray,
        
        BigAlienSpots_Green,
        BigAlienSpots_Blue,
        BigAlienSpots_Red,
        BigAlienSpots_Orange,
        
        BigAlienStripes_Green,
        BigAlienStripes_Blue,
        BigAlienStripes_Red,
        BigAlienStripes_Orange,
        
        MechAlien_Light,
        MechAlien_Dark,
        
        ReaperAlien,
    }

    /// <summary>
    /// Component containing the <see cref="EnemyType"/> value. This component is only used for initialization and will be removed from the enemy prefab before any enemies are instantiated. See <see cref="GameStartSystem"/> for usage.
    /// </summary>
    /// <remarks>
    /// This is a regular tag component and not an enableable component as this component will be removed from the enemy prefab before any enemy instances will be spawned.
    /// </remarks>
    public struct EnemyTypeInitialization : IComponentData
    {
        public EnemyType Value;
    }
    
    /// <summary>
    /// Empty Tag component used to identify Enemy entities.
    /// </summary>
    public struct EnemyTag : IComponentData {}

    /// <summary>
    /// Data component containing data related to enemy attacking.
    /// </summary>
    /// <seealso cref="EnemyAttackSystem"/>
    /// <seealso cref="EnemyAttackCooldownExpiration"/>
    public struct EnemyAttackData : IComponentData
    {
        /// <summary>
        /// Number of points of damage to add during a single attack.
        /// </summary>
        public int HitPoints;
        
        /// <summary>
        /// Time in seconds defining the cooldown time of which an enemy cannot deal damage after an attack. This value will remain unchanged through the lifetime of the enemy. See the <see cref="EnemyAttackCooldownExpiration"/> component which contains the timestamp at which the enemy is able to attack again.
        /// </summary>
        public float CooldownTime;
    }

    /// <summary>
    /// Component to store the timestamp of when the enemy attack cooldown expires. If the game time is greater than or equal to this value, the enemy is able to attack.
    /// </summary>
    /// <seealso cref="EnemyAttackSystem"/>
    /// <seealso cref="EnemyAttackData"/>
    public struct EnemyAttackCooldownExpiration : IComponentData
    {
        /// <summary>
        /// After the enemy attacks, this value will be set to current game time + <see cref="EnemyAttackData.CooldownTime"/>
        /// </summary>
        public float Value;
    }
    
    /// <summary>
    /// Tag component to identify this entity as an enemy that should scale its health with the player level. Level scaling is defined by Enemy Base Hit Points multiplied by Player Level.
    /// </summary>
    /// <remarks>
    /// i.e. If the enemy has 10 base hit points and the player is on level 3, the enemy will spawn with 30 hit points of health.
    /// </remarks>
    public struct EnemyHitPointsScaleWithPlayerLevelTag : IComponentData {}
    
    /// <summary>
    /// Authoring script to set initial values for enemies.
    /// </summary>
    /// <seealso cref="EnemyAttackData"/>
    /// <seealso cref="EnemyAttackCooldownExpiration"/>
    /// <seealso cref="EnemyTypeInitialization"/>
    /// <seealso cref="EnemyHitPointsScaleWithPlayerLevelTag"/>
    [RequireComponent(typeof(CharacterAuthoring))]
    [RequireComponent(typeof(DropExperienceOnDestroyAuthoring))]
    [RequireComponent(typeof(ShowDamageNumberOnDamageAuthoring))]
    public class EnemyAuthoring : MonoBehaviour
    {
        public int BaseAttackDamage;
        public float EnemyAttackCooldown;
        public EnemyType EnemyType;
        public bool HealthScalesWithPlayerLevel;
        
        private class Baker : Baker<EnemyAuthoring>
        {
            public override void Bake(EnemyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<EnemyTag>(entity);
                AddComponent(entity, new EnemyAttackData { 
                    HitPoints = authoring.BaseAttackDamage, 
                    CooldownTime = authoring.EnemyAttackCooldown
                });
                AddComponent<EnemyAttackCooldownExpiration>(entity);
                AddComponent(entity, new EnemyTypeInitialization
                {
                    Value = authoring.EnemyType
                });

                if (authoring.HealthScalesWithPlayerLevel)
                {
                    AddComponent<EnemyHitPointsScaleWithPlayerLevelTag>(entity);
                }
            }
        }
    }

    /// <summary>
    /// System to control initialization steps pertaining to enemy entities.
    /// </summary>
    /// <remarks>
    /// Currently the only initialization step is to perform hit point scaling on enemies tagged with the <see cref="EnemyHitPointsScaleWithPlayerLevelTag"/>.
    /// System updates before the <see cref="CharacterInitializationSystem"/> as the <see cref="InitializeCharacterFlag"/> gets disabled in that system.
    /// </remarks>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup))]
    [UpdateBefore(typeof(CharacterInitializationSystem))]
    public partial struct EnemyInitializationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerExperienceState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var playerLevel = SystemAPI.GetSingleton<PlayerExperienceState>().Level;
            
            foreach (var (currentHitPoints, baseHitPoints) in SystemAPI.Query<RefRW<CurrentHitPoints>, RefRW<BaseHitPoints>>().WithAll<EnemyTag, EnemyHitPointsScaleWithPlayerLevelTag, InitializeCharacterFlag>())
            {
                var scaledHitPoints = baseHitPoints.ValueRO.Value * playerLevel;
                baseHitPoints.ValueRW.Value = scaledHitPoints;
                currentHitPoints.ValueRW.Value = scaledHitPoints;
            }
        }
    }

    /// <summary>
    /// System that schedules <see cref="EnemyAttackCollisionJob"/> which detects collisions between enemies and the player and deals damage to the player. Updates in the <see cref="DS_PhysicsSystemGroup"/> so collision events for this frame have been raised.
    /// </summary>
    /// <seealso cref="EnemyAttackCollisionJob"/>
    [UpdateInGroup(typeof(DS_PhysicsSystemGroup))]
    public partial struct EnemyAttackSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();

            state.Dependency = new EnemyAttackCollisionJob
            {
                PlayerLookup = SystemAPI.GetComponentLookup<PlayerTag>(true),
                EnemyLookup = SystemAPI.GetComponentLookup<EnemyTag>(true),
                EnemyAttackPropertiesLookup = SystemAPI.GetComponentLookup<EnemyAttackData>(true),
                CharacterStatsLookup = SystemAPI.GetComponentLookup<CharacterStatModificationState>(true),
                CooldownExpirationLookup = SystemAPI.GetComponentLookup<EnemyAttackCooldownExpiration>(),
                DamageThisFrameLookup = SystemAPI.GetBufferLookup<DamageThisFrame>(),
                ElapsedTime = SystemAPI.Time.ElapsedTime
            }.Schedule(simulationSingleton, state.Dependency);
        }
    }
    
    /// <summary>
    /// Collision Event Job scheduled by <see cref="EnemyAttackSystem"/>. This job is ran on all collision events raised by the physics system for this physics step. The Execute method will first determine which of the entities in the collision event is the player entity and which is the enemy entity. If the collision event is not between exactly 1 player and 1 enemy, the job will early out. Next the job will determine if the enemy is on cooldown, if the enemy is not on cooldown, it will apply damage to the player via the <see cref="DamageThisFrame"/> Dynamic Buffer and reset the <see cref="EnemyAttackCooldownExpiration"/> for the enemy.
    /// </summary>
    [BurstCompile]
    public struct EnemyAttackCollisionJob : ICollisionEventsJob
    {
        /// <summary>
        /// Component lookup used to determine the player entity.
        /// </summary>
        [ReadOnly] public ComponentLookup<PlayerTag> PlayerLookup;
        /// <summary>
        /// Component lookup used to determine the enemy entity.
        /// </summary>
        [ReadOnly] public ComponentLookup<EnemyTag> EnemyLookup;
        /// <summary>
        /// Component lookup used to get data for the enemy attack.
        /// </summary>
        [ReadOnly] public ComponentLookup<EnemyAttackData> EnemyAttackPropertiesLookup;
        /// <summary>
        /// Component lookup used to determine if any additional damage scaling should be added to the enemy's attack.
        /// </summary>
        [ReadOnly] public ComponentLookup<CharacterStatModificationState> CharacterStatsLookup;
        /// <summary>
        /// Read/Write component lookup used to determine if the enemy is on cooldown. Write access is also required so the expiration timer can be updated after the enemy attacks.
        /// </summary>
        public ComponentLookup<EnemyAttackCooldownExpiration> CooldownExpirationLookup;
        /// <summary>
        /// Buffer lookup used to apply points of damage to the player.
        /// </summary>
        public BufferLookup<DamageThisFrame> DamageThisFrameLookup;
        /// <summary>
        /// Elapsed game time used to determine if an enemy is on cooldown.
        /// </summary>
        public double ElapsedTime;
        
        [BurstCompile]
        public void Execute(CollisionEvent collisionEvent)
        {
            Entity playerEntity;
            Entity enemyEntity;

            if (PlayerLookup.HasComponent(collisionEvent.EntityA) && EnemyLookup.HasComponent(collisionEvent.EntityB))
            {
                playerEntity = collisionEvent.EntityA;
                enemyEntity = collisionEvent.EntityB;
            }
            else if (PlayerLookup.HasComponent(collisionEvent.EntityB) && EnemyLookup.HasComponent(collisionEvent.EntityA))
            {
                playerEntity = collisionEvent.EntityB;
                enemyEntity = collisionEvent.EntityA;
            }
            else
            {
                return;
            }

            var cooldownExpiration = CooldownExpirationLookup[enemyEntity];
            if (cooldownExpiration.Value > ElapsedTime) return;
            var attackProperties = EnemyAttackPropertiesLookup[enemyEntity];
            cooldownExpiration.Value = (float)ElapsedTime + attackProperties.CooldownTime;
            CooldownExpirationLookup[enemyEntity] = cooldownExpiration;

            var attackDamage = attackProperties.HitPoints;
            var damageModifier = CharacterStatsLookup[enemyEntity].DamageDealt;
            attackDamage = (int)math.ceil(attackDamage * damageModifier);
            
            var hitBuffer = DamageThisFrameLookup[playerEntity];
            hitBuffer.Add(new DamageThisFrame { Value = attackDamage });
        }
    }
}