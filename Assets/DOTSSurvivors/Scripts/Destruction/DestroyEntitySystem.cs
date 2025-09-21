using System;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Random = Unity.Mathematics.Random;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// This system is responsible for destroying all entities with the <see cref="DestroyEntityFlag"/> enabled. There are various checks to determine if any additional logic should be executed when the entity is destroyed.
    /// This system should be the last system to execute in the <see cref="DS_DestructionSystemGroup"/>, which itself updates at the end of the SimulationSystemGroup. The only system to run after this system in the SimulationSystemGroup should be the EndSimulationEntityCommandBufferSystem.
    /// </summary>
    /// <remarks>
    /// Examples of additional logic on destroy includes spawning experience gems, playing destruction VFX and SFX, and triggering game over logic.
    /// </remarks>
    [UpdateInGroup(typeof(DS_DestructionSystemGroup), OrderLast = true)]
    public partial struct DestroyEntitySystem :ISystem 
    {
        /// <summary>
        /// Unity.Mathematics.Random to calculate chances of random events that are triggered from this system.
        /// </summary>
        /// <remarks>
        /// Initial seed is the Millisecond value from system time when the system starts.
        /// </remarks>
        private Random _systemRandom;

        /// <summary>
        /// Constant time value that is set on entities that will play a dissolve animation.
        /// </summary>
        /// <seealso cref="DissolveData"/>
        private const float DISSOLVE_DURATION= 0.4f;

        /// <summary>
        /// Entity archetype of the entity that will be spawned when the player entity is destroyed to trigger the game over sequence.
        /// </summary>
        private EntityArchetype _gameOverArchetype;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameEntityPrefabs>();
            _systemRandom = Random.CreateFromIndex((uint)DateTime.Now.Millisecond);
            _gameOverArchetype = state.EntityManager.CreateArchetype(typeof(BeginGameOverTag));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var beginFrameECB = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var endFrameECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (_, entity) in SystemAPI.Query<DestroyEntityFlag>().WithEntityAccess())
            {
                if (SystemAPI.HasComponent<PlayerTag>(entity))
                {
                    beginFrameECB.CreateEntity(_gameOverArchetype);
                }

                if (SystemAPI.HasComponent<GraphicsEntityPlayDestroyEffectTag>(entity))
                {
                    var graphicsEntity = SystemAPI.GetComponent<GraphicsEntity>(entity).Value;
                    var newGraphicsEntity = endFrameECB.Instantiate(graphicsEntity);
                    endFrameECB.RemoveComponent<LocalTransform>(newGraphicsEntity);
                    endFrameECB.AddComponent(newGraphicsEntity, new DissolveData
                    {
                        StartTimestamp = (float)SystemAPI.Time.ElapsedTime,
                        Duration = DISSOLVE_DURATION
                    });
                }
                
                if (SystemAPI.HasComponent<EnemyTag>(entity))
                {
                    var aliensDefeatedCount = SystemAPI.GetSingletonRW<AliensDefeatedCount>();
                    aliensDefeatedCount.ValueRW.Value += 1;
                }

                if (SystemAPI.HasComponent<DropExperienceOnDestroy>(entity))
                {
                    var experiencePointProperties = SystemAPI.GetComponent<DropExperienceOnDestroy>(entity);
                    var shouldDropExperience = _systemRandom.NextInt(1, 101) <= experiencePointProperties.ChanceToDrop;
                    if (shouldDropExperience)
                    {
                        var experienceGemPrefab = SystemAPI.GetSingleton<GameEntityPrefabs>().ExperienceGemPrefab;

                        var newExperiencePointEntity = beginFrameECB.Instantiate(experienceGemPrefab);
                        beginFrameECB.SetComponent(newExperiencePointEntity, new ExperienceGemItemData
                        {
                            Value = experiencePointProperties.ExperienceValue
                        });
                        var spawnPosition = SystemAPI.GetComponent<LocalTransform>(entity).Position;
                        beginFrameECB.SetComponent(newExperiencePointEntity, LocalTransform.FromPosition(spawnPosition));
                    }
                }

                if (SystemAPI.HasComponent<SpawnOnDestroy>(entity))
                {
                    var entityToSpawn = SystemAPI.GetComponent<SpawnOnDestroy>(entity).Value;
                    var entityToSpawnScale = SystemAPI.GetComponent<LocalTransform>(entityToSpawn).Scale;
                    var newEntity = beginFrameECB.Instantiate(entityToSpawn);
                    var entityTransform = SystemAPI.GetComponent<LocalTransform>(entity);
                    entityTransform = entityTransform.ApplyScale(entityToSpawnScale);
                    beginFrameECB.SetComponent(newEntity, entityTransform);

                    if (SystemAPI.HasComponent<ExplodeOnTimerData>(entity))
                    {
                        var explodeOnTimerProperties = SystemAPI.GetComponent<ExplodeOnTimerData>(entity);
                        beginFrameECB.SetComponent(newEntity, new DealHitPointsOnInteraction { Value = explodeOnTimerProperties.Damage });
                        entityTransform = entityTransform.ApplyScale(explodeOnTimerProperties.Area);
                        beginFrameECB.SetComponent(newEntity, entityTransform);
                        beginFrameECB.SetComponent(newEntity, new DestroyAfterTime { Value = explodeOnTimerProperties.ExplosionTimeToLive });
                    }
                }

                if (SystemAPI.HasComponent<RandomItemToSpawnBlob>(entity))
                {
                    ref var randomItemsToSpawn = ref SystemAPI.GetComponent<RandomItemToSpawnBlob>(entity).Value.Value;
                    var maxRarity = randomItemsToSpawn[^1].RarityKey;
                    var randomRarity = _systemRandom.NextInt(maxRarity + 1);
                    var entityToSpawn = Entity.Null;
                    
                    for (var i = 0; i < randomItemsToSpawn.Length; i++)
                    {
                        if (randomRarity < randomItemsToSpawn[i].RarityKey)
                        {
                            entityToSpawn = randomItemsToSpawn[i].Entity;
                            break;
                        }
                    }

                    if (SystemAPI.Exists(entityToSpawn))
                    {
                        var entityToSpawnScale = SystemAPI.GetComponent<LocalTransform>(entityToSpawn).Scale;
                        var newEntity = beginFrameECB.Instantiate(entityToSpawn);
                        var entityTransform = SystemAPI.GetComponent<LocalTransform>(entity);
                        entityTransform = entityTransform.ApplyScale(entityToSpawnScale);
                        beginFrameECB.SetComponent(newEntity, entityTransform);
                    }
                }
                
                if (SystemAPI.HasComponent<StatModifierEntityTag>(entity))
                {
                    var characterEntity = SystemAPI.GetComponent<CharacterEntity>(entity).Value;
                    SystemAPI.SetComponentEnabled<RecalculateStatsFlag>(characterEntity, true);
                }

                endFrameECB.DestroyEntity(entity);
            }
        }
    }
}