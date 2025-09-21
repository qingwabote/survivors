using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using RotationOrder = Unity.Mathematics.math.RotationOrder;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component to store data related to constant rotation of an entity.
    /// </summary>
    /// <remarks>
    /// <see cref="ConstantRotationSystem"/> will apply constant rotation in the euler defined in this component as long as this component is on the entity.
    /// Used mostly for visual effects where an attack entity may appear to constantly rotate through its full lifecycle.
    /// </remarks>
    /// <seealso cref="ConstantRotationAuthoring"/>
    public struct ConstantRotationData : IComponentData
    {
        /// <summary>
        /// Order the euler axes will be applied when converting to a quaternion before applying the rotation.
        /// </summary>
        public RotationOrder RotationOrder;
        /// <summary>
        /// Euler rotation to be applied along each axis, stored in radians per second.
        /// </summary>
        public float3 EulerRadiansPerSecond;
    }
    
    /// <summary>
    /// Authoring script to add the <see cref="ConstantRotationAuthoring"/> component to entities.
    /// </summary>
    /// <seealso cref="ConstantRotationSystem"/>
    public class ConstantRotationAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Order the euler axes will be applied when converting to a quaternion before applying the rotation.
        /// </summary>
        public RotationOrder RotationOrder = RotationOrder.XZY;
        /// <summary>
        /// Euler rotation to be applied along each axis, authored in degrees per second and converted to radians per second during baking.
        /// </summary>
        public Vector3 EulerDegreesPerSecond;

        private class Baker : Baker<ConstantRotationAuthoring>
        {
            public override void Bake(ConstantRotationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ConstantRotationData
                {
                    RotationOrder = authoring.RotationOrder,
                    EulerRadiansPerSecond = math.radians(authoring.EulerDegreesPerSecond)
                });
            }
        }
    }

    /// <summary>
    /// System to apply a constant rotation to entities.
    /// </summary>
    /// <seealso cref="ConstantRotationData"/>
    /// <seealso cref="ConstantRotationAuthoring"/>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct ConstantRotationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (transform, rotationData) in SystemAPI.Query<RefRW<LocalTransform>, ConstantRotationData>())
            {
                var rotationThisFrame = quaternion.Euler(rotationData.EulerRadiansPerSecond * deltaTime, rotationData.RotationOrder);
                transform.ValueRW = transform.ValueRW.Rotate(rotationThisFrame);
            }
        }
    }
}