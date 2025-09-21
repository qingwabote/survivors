using UnityEngine;
using Unity.Entities;
using Unity.Burst;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Entities with this component will be destroyed when HitsRemaining reaches 0.
    /// </summary>
    /// <remarks>
    /// Typically used for attack entities that can only hit a max number of entities. For example implementation see <see cref="SawBladeAttackSystem"/>
    /// </remarks>
    /// <seealso cref="DestroyAfterNumberHitsAuthoring"/>
    /// <seealso cref="DestroyAfterNumberHitsSystem"/>
    public struct DestroyAfterNumberHits : IComponentData
    {
        /// <summary>
        /// Number of hits remaining before entity is destroyed.
        /// </summary>
        public int HitsRemaining;
    }
    
    /// <summary>
    /// Authoring script to initialize the <see cref="DestroyAfterNumberHits"/> component.
    /// </summary>
    /// <seealso cref="DestroyAfterNumberHits"/>
    /// <seealso cref="DestroyEntityFlag"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class DestroyAfterNumberHitsAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Maximum number of hits this entity can have before being destroyed.
        /// </summary>
        public int MaxHitCount;
        
        private class Baker : Baker<DestroyAfterNumberHitsAuthoring>
        {
            public override void Bake(DestroyAfterNumberHitsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new DestroyAfterNumberHits
                {
                    HitsRemaining = authoring.MaxHitCount
                });
            }
        }
    }
    
    /// <summary>
    /// System that determines when the entity has hit the max number of entities. Each hit will decrement <see cref="DestroyAfterNumberHits.HitsRemaining"/> until it reaches zero. Once the counter reaches zero, the entity's <see cref="DestroyEntityFlag"/> will be enabled.
    /// </summary>
    /// <remarks>
    /// This system runs at the beginning of the interaction system because if this entity hits multiple entities in a single frame, that puts the hit count OVER the max hit count, additional hits will be ignored by future interactions. i.e. an attack entity won't deal damage to more entities than the max hit count.
    /// </remarks>
    /// <seealso cref="DestroyAfterNumberHits"/>
    /// <seealso cref="DestroyAfterNumberHitsAuthoring"/>
    /// <seealso cref="DestroyEntityFlag"/>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup), OrderFirst = true)]
    public partial struct DestroyAfterNumberHitsSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (interactionBuffer, destroyAfterNumberHits, destroyEntityFlag) in SystemAPI.Query<DynamicBuffer<EntityInteraction>, RefRW<DestroyAfterNumberHits>, EnabledRefRW<DestroyEntityFlag>>().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                for (var i = 0; i < interactionBuffer.Length; i++)
                {
                    var interaction = interactionBuffer[i];
                    if (interaction.IsHandled) continue;
                    if (!SystemAPI.HasComponent<EnemyTag>(interaction.TargetEntity)) continue;
                    destroyAfterNumberHits.ValueRW.HitsRemaining -= 1;
                    if (destroyAfterNumberHits.ValueRO.HitsRemaining > 0) continue;
                    
                    for (var j = interactionBuffer.Length - 1; j > i; j--)
                    {
                        interactionBuffer.RemoveAt(j);
                    }

                    destroyEntityFlag.ValueRW = true;
                    break;
                }
            }
        }
    }
}