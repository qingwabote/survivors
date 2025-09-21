using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Entities with this component will get destroyed once they reach this world position.
    /// </summary>
    public struct DestroyAtPosition : IComponentData
    {
        /// <summary>
        /// Target position the entity will be destroyed at.
        /// </summary>
        public float3 TargetPosition;
        /// <summary>
        /// Last distance calculated between entity and target position. If this value is decreasing, the entity is still moving towards the target. If this value increases then it means the entity has gone past the TargetPosition and should be destroyed.
        /// </summary>
        public float LastDistanceSq;
    }
    
    /// <summary>
    /// Authoring component for <see cref="DestroyAtPosition"/>
    /// </summary>
    /// <seeaslo cref="DestroyAtPosition"/>
    /// <seeaslo cref="DestroyAtPositionSystem"/>
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class DestroyAtPositionAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Target world position this entity should be destroyed at.
        /// </summary>
        public Vector3 Position;

        private class Baker : Baker<DestroyAtPositionAuthoring>
        {
            public override void Bake(DestroyAtPositionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new DestroyAtPosition
                {
                    TargetPosition = authoring.Position,
                    LastDistanceSq = float.MaxValue
                });
            }
        }
    }

    /// <summary>
    /// This system will set the <see cref="DestroyEntityFlag"/> to true once the associated entity reaches a target world position. It determines if an entity has reached the position by comparing the current distance to the target with the previous distance. If the current distance is greater than the previous distance, it knows the entity has passed the target position. The distance should be less than two units to reduce the possibility of false positives.
    /// <remarks>
    /// This method of determining if an entity has reached a target position is to ensure fast moving entities that aren't guaranteed to be within a small tolerance of the target position are accounted for.
    /// </remarks>
    /// </summary>
    /// <seeaslo cref="DestroyAtPosition"/>
    /// <seeaslo cref="DestroyAtPositionAuthoring"/>
    [UpdateInGroup(typeof(DS_DestructionSystemGroup))]
    public partial struct DestroyAtPositionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (destroyAtPosition, transform, entity) in SystemAPI.Query<RefRW<DestroyAtPosition>, LocalToWorld>().WithEntityAccess())
            {
                var distanceToDestructionPositionSq = math.lengthsq(destroyAtPosition.ValueRO.TargetPosition.xz - transform.Position.xz);

                if (distanceToDestructionPositionSq < 4 && distanceToDestructionPositionSq > destroyAtPosition.ValueRO.LastDistanceSq)
                {
                    SystemAPI.SetComponentEnabled<DestroyEntityFlag>(entity, true);
                }
                else
                {
                    destroyAtPosition.ValueRW.LastDistanceSq = distanceToDestructionPositionSq;
                }
            }
        }
    }
}