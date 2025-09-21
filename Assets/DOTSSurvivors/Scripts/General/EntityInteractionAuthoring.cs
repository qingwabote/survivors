using Unity.Burst;
using UnityEngine;
using Unity.Entities;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Dynamic buffer component to store entity interactions.
    /// </summary>
    /// <remarks>
    /// Entity interactions are a key concept to DOTS Survivors as they facilitate interactions between multiple entities. Interactions can come in various forms - an attack projectile applying damage to an enemy entity, a healing item granting health to the player, instructing a projectile to bounce off another entity, and many more.
    /// Entity interactions can also be raised in several ways - most commonly this is through collision or time-based events, but logic-only interactions can also exist to facilitate interactions of data-only entities that do not exist physically in the game world. For example, when a damage dealing entity collides with a damage receiving entity, a new entry to the EntityInteraction buffer will be recorded. In this case the damage dealing entity will have the dynamic buffer and the entity receiving damage will be added as the TargetEntity for this record. Note that there is nothing stopping the damage receiving entity to have its own EntityInteraction buffer and adding the damage dealing entity as a record during this same collision event.
    /// When an interaction between two entities has been recorded, various interaction systems can trigger additional behavior based on the component makeup of the entity that holds the interaction buffer. In the example of the damage dealing entity, a system could execute to deal damage to the entity stored in the TargetEntity field. However other interaction systems could trigger behavior that don't use the target entity field - there could be a system to play a sound effect for any new entity interaction, or just outright self-destruct on any interaction raised. The inclusion in these interaction systems and their behaviors are determined by the other data components added to the entity with the EntityInteraction buffer.
    /// At the end of the of the <see cref="DS_InteractionSystemGroup"/> the IsHandled field for every element in the EntityInteraction buffer is set to true. This is so that when the interaction systems run in the next frame, they can check the buffer and only trigger behavior for new interactions that have not yet been handled. Rather than clearing the buffer, all previous interactions remain in the buffer. This is because most systems that raise interactions should scan the buffer and see if the target entity is already in the buffer so it doesn't get added multiple times. This is especially useful for trigger events where one entity may pass through another entity for many frames, but the intended behavior is to only trigger behavior on the first frame the entities overlap.
    /// Note that depending on the desired behavior of systems that raise interactions and systems that invoke behavior based on interactions, these systems can simply ignore checking the buffer for the target entity or if interactions are already handled. The vast majority of systems in this game do implement these checks, but you will find examples of ones that do not. <see cref="BounceOnEnvironmentJob"/> is an example of a trigger events job that does not scan the interaction buffer for the existence of previously added environment entities.
    /// </remarks>
    public struct EntityInteraction : IBufferElementData
    {
        /// <summary>
        /// Boolean field to denote that this interaction has already been handled.
        /// </summary>
        /// <remarks>
        /// This field is checked by systems that trigger behavior from interactions to ensure the behavior is only executed against new, unhandled interactions.
        /// </remarks>
        public bool IsHandled;
        /// <summary>
        /// Entity field to store the target entity of the interaction.
        /// </summary>
        /// <remarks>
        /// This can be used when one entity needs to interact with another in a certain way. For example if one entity should apply damage to another, the entity receiving damage will exist in this field.
        /// This field can also be used to ensure a particular entity hasn't already been added to the buffer to avoid situations where interactions are being raised more frequently than they should.
        /// </remarks>
        public Entity TargetEntity;
    }

    /// <summary>
    /// Authoring component to add the <see cref="EntityInteraction"/> buffer to an entity.
    /// </summary>
    /// <remarks>
    /// This authoring component is commonly added as a "RequireComponent" on other authoring scripts as many different types of entities have the ability to interact with other entities.
    /// </remarks>
    public class EntityInteractionAuthoring : MonoBehaviour
    {
        private class Baker : Baker<EntityInteractionAuthoring>
        {
            public override void Bake(EntityInteractionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddBuffer<EntityInteraction>(entity);
            }
        }
    }

    /// <summary>
    /// System to mark the <see cref="EntityInteraction.IsHandled"/> field to true.
    /// </summary>
    /// <remarks>
    /// This system runs at the end of the <see cref="DS_InteractionSystemGroup"/> to ensure that all systems that trigger behavior based on interactions have already executed before setting this value.
    /// See documentation on the <see cref="EntityInteraction"/> dynamic buffer component for more information on the purpose and use-case for the IsHandled field.
    /// </remarks>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup), OrderLast = true)]
    public partial struct HandleEntityInteractionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var interactionBuffer in SystemAPI.Query<DynamicBuffer<EntityInteraction>>())
            {
                for (var i = 0; i < interactionBuffer.Length; i++)
                {
                    var interaction = interactionBuffer.ElementAt(i);
                    interaction.IsHandled = true;
                    interactionBuffer.ElementAt(i) = interaction;
                }
            }
        }
    }
}