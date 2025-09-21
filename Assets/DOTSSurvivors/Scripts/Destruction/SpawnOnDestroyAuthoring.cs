using Unity.Entities;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Component to store the entity prefab that will be instantiated when the entity with this component is destroyed.
    /// </summary>
    /// <seealso cref="DestroyEntitySystem"/>
    /// <seealso cref="SpawnOnDestroyAuthoring"/>
    public struct SpawnOnDestroy : IComponentData
    {
        public Entity Value;
    }
    
    /// <summary>
    /// Authoring script to initialize the entity prefab stored in the <see cref="SpawnOnDestroy"/> component.
    /// </summary>
    /// <seealso cref="DestroyEntitySystem"/>
    /// <seealso cref="SpawnOnDestroy"/>
    public class SpawnOnDestroyAuthoring : MonoBehaviour
    {
        /// <summary>
        /// GameObject prefab to be converted into an entity in this class's baker which gets assigned to the <see cref="SpawnOnDestroy"/> component added to this entity.
        /// </summary>
        public GameObject EntityPrefabToSpawn;

        private class Baker : Baker<SpawnOnDestroyAuthoring>
        {
            public override void Bake(SpawnOnDestroyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SpawnOnDestroy
                {
                    Value = GetEntity(authoring.EntityPrefabToSpawn, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}