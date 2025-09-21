using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component stores data related to counter rotation. Counter rotation is when the rotation of a child entity applies a rotation opposite to that of the parent entity, giving it the visual appearance of having a fixed orientation.
    /// </summary>
    /// <remarks>
    /// Used on drone attack as main entity will constantly rotate around the player and drone entities will stay visually up and down.
    /// </remarks>
    public struct CounterRotationData : IComponentData
    {
        /// <summary>
        /// Euler offset to apply additional rotation correction, stored in radians.
        /// </summary>
        public float3 EulerOffset;
    }
    
    /// <summary>
    /// Authoring script to initialize <see cref="CounterRotationData"/> on the associated entity.
    /// </summary>
    public class CounterRotationAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Euler offset to apply additional rotation correction. Authored in degrees, stored in radians.
        /// </summary>
        [Tooltip("Enter in Degrees")]
        public Vector3 EulerOffset;
        
        private class Baker : Baker<CounterRotationAuthoring>
        {
            public override void Bake(CounterRotationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CounterRotationData
                {
                    EulerOffset = math.radians(authoring.EulerOffset)
                });
            }
        }
    }

    /// <summary>
    /// System to apply counter rotation to an entity. Counter rotation is when the rotation of a child entity applies a rotation opposite to that of the parent entity, giving it the visual appearance of having a fixed orientation.
    /// </summary>
    /// <remarks>
    /// Used on drone attack as main entity will constantly rotate around the player and drone entities will stay visually up and down.
    /// Updates in the <see cref="DS_TranslationSystemGroup"/> which updates before Unity's TransformSystemGroup, so LocalTransform can be safely modified.
    /// </remarks>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct CounterRotationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (transform, counterRotation, parent) in SystemAPI.Query<RefRW<LocalTransform>, CounterRotationData, Parent>())
            {
                var parentEuler = math.Euler(SystemAPI.GetComponent<LocalTransform>(parent.Value).Rotation);
                var rotation = counterRotation.EulerOffset - parentEuler;
                transform.ValueRW.Rotation = quaternion.Euler(rotation);
            }
        }
    }
}