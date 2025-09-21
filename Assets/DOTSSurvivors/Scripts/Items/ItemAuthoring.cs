using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Burst;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Tag component to identify this as an item entity.
    /// </summary>
    public struct ItemTag : IComponentData {}
    
    /// <summary>
    /// Authoring component to add <see cref="ItemTag"/> to the baked entity.
    /// </summary>
    /// <remarks>
    /// Additional authoring scripts required to add components necessary for desired behavior.
    /// </remarks>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    public class ItemAuthoring : MonoBehaviour
    {
        private class Baker : Baker<ItemAuthoring>
        {
            public override void Bake(ItemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ItemTag>(entity);
            }
        }
    }

    /// <summary>
    /// System to schedule the <see cref="ItemCollectionJob"/>. Job is a collision event job so it is scheduled from the <see cref="DS_PhysicsSystemGroup"/>.
    /// </summary>
    [UpdateInGroup(typeof(DS_PhysicsSystemGroup))]
    public partial struct ItemCollectionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();

            state.Dependency = new ItemCollectionJob
            {
                ItemLookup = SystemAPI.GetComponentLookup<ItemTag>(true),
                PlayerLookup = SystemAPI.GetComponentLookup<PlayerTag>(true),
                InteractionBufferLookup = SystemAPI.GetBufferLookup<EntityInteraction>()
            }.Schedule(simulationSingleton, state.Dependency);
        }
    }
    
    /// <summary>
    /// Job that checks for collision events between the player and items in the game world. Valid collisions will add an interaction to the <see cref="EntityInteraction"/> dyanmic buffer so subsequent systems can apply additional logic pertaining to the specific item.
    /// </summary>
    [BurstCompile]
    public struct ItemCollectionJob : ICollisionEventsJob
    {
        /// <summary>
        /// ReadOnly component lookup used to identity the item entity in the collision event.
        /// </summary>
        [ReadOnly] public ComponentLookup<ItemTag> ItemLookup;
        /// <summary>
        /// ReadOnly component lookup used to identity the player entity in the collision event.
        /// </summary>
        [ReadOnly] public ComponentLookup<PlayerTag> PlayerLookup;
        /// <summary>
        /// Buffer lookup used to add the player entity to the interaction buffer for the item in the collision event.
        /// </summary>
        public BufferLookup<EntityInteraction> InteractionBufferLookup;

        [BurstCompile]
        public void Execute(CollisionEvent collisionEvent)
        {
            Entity playerEntity;
            Entity itemEntity;

            if (PlayerLookup.HasComponent(collisionEvent.EntityA) && ItemLookup.HasComponent(collisionEvent.EntityB))
            {
                playerEntity = collisionEvent.EntityA;
                itemEntity = collisionEvent.EntityB;
            }
            else if (PlayerLookup.HasComponent(collisionEvent.EntityB) && ItemLookup.HasComponent(collisionEvent.EntityA))
            {
                playerEntity = collisionEvent.EntityB;
                itemEntity = collisionEvent.EntityA;
            }
            else
            {
                return;
            }

            var interactionBuffer = InteractionBufferLookup[itemEntity];
            foreach (var interaction in interactionBuffer)
            {
                if (interaction.TargetEntity.Equals(playerEntity)) return;
            }

            interactionBuffer.Add(new EntityInteraction { TargetEntity = playerEntity });
        }
    }
}