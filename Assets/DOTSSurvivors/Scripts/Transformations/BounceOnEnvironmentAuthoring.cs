using UnityEngine;
using Unity.Entities;
using Unity.Physics;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Collider = Unity.Physics.Collider;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Tag component to signify this entity should bounce off environment entities with a collider.
    /// </summary>
    public struct BounceOnEnvironmentTag : IComponentData {}
    
    /// <summary>
    /// Authoring script to add the <see cref="BounceOnEnvironmentTag"/> to an entity.
    /// </summary>
    /// <remarks>
    /// Requires the <see cref="EntityInteractionAuthoring"/> component as the bounce behavior is implemented as an entity interaction.
    /// </remarks>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    public class BounceOnEnvironmentAuthoring : MonoBehaviour
    {
        private class Baker : Baker<BounceOnEnvironmentAuthoring>
        {
            public override void Bake(BounceOnEnvironmentAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<BounceOnEnvironmentTag>(entity);
            }
        }
    }
    
    /// <summary>
    /// System to schedule the <see cref="BounceOnEnvironmentJob"/> to detect trigger events between entities tagged with <see cref="BounceOnEnvironmentTag"/> and <see cref="EnvironmentTag"/> and will raise an <see cref="EntityInteraction"/>.
    /// </summary>
    /// <remarks>
    /// As the scheduled job is a trigger events job, this system updates in the <see cref="DS_PhysicsSystemGroup"/> to ensure trigger events for the current physics step have been raised.
    /// </remarks>
    [UpdateInGroup(typeof(DS_PhysicsSystemGroup))]
    public partial struct BounceOnEnvironmentSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
            
            state.Dependency = new BounceOnEnvironmentJob
            {
                BounceOnEnvironmentLookup = SystemAPI.GetComponentLookup<BounceOnEnvironmentTag>(true),
                EnvironmentLookup = SystemAPI.GetComponentLookup<EnvironmentTag>(true),
                InteractionBufferLookup = SystemAPI.GetBufferLookup<EntityInteraction>()
            }.Schedule(simulationSingleton, state.Dependency);
        }
    }

    /// <summary>
    /// Trigger events job to detect trigger events between entities tagged with <see cref="BounceOnEnvironmentTag"/> and <see cref="EnvironmentTag"/> and will raise an <see cref="EntityInteraction"/> on the entity tagged with <see cref="BounceOnEnvironmentTag"/>.
    /// Scheduled by the <see cref="BounceOnEnvironmentJob"/>.
    /// </summary>
    /// <remarks>
    /// This trigger job is unique in that when raising <see cref="EntityInteraction"/>s it will not scan the buffer for the existence of the environment entity. This is because the desired behavior is such that if a bounceable entity collided with the same environment entity multiple times, it should bounce off it every time.
    /// </remarks>
    [BurstCompile]
    public struct BounceOnEnvironmentJob: ITriggerEventsJob
    {
        /// <summary>
        /// Component lookup used for identifying entities that will bounce off environment entities.
        /// </summary>
        [ReadOnly] public ComponentLookup<BounceOnEnvironmentTag> BounceOnEnvironmentLookup;
        /// <summary>
        /// Component lookup used for identifying environment entities.
        /// </summary>
        [ReadOnly] public ComponentLookup<EnvironmentTag> EnvironmentLookup;
        /// <summary>
        /// Buffer lookup used for raising entity interactions on the bounceable entity.
        /// </summary>
        public BufferLookup<EntityInteraction> InteractionBufferLookup;
        
        [BurstCompile]
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity environmentEntity;
            Entity entityToBounce;

            if (EnvironmentLookup.HasComponent(triggerEvent.EntityA) && BounceOnEnvironmentLookup.HasComponent(triggerEvent.EntityB))
            {
                environmentEntity = triggerEvent.EntityA;
                entityToBounce = triggerEvent.EntityB;
            }
            else if (EnvironmentLookup.HasComponent(triggerEvent.EntityB) && BounceOnEnvironmentLookup.HasComponent(triggerEvent.EntityA))
            {
                environmentEntity = triggerEvent.EntityB;
                entityToBounce = triggerEvent.EntityA;
            }
            else
            {
                return;
            }

            var environmentEntityInteractionBuffer = InteractionBufferLookup[entityToBounce];
            environmentEntityInteractionBuffer.Add(new EntityInteraction
            {
                TargetEntity = environmentEntity,
                IsHandled = false
            });
        }
    }

    /// <summary>
    /// System to handle <see cref="EntityInteraction"/>s between environment entities (<see cref="EnvironmentTag"/>) and entities tagged with <see cref="BounceOnEnvironmentTag"/>)
    /// </summary>
    /// <remarks>
    /// System updates in the <see cref="DS_PhysicsSystemGroup"/> to ensure the bounce is handled at the appropriate time.
    /// This system iterates over entity interactions on the bounceable entity. Skip over handled interactions and interactions with entities that do not have the environment tag. Next the physics collider of the environment entity is used to do a collider cast at the environment entity's position to find the collision with the bounceable entity. The purpose of this is to find the surface normal of the environment entity to perform the bounce. As the bounce is initially recorded as a trigger event in <see cref="BounceOnEnvironmentJob"/>, there is no way to get information regarding the collision point or surface normal in the trigger event. Finally, using the surface normal a reflection can be applied, also the bounceable entity is moved slightly away from the environment entity so multiple bounces are not incurred.
    /// </remarks>
    [UpdateInGroup(typeof(DS_PhysicsSystemGroup))]
    [UpdateAfter(typeof(BounceOnEnvironmentSystem))]
    public partial struct HandleBounceOnEnvironmentSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }
        
        [BurstCompile]
        public unsafe void OnUpdate(ref SystemState state)
        {
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            
            foreach (var (entityInteractions, transform, entity) in SystemAPI.Query<DynamicBuffer<EntityInteraction>, RefRW<LocalTransform>>().WithAll<BounceOnEnvironmentTag>().WithEntityAccess())
            {
                foreach (var entityInteraction in entityInteractions)
                {
                    if (entityInteraction.IsHandled) continue;
                    if (!SystemAPI.HasComponent<EnvironmentTag>(entityInteraction.TargetEntity)) continue;
                    
                    var environmentCollider = SystemAPI.GetComponent<PhysicsCollider>(entityInteraction.TargetEntity);
                    var environmentTransform = SystemAPI.GetComponent<LocalTransform>(entityInteraction.TargetEntity);
                    var collisionInput = new ColliderCastInput
                    {
                        Collider = (Collider*)environmentCollider.Value.GetUnsafePtr(),
                        Start = environmentTransform.Position,
                        End = environmentTransform.Position,
                        Orientation = environmentTransform.Rotation
                    };
                    var allHits = new NativeList<ColliderCastHit>(state.WorldUpdateAllocator);
                    if (!collisionWorld.CastCollider(collisionInput, ref allHits)) continue;

                    foreach (var hit in allHits)
                    {
                        if (hit.Entity != entity) continue;
                        var reflectedDirection = math.reflect(transform.ValueRO.Forward(), hit.SurfaceNormal);
                        transform.ValueRW.Rotation = quaternion.LookRotation(reflectedDirection, math.up());
                        transform.ValueRW.Position += math.normalize(hit.SurfaceNormal) * -0.15f;
                    }
                }
            }
        }
    }
}