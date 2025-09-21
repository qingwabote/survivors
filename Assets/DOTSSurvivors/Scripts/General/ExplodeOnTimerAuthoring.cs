using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data used to initialize the <see cref="ExplosionGraphicsData"/>.
    /// </summary>
    /// <remarks>
    /// This is required as the <see cref="ExplosionGraphicsData"/> component will be added to the <see cref="GraphicsEntity"/> for this explosion entity, so these values cannot be baked onto the other entity and must be set at runtime.
    /// Enableable component will be disabled after initialization.
    /// </remarks>
    public struct ExplosionGraphicsInitializationData : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// Color tint that will be applied to the graphics entity when flashing.
        /// </summary>
        public float4 Color;
        /// <summary>
        /// Time at which the graphics entity will begin to flash. When the parent's <see cref="DestroyAfterTime.Value"/> is less than this number, the graphics entity will begin to flash.
        /// </summary>
        public float FlashTime;
        /// <summary>
        /// Frequency at which the graphics entity will flash - meaning the graphics entity will be alternate between no color tint and being tinted with defined color value each for this amount of time.
        /// </summary>
        public float FlashFrequency;
    }

    /// <summary>
    /// Component to store data relevant to the flashing of a <see cref="GraphicsEntity"/> for exploding entities that have the <see cref="ExplodeOnTimerData"/> component.
    /// </summary>
    public struct ExplosionGraphicsData : IComponentData
    {
        /// <summary>
        /// Color tint that will be applied to the graphics entity when flashing.
        /// </summary>
        public float4 Color;
        /// <summary>
        /// Time at which the graphics entity will begin to flash. When the parent's <see cref="DestroyAfterTime.Value"/> is less than this number, the graphics entity will begin to flash.
        /// </summary>
        public float FlashTime;
        /// <summary>
        /// Frequency at which the graphics entity will flash - meaning the graphics entity will be alternate between no color tint and being tinted with defined color value each for this amount of time.
        /// </summary>
        public float FlashFrequency;
    }

    /// <summary>
    /// Data component to store data relevant to entities that can explode.
    /// </summary>
    /// <remarks>
    /// During explosion, entities will spawn an explosion entity via a <see cref="SpawnOnDestroy"/> component in the <see cref="DestroyEntitySystem"/>. Data in this component will be applied to the explosion entity.
    /// </remarks>
    public struct ExplodeOnTimerData : IComponentData
    {
        /// <summary>
        /// Amount of damage the explosion entity will deal to opposing enemies.
        /// </summary>
        public int Damage;
        /// <summary>
        /// The scale of the explosion entity.
        /// </summary>
        public float Area;
        /// <summary>
        /// Duration in seconds for how long the explosion entity will exist in the game world.
        /// </summary>
        public float ExplosionTimeToLive;
    }
    
    /// <summary>
    /// Authoring script to add components necessary for explosion to an entity.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="DestroyAfterTimeAuthoring"/> and <see cref="GraphicsEntityAuthoring"/> to ensure other necessary components are present.
    /// </remarks>
    [RequireComponent(typeof(DestroyAfterTimeAuthoring))]
    [RequireComponent(typeof(GraphicsEntityAuthoring))]
    public class ExplodeOnTimerAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Amount of damage the explosion entity will deal to opposing enemies.
        /// </summary>
        public int Damage;
        /// <summary>
        /// The scale of the explosion entity.
        /// </summary>
        public float Area;
        /// <summary>
        /// Duration in seconds for how long the explosion entity will exist in the game world.
        /// </summary>
        public float ExplosionTimeToLive;
        /// <summary>
        /// Time at which the graphics entity will begin to flash. When the parent's <see cref="DestroyAfterTime.Value"/> is less than this number, the graphics entity will begin to flash.
        /// </summary>
        public float FlashTime;
        /// <summary>
        /// Frequency at which the graphics entity will flash - meaning the graphics entity will be alternate between no color tint and being tinted with defined color value each for this amount of time.
        /// </summary>
        public float FlashFrequency;
        /// <summary>
        /// Color tint that will be applied to the graphics entity when flashing.
        /// </summary>
        public Color FlashColor;
        /// <summary>
        /// GameObject prefab that will be spawned via <see cref="SpawnOnDestroy"/> component in the <see cref="DestroyEntitySystem"/>.
        /// </summary>
        public GameObject ExplosionPrefab;
        
        private class Baker : Baker<ExplodeOnTimerAuthoring>
        {
            public override void Bake(ExplodeOnTimerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ExplosionGraphicsInitializationData
                {
                    Color = (Vector4)authoring.FlashColor,
                    FlashTime = authoring.FlashTime,
                    FlashFrequency = authoring.FlashFrequency
                });
                AddComponent(entity, new ExplodeOnTimerData
                {
                    Damage = authoring.Damage,
                    Area = authoring.Area,
                    ExplosionTimeToLive = authoring.ExplosionTimeToLive
                });
                var explosionPrefab = GetEntity(authoring.ExplosionPrefab, TransformUsageFlags.Dynamic);
                AddComponent(entity, new SpawnOnDestroy { Value = explosionPrefab });
            }
        }
    }

    /// <summary>
    /// System to handle initialization of <see cref="ExplosionGraphicsData"/> on the <see cref="GraphicsEntity"/> and flash the graphics entity during the flash phase.
    /// </summary>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct FlashOnTimerSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Initialize graphics so they have the appropriate properties
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            
            foreach (var (initializationData, graphicsEntity, shouldInitialize) in SystemAPI.Query<ExplosionGraphicsInitializationData, GraphicsEntity, EnabledRefRW<ExplosionGraphicsInitializationData>>())
            {
                ecb.AddComponent(graphicsEntity.Value, new ExplosionGraphicsData
                {
                    Color = initializationData.Color,
                    FlashTime = initializationData.FlashTime,
                    FlashFrequency = initializationData.FlashFrequency
                });

                shouldInitialize.ValueRW = false;
            }

            foreach (var (colorOverride, explosionGraphicsData, parent) in SystemAPI.Query<RefRW<URPMaterialPropertyBaseColor>, ExplosionGraphicsData, Parent>())
            {
                var timeRemaining = SystemAPI.GetComponent<DestroyAfterTime>(parent.Value).Value;
                if (timeRemaining >= explosionGraphicsData.FlashTime) continue;
                var shouldFlash = math.ceil(timeRemaining / explosionGraphicsData.FlashFrequency) % 2 == 0;
                colorOverride.ValueRW.Value = shouldFlash ? explosionGraphicsData.Color : new float4(1);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}