using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Unity.Burst;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Tag component to identify this entity as an attractable item.
    /// </summary>
    /// <remarks>
    /// Attractable items are items that exist in the game world that will move towards the player when the item is within the attractable radius of the player (<see cref="PlayerAttractionAreaData.Radius"/>).
    /// </remarks>
    public struct AttractableItemTag : IComponentData {}

    /// <summary>
    /// Enableable component to signify when the item is attracted to the player.
    /// </summary>
    /// <remarks>
    /// This component also contains data related to the movement of the item from its start position to the player.
    /// A component becomes attracted to the player when the item is within attractable radius of the player (<see cref="PlayerAttractionAreaData.Radius"/>).
    /// </remarks>
    public struct IsAttractedToPlayerData : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// Start time in game time that the item became attracted to the player.
        /// </summary>
        public double StartTime;
        /// <summary>
        /// Start position of the item. Required for calculating movement towards the player.
        /// </summary>
        public float3 StartPosition;
    }

    /// <summary>
    /// Component that defines the move speed of attractable items from their starting position to the player.
    /// </summary>
    /// <seeaslo cref="AttractableItemSystem"/>
    public struct ItemMoveSpeed : IComponentData
    {
        public float Value;
    }
    
    /// <summary>
    /// Authoring script to add components required for item attraction.
    /// </summary>
    /// <remarks>
    /// <see cref="ItemAuthoring"/> is a required component to add other components necessary for items.
    /// </remarks>
    [RequireComponent(typeof(ItemAuthoring))]
    public class AttractableItemAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Move speed of attractable items from starting position to the player.
        /// </summary>
        public float ItemMoveSpeed;
        
        private class Baker : Baker<AttractableItemAuthoring>
        {
            public override void Bake(AttractableItemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<AttractableItemTag>(entity);
                AddComponent<IsAttractedToPlayerData>(entity);
                SetComponentEnabled<IsAttractedToPlayerData>(entity, false);
                AddComponent(entity, new ItemMoveSpeed { Value = authoring.ItemMoveSpeed });
            }
        }
    }

    /// <summary>
    /// System that moves attractable items from their start position to the player.
    /// </summary>
    /// <remarks>
    /// System uses an easing function to move attractable item first away from the player then towards the player. This easing function was chosen for aesthetic purposes as it gives visual feedback to the player when the item begins to attract to the player.
    /// </remarks>
    /// <seealso cref="DetectItemAttractionSystem"/>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct AttractableItemSystem : ISystem
    {
        /// <summary>
        /// Constant required for easing function
        /// </summary>
        private const float EASE1 = 3f;
        
        /// <summary>
        /// Constant required for easing function
        /// </summary>
        private const float EASE2 = EASE1 + -0.1f;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<IsAttractedToPlayerData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
            var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
            var currentTime = SystemAPI.Time.ElapsedTime;
            
            foreach (var (transform, moveSpeed, playerAttraction) in SystemAPI.Query<RefRW<LocalTransform>, ItemMoveSpeed, IsAttractedToPlayerData>())
            {
                var easeTime = (float)(currentTime - playerAttraction.StartTime);
                easeTime *= moveSpeed.Value;
                easeTime += 0.25f;
                // https://easings.net/#easeInBack
                var t = EASE2 * easeTime * easeTime * easeTime - EASE1 * easeTime * easeTime + 0.125f;
                // math.lerp extrapolates position when t is < 0 or > 1.
                var interpolatedPosition = math.lerp(playerAttraction.StartPosition, playerPosition, t);
                transform.ValueRW.Position = interpolatedPosition;
            }
        }
    }

    /// <summary>
    /// System to detect attractable items. 
    /// </summary>
    /// <remarks>
    /// Uses the <see cref="PlayerAttractionAreaData"/> to execute an OverlapSphere method to locate any attractable items within the attraction radius. When an item is within radius, components related to moving the item towards the player are initialized.
    /// </remarks>
    /// <seealso cref="AttractableItemSystem"/>
    [UpdateInGroup(typeof(DS_PhysicsSystemGroup))]
    public partial struct DetectItemAttractionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            foreach (var (attractionAreaData, localToWorld, characterEntity) in SystemAPI.Query<PlayerAttractionAreaData, LocalToWorld, CharacterEntity>())
            {
                var itemAttractionRadiusModifier = SystemAPI.GetComponent<CharacterStatModificationState>(characterEntity.Value).ItemAttractionRadius;
                var itemAttractionRadius = attractionAreaData.Radius * itemAttractionRadiusModifier;
                var hits = new NativeList<DistanceHit>(state.WorldUpdateAllocator);
                physicsWorld.OverlapSphere(localToWorld.Position, itemAttractionRadius, ref hits, attractionAreaData.CollisionFilter);

                foreach (var hit in hits)
                {
                    if (!SystemAPI.HasComponent<AttractableItemTag>(hit.Entity)) continue;
                    if (SystemAPI.IsComponentEnabled<IsAttractedToPlayerData>(hit.Entity)) continue;
                    SystemAPI.SetComponentEnabled<IsAttractedToPlayerData>(hit.Entity, true);
                    var startTime = SystemAPI.Time.ElapsedTime;
                    var startPosition = SystemAPI.GetComponent<LocalTransform>(hit.Entity).Position;
                    SystemAPI.SetComponent(hit.Entity, new IsAttractedToPlayerData { StartTime = startTime, StartPosition = startPosition });
                }
            }
        }
    }
}