using Unity.Entities;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component defining the time in seconds this temporary stat modifier entity should remain active.
    /// </summary>
    /// <remarks>
    /// In the <see cref="HandleTemporaryStatModifierInteractionSystem"/> a new stat modifier entity is created with a <see cref="DestroyAfterTime"/> component.
    /// </remarks>
    public struct TemporaryStatModifierActiveTime : IComponentData
    {
        public float Value;
    }
    
    /// <summary>
    /// Authoring script to initialize data components on temporary stat modifier entities.
    /// </summary>
    /// <seealso cref="TemporaryStatModifierActiveTime"/>
    /// <seealso cref="HandleTemporaryStatModifierInteractionSystem"/>
    /// <seealso cref="StatModifier"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    public class TemporaryStatModifierAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Array of stat modifications to be applied when this stat modifier is active.
        /// </summary>
        public StatModifierInfo[] StatModifiers;
        /// <summary>
        /// Time in seconds this stat modifier should be activel
        /// </summary>
        public float TimeActive;
        
        private class Baker : Baker<TemporaryStatModifierAuthoring>
        {
            public override void Bake(TemporaryStatModifierAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new TemporaryStatModifierActiveTime { Value = authoring.TimeActive });
                
                var statModifiers = AddBuffer<StatModifier>(entity);
                foreach (var statModifier in authoring.StatModifiers)
                {
                    statModifiers.Add(new StatModifier
                    {
                        Type = statModifier.Type,
                        Value = statModifier.Value
                    });
                }
            }
        }
    }

    /// <summary>
    /// System to create temporary stat modifier entities that will last for a certain period of time as defined by <see cref="TemporaryStatModifierActiveTime.Value"/>. Created as an interaction system as the <see cref="TemporaryStatModifierActiveTime"/> is applied to items that are picked up by the player.
    /// </summary>
    /// <remarks>
    /// This system creates a new stat modifier entity with a <see cref="DestroyAfterTime"/> component.
    /// </remarks>
    /// <seeaslo cref="EntityInteraction"/>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup))]
    public partial struct HandleTemporaryStatModifierInteractionSystem : ISystem
    {
        /// <summary>
        /// Archetype for the stat modifier entity that is created.
        /// </summary>
        private EntityArchetype _statModifierArchetype;

        public void OnCreate(ref SystemState state)
        {
            _statModifierArchetype = state.EntityManager.CreateArchetype(ComponentType.ReadWrite<StatModifierEntityTag>(), ComponentType.ReadWrite<StatModifier>(), ComponentType.ReadWrite<DestroyEntityFlag>(), ComponentType.ReadWrite<DestroyAfterTime>(), ComponentType.ReadWrite<CharacterEntity>());
        }
        
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (statModifiers, interactionBuffer, activeTime) in SystemAPI.Query<DynamicBuffer<StatModifier>, DynamicBuffer<EntityInteraction>, TemporaryStatModifierActiveTime>())
            {
                foreach (var interaction in interactionBuffer)
                {
                    if (interaction.IsHandled) continue;
                    if (!SystemAPI.HasBuffer<ActiveStatModifierEntity>(interaction.TargetEntity)) continue;

                    var newStatModifierEntity = state.EntityManager.CreateEntity(_statModifierArchetype);
                    SystemAPI.SetComponentEnabled<DestroyEntityFlag>(newStatModifierEntity, false);
                    SystemAPI.SetComponent(newStatModifierEntity, new DestroyAfterTime { Value = activeTime.Value });
                    SystemAPI.SetComponent(newStatModifierEntity, new CharacterEntity { Value = interaction.TargetEntity });
                    var newStatModifierBuffer = SystemAPI.GetBuffer<StatModifier>(newStatModifierEntity);
                    newStatModifierBuffer.CopyFrom(statModifiers);

                    var activeStatModifierEntities = SystemAPI.GetBuffer<ActiveStatModifierEntity>(interaction.TargetEntity);
                    activeStatModifierEntities.Add(new ActiveStatModifierEntity { Value = newStatModifierEntity });
                    SystemAPI.SetComponentEnabled<RecalculateStatsFlag>(interaction.TargetEntity, true);
                }
            }
        }
    }
}