using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component to define the number of hit points to deal when the associated entity interacts with another.
    /// </summary>
    /// <remarks>
    /// Typically this component is added to an in-world attack entity and <see cref="EntityInteraction"/>s are raised by trigger events in <see cref="DetectCapabilityTriggerJob"/>.
    /// </remarks>
    public struct DealHitPointsOnInteraction : IComponentData
    {
        public int Value;
    }
    
    /// <summary>
    /// Authoring script to add <see cref="DealHitPointsOnInteraction"/> component to an entity.
    /// </summary>
    /// <remarks>
    /// Requires the <see cref="EntityInteractionAuthoring"/> component to ensure <see cref="EntityInteraction"/> is added to the entity as well.
    /// </remarks>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    public class DealHitPointsOnInteractionAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Hit points to deal when the associated entity interacts with another entity.
        /// </summary>
        public int HitPoints;

        private class Baker : Baker<DealHitPointsOnInteractionAuthoring>
        {
            public override void Bake(DealHitPointsOnInteractionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new DealHitPointsOnInteraction
                {
                    Value = authoring.HitPoints
                });
            }
        }
    }

    /// <summary>
    /// System to add damage hit points to the target entity's <see cref="DamageThisFrame"/> buffer.
    /// </summary>
    /// <remarks>
    /// Updates in the <see cref="DS_InteractionSystemGroup"/> to ensure interactions for the current frame have already been added to this entity's <see cref="EntityInteraction"/> buffer.
    /// </remarks>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup))]
    public partial struct DealHitPointsOnInteractionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (interactionBuffer, hitPointsToDeal) in SystemAPI.Query<DynamicBuffer<EntityInteraction>, DealHitPointsOnInteraction>())
            {
                foreach (var interaction in interactionBuffer)
                {
                    if (interaction.IsHandled) continue;
                    if (!SystemAPI.HasBuffer<DamageThisFrame>(interaction.TargetEntity)) continue;
                    var targetEntityDamageBuffer = SystemAPI.GetBuffer<DamageThisFrame>(interaction.TargetEntity);
                    targetEntityDamageBuffer.Add(new DamageThisFrame { Value = hitPointsToDeal.Value });
                }
            }
        }
    }
}