using Unity.Entities;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Authoring script to add components necessary for radioactive waste drop in-world attack entity to function.
    /// </summary>
    /// <remarks>
    /// The radioactive waste drop entity is the initial entity that spawns from the top of the screen and "falls" to a determined position in the game world. This attack does not deal any damage, however once it reaches its target position, it will self-destruct and spawn a radioactive waste spill entity (see <see cref="RadioactiveWasteSpillEntityAuthoring"/>).
    /// Requires <see cref="EntityInteractionAuthoring"/> and <see cref="DestructibleEntityAuthoring"/> to ensure additional components are added to perform desired behaviors.
    /// </remarks>
    /// <seealso cref="DestroyAtPosition"/>
    /// <seealso cref="LinearMovementSpeed"/>
    /// <seealso cref="SpawnOnDestroy"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class RadioactiveWasteDropEntityAuthoring : MonoBehaviour
    {
        public GameObject RadioactiveWasteSpillPrefab;
        
        private class Baker : Baker<RadioactiveWasteDropEntityAuthoring>
        {
            public override void Bake(RadioactiveWasteDropEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DestroyAtPosition>(entity);
                AddComponent<LinearMovementSpeed>(entity);
                AddComponent(entity, new SpawnOnDestroy
                {
                    Value = GetEntity(authoring.RadioactiveWasteSpillPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}