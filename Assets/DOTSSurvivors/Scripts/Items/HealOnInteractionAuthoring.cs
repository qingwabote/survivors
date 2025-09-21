using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Hit points to be granted if player collects a health item.
    /// </summary>
    /// <seealso cref="HealOnInteractionSystem"/>
    public struct HealOnInteraction : IComponentData
    {
        public int Value;
    }
    
    /// <summary>
    /// Authoring script to initialize value of health item.
    /// </summary>
    /// <remarks>
    /// Additional authoring scripts required to add components necessary for desired behavior.
    /// </remarks>
    [RequireComponent(typeof(ItemAuthoring))]
    [RequireComponent(typeof(DestroySelfOnInteractionAuthoring))]
    public class HealOnInteractionAuthoring : MonoBehaviour
    {
        public int HealthValue;
        
        private class Baker : Baker<HealOnInteractionAuthoring>
        {
            public override void Bake(HealOnInteractionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new HealOnInteraction { Value = authoring.HealthValue });
            }
        }
    }

    /// <summary>
    /// System to grant health to the player when a health item is collected.
    /// </summary>
    /// <seealso cref="HealOnInteraction"/>
    /// <seealso cref="EntityInteraction"/>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup))]
    public partial struct HealOnInteractionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (interactionBuffer, healthItemProperties) in SystemAPI.Query<DynamicBuffer<EntityInteraction>, HealOnInteraction>().WithAll<ItemTag>())
            {
                foreach (var interaction in interactionBuffer)
                {
                    if (interaction.IsHandled) continue;

                    // Need to multiply by -1 as ProcessDamageThisFrameSystem will then subtract negative hit points to increment value.
                    var amountToHeal = -1 * healthItemProperties.Value;
                    var targetDamageThisFrameBuffer = SystemAPI.GetBuffer<DamageThisFrame>(interaction.TargetEntity);
                    targetDamageThisFrameBuffer.Add(new DamageThisFrame { Value = amountToHeal });
                }
            }
        }
    }
}