using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Initialization data for <see cref="CompanionGameObjectData"/>.
    /// </summary>
    /// <remarks>
    /// Defines a GameObject to spawn and match the position of the associated entity.
    /// Initialization data is required as <see cref="CompanionGameObjectData"/> is an ICleanupComponentData which cannot be added to an entity through baking.
    /// </remarks>
    public struct CompanionGameObjectInitializationData : IComponentData
    {
        /// <summary>
        /// GameObject prefab that will be spawned during initialization. Instance of this prefab will match the position of the associated entity.
        /// </summary>
        public UnityObjectRef<GameObject> Prefab;

        /// <summary>
        /// Time in seconds to delay destruction of the companion GameObject.
        /// </summary>
        public float DelayCleanupTime;
    }

    /// <summary>
    /// Data related to the companion GameObject that will be matching the position of the associated entity.
    /// </summary>
    /// <remarks>
    /// This component is a cleanup component which cannot be added to the entity through baking. As such <see cref="CompanionGameObjectInitializationData"/> is required to initialize this component.
    /// </remarks>
    public struct CompanionGameObjectData : ICleanupComponentData
    {
        /// <summary>
        /// Transform reference of the instantiated GameObject that will match the position of the associated entity.
        /// </summary>
        public UnityObjectRef<Transform> Transform;

        /// <summary>
        /// Time in seconds to delay destruction of the companion GameObject.
        /// </summary>
        public float DelayCleanupTimer;
    }

    /// <summary>
    /// Authoring script to set values for <see cref="CompanionGameObjectInitializationData"/> which in turn will initialize values of <see cref="CompanionGameObjectData"/> in the <see cref="UpdateCompanionGameObjectSystem"/>.
    /// </summary>
    public class CompanionGameObjectAuthoring : MonoBehaviour
    {
        /// <summary>
        /// GameObject prefab that will be spawned during initialization. Instance of this prefab will match the position of the associated entity.
        /// </summary>
        public GameObject CompanionPrefab;

        /// <summary>
        /// Time in seconds to delay destruction of the companion GameObject.
        /// </summary>
        public float DelayCleanupTime;

        private class Baker : Baker<CompanionGameObjectAuthoring>
        {
            public override void Bake(CompanionGameObjectAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CompanionGameObjectInitializationData
                {
                    Prefab = authoring.CompanionPrefab,
                    DelayCleanupTime = authoring.DelayCleanupTime
                });
            }
        }
    }

    /// <summary>
    /// System to handle the lifecycle of companion GameObjects. Companion GameObjects are GameObjects that will match the position and rotation of their associated entity.
    /// This system has 3 foreach loops that do the following:
    /// 1. Instantiate the companion GameObject from <see cref="CompanionGameObjectInitializationData.Prefab"/> assign values to a <see cref="CompanionGameObjectData"/> and add it to the entity.
    /// 2. Update the position and rotation of the companion GameObject from values in the entity's LocalToWorld component.
    /// 3. When the entity is destroyed, destroy the companion GameObject. Depending on settings, this may happen after a delay set in <see cref="CompanionGameObjectInitializationData.DelayCleanupTime"/>.
    /// </summary>
    /// <remarks>
    /// System updates in the <see cref="DS_EffectsSystemGroup"/> which updates after the Unity's TransformSystemGroup. To ensure accuracy of position and rotation, these values are sourced from the entity's LocalToWorld component.
    /// </remarks>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct UpdateCompanionGameObjectSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (properties, localToWorld, entity) in SystemAPI.Query<CompanionGameObjectInitializationData, LocalToWorld>().WithNone<CompanionGameObjectData>().WithEntityAccess())
            {
                var newCompanionGameObject = (GameObject)Object.Instantiate(properties.Prefab, localToWorld.Position, Quaternion.identity);
                ecb.AddComponent(entity, new CompanionGameObjectData
                {
                    Transform = newCompanionGameObject.transform,
                    DelayCleanupTimer = properties.DelayCleanupTime
                });
            }

            foreach (var (companionGameObject, localToWorld) in SystemAPI.Query<CompanionGameObjectData, LocalToWorld>())
            {
                companionGameObject.Transform.Value.SetPositionAndRotation(localToWorld.Position, localToWorld.Rotation);
            }

            foreach (var (companionGameObject, entity) in SystemAPI.Query<RefRW<CompanionGameObjectData>>().WithNone<LocalToWorld>().WithEntityAccess())
            {
                companionGameObject.ValueRW.DelayCleanupTimer -= deltaTime;
                if (companionGameObject.ValueRO.DelayCleanupTimer > 0f) continue;
                if (companionGameObject.ValueRO.Transform.Value != null)
                {
                    Object.Destroy(companionGameObject.ValueRO.Transform.Value.gameObject);
                }
                ecb.RemoveComponent<CompanionGameObjectData>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}