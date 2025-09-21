using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Authoring script to add components necessary for drone in-world attack entity to function.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="EntityInteractionAuthoring"/> and <see cref="DestructibleEntityAuthoring"/> to ensure additional components are added to perform desired behaviors.
    /// </remarks>
    /// <seealso cref="DealHitPointsOnInteraction"/>
    /// <seealso cref="DestroyAfterTime"/>
    /// <seealso cref="ConstantRotationData"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class DroneEntityAuthoring : MonoBehaviour
    {
        private class Baker : Baker<DroneEntityAuthoring>
        {
            public override void Bake(DroneEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DealHitPointsOnInteraction>(entity);
                AddComponent<Parent>(entity);
                AddComponent<DestroyAfterTime>(entity);
                AddComponent<ConstantRotationData>(entity);
            }
        }
    }
}