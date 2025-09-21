using Unity.Entities;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Authoring script to add components necessary for oxygen hose in-world attack entity to function.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="EntityInteractionAuthoring"/> and <see cref="DestructibleEntityAuthoring"/> to ensure additional components are added to perform desired behaviors.
    /// </remarks>
    /// <seealso cref="DealHitPointsOnInteraction"/>
    /// <seealso cref="DestroyAfterTime"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class OxygenHoseEntityAuthoring : MonoBehaviour
    {
        private class Baker : Baker<OxygenHoseEntityAuthoring>
        {
            public override void Bake(OxygenHoseEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DealHitPointsOnInteraction>(entity);
                AddComponent<DestroyAfterTime>(entity);
            }
        }
    }
}