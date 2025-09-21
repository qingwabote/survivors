using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Physics;
using Unity.Collections;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Entities with this component will be destroyed upon contact with an environment entity.
    /// </summary>
    /// <seealso cref="DestroyOnEnvironmentContactSystem"/>
    /// <seealso cref="DestroyOnEnvironmentContactAuthoring"/>
    /// <seealso cref="EnvironmentTag"/>
    public struct DestroyOnEnvironmentContactTag: IComponentData {}
    
    /// <summary>
    /// Authoring script that adds <see cref="DestroyOnEnvironmentContactTag"/> component to the entity.
    /// </summary>
    /// <seealso cref="DestroyOnEnvironmentContactSystem"/>
    /// <seealso cref="DestroyOnEnvironmentContactTag"/>
    /// <seealso cref="EnvironmentTag"/>
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class DestroyOnEnvironmentContactAuthoring : MonoBehaviour
    {
        private class Baker : Baker<DestroyOnEnvironmentContactAuthoring>
        {
            public override void Bake(DestroyOnEnvironmentContactAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DestroyOnEnvironmentContactTag>(entity);
            }
        }
    }

    /// <summary>
    /// System that schedules the <see cref="DestroyOnEnvironmentContactTriggerJob"/> to destroy an entity upon contact with an environment entity.
    /// </summary>
    /// <seealso cref="DestroyOnEnvironmentContactTag"/>
    /// <seealso cref="DestroyOnEnvironmentContactTriggerJob"/>
    /// <seealso cref="EnvironmentTag"/>
    [UpdateInGroup(typeof(DS_PhysicsSystemGroup))]
    public partial struct DestroyOnEnvironmentContactSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
            state.Dependency = new DestroyOnEnvironmentContactTriggerJob
            {
                DestroyOnEnvironmentLookup = SystemAPI.GetComponentLookup<DestroyOnEnvironmentContactTag>(true),
                EnvironmentLookup = SystemAPI.GetComponentLookup<EnvironmentTag>(true),
                DestroyEntityLookup = SystemAPI.GetComponentLookup<DestroyEntityFlag>()
            }.Schedule(simulationSingleton, state.Dependency);
        }
    }

    /// <summary>
    /// Trigger Event Job scheduled by the <see cref="DestroyOnEnvironmentContactSystem"/>. Enables the <see cref="DestroyEntityFlag"/> component on an entity tagged with the <see cref="DestroyOnEnvironmentContactTag"/> component that comes in contact with an entity with an <see cref="EnvironmentTag"/> component.
    /// </summary>
    /// <remarks>
    /// This is an ITriggerEventsJob as entities that match the query (typically player attacks) raise trigger events, not collision events.
    /// </remarks>
    /// <seealso cref="DestroyOnEnvironmentContactSystem"/>
    /// <seealso cref="DestroyOnEnvironmentContactTag"/>
    /// <seealso cref="EnvironmentTag"/>
    [BurstCompile]
    public struct DestroyOnEnvironmentContactTriggerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<DestroyOnEnvironmentContactTag> DestroyOnEnvironmentLookup;
        [ReadOnly] public ComponentLookup<EnvironmentTag> EnvironmentLookup;
        public ComponentLookup<DestroyEntityFlag> DestroyEntityLookup;
        
        [BurstCompile]
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity entityToDestroy;

            if (EnvironmentLookup.HasComponent(triggerEvent.EntityA) && DestroyOnEnvironmentLookup.HasComponent(triggerEvent.EntityB))
            {
                entityToDestroy = triggerEvent.EntityB;
            }
            else if (EnvironmentLookup.HasComponent(triggerEvent.EntityB) && DestroyOnEnvironmentLookup.HasComponent(triggerEvent.EntityA))
            {
                entityToDestroy = triggerEvent.EntityA;
            }
            else
            {
                return;
            }

            DestroyEntityLookup.SetComponentEnabled(entityToDestroy, true);
        }
    }
}