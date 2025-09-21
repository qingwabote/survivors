using UnityEngine;
using Unity.Entities;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Entities with this component have a chance to drop an experience point of a specified value when the entity is destroyed via the <see cref="DestroyEntitySystem"/>.
    /// </summary>
    /// <remarks>
    /// This could be implemented as a cleanup component to avoid having the logic in <see cref="DestroyEntitySystem"/>, however I chose to have the logic held there so that we can also have an <see cref="InstantDestroyEntitySystem"/> which does not spawn experience gems. That implementation would get messy with this as a cleanup component.
    /// -Johnny
    /// </remarks>
    /// <seealso cref="DropExperienceOnDestroyAuthoring"/>
    /// <seealso cref="DestroyEntitySystem"/>
    public struct DropExperienceOnDestroy : IComponentData
    {
        /// <summary>
        /// The number of experience points assigned to the gem that will be dropped when this entity is destroyed.
        /// </summary>
        /// <seealso cref="ExperienceGemItemData"/>
        public int ExperienceValue;
        /// <summary>
        /// Value from 0 to 100 representing the percentage chance the destroyed entity will drop an experience point gem. 0 will never drop a gem and 100 will always drop a gem.
        /// </summary>
        public int ChanceToDrop;
    }
    
    /// <summary>
    /// Authoring script to add the <see cref="DropExperienceOnDestroy"/> to an entity.
    /// </summary>
    /// <remarks>
    /// This is typically added to enemies who will drop experience point gems when they are destroyed.
    /// </remarks>
    /// <seealso cref="DropExperienceOnDestroy"/>
    /// <seealso cref="DestroyEntitySystem"/>
    /// <seealso cref="ExperienceGemItemData"/>
    public class DropExperienceOnDestroyAuthoring : MonoBehaviour
    {
        /// <summary>
        /// The number of experience points assigned to the gem that will be dropped when this entity is destroyed.
        /// </summary>
        /// <seealso cref="ExperienceGemItemData"/>
        public int ExperienceValue;
        /// <summary>
        /// Value from 0 to 100 representing the percentage chance the destroyed entity will drop an experience point gem. 0 will never drop a gem and 100 will always drop a gem.
        /// </summary>
        [Range(0, 100)] public int ChanceToDrop = 100;

        private class Baker : Baker<DropExperienceOnDestroyAuthoring>
        {
            public override void Bake(DropExperienceOnDestroyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new DropExperienceOnDestroy
                {
                    ExperienceValue = authoring.ExperienceValue,
                    ChanceToDrop = authoring.ChanceToDrop
                });
            }
        }
    }
}