using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Authoring script to add components necessary for CO2 cloud in-world attack entity to function.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="EntityInteractionAuthoring"/> to ensure additional components are added to perform desired behaviors.
    /// </remarks>
    /// <seealso cref="DealHitPointsOnInteraction"/>
    /// <seealso cref="DestroyAfterTime"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    public class CO2CloudEntityAuthoring : MonoBehaviour
    {
        private class Baker : Baker<CO2CloudEntityAuthoring>
        {
            public override void Bake(CO2CloudEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Parent>(entity);
                AddComponent<DestroyAfterTime>(entity);
                AddComponent<DealHitPointsOnInteraction>(entity);
            }
        }
    }
}