using UnityEngine;
using Unity.Entities;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component to store a reference to the graphics entity for this entity.
    /// </summary>
    /// <remarks>
    /// A common pattern in this game is to have the visual representation of an entity exist as a child of the primary entity. This is useful to reduce the memory footprint of the main entity which is often a part of more complex systems. This also allows for independent effects to animate via the transform of the graphics entity without messing with the transform of the main entity. Often the player entity will need to reference its graphics entity to play effects and this component provides reliable access to that entity without needing to iterate the child buffer.
    /// </remarks>
    public struct GraphicsEntity : IComponentData
    {
        public Entity Value;
    }

    /// <summary>
    /// Authoring script to add <see cref="GraphicsEntity"/> component to an entity.
    /// </summary>
    public class GraphicsEntityAuthoring : MonoBehaviour
    {
        public GameObject GraphicsEntity;
        public bool PlayDestructionAnimation;

        private class Baker : Baker<GraphicsEntityAuthoring>
        {
            public override void Bake(GraphicsEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new GraphicsEntity
                {
                    Value = GetEntity(authoring.GraphicsEntity, TransformUsageFlags.Dynamic)
                });
                if (authoring.PlayDestructionAnimation)
                {
                    AddComponent<GraphicsEntityPlayDestroyEffectTag>(entity);
                }
            }
        }
    }
}