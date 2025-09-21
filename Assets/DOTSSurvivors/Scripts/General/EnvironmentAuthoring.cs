using UnityEngine;
using Unity.Entities;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Tag component to signify this entity is an environment entity.
    /// </summary>
    /// <remarks>
    /// Environment entities are similar to decoration entities (<see cref="DecorationTag"/>) however these do have colliders which can stop movement of player and their attacks.
    /// For design decisions, enemies can still pass through environment entities. This is determined by the collision filters on both enemies and environment entities.
    /// </remarks>
    public struct EnvironmentTag : IComponentData {}
    
    /// <summary>
    /// Authoring script to apply the <see cref="EnvironmentTag"/> to an entity.
    /// </summary>
    public class EnvironmentAuthoring : MonoBehaviour
    {
        private class Baker : Baker<EnvironmentAuthoring>
        {
            public override void Bake(EnvironmentAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<EnvironmentTag>(entity);
            }
        }
    }
}