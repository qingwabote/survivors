using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component storing the current hit points of a damageable entity.
    /// </summary>
    /// <seealso cref="BaseHitPoints"/>
    /// <seealso cref="DamageableEntityAuthoring"/>
    public struct CurrentHitPoints : IComponentData
    {
        public int Value;
    }

    /// <summary>
    /// Data component storing the base hit points of a damageable entity.
    /// </summary>
    /// <remarks>
    /// Essentially max hit points, but max HP can go above this number with modifications
    /// </remarks>
    /// <seealso cref="CurrentHitPoints"/>
    /// <seealso cref="DamageableEntityAuthoring"/>
    public struct BaseHitPoints : IComponentData
    {
        public int Value;
    }

    /// <summary>
    /// Dynamic buffer to store damage accumulated in the current frame.
    /// </summary>
    /// <remarks>
    /// There are two primary purposes to this approach to handling damage:
    /// 1. Damage can be accumulated throughout the duration of the frame from various systems and even parallel jobs without the possibility of overwriting other changes to damage in the current frame.
    /// 2. Applying damage to a character takes place in a single system (<see cref="ProcessDamageThisFrameSystem"/>) which provides a single point for handling all cases related to taking damage, factoring modifiers, etc. rather than having this logic spread out or in multiple systems.
    /// </remarks>
    [InternalBufferCapacity(1)]
    public struct DamageThisFrame : IBufferElementData
    {
        public int Value;
    }

    /// <summary>
    /// Authoring script to add components required for entities taking damage.
    /// </summary>
    /// <remarks>
    /// <see cref="DestructibleEntityAuthoring"/> is a required component as damageable entities should also be destructible.
    /// </remarks>
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class DamageableEntityAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Starting number of hit points.
        /// </summary>
        /// <remarks>
        /// Sets both <see cref="CurrentHitPoints"/> and <see cref="BaseHitPoints"/>.
        /// </remarks>
        public int HitPoints;
        
        private class Baker : Baker<DamageableEntityAuthoring>
        {
            public override void Bake(DamageableEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new CurrentHitPoints { Value = authoring.HitPoints });
                AddComponent(entity, new BaseHitPoints { Value = authoring.HitPoints });
                AddBuffer<DamageThisFrame>(entity);
            }
        }
    }

    /// <summary>
    /// System to process damage accumulated in the <see cref="DamageThisFrame"/> dynamic buffer and apply that damage towards the entity's current hit points.
    /// </summary>
    /// <remarks>
    /// This system accumulates total damage to be applied to the entity. Entities can receive negative damage hit points to heal. Value can be modified if an entity has any stat modifications (i.e. <see cref="CharacterStatModificationState"/>. Current hit points will be clamped between 0 and max hit points (<see cref="BaseHitPoints"/> plus any modifiers to health).
    /// If the entity is out of hit points, this system will enable the character's <see cref="DestroyEntityFlag"/> to clean up the entity at the end of the frame.
    /// This system can also trigger VFX and SFX related to taking damage or healing. This system uses enableable components to trigger VFX and SFX so that this system can be fully burst compiled - it is important in this case as this system will be executing across all damageable entities (i.e. player, enemies, and damageable items in world).
    /// </remarks>
    [UpdateAfter(typeof(DS_InteractionSystemGroup))]
    public partial struct ProcessDamageThisFrameSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        { 
            foreach (var (damageThisFrame, hitPoints, baseHitPoints, entity) in SystemAPI.Query<DynamicBuffer<DamageThisFrame>, RefRW<CurrentHitPoints>, BaseHitPoints>().WithEntityAccess())
            {
                if(damageThisFrame.IsEmpty) continue;
                
                var totalHitPoints = 0;
                foreach (var damage in damageThisFrame)
                {
                    var curDamage = damage.Value;

                    if (curDamage > 0)
                    {
                        // Ignore positive damage if character is invincible
                        if (SystemAPI.HasComponent<InvincibilityExpirationTimestamp>(entity) && SystemAPI.IsComponentEnabled<InvincibilityExpirationTimestamp>(entity))
                        {
                            continue;
                        }

                        // Armor will only reduce positive damage, but will not regenerate hit points
                        if (SystemAPI.HasComponent<CharacterStatModificationState>(entity))
                        {
                            var damageReceivedModifier = SystemAPI.GetComponent<CharacterStatModificationState>(entity).DamageReceived;
                            curDamage = math.max(0, curDamage - damageReceivedModifier);
                        }

                        if (SystemAPI.HasComponent<ShowDamageNumberOnDamage>(entity))
                        {
                            SystemAPI.SetComponentEnabled<ShowDamageNumberOnDamage>(entity, true);
                            var damageNumber = SystemAPI.GetComponentRW<ShowDamageNumberOnDamage>(entity);
                            damageNumber.ValueRW.DamageThisFrame = curDamage;
                        }
                    }

                    totalHitPoints += curDamage;
                }

                var maxHitPoints = baseHitPoints.Value;
                if (SystemAPI.HasComponent<CharacterStatModificationState>(entity))
                {
                    maxHitPoints += SystemAPI.GetComponent<CharacterStatModificationState>(entity).AdditionalHitPoints;
                }
                
                damageThisFrame.Clear();
                hitPoints.ValueRW.Value -= totalHitPoints;
                hitPoints.ValueRW.Value = math.clamp(hitPoints.ValueRO.Value, 0, maxHitPoints);

                if (totalHitPoints > 0)
                {
                    if(SystemAPI.HasComponent<GraphicsEntity>(entity))
                    {
                        var graphicsEntity = SystemAPI.GetComponent<GraphicsEntity>(entity).Value;
                        SystemAPI.SetComponentEnabled<FlashColorOnDamageData>(graphicsEntity, true);
                        var flashColorOnDamageTimer = SystemAPI.GetComponentRW<FlashColorOnDamageTimer>(graphicsEntity);
                        var flashTime = SystemAPI.GetComponent<FlashColorOnDamageData>(graphicsEntity).FlashTime;
                        flashColorOnDamageTimer.ValueRW.Value = flashTime;
                    }

                    if (SystemAPI.HasComponent<PlayAudioClipOnDamageData>(entity))
                    {
                        SystemAPI.SetComponentEnabled<PlayAudioClipOnDamageData>(entity, true);
                    }

                    if (SystemAPI.HasComponent<PlayParticleSystemOnDamage>(entity))
                    {
                        SystemAPI.SetComponentEnabled<PlayParticleSystemOnDamageFlag>(entity, true);
                    }
                }
                
                if (SystemAPI.HasComponent<PlayerTag>(entity))
                {
                    SystemAPI.SetComponentEnabled<UpdatePlayerHealthUIFlag>(entity, true);
                }
                
                if (hitPoints.ValueRO.Value <= 0)
                {
                    SystemAPI.SetComponentEnabled<DestroyEntityFlag>(entity, true);
                }

                if (!SystemAPI.HasComponent<CharacterHealthRegenerationState>(entity)) continue;
                var enableRegeneration = SystemAPI.GetComponent<CharacterStatModificationState>(entity).HealthRegeneration > 0f && hitPoints.ValueRO.Value < maxHitPoints;
                SystemAPI.SetComponentEnabled<CharacterHealthRegenerationState>(entity, enableRegeneration);
            }
        }
    }
}