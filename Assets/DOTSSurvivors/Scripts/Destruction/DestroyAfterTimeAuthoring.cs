using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Entities with this component will be destroyed once this time value expires.
    /// </summary>
    /// <remarks>
    /// Time is tracked in game time, meaning the timer will pause when the game pauses.
    /// </remarks>
    /// <seealso cref="DestroyAfterTimeSystem"/>
    /// <seealso cref="DestroyAfterTimeAuthoring"/>
    public struct DestroyAfterTime : IComponentData
    {
        /// <summary>
        /// This value will represent the time remaining (in game time) until the entity is destroyed.
        /// </summary>
        public float Value;
    }

    /// <summary>
    /// Authoring script to set the initial value for the <see cref="DestroyAfterTime"/> component.
    /// </summary>
    /// <seealso cref="DestroyAfterTime"/>
    /// <seealso cref="DestroyAfterTimeSystem"/>
    /// <seealso cref="DestroyEntityFlag"/>
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class DestroyAfterTimeAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Amount of time in seconds the entity should exist in the world.
        /// </summary>
        public float DestroyAfterTime;

        private class Baker : Baker<DestroyAfterTimeAuthoring>
        {
            public override void Bake(DestroyAfterTimeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new DestroyAfterTime { Value = authoring.DestroyAfterTime });
            }
        }
    }

    /// <summary>
    /// System to destroy entities after a given time. This system ticks <see cref="DestroyAfterTime"/> Value down and will set the <see cref="DestroyEntityFlag"/> to true once the timer value has reached zero. This will count only in-game time and not be affected by game pause.
    /// </summary>
    /// <remarks>
    /// If an enemy entity is destroyed in this fashion, it should not drop an experience point gem.
    /// </remarks>
    /// <seealso cref="DestroyAfterTime"/>
    /// <seealso cref="DestroyAfterTimeAuthoring"/>
    /// <seealso cref="DestroyEntityFlag"/>
    [UpdateInGroup(typeof(DS_DestructionSystemGroup))]
    public partial struct DestroyAfterTimeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (destroyAfterTime, entity) in SystemAPI.Query<RefRW<DestroyAfterTime>>().WithEntityAccess())
            {
                destroyAfterTime.ValueRW.Value -= deltaTime;
                if (destroyAfterTime.ValueRO.Value > 0f) continue;
                SystemAPI.SetComponentEnabled<DestroyEntityFlag>(entity, true);

                // When destroying an enemy in this fashion, we don't want it to drop an experience gem
                if (!SystemAPI.HasComponent<DropExperienceOnDestroy>(entity)) continue;
                var dropExperienceOnDestroy = SystemAPI.GetComponentRW<DropExperienceOnDestroy>(entity);
                dropExperienceOnDestroy.ValueRW.ChanceToDrop = 0;
            }
        }
    }
}