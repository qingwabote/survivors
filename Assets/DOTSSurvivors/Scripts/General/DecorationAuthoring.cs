using Unity.Entities;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Tag component to signify this entity is a decoration entity.
    /// </summary>
    /// <remarks>
    /// Decoration entities are similar to environment entities (<see cref="EnvironmentTag"/>) however these do not have colliders and do not affect gameplay.
    /// </remarks>
    public struct DecorationTag : IComponentData {}
    
    /// <summary>
    /// Authoring script to apply the <see cref="DecorationTag"/> to an entity.
    /// </summary>
    public class DecorationAuthoring : MonoBehaviour
    {
        private class Baker : Baker<DecorationAuthoring>
        {
            public override void Bake(DecorationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent<DecorationTag>(entity);
            }
        }
    }
}