using Unity.Entities;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Authoring script to add components necessary for oxygen hose in-world attack entity to function.
    /// </summary>
    /// <remarks>
    /// Radioactive waste spill entity is spawned by the radioactive waste drop entity via its <see cref="SpawnOnDestroy"/> component. See <see cref="RadioactiveWasteDropEntityAuthoring"/> for more information.
    /// Requires <see cref="EntityInteractionAuthoring"/> and <see cref="DestructibleEntityAuthoring"/> to ensure additional components are added to perform desired behaviors.
    /// </remarks>
    /// <seealso cref="DealHitPointsOnInteraction"/>
    /// <seealso cref="DestroyAfterTime"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class RadioactiveWasteSpillEntityAuthoring : MonoBehaviour
    {
        private class Baker : Baker<RadioactiveWasteSpillEntityAuthoring>
        {
            public override void Bake(RadioactiveWasteSpillEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DealHitPointsOnInteraction>(entity);
                AddComponent<DestroyAfterTime>(entity);
            }
        }
    }
}