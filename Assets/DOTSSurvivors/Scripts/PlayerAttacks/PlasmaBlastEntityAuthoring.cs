using UnityEngine;
using Unity.Entities;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Authoring script to add components necessary for plasma blast in-world attack entity to function.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="EntityInteractionAuthoring"/> and <see cref="DestructibleEntityAuthoring"/> to ensure additional components are added to perform desired behaviors.
    /// </remarks>
    /// <seealso cref="DealHitPointsOnInteraction"/>
    /// <seealso cref="DestroyAfterTime"/>
    /// <seealso cref="LinearMovementSpeed"/>
    /// <seealso cref="DestroyAfterNumberHits"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class PlasmaBlastEntityAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PlasmaBlastEntityAuthoring>
        {
            public override void Bake(PlasmaBlastEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DealHitPointsOnInteraction>(entity);
                AddComponent<DestroyAfterTime>(entity);
                AddComponent<LinearMovementSpeed>(entity);
                AddComponent<DestroyAfterNumberHits>(entity);
            }
        }
    }
}