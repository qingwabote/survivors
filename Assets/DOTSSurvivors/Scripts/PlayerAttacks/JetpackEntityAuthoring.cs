using Unity.Entities;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Authoring script to add components necessary for jetpack in-world attack entity to function.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="EntityInteractionAuthoring"/> and <see cref="DestructibleEntityAuthoring"/> to ensure additional components are added to perform desired behaviors.
    /// </remarks>
    /// <seealso cref="DealHitPointsOnInteraction"/>
    /// <seealso cref="DestroyAfterTime"/>
    /// <seealso cref="LinearMovementSpeed"/>
    /// <seealso cref="ScreenEdgeBounceTag"/>
    /// <seealso cref="DestroyAfterNumberHits"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class JetpackEntityAuthoring : MonoBehaviour
    {
        private class Baker : Baker<JetpackEntityAuthoring>
        {
            public override void Bake(JetpackEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DealHitPointsOnInteraction>(entity);
                AddComponent<DestroyAfterTime>(entity);
                AddComponent<LinearMovementSpeed>(entity);
                AddComponent<ScreenEdgeBounceTag>(entity);
                AddComponent<DestroyAfterNumberHits>(entity);
            }
        }
    }
}