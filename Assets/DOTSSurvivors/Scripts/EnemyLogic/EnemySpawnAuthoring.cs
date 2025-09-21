using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Burst;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data pertaining to enemy spawning. This data is specific to a level and will remain unchanged through gameplay of a specific level.
    /// </summary>
    /// <seealso cref="EnemySpawnSystem"/>
    public struct EnemySpawnData : IComponentData
    {
        /// <summary>
        /// Time in seconds for how long the game will be spawning enemies of a particular spawn wave.
        /// </summary>
        public float WaveInterval;
        /// <summary>
        /// Enemies will be spawned offscreen at a random distance from the edge of the camera bounds. This value defines the minimum distance away from the camera bounds an enemy will spawn.
        /// </summary>
        public float MinSpawnDistanceFromCameraBounds;
        /// <summary>
        /// Enemies will be spawned offscreen at a random distance from the edge of the camera bounds. This value defines the maximum distance away from the camera bounds an enemy will spawn.
        /// </summary>
        public float MaxSpawnDistanceFromCameraBounds;
        
        /// <summary>
        /// Helper function to get a random spawn position offscreen.
        /// </summary>
        /// <param name="random">Used for generating the random position.</param>
        /// <param name="cameraPosition">Center point of the camera.</param>
        /// <param name="cameraHalfExtents">Half dimensions of the camera view space.</param>
        /// <returns></returns>
        public float3 GetRandomSpawnPos(RefRW<EntityRandom> random, float3 cameraPosition, float3 cameraHalfExtents)
        {
            // Pick a random side of the screen to spawn off of.
            var randomSideIndex = random.ValueRW.Value.NextInt(0, 4);

            var minSpawnPosition = float3.zero;
            var maxSpawnPosition = float3.zero;

            // Determine the minimum and maximum position for the chosen random side.
            switch (randomSideIndex)
            {
                case 0:
                    minSpawnPosition = new float3
                    {
                        x = cameraPosition.x - cameraHalfExtents.x - MaxSpawnDistanceFromCameraBounds,
                        y = 0f,
                        z = cameraPosition.z - cameraHalfExtents.z - MaxSpawnDistanceFromCameraBounds,
                    };
                    maxSpawnPosition = new float3
                    {
                        x = cameraPosition.x - cameraHalfExtents.x - MinSpawnDistanceFromCameraBounds,
                        y = 0f,
                        z = cameraPosition.z + cameraHalfExtents.z + MaxSpawnDistanceFromCameraBounds,
                    };
                    break;
                case 1:
                    minSpawnPosition = new float3
                    {
                        x = cameraPosition.x - cameraHalfExtents.x - MaxSpawnDistanceFromCameraBounds,
                        y = 0f,
                        z = cameraPosition.z + cameraHalfExtents.z + MinSpawnDistanceFromCameraBounds,
                    };
                    maxSpawnPosition = new float3
                    {
                        x = cameraPosition.x + cameraHalfExtents.x + MaxSpawnDistanceFromCameraBounds,
                        y = 0f,
                        z = cameraPosition.z + cameraHalfExtents.z + MaxSpawnDistanceFromCameraBounds,
                    };
                    break;
                case 2:
                    minSpawnPosition = new float3
                    {
                        x = cameraPosition.x + cameraHalfExtents.x + MinSpawnDistanceFromCameraBounds,
                        y = 0f,
                        z = cameraPosition.z - cameraHalfExtents.z - MaxSpawnDistanceFromCameraBounds,
                    };
                    maxSpawnPosition = new float3
                    {
                        x = cameraPosition.x + cameraHalfExtents.x + MaxSpawnDistanceFromCameraBounds,
                        y = 0f,
                        z = cameraPosition.z + cameraHalfExtents.z + MaxSpawnDistanceFromCameraBounds,
                    };
                    break;
                case 3:
                    minSpawnPosition = new float3
                    {
                        x = cameraPosition.x - cameraHalfExtents.x - MaxSpawnDistanceFromCameraBounds,
                        y = 0f,
                        z = cameraPosition.z - cameraHalfExtents.z - MaxSpawnDistanceFromCameraBounds,
                    };
                    maxSpawnPosition = new float3
                    {
                        x = cameraPosition.x + cameraHalfExtents.x + MaxSpawnDistanceFromCameraBounds,
                        y = 0f,
                        z = cameraPosition.z - cameraHalfExtents.z - MinSpawnDistanceFromCameraBounds,
                    };
                    break;
            }

            // Return a random position inside the minimum and maximum positions for the given side.
            var newSpawnPos = random.ValueRW.Value.NextFloat3(minSpawnPosition, maxSpawnPosition);
            return newSpawnPos;
        }
    }

    /// <summary>
    /// Data pertaining to the currently active spawn wave. This data will be updated at the change of each spawn wave.
    /// </summary>
    /// <seealso cref="EnemySpawnSystem"/>
    /// <seealso cref="EnemySpawnerState"/>
    public struct EnemySpawnWaveData : IComponentData
    {
        /// <summary>
        /// Defines the minimum number of enemies that will be present while this wave is active. If there are fewer than the minimum enemies in the world for a particular spawn wave, the number of enemies required to meet this minimum value will be spawned in 1 frame.
        /// </summary>
        public int MinEnemyCount;
        /// <summary>
        /// If the minimum enemy count is met for this wave, additional enemies will be spawned on an interval defined by this value. One enemy of each type stored in the <see cref="SpawnWaveEnemyPrefab"/> Dynamic Buffer will be spawned once the <see cref="EnemySpawnerState.WaveTimer"/> reaches 0.
        /// </summary>
        public float SpawnInterval;
    }

    /// <summary>
    /// Dynamic Buffer to hold all the enemy prefabs that will be spawned in the current wave.
    /// </summary>
    /// <remarks>
    /// Note that this buffer does not store the prefabs for boss enemies or enemies belonging to a spawn event.
    /// Boss enemies are instantiated at the wave start and thus there is no need to hold onto a prefab reference.
    /// Spawn events currently spawn enemies of a single type and their prefab is held in the <see cref="SpawnEvent"/> Dynamic Buffer.
    /// </remarks>
    public struct SpawnWaveEnemyPrefab : IBufferElementData
    {
        public Entity Value;
    }

    /// <summary>
    /// Dynamic buffer component to hold all the <see cref="EnemySpawnWaveProperties"/> ScriptableObjects for a given stage.
    /// </summary>
    /// <remarks>
    /// In the <see cref="EnemySpawnSystem"/> when a new wave is to be selected, the next element of the buffer is referenced and appropriate data components are populated with runtime friendly, unmanaged data. Index is tracked in <see cref="EnemySpawnerState"/>.
    /// </remarks>
    public struct EnemySpawnWave : IBufferElementData
    {
        public UnityObjectRef<EnemySpawnWaveProperties> Value;
    }

    /// <summary>
    /// Holds the current state data of the enemy spawner.
    /// </summary>
    /// <seeaslo cref="EnemySpawnSystem"/>
    public struct EnemySpawnerState : IComponentData
    {
        /// <summary>
        /// Index associated with the current wave as held in the <see cref="EnemySpawnWave.Value"/> managed array of <see cref="EnemySpawnWaveProperties"/> ScriptableObjects.
        /// </summary>
        public int CurWaveIndex;
        /// <summary>
        /// Timer for the current wave. At the start of each wave, this value is initialized to <see cref="EnemySpawnData.WaveInterval"/>, each frame the value is decremented by delta time, once the timer reaches 0, the next wave should begin.
        /// </summary>
        public float WaveTimer;
    }
    
    /// <summary>
    /// Timer to inform the <see cref="EnemySpawnSystem"/> when to spawn the next batch of enemies for a given wave.
    /// </summary>
    public struct EnemySpawnTimer : IComponentData
    {
        /// <summary>
        /// Timer value is initialized to <see cref="EnemySpawnWaveData.SpawnInterval"/>, each frame the value is decremented by delta time, once the timer reaches 0, the next batch of enemies for the wave will be spawned.
        /// </summary>
        public float Value;
    }

    /// <summary>
    /// Dynamic buffer containing information pertaining to the spawn events for the current wave.
    /// </summary>
    /// <remarks>
    /// At the start of each wave, this dynamic buffer is cleared out and populated with runtime data for each <see cref="EnemySpawnEventProperties"/> element in the <see cref="EnemySpawnWaveProperties.SpawnEvents"/> array.
    /// This dynamic buffer contains all data pertaining to any spawn event that could occur, so not all fields are relevant to all spawn events.
    /// </remarks>
    [InternalBufferCapacity(4)]
    public struct SpawnEvent : IBufferElementData
    {
        /// <summary>
        /// Flag to determine if this spawn event has already occurred.
        /// </summary>
        public bool HasOccurred;
        /// <summary>
        /// Entity prefab for the enemy type that will be spawned in this spawn event.
        /// </summary>
        public Entity EnemyPrefab;
        /// <summary>
        /// Arrangement the enemies will be spawned in.
        /// </summary>
        public SpawnFormation SpawnFormation;
        /// <summary>
        /// Number of enemies that will be spawned during this spawn event.
        /// </summary>
        public int EnemyCount;
        /// <summary>
        /// Delay time is the time in seconds from the start of the wave until the spawn event occurs. It is up to the event designer to ensure that all events occur within the duration of the spawn event as events that take place after the conclusion of the spawn event will not occur.
        /// </summary>
        public float DelayTime;
        /// <summary>
        /// Percentage chance from 0 to 100 that the event will occur. A value of 0 means the spawn event will never occur and a value of 100 means the event will always occur.
        /// </summary>
        public int ChanceOfOccurrence;
        /// <summary>
        /// Optional field relevant to certain spawn formations. Defines the spacing between enemies.
        /// </summary>
        public float EnemySpacing;
        /// <summary>
        /// Optional field relevant to certain spawn formations. Defines the time to live for a given enemy before it is destroyed.
        /// </summary>
        public float TimeToLive;
        /// <summary>
        /// Optional field relevant to certain spawn formations. Defines the movement speed for linear moving enemies (<see cref="EnemyLinearMovement"/>) and constant move speed for sine wave enemies (<see cref="EnemySineWaveMovement"/>).
        /// </summary>
        public float MoveSpeed;
        /// <summary>
        /// Optional field relevant to certain spawn formations. Defines the period for enemy sine wave movement. <see cref="EnemySineWaveMovement"/>
        /// </summary>
        public float Period;
        /// <summary>
        /// Optional field relevant to certain spawn formations. Defines the Amplitude for enemy sine wave movement. <see cref="EnemySineWaveMovement"/>
        /// </summary>
        public float Amplitude;

        /// <summary>
        /// Constructor to assist with creation of SpawnEvent elements.
        /// </summary>
        /// <param name="properties">Authoring data for the spawn event.</param>
        /// <param name="wavePropertiesProperties">Authoring data for the spawn wave.</param>
        /// <param name="spawnEventIndex">Index of this spawn event to be added. Used to calculate delay time.</param>
        /// <param name="enemyPrefab">Entity prefab of the enemy to be spawned for this event.</param>
        public SpawnEvent(EnemySpawnEventProperties properties, EnemySpawnWaveProperties wavePropertiesProperties, int spawnEventIndex, Entity enemyPrefab)
        {
            HasOccurred = false;
            EnemyPrefab = enemyPrefab;
            SpawnFormation = properties.SpawnFormation;
            EnemyCount = properties.EnemyCount;
            DelayTime = wavePropertiesProperties.DelayTime * (spawnEventIndex + 1);
            ChanceOfOccurrence = wavePropertiesProperties.ChanceOfOccurrence;
            EnemySpacing = properties.EnemySpacing;
            TimeToLive = properties.TimeToLive;
            MoveSpeed = properties.MoveSpeed;
            Period = properties.Period;
            Amplitude = properties.Amplitude;
        }
    }
    
    /// <summary>
    /// Authoring script to define behavior of enemy spawning for a particular level.
    /// </summary>
    /// <remarks>
    /// Requires the <see cref="EntityRandomAuthoring"/> component to add <see cref="EntityRandom"/> component to the entity for spawn randomization.
    /// It is important that we include DependsOn(authoring.StageProperties) so baking is re-ran when values on the ScriptableObject are changed.
    /// </remarks>
    [RequireComponent(typeof(EntityRandomAuthoring))]
    public class EnemySpawnAuthoring : MonoBehaviour
    {
        /// <summary>
        /// ScriptableObject containing information relevant to the current stage, notably the interval between spawn waves and array of <see cref="EnemySpawnWaveProperties"/>
        /// </summary>
        /// <remarks>
        /// It is important that we include DependsOn(authoring.StageProperties) so baking is re-ran when values on the ScriptableObject are changed.
        /// </remarks>
        public StageProperties StageProperties;
        /// <summary>
        /// Enemies will be spawned offscreen at a random distance from the edge of the camera bounds. This value defines the minimum distance away from the camera bounds an enemy will spawn.
        /// </summary>
        public float MinSpawnDistanceFromCameraBounds;
        /// <summary>
        /// Enemies will be spawned offscreen at a random distance from the edge of the camera bounds. This value defines the maximum distance away from the camera bounds an enemy will spawn.
        /// </summary>
        public float MaxSpawnDistanceFromCameraBounds;
        
        private class Baker : Baker<EnemySpawnAuthoring>
        {
            public override void Bake(EnemySpawnAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                DependsOn(authoring.StageProperties);
                AddComponent(entity, new EnemySpawnData
                {
                    WaveInterval = authoring.StageProperties.WaveInterval,
                    MinSpawnDistanceFromCameraBounds= authoring.MinSpawnDistanceFromCameraBounds,
                    MaxSpawnDistanceFromCameraBounds= authoring.MaxSpawnDistanceFromCameraBounds
                });
                AddComponent<EnemySpawnTimer>(entity);
                AddComponent<EnemySpawnWaveData>(entity);
                AddBuffer<SpawnWaveEnemyPrefab>(entity);
                AddComponent<EnemySpawnerState>(entity);
                var spawnWaveBuffer = AddBuffer<EnemySpawnWave>(entity);
                foreach (var spawnWave in authoring.StageProperties.EnemySpawnWaves)
                {
                    spawnWaveBuffer.Add(new EnemySpawnWave { Value = spawnWave });
                }
                AddBuffer<SpawnEvent>(entity);
            }
        }
    }

    /// <summary>
    /// System to advance to the next spawn wave once the <see cref="EnemySpawnerState.WaveTimer"/> expires.
    /// </summary>
    /// <remarks>
    /// This system also spawns a new boss entity at the start of the next wave.
    /// System updates at the beginning of the <see cref="DS_InitializationSystemGroup"/> and before <see cref="EnemySpawnSystem"/> to ensure next wave is active before spawning new enemies. Also important that boss enemies spawn before the <see cref="CharacterInitializationSystem"/>.
    /// </remarks>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(EnemySpawnSystem))]
    public partial struct EnemyWaveSystem : ISystem
    {
        /// <summary>
        /// Query of all entities tagged with the <see cref="EnemyTag"/> to get a count of all enemies currently spawned so the system knows how many it needs to spawn to reach the minimum spawn count for the current wave.
        /// </summary>

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerExperienceState>();
            state.RequireForUpdate<CameraTarget>();
            state.RequireForUpdate<GameEntityTag>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var gameControllerEntity = SystemAPI.GetSingletonEntity<GameEntityTag>();
            var gamePrefabs = SystemAPI.GetComponent<GameEntityPrefabs>(gameControllerEntity);
            var enemyEntityPrefabLookup = state.EntityManager.GetComponentObject<ManagedGameData>(gameControllerEntity).EnemyEntityPrefabLookup;
            var cameraReferenceEntity = SystemAPI.GetSingletonEntity<CameraTarget>();
            var cameraPosition = SystemAPI.GetComponent<LocalToWorld>(cameraReferenceEntity).Position;
            var cameraHalfExtents = SystemAPI.GetComponent<CameraTarget>(cameraReferenceEntity).HalfExtents;
            
            // This foreach loop determines when a new wave should be active. Once the target time to change waves has been reached, this loop will assign the appropriate values in the data components.
            foreach (var (spawnerState, spawnData, spawnWaveData, spawnWavePrefabs, spawnEvents, spawnWaves, random, entity) in SystemAPI.Query<RefRW<EnemySpawnerState>, EnemySpawnData, RefRW<EnemySpawnWaveData>, DynamicBuffer<SpawnWaveEnemyPrefab>, DynamicBuffer<SpawnEvent>, DynamicBuffer<EnemySpawnWave>, RefRW<EntityRandom>>().WithEntityAccess())
            {
                // Determine when to advance to the next wave.
                spawnerState.ValueRW.WaveTimer -= deltaTime;
                if (spawnerState.ValueRO.WaveTimer > 0f) continue;
                spawnerState.ValueRW.WaveTimer = spawnData.WaveInterval;

                // Get properties for the new wave and populate data for the current wave.
                var curWaveProperties = spawnWaves[spawnerState.ValueRO.CurWaveIndex].Value.Value;
                spawnWaveData.ValueRW = new EnemySpawnWaveData
                {
                    MinEnemyCount = curWaveProperties.MinEnemyCount,
                    SpawnInterval = curWaveProperties.SpawnInterval
                };

                // Store prefab references for all enemies to be spawned in the current wave.
                spawnWavePrefabs.Clear();
                foreach (var enemyType in curWaveProperties.EnemyTypes)
                {
                    if (enemyType == EnemyType.None) continue;
                    var enemyPrefab = enemyEntityPrefabLookup[enemyType];
                    spawnWavePrefabs.Add(new SpawnWaveEnemyPrefab { Value = enemyPrefab });
                }

                // Populate spawnEvents Dynamic Buffer with information pertaining to spawn events for the current wave.
                spawnEvents.Clear();
                var spawnEventIndex = 0;
                foreach (var spawnEvent in curWaveProperties.SpawnEvents)
                {
                    if (spawnEvent.EnemyType == EnemyType.None) continue;
                    var enemyPrefab = enemyEntityPrefabLookup[spawnEvent.EnemyType];
                    spawnEvents.Add(new SpawnEvent(spawnEvent, curWaveProperties, spawnEventIndex, enemyPrefab));
                    spawnEventIndex += 1;
                }

                // If the current wave has a boss, spawn it at the start of the new wave. Boss enemies have more health and the enhanced material effect.
                if (curWaveProperties.BossType != EnemyType.None)
                {
                    var bossPrefab = enemyEntityPrefabLookup[curWaveProperties.BossType];
                    var newBossEntity = ecb.Instantiate(bossPrefab);
                    var bossSpawnPosition = spawnData.GetRandomSpawnPos(random, cameraPosition, cameraHalfExtents);
                    var bossTransform = LocalTransform.FromPosition(bossSpawnPosition);
                    ecb.SetComponent(newBossEntity, bossTransform);
                    ecb.AddComponent<EnemyMoveToPlayerTag>(newBossEntity);
                    ecb.AddComponent<InitializeEnhancementMaterialFlag>(newBossEntity);

                    var baseHealth = curWaveProperties.BossBaseHitPoints;
                    ecb.SetComponent(newBossEntity, new CurrentHitPoints { Value = baseHealth });
                    ecb.SetComponent(newBossEntity, new BaseHitPoints { Value = baseHealth });
                    ecb.AddComponent<EnemyHitPointsScaleWithPlayerLevelTag>(newBossEntity);
                    ecb.SetComponent(newBossEntity, new DropExperienceOnDestroy
                    {
                        ExperienceValue = curWaveProperties.BossExperiencePoints,
                        ChanceToDrop = 100,
                    });

                    var shouldDropCrate = random.ValueRW.Value.NextInt(100) <= curWaveProperties.BossChanceToDropCrate;
                    if (shouldDropCrate)
                    {
                        var cratePrefab = gamePrefabs.CratePrefab;
                        var spawnCrateOnDestroy = new SpawnOnDestroy { Value = cratePrefab };
                        ecb.AddComponent(newBossEntity, spawnCrateOnDestroy);
                    }
                }

                spawnerState.ValueRW.CurWaveIndex += 1;

                // Once the final wave has spawned, remove the wave status component to avoid attempting to spawn a new wave that does not exist.
                // Also, instantly destroy all existing enemies so final boss enemy is the only one on screen. Before destruction is queued, any enemies that have a destruction effect (all enemies should), setup the graphic dissolve effect.
                // A native array of enemies is created to ensure the InstantDestroyTag is only added to entities currently spawned and not entities still queued for instantiation inside the entity command buffer i.e. the final reaper entity. This is because the default behavior of EntityCommandBuffer operations on EntityQueries is to evaluate the query at playback time.
                if (spawnerState.ValueRO.CurWaveIndex >= spawnWaves.Length)
                {
                    ecb.RemoveComponent<EnemySpawnerState>(entity);

                    foreach (var graphicsEntity in SystemAPI.Query<GraphicsEntity>().WithAll<GraphicsEntityPlayDestroyEffectTag, EnemyTag>())
                    {
                        var newGraphicsEntity = ecb.Instantiate(graphicsEntity.Value);
                        ecb.RemoveComponent<LocalTransform>(newGraphicsEntity);
                        ecb.AddComponent(newGraphicsEntity, new DissolveData
                        {
                            StartTimestamp = (float)SystemAPI.Time.ElapsedTime,
                            Duration = 0.4f
                        });
                    }
                    
                    var enemyQuery = SystemAPI.QueryBuilder().WithAll<EnemyTag>().Build();
                    var enemies = enemyQuery.ToEntityArray(state.WorldUpdateAllocator);
                    ecb.AddComponent<InstantDestroyTag>(enemies);
                }
            }
            
            ecb.Playback(state.EntityManager);
        }
    }
    
    /// <summary>
    /// System to spawn enemies as defined by spawn waves and spawn events.
    /// </summary>
    /// <remarks>
    /// System updates at the beginning of the <see cref="DS_InitializationSystemGroup"/> and before <see cref="CharacterInitializationSystem"/> to ensure all initialization logic is executed for new enemies.
    /// </remarks>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(CharacterInitializationSystem))]
    public partial struct EnemySpawnSystem : ISystem
    {
        /// <summary>
        /// Query of all entities tagged with the <see cref="EnemyTag"/> to get a count of all enemies currently spawned so the system knows how many it needs to spawn to reach the minimum spawn count for the current wave.
        /// </summary>
        private EntityQuery _enemyQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerExperienceState>();
            state.RequireForUpdate<CameraTarget>();
            _enemyQuery = SystemAPI.QueryBuilder().WithAll<EnemyTag>().Build();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            var enemyCount = _enemyQuery.CalculateEntityCount();

            var cameraReferenceEntity = SystemAPI.GetSingletonEntity<CameraTarget>();
            var cameraPosition = SystemAPI.GetComponent<LocalToWorld>(cameraReferenceEntity).Position;
            var cameraHalfExtents = SystemAPI.GetComponent<CameraTarget>(cameraReferenceEntity).HalfExtents;
            
            // This foreach loop handles all the spawn events for a given wave and spawns enemies at the appropriate time.
            foreach(var (spawnEvents, random, spawnData, spawnerState) in SystemAPI.Query<DynamicBuffer<SpawnEvent>, RefRW<EntityRandom>, EnemySpawnData, EnemySpawnerState>())
            {
                var sharedSpawnIndex = new WaveIndexOfSpawn { Value = spawnerState.CurWaveIndex };
                
                // Iterate all spawn events for the current wave.
                for (var i = 0; i < spawnEvents.Length; i++)
                {
                    // If the spawn event has already occurred, continue to the next.
                    var curSpawnEvent = spawnEvents.ElementAt(i);
                    if (curSpawnEvent.HasOccurred) continue;
                    
                    // Decrement the delay time for the spawn event, if the timer is at or below 0, execute the spawn event.
                    curSpawnEvent.DelayTime -= deltaTime;
                    if (curSpawnEvent.DelayTime <= 0f)
                    {
                        // Determine if the spawn event should spawn enemies based on random chance.
                        var randomChance = random.ValueRW.Value.NextInt(0, 101);
                        if (randomChance < curSpawnEvent.ChanceOfOccurrence)
                        {
                            
                            // Handle different spawn formations.
                            switch (curSpawnEvent.SpawnFormation)
                            {
                                case SpawnFormation.LinearMoveGroup:
                                    var centerPosition = spawnData.GetRandomSpawnPos(random, cameraPosition, cameraHalfExtents);
                                    var directionToPlayer = math.normalize(cameraPosition - centerPosition);
                                    var angleToPlayer = math.atan2(directionToPlayer.x, directionToPlayer.z);
                                    var enemyLinearMovement = new EnemyLinearMovement { Angle = angleToPlayer };
                                    var characterMoveSpeed = new CharacterBaseMoveSpeed { Value = curSpawnEvent.MoveSpeed };

                                    var sideLength = (int)math.ceil(math.sqrt(curSpawnEvent.EnemyCount));
                                    for (var x = 0; x < sideLength; x++)
                                    {
                                        for (var y = 0; y < sideLength; y++)
                                        {
                                            var spawnPosition = centerPosition + new float3(x * curSpawnEvent.EnemySpacing, 0f, y * curSpawnEvent.EnemySpacing);
                                            var newEnemy = ecb.Instantiate(curSpawnEvent.EnemyPrefab);
                                            ecb.SetComponent(newEnemy, LocalTransform.FromPosition(spawnPosition));
                                            ecb.SetComponent(newEnemy, characterMoveSpeed);
                                            ecb.AddComponent(newEnemy, enemyLinearMovement);
                                            ecb.AddComponent<SetEnemyLinearMovementFlag>(newEnemy);
                                            ecb.AddSharedComponent(newEnemy, sharedSpawnIndex);
                                        }
                                    }
                                    break;
                                
                                case SpawnFormation.EllipseAroundView:
                                    var a = 1.5f * cameraHalfExtents.x;
                                    var b = 1.5f * cameraHalfExtents.z;

                                    var destroyAfterTime = new DestroyAfterTime { Value = curSpawnEvent.TimeToLive };
                                    
                                    for (var spawnIndex = 0; spawnIndex < curSpawnEvent.EnemyCount; spawnIndex++)
                                    {
                                        var angle = math.TAU * spawnIndex / curSpawnEvent.EnemyCount;
                                        var x = a * math.cos(angle);
                                        var y = b * math.sin(angle);
                                        var spawnPosition = cameraPosition + new float3(x, 0f, y);
                                        var newEnemy = ecb.Instantiate(curSpawnEvent.EnemyPrefab);
                                        ecb.SetComponent(newEnemy, LocalTransform.FromPosition(spawnPosition));
                                        var ellipseMoveSpeed = new CharacterBaseMoveSpeed { Value = curSpawnEvent.MoveSpeed };
                                        ecb.SetComponent(newEnemy, ellipseMoveSpeed);
                                        var ellipseDirectionToPlayer = math.normalize(cameraPosition - spawnPosition);
                                        var ellipseAngleToPlayer = math.atan2(ellipseDirectionToPlayer.x, ellipseDirectionToPlayer.z);
                                        ecb.AddComponent(newEnemy, new EnemyLinearMovement { Angle = ellipseAngleToPlayer });
                                        ecb.AddComponent<SetEnemyLinearMovementFlag>(newEnemy);
                                        ecb.AddComponent(newEnemy, destroyAfterTime);
                                        ecb.AddSharedComponent(newEnemy, sharedSpawnIndex);
                                    }
                                    
                                    break;
                                
                                case SpawnFormation.SineMoveVerticalLine:
                                    var isLeftSide = random.ValueRW.Value.NextBool();
                                    var verticalX = isLeftSide ? cameraPosition.x - cameraHalfExtents.x - spawnData.MinSpawnDistanceFromCameraBounds : cameraPosition.x + cameraHalfExtents.x + spawnData.MinSpawnDistanceFromCameraBounds;
                                    var verticalLineMoveDirection = isLeftSide ? SineWaveMoveDirection.Right : SineWaveMoveDirection.Left;
                                    var verticalSineWaveMovement = new EnemySineWaveMovement
                                    {
                                        MoveDirection = verticalLineMoveDirection,
                                        ConstantMoveSpeed = curSpawnEvent.MoveSpeed,
                                        Period = curSpawnEvent.Period,
                                        Amplitude = curSpawnEvent.Amplitude
                                    };

                                    for (var spawnIndex = 0; spawnIndex < curSpawnEvent.EnemyCount; spawnIndex++)
                                    {
                                        var verticalY = 1f / (curSpawnEvent.EnemyCount + 1) * (spawnIndex + 1) * cameraHalfExtents.z * 2;
                                        verticalY += cameraPosition.z - cameraHalfExtents.z;
                                        var spawnPosition = new float3(verticalX, 0f, verticalY);
                                        var newEnemy = ecb.Instantiate(curSpawnEvent.EnemyPrefab);
                                        ecb.SetComponent(newEnemy, LocalTransform.FromPosition(spawnPosition));
                                        ecb.AddComponent(newEnemy, verticalSineWaveMovement);
                                        ecb.AddSharedComponent(newEnemy, sharedSpawnIndex);
                                    }
                                    break;

                                case SpawnFormation.SineMoveHorizontalLine:
                                    var isBottomSide = random.ValueRW.Value.NextBool();
                                    var horizontalY = isBottomSide ? cameraPosition.z - cameraHalfExtents.z - spawnData.MinSpawnDistanceFromCameraBounds : cameraPosition.z + cameraHalfExtents.z + spawnData.MinSpawnDistanceFromCameraBounds;
                                    var horizontalLineMoveDirection = isBottomSide ? SineWaveMoveDirection.Up: SineWaveMoveDirection.Down;
                                    var horizontalSineWaveMovement = new EnemySineWaveMovement
                                    {
                                        MoveDirection = horizontalLineMoveDirection,
                                        ConstantMoveSpeed = curSpawnEvent.MoveSpeed,
                                        Period = curSpawnEvent.Period,
                                        Amplitude = curSpawnEvent.Amplitude
                                    };

                                    for (var spawnIndex = 0; spawnIndex < curSpawnEvent.EnemyCount; spawnIndex++)
                                    {
                                        var horizontalX = 1f / (curSpawnEvent.EnemyCount + 1) * (spawnIndex + 1) * cameraHalfExtents.x * 2;
                                        horizontalX += cameraPosition.x - cameraHalfExtents.x;
                                        var spawnPosition = new float3(horizontalX, 0f, horizontalY);
                                        var newEnemy = ecb.Instantiate(curSpawnEvent.EnemyPrefab);
                                        ecb.SetComponent(newEnemy, LocalTransform.FromPosition(spawnPosition));
                                        ecb.AddComponent(newEnemy, horizontalSineWaveMovement);
                                        ecb.AddSharedComponent(newEnemy, sharedSpawnIndex);
                                    }
                                    break;

                                default:
                                    Debug.LogWarning($"Warning: attempting to spawn enemy spawn formation: {curSpawnEvent.SpawnFormation} which has no definition of how spawn is structured");
                                    break;
                            }
                        }

                        curSpawnEvent.HasOccurred = true;
                    }

                    spawnEvents.ElementAt(i) = curSpawnEvent;
                }
            }
            
            // This foreach loop handles spawning of regular enemies in the spawn wave.
            foreach (var (spawnTimer, random, spawnData, spawnWaveData, spawnWavePrefabs, spawnerState) in SystemAPI.Query<RefRW<EnemySpawnTimer>, RefRW<EntityRandom>, EnemySpawnData, EnemySpawnWaveData, DynamicBuffer<SpawnWaveEnemyPrefab>, EnemySpawnerState>())
            {
                // If there are no prefabs to spawn, continue.
                if (spawnWavePrefabs.IsEmpty) continue;
                
                var sharedSpawnIndex = new WaveIndexOfSpawn { Value = spawnerState.CurWaveIndex };
                
                // Determine when the next batch of enemies should be spawned.
                spawnTimer.ValueRW.Value -= deltaTime;
                if (spawnTimer.ValueRO.Value > 0f) continue;
                spawnTimer.ValueRW.Value += spawnWaveData.SpawnInterval;

                // Spawn one enemy of each type for the current spawn wave until the minimum enemy count for the current wave is reached.
                do
                {
                    foreach (var enemyTypePrefab in spawnWavePrefabs)
                    {
                        var newSpawnPos = spawnData.GetRandomSpawnPos(random, cameraPosition, cameraHalfExtents);
                        var entityTransform = LocalTransform.FromPosition(newSpawnPos);

                        var newEnemy = ecb.Instantiate(enemyTypePrefab.Value);
                        ecb.SetComponent(newEnemy, entityTransform);
                        ecb.AddComponent<EnemyMoveToPlayerTag>(newEnemy);
                        ecb.AddSharedComponent(newEnemy, sharedSpawnIndex);
                        
                        enemyCount += 1;
                    }
                } while (enemyCount < spawnWaveData.MinEnemyCount);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}