using UnityEngine;
using Unity.Entities;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Authoring script to add components necessary for satellite in-world attack entity to function.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="EntityInteractionAuthoring"/>, <see cref="DestructibleEntityAuthoring"/>, and <see cref="DestroyOffCameraAuthoring"/> to ensure additional components are added to perform desired behaviors.
    /// </remarks>
    /// <seealso cref="DealHitPointsOnInteraction"/>
    /// <seealso cref="ParabolicMovementState"/>
    /// <seealso cref="DestroyAfterNumberHits"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    [RequireComponent(typeof(DestroyOffCameraAuthoring))]
    public class SatelliteEntityAuthoring : MonoBehaviour
    {
        private class Baker : Baker<SatelliteEntityAuthoring>
        {
            public override void Bake(SatelliteEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DealHitPointsOnInteraction>(entity);
                AddComponent<ParabolicMovementState>(entity);
                AddComponent<DestroyAfterNumberHits>(entity);
            }
        }
    }
}