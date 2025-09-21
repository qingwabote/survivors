using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component to store data related to knockback force to give.
    /// </summary>
    /// <seealso cref="KnockbackState"/>
    /// <seealso cref="KnockbackTakenMultiplier"/>
    /// <seealso cref="KnockbackSystem"/>
    /// <seealso cref="KnockbackOnInteractionSystem"/>
    public struct KnockbackOnInteractionData : IComponentData
    {
        /// <summary>
        /// Strength of the knockback.
        /// </summary>
        public float Strength;
    }
    
    /// <summary>
    /// Authoring script to add <see cref="KnockbackOnInteractionData"/> to an entity.
    /// </summary>
    /// <remarks>
    /// Add this to an entity that will give knockback force to an entity with <see cref="KnockbackState"/> and <see cref="KnockbackTakenMultiplier"/>.
    /// </remarks>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    public class KnockbackOnInteractionAuthoring : MonoBehaviour
    {
        public float Strength;

        private class Baker : Baker<KnockbackOnInteractionAuthoring>
        {
            public override void Bake(KnockbackOnInteractionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new KnockbackOnInteractionData
                {
                    Strength = authoring.Strength
                });
            }
        }
    }

    /// <summary>
    /// System to apply knockback force to an entity.
    /// </summary>
    /// <seealso cref="KnockbackState"/>
    /// <seealso cref="KnockbackTakenMultiplier"/>
    /// <seealso cref="KnockbackSystem"/>
    /// <seealso cref="KnockbackOnInteractionData"/>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup))]
    public partial struct KnockbackOnInteractionSystem : ISystem
    {
        /// <summary>
        /// Duration of the knockback effect in seconds.
        /// </summary>
        private const float KNOCKBACK_TIME = 0.15f;
        
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (interactionBuffer, knockback, transform) in SystemAPI.Query<DynamicBuffer<EntityInteraction>, KnockbackOnInteractionData, LocalTransform>())
            {
                foreach (var interaction in interactionBuffer)
                {
                    if (interaction.IsHandled) continue;
                    if (!SystemAPI.HasComponent<KnockbackState>(interaction.TargetEntity)) continue;
                    if (!SystemAPI.HasComponent<LocalTransform>(interaction.TargetEntity)) continue;
                    if (!SystemAPI.HasComponent<PhysicsVelocity>(interaction.TargetEntity)) continue;
                    var targetEntityPosition = SystemAPI.GetComponent<LocalTransform>(interaction.TargetEntity).Position;
                    var knockbackDirection = math.normalize(targetEntityPosition - transform.Position).xz;
                    SystemAPI.SetComponent(interaction.TargetEntity, new KnockbackState
                    {
                        Direction = knockbackDirection,
                        EndTimestamp = (float)SystemAPI.Time.ElapsedTime + KNOCKBACK_TIME,
                        Strength = knockback.Strength
                    });
                    SystemAPI.SetComponentEnabled<KnockbackState>(interaction.TargetEntity, true);
                }
            }
        }
    }
}