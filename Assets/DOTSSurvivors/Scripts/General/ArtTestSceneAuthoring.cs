using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component to store data pertinent to the secret art test scene. 
    /// </summary>
    /// <seealso cref="InitializeArtTestSceneSystem"/>
    public struct ArtTestSceneData : IComponentData
    {
        /// <summary>
        /// Entity prefab for the caution tape graphics entity that is displayed around the far edges of the play area.
        /// </summary>
        public Entity CautionPrefab;
        /// <summary>
        /// Prefab of the player entity.
        /// </summary>
        public Entity PlayerPrefab;
        /// <summary>
        /// Level is created in a square, this defines the size of each side of the square.
        /// </summary>
        public float LevelSize;
    }
    
    /// <summary>
    /// Flag component to determine if the secret art test scene should be initialized
    /// </summary>
    /// <remarks>
    /// Enableable component will be enabled before initialization and disabled after initialization has completed.
    /// </remarks>
    public struct InitializeArtTestSceneFlag : IComponentData, IEnableableComponent {}
    
    /// <summary>
    /// Authoring script to set values on the <see cref="ArtTestSceneData"/> used for initializing the secret art test scene.
    /// </summary>
    public class ArtTestSceneAuthoring : MonoBehaviour
    {
        /// <summary>
        /// GameObject prefab of the caution tape entity that will be spawned around the edge of the play area.
        /// </summary>
        public GameObject CautionPrefab;
        /// <summary>
        /// GameObject prefab of the player entity.
        /// </summary>
        public GameObject PlayerPrefab;
        /// <summary>
        /// Level is created in a square, this defines the size of each side of the square.
        /// </summary>
        public float LevelSize;
        
        private class Baker : Baker<ArtTestSceneAuthoring>
        {
            public override void Bake(ArtTestSceneAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ArtTestSceneData
                {
                    CautionPrefab = GetEntity(authoring.CautionPrefab, TransformUsageFlags.Dynamic),
                    PlayerPrefab = GetEntity(authoring.PlayerPrefab, TransformUsageFlags.Dynamic),
                    LevelSize = authoring.LevelSize
                });
                AddComponent<InitializeArtTestSceneFlag>(entity);
            }
        }
    }

    /// <summary>
    /// System to initialize data for the secret art test scene.
    /// </summary>
    /// <seealso cref="ArtTestSceneData"/>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup))]
    public partial struct InitializeArtTestSceneSystem : ISystem
    {
        /// <summary>
        /// Entity archetype for a data-only entity containing the <see cref="LevelSafeBounds"/>.
        /// </summary>
        private EntityArchetype _levelSafeAreaArchetype;
        
        /// <summary>
        /// Entity query for the data-only entity with a <see cref="LevelSafeBounds"/>.
        /// </summary>
        /// <remarks>
        /// Used to destroy the entity if it already exists before creating a new one.
        /// </remarks>
        private EntityQuery _levelSafeBoundsQuery;
        
        public void OnCreate(ref SystemState state)
        {
            _levelSafeAreaArchetype = state.EntityManager.CreateArchetype(typeof(LevelSafeBounds));
            _levelSafeBoundsQuery = state.EntityManager.CreateEntityQuery(typeof(LevelSafeBounds));
        }
        
        public void OnUpdate(ref SystemState state)
        {
            if (!_levelSafeBoundsQuery.IsEmpty)
            {
                var levelSafeBoundsEntity = _levelSafeBoundsQuery.GetSingletonEntity();
                state.EntityManager.DestroyEntity(levelSafeBoundsEntity);
            }
            
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            
            foreach (var (sceneData, shouldInitialize) in SystemAPI.Query<ArtTestSceneData, EnabledRefRW<InitializeArtTestSceneFlag>>())
            {
                var center = new float3(0f, -10f, 0f);
                var cautionPositionOffsetList = new FixedList64Bytes<float3>
                {
                    new(0f, 15.1f, sceneData.LevelSize - 1.375f),
                    new(sceneData.LevelSize - 1.375f, 15f, 0f),
                    new(0f, 15.1f, -1f * sceneData.LevelSize + 1.375f),
                    new(-1f * sceneData.LevelSize + 1.375f, 15f, 0f)
                };

                for (var i = 0; i < 4; i++)
                {
                    var newCaution = ecb.Instantiate(sceneData.CautionPrefab);
                    var cautionPosition = center + cautionPositionOffsetList[i];
                    var cautionRotation = quaternion.Euler(0.5f * math.PI, 0.5f * i * math.PI, 0f);
                    ecb.SetComponent(newCaution, LocalTransform.FromPositionRotationScale(cautionPosition, cautionRotation, sceneData.LevelSize));
                    ecb.AddComponent(newCaution, new TileScaleOverride { Value = sceneData.LevelSize / 2f });
                }
                
                var levelSafeBoundsEntity = ecb.CreateEntity(_levelSafeAreaArchetype);
                ecb.SetComponent(levelSafeBoundsEntity, new LevelSafeBounds
                {
                    MinPosition = new float2(-0.5f * sceneData.LevelSize),
                    MaxPosition = new float2(0.5f * sceneData.LevelSize)
                });

                ecb.Instantiate(sceneData.PlayerPrefab);
                
                if (LoadingScreenUIController.Instance != null)
                {
                    LoadingScreenUIController.Instance.HideLoadingScreen();
                }

                shouldInitialize.ValueRW = false;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}