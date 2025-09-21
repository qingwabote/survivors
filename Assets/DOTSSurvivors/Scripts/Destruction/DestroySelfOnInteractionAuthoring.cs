using Unity.Burst;
using UnityEngine;
using Unity.Entities;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Entities with this component will self-destruct upon raising an interaction.
    /// </summary>
    /// <remarks>
    /// This is typically used for attractable items that need to get destroyed when they come in contact with the player.
    /// </remarks>
    /// <seealso cref="DestroySelfOnInteractionSystem"/>
    /// <seealso cref="DestroySelfOnInteractionAuthoring"/>
    public struct DestroySelfOnInteractionTag : IComponentData {}
    
    /// <summary>
    /// Authoring script to add the <see cref="DestroySelfOnInteractionTag"/> component to an entity.
    /// </summary>
    /// <seealso cref="DestroySelfOnInteractionTag"/>
    /// <seealso cref="DestroySelfOnInteractionSystem"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class DestroySelfOnInteractionAuthoring : MonoBehaviour
    {
        private class Baker : Baker<DestroySelfOnInteractionAuthoring>
        {
            public override void Bake(DestroySelfOnInteractionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<DestroySelfOnInteractionTag>(entity);
            }
        }
    }

    /// <summary>
    /// System that enables the <see cref="DestroyEntityFlag"/> on an entity tagged with the <see cref="DestroySelfOnInteractionTag"/> when an interaction is raised.
    /// </summary>
    /// <seealso cref="DestroySelfOnInteractionTag"/>
    /// <seealso cref="DestroySelfOnInteractionAuthoring"/>
    /// <seealso cref="EntityInteraction"/>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup))]
    public partial struct DestroySelfOnInteractionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (interactions, entity) in SystemAPI.Query<DynamicBuffer<EntityInteraction>>().WithAll<DestroySelfOnInteractionTag>().WithEntityAccess())
            {
                if (interactions.IsEmpty) continue;
                SystemAPI.SetComponentEnabled<DestroyEntityFlag>(entity, true);
            }
        }
    }
}