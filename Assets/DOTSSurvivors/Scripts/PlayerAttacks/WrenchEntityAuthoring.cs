using UnityEngine;
using Unity.Entities;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Authoring script to add components necessary for wrench in-world attack entity to function.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="EntityInteractionAuthoring"/>, <see cref="DestructibleEntityAuthoring"/>, and <see cref="DestroyOffCameraAuthoring"/> to ensure additional components are added to perform desired behaviors.
    /// </remarks>
    /// <seealso cref="DealHitPointsOnInteraction"/>
    /// <seealso cref="BoomerangMovementData"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    [RequireComponent(typeof(DestroyOffCameraAuthoring))]
    public class WrenchEntityAuthoring : MonoBehaviour
    {
        private class Baker : Baker<WrenchEntityAuthoring>
        {
            public override void Bake(WrenchEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DealHitPointsOnInteraction>(entity);
                AddComponent<BoomerangMovementData>(entity);
            }
        }
    }
}