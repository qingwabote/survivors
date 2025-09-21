using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Entities with this component wll be instantly destroyed when an interaction is raised.
    /// </summary>
    /// <seealso cref="InstantDestroyOnInteractionSystem"/>
    /// <seealso cref="InstantDestroyOnInteractionAuthoring"/>
    /// <seealso cref="EntityInteraction"/>
    public struct InstantDestroyOnInteractionTag : IComponentData {}
    
    /// <summary>
    /// Authoring script to add <see cref="InstantDestroyOnInteractionTag"/> component to entity.
    /// </summary>
    /// <seealso cref="InstantDestroyOnInteractionSystem"/>
    /// <seealso cref="InstantDestroyOnInteractionTag"/>
    /// <seealso cref="EntityInteraction"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    public class InstantDestroyOnInteractionAuthoring : MonoBehaviour
    {
        private class Baker : Baker<InstantDestroyOnInteractionAuthoring>
        {
            public override void Bake(InstantDestroyOnInteractionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<InstantDestroyOnInteractionTag>(entity);
            }
        }
    }

    /// <summary>
    /// System to add the <see cref="InstantDestroyTag"/> to an entity tagged with the <see cref="InstantDestroyOnInteractionTag"/> when an <see cref="EntityInteraction"/> is raised.
    /// </summary>
    /// <seealso cref="InstantDestroyOnInteractionTag"/>
    /// <seealso cref="EntityInteraction"/>
    /// <seealso cref="InstantDestroyTag"/>
    /// <seealso cref="InstantDestroyEntitySystem"/>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup))]
    public partial struct InstantDestroyOnInteractionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var interactionBuffer in SystemAPI.Query<DynamicBuffer<EntityInteraction>>().WithAll<InstantDestroyOnInteractionTag>())
            {
                foreach (var interaction in interactionBuffer)
                {
                    if (interaction.IsHandled) continue;
                    ecb.AddComponent<InstantDestroyTag>(interaction.TargetEntity);
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}