using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component to hold current state to knockback.
    /// </summary>
    /// <remarks>
    /// Enableable component - when enabled, the entity is currently being moved by knockback.
    /// </remarks>
    public struct KnockbackState : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// Strength of the knockback.
        /// </summary>
        public float Strength;
        /// <summary>
        /// Timestamp at which the knockback will expire.
        /// </summary>
        public float EndTimestamp;
        /// <summary>
        /// Direction of the knockback.
        /// </summary>
        public float2 Direction;
    }

    /// <summary>
    /// Multiplier to change the strength of knockback received.
    /// </summary>
    public struct KnockbackTakenMultiplier : IComponentData
    {
        public float Value;
    }
    
    /// <summary>
    /// Authoring script to add components required for knockback
    /// </summary>
    /// <remarks>
    /// Add this to an entity that will receive knockback from an entity with <see cref="KnockbackOnInteractionData"/>.
    /// </remarks>
    /// <seealso cref="KnockbackState"/>
    /// <seealso cref="KnockbackTakenMultiplier"/>
    /// <seealso cref="KnockbackSystem"/>
    /// <seealso cref="KnockbackOnInteractionSystem"/>
    public class KnockbackAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Multiplier to change the strength of knockback received.
        /// </summary>
        public float KnockbackTakenMultiplier = 1f;

        private class Baker : Baker<KnockbackAuthoring>
        {
            public override void Bake(KnockbackAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new KnockbackTakenMultiplier
                {
                    Value = authoring.KnockbackTakenMultiplier
                });
                AddComponent<KnockbackState>(entity);
                SetComponentEnabled<KnockbackState>(entity, false);
            }
        }
    }

    /// <summary>
    /// System to apply knockback force to an entity.
    /// </summary>
    /// <seealso cref="KnockbackState"/>
    /// <seealso cref="KnockbackTakenMultiplier"/>
    /// <seealso cref="KnockbackOnInteractionSystem"/>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct KnockbackSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var elapsedTime = SystemAPI.Time.ElapsedTime;
            foreach (var (physicsVelocity, knockbackState, knockbackTakenMultiplier, shouldKnockback) in SystemAPI.Query<RefRW<PhysicsVelocity>, KnockbackState, KnockbackTakenMultiplier, EnabledRefRW<KnockbackState>>())
            {
                if (elapsedTime >= knockbackState.EndTimestamp)
                {
                    shouldKnockback.ValueRW = false;
                    continue;
                }

                physicsVelocity.ValueRW.Linear.xz = knockbackState.Direction * knockbackState.Strength * knockbackTakenMultiplier.Value;
            }
        }
    }
}