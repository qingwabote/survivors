using Unity.Entities;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// An entity with this component is capable of being destroyed by various systems throughout the game. This component should be disabled by default and once enabled it will be destroyed at the end of the frame in the <see cref="DestroyEntitySystem"/>
    /// </summary>
    /// <seealso cref="DestroyEntitySystem"/>
    /// <seealso cref="DestructibleEntityAuthoring"/>
    public struct DestroyEntityFlag : IComponentData, IEnableableComponent {}
    
    /// <summary>
    /// Authoring component to add <see cref="DestroyEntityFlag"/> to an entity. This component will be disabled by default to ensure the entity isn't immediately destroyed after instantiation.
    /// </summary>
    /// <seealso cref="DestroyEntitySystem"/>
    /// <seealso cref="DestroyEntityFlag"/>
    public class DestructibleEntityAuthoring : MonoBehaviour
    {
        private class Baker : Baker<DestructibleEntityAuthoring>
        {
            public override void Bake(DestructibleEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity, false);
            }
        }
    }
}