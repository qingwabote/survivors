using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// System to schedule the <see cref="DetectCapabilityTriggerJob"/>.
    /// </summary>
    /// <remarks>
    /// Only execute this system if the player entity exists. This avoids damaging enemies in game over state to potentially inaccurately account for number of enemies defeated during the game.
    /// </remarks>
    [UpdateInGroup(typeof(DS_PhysicsSystemGroup))]
    public partial struct DetectCapabilityTriggerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<SimulationSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
            state.Dependency = new DetectCapabilityTriggerJob
            {
                DamageableEntityLookup = SystemAPI.GetComponentLookup<CurrentHitPoints>(true),
                InteractionBufferLookup = SystemAPI.GetBufferLookup<EntityInteraction>()
            }.Schedule(simulationSingleton, state.Dependency);
        }
    }

    /// <summary>
    /// Job to detect trigger events between capabilities and damageable entities.
    /// </summary>
    /// <remarks>
    /// This is typically used to detect collisions between player attack entities and enemy entities. If true, the damageable entity will be added to the <see cref="EntityInteraction"/> buffer on the entity that will apply damage.
    /// </remarks>
    [BurstCompile]
    public struct DetectCapabilityTriggerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<CurrentHitPoints> DamageableEntityLookup;
        public BufferLookup<EntityInteraction> InteractionBufferLookup;

        [BurstCompile]
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity damageableEntity;
            Entity capabilityEntity;

            if (InteractionBufferLookup.HasBuffer(triggerEvent.EntityA) && DamageableEntityLookup.HasComponent(triggerEvent.EntityB))
            {
                capabilityEntity = triggerEvent.EntityA;
                damageableEntity = triggerEvent.EntityB;
            }
            else if (InteractionBufferLookup.HasBuffer(triggerEvent.EntityB) && DamageableEntityLookup.HasComponent(triggerEvent.EntityA))
            {
                capabilityEntity = triggerEvent.EntityB;
                damageableEntity = triggerEvent.EntityA;
            }
            else
            {
                return;
            }

            var interactionBuffer = InteractionBufferLookup[capabilityEntity];
            foreach (var interaction in interactionBuffer)
            {
                if (interaction.TargetEntity.Equals(damageableEntity)) return;
            }

            interactionBuffer.Add(new EntityInteraction { IsHandled = false, TargetEntity = damageableEntity });
        }
    }
}