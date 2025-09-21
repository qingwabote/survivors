using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Random = Unity.Mathematics.Random;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data related to level generation.
    /// </summary>
    /// <seealso cref="LevelGenerationSystem"/>
    public struct LevelGenerationData : IComponentData
    {
        /// <summary>
        /// Entity prefab for the background entity - ground surface.
        /// </summary>
        public Entity BackgroundPrefab;
        /// <summary>
        /// Entity prefab for the caution tape graphics entity that is displayed around the far edges of the play area.
        /// </summary>
        public Entity CautionPrefab;
        /// <summary>
        /// Scaling factor used to determine the number of units per background tile on the background prefab.
        /// </summary>
        /// <remarks>
        /// Note that the background entity is a single quad mesh for the entire play area and tiling is set in the shader via the <see cref="TileScaleOverride"/> component.
        /// </remarks>
        public float UnitsPerBackgroundTile;
        /// <summary>
        /// Level is generated in a square centered at the origin with length and height defined by this level size.
        /// </summary>
        public float LevelSize;
        /// <summary>
        /// Number of decoration and environment items to spawn in the play area.
        /// </summary>
        public int ItemCount;
        /// <summary>
        /// Each level element has a rarity component to influence how often it will be spawned in the world. This value is the sum of all rarity values on all level elements used for random number generation to determine what level element should spawn next.
        /// </summary>
        public int MaxRarity;
    }

    /// <summary>
    /// Dynamic buffer data for level elements. Level elements are decoration and environment entities that will spawn in the level.
    /// </summary>
    /// <seealso cref="DecorationTag"/>
    /// <seealso cref="EnvironmentTag"/>
    public struct LevelElementData : IBufferElementData
    {
        /// <summary>
        /// Entity prefab that can be spawned via level generation.
        /// </summary>
        public Entity Value;
        /// <summary>
        /// Rarity value for how frequently this item will be spawned.
        /// </summary>
        /// <remarks>
        /// A rarity value of 0 will never be selected for spawning while a rarity value of 100 will have the highest likelihood of being selected to be spawned.
        /// </remarks>
        public int Rarity;
        /// <summary>
        /// Minimum scale value to set this entity at when spawning.
        /// </summary>
        /// <remarks>
        /// During spawning, a random scale value will be selected between min and max to add some additional randomness to spawning.
        /// </remarks>
        public float MinScale;
        /// <summary>
        /// Maximum scale value to set this entity at when spawning.
        /// </summary>
        /// <remarks>
        /// During spawning, a random scale value will be selected between min and max to add some additional randomness to spawning.
        /// </remarks>
        public float MaxScale;
    }

    /// <summary>
    /// Enableable component to inform the <see cref="LevelGenerationSystem"/> that the level should be regenerated.
    /// </summary>
    public struct RegenerateLevelFlag : IComponentData, IEnableableComponent {}

    /// <summary>
    /// Data struct used to author a collection of level elements in the Unity editor.
    /// </summary>
    [System.Serializable]
    public struct LevelElementInfo
    {
        /// <summary>
        /// GameObject prefab for the entity that will be spawned by level generation.
        /// </summary>
        public GameObject Prefab;
        /// <summary>
        /// Rarity value for how frequently this item will be spawned.
        /// </summary>
        /// <remarks>
        /// A rarity value of 0 will never be selected for spawning while a rarity value of 100 will have the highest likelihood of being selected to be spawned.
        /// </remarks>
        [Range(0,100)]
        public int Rarity;
        /// <summary>
        /// Minimum scale value to set this entity at when spawning.
        /// </summary>
        /// <remarks>
        /// During spawning, a random scale value will be selected between min and max to add some additional randomness to spawning.
        /// </remarks>
        public float MinScale;
        /// <summary>
        /// Maximum scale value to set this entity at when spawning.
        /// </summary>
        /// <remarks>
        /// During spawning, a random scale value will be selected between min and max to add some additional randomness to spawning.
        /// </remarks>
        public float MaxScale;
    }

    /// <summary>
    /// Material property override to set the tile scale factor.
    /// </summary>
    /// <remarks>
    /// Your IDE may gray out the Value field as this value is not used in our code. However, Unity uses it to apply this value to the material property defined in the MaterialProperty attribute.
    /// Be sure the MaterialProperty string exactly matches the reference string defined in the shader as this will silently fail if there is a typo.
    /// </remarks>
    [MaterialProperty("_TileScale")]
    public struct TileScaleOverride : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// Data component used to store the safe bounds of the play area.
    /// </summary>
    /// <remarks>
    /// Used by the <see cref="DamagePlayerOutsideSafeBoundsSystem"/> to apply damage to the player when they have left the play area.
    /// </remarks>
    public struct LevelSafeBounds : IComponentData
    {
        public float2 MinPosition;
        public float2 MaxPosition;

        public bool InsideSafeBounds(float2 testPosition)
        {
            return testPosition.x > MinPosition.x && testPosition.x < MaxPosition.x && testPosition.y > MinPosition.y && testPosition.y < MaxPosition.y;
        }
    }
    
    /// <summary>
    /// Authoring script to add components necessary for level generation.
    /// </summary>
    /// <remarks>
    /// This script uses the OnValidate method to determine when the seed value has changed in the editor, which will trigger a regeneration of the level. This regeneration occurs both in and out of playmode as the <see cref="LevelGenerationSystem"/> is set to update in both Editor and Default worlds.
    /// </remarks>
    public class LevelGenerationAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Random seed to generate level data from.
        /// </summary>
        public uint Seed;
        /// <summary>
        /// GameObject prefab of the background entity that will be spawned.
        /// </summary>
        public GameObject BackgroundPrefab;
        /// <summary>
        /// GameObject prefab of the caution tape entity that will be spawned around the edge of the play area.
        /// </summary>
        public GameObject CautionPrefab;
        /// <summary>
        /// Scaling factor used to determine the number of units per background tile on the background prefab.
        /// </summary>
        /// <remarks>
        /// Note that the background entity is a single quad mesh for the entire play area and tiling is set in the shader via the <see cref="TileScaleOverride"/> component.
        /// </remarks>
        public float UnitsPerBackgroundTile;
        /// <summary>
        /// Level is generated in a square centered at the origin with length and height defined by this level size.
        /// </summary>
        public float LevelSize;
        /// <summary>
        /// Collection of level elements that store data about entities to be spawned during level generation.
        /// </summary>
        /// <seealso cref="LevelElementData"/>
        public LevelElementInfo[] LevelElements;
        /// <summary>
        /// Number of decoration and environment items to spawn in the play area.
        /// </summary>
        public int ItemCount;
        
        /// <summary>
        /// Used in OnValidate to determine when the seed value has changed and when level generation should take place.
        /// </summary>
        private uint _prevSeed;
        
        private class Baker : Baker<LevelGenerationAuthoring>
        {
            public override void Bake(LevelGenerationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var levelPrefabs = AddBuffer<LevelElementData>(entity);
                var maxRarity = 0;
                foreach (var levelElement in authoring.LevelElements)
                {
                    maxRarity += levelElement.Rarity;
                    levelPrefabs.Add(new LevelElementData
                    {
                        Value = GetEntity(levelElement.Prefab, TransformUsageFlags.Dynamic),
                        Rarity = maxRarity,
                        MinScale = levelElement.MinScale,
                        MaxScale = levelElement.MaxScale
                    });
                }

                AddComponent(entity, new LevelGenerationData
                {
                    BackgroundPrefab = GetEntity(authoring.BackgroundPrefab, TransformUsageFlags.Dynamic),
                    CautionPrefab = GetEntity(authoring.CautionPrefab, TransformUsageFlags.Dynamic),
                    UnitsPerBackgroundTile = authoring.UnitsPerBackgroundTile,
                    LevelSize = authoring.LevelSize,
                    ItemCount = authoring.ItemCount,
                    MaxRarity = maxRarity
                });
                AddComponent<RegenerateLevelFlag>(entity);
                AddComponent(entity, new EntityRandom
                {
                    Value = Random.CreateFromIndex(authoring.Seed),
                });
            }
        }

        private void OnValidate()
        {
            if (_prevSeed != Seed)
            {
                if (World.DefaultGameObjectInjectionWorld == null) return;
                _prevSeed = Seed;
                var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                var query = entityManager.CreateEntityQuery(typeof(LevelGenerationData));
                if (!query.HasSingleton<LevelGenerationData>()) return;
                var levelEntity = query.GetSingletonEntity();
                entityManager.SetComponentEnabled<RegenerateLevelFlag>(levelEntity, true);
            }
        }
    }

    /// <summary>
    /// System to randomly generate a level with decoration and environment entities.
    /// </summary>
    /// <remarks>
    /// This system utilizes WorldSystemFilter flags to determine which worlds this system should run in. This system will run in the editor world so changes to level generation data can be seen and experimented with outside playmode. This system will also run during playmode to ensure the level is created as these entities will not be baked into the subscene.
    /// This system will first destroy any entities previously generated by this level generator. To do this, a native array of entities is first generated from the query, this is to effectively "capture" the entities to be destroyed and will not destroy any entities spawned later in the system as the default behavior of EntityCommandBuffer operations on EntityQueries is to evaluate the query at playback time.
    /// Next it will spawn the background entity and set the size and graphics tiling as needed.
    /// It will next do something similar with the caution tape around the bounds of the play area.
    /// Finally, all the level elements will be randomly selected and placed randomly around the map.
    /// </remarks>
    [WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
    public partial struct LevelGenerationSystem : ISystem
    {
        /// <summary>
        /// Entity query for all entities that should be destroyed when the level generation system runs.
        /// </summary>
        /// <remarks>
        /// This is effectively all entities generated by previous iterations of the level generator, so the generator is always building up from a clean slate.
        /// </remarks>
        private EntityQuery _destroyOnRegenerateQuery;
        /// <summary>
        /// Entity archetype for a data-only entity containing the <see cref="LevelSafeBounds"/>.
        /// </summary>
        private EntityArchetype _levelSafeAreaArchetype;
        
        public void OnCreate(ref SystemState state)
        {
            _destroyOnRegenerateQuery = new EntityQueryBuilder(state.WorldUpdateAllocator).WithAny<EnvironmentTag, DecorationTag, LevelSafeBounds>().Build(ref state);
            _levelSafeAreaArchetype = state.EntityManager.CreateArchetype(typeof(LevelSafeBounds));
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var (levelElements, random, regenerateLevel, generationData) in SystemAPI.Query<DynamicBuffer<LevelElementData>, RefRW<EntityRandom>, EnabledRefRW<RegenerateLevelFlag>, LevelGenerationData>())
            {
                var entitiesToDestroy = _destroyOnRegenerateQuery.ToEntityArray(state.WorldUpdateAllocator);
                ecb.DestroyEntity(entitiesToDestroy);

                var newBackground = ecb.Instantiate(generationData.BackgroundPrefab);

                var center = new float3(0f, -10f, 0f);
                var rotation = quaternion.Euler(0.5f * math.PI, 0f, 0f);
                var bgScale = generationData.LevelSize + 100;
                ecb.SetComponent(newBackground, LocalTransform.FromPositionRotationScale(center, rotation, bgScale));
                ecb.AddComponent(newBackground, new TileScaleOverride { Value = bgScale / generationData.UnitsPerBackgroundTile });

                var cautionPositionOffsetList = new FixedList64Bytes<float3>
                {
                    new(0f, 15.1f, generationData.LevelSize - 1.375f),
                    new(generationData.LevelSize - 1.375f, 15f, 0f),
                    new(0f, 15.1f, -1f * generationData.LevelSize + 1.375f),
                    new(-1f * generationData.LevelSize + 1.375f, 15f, 0f)
                };

                for (var i = 0; i < 4; i++)
                {
                    var newCaution = ecb.Instantiate(generationData.CautionPrefab);
                    var cautionPosition = center + cautionPositionOffsetList[i];
                    var cautionRotation = quaternion.Euler(0.5f * math.PI, 0.5f * i * math.PI, 0f);
                    ecb.SetComponent(newCaution, LocalTransform.FromPositionRotationScale(cautionPosition, cautionRotation, generationData.LevelSize));
                    ecb.AddComponent(newCaution, new TileScaleOverride { Value = generationData.LevelSize / 2f });
                }

                var minPosition = new float2(generationData.LevelSize * -0.5f);
                var maxPosition = new float2(generationData.LevelSize * 0.5f);
                var levelSafeBoundsEntity = ecb.CreateEntity(_levelSafeAreaArchetype);
                ecb.SetComponent(levelSafeBoundsEntity, new LevelSafeBounds
                {
                    MinPosition = minPosition,
                    MaxPosition = maxPosition
                });
                
                if (levelElements.IsEmpty) continue;
                if (generationData.MaxRarity <= 0) continue;
                regenerateLevel.ValueRW = false;
                
                for (var i = 0; i < generationData.ItemCount; i++)
                {
                    var rarity = random.ValueRW.Value.NextInt(generationData.MaxRarity);
                    var prefabIndex = -1;
                    for (var j = 0; j < levelElements.Length; j++)
                    {
                        if (rarity >= levelElements[j].Rarity) continue;
                        prefabIndex = j;
                        break;
                    }

                    if (prefabIndex < 0 || prefabIndex >= levelElements.Length) continue;
                    
                    var levelElementData = levelElements[prefabIndex];
                    var newElement = ecb.Instantiate(levelElementData.Value);
                    var randPos2 = random.ValueRW.Value.NextFloat2(minPosition, maxPosition);
                    
                    var yPos = SystemAPI.HasComponent<DecorationTag>(levelElementData.Value) ? -2.5f : -0.025f;
                    var randPos = new float3(randPos2.x, yPos, randPos2.y);
                    var randScale = random.ValueRW.Value.NextFloat(levelElementData.MinScale, levelElementData.MaxScale);
                    ecb.SetComponent(newElement, LocalTransform.FromPositionRotationScale(randPos, quaternion.identity, randScale));
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}