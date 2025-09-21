using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component to store speed of linear movement. Linear movement is when an entity will move along its forward axis constantly at the speed defined here.
    /// </summary>
    /// <remarks>
    /// Used for lots of player attack projectiles to move them along their forward facing axis.
    /// </remarks>
    /// <seealso cref="LinearMovementAuthoring"/>
    /// <seealso cref="LinearMovementSystem"/>
    public struct LinearMovementSpeed : IComponentData
    {
        public float Value;
    }
    
    /// <summary>
    /// Authoring script to add <see cref="LinearMovementSpeed"/> component to an entity, so it will constantly move along its forward axis in the <see cref="LinearMovementSystem"/>
    /// </summary>
    public class LinearMovementAuthoring : MonoBehaviour
    {
        public float MoveSpeed;
        
        private class Baker : Baker<LinearMovementAuthoring>
        {
            public override void Bake(LinearMovementAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new LinearMovementSpeed { Value = authoring.MoveSpeed });
            }
        }
    }

    /// <summary>
    /// System to move an entity along its forward axis, with data defined in <see cref="LinearMovementSpeed"/>.
    /// </summary>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct LinearMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, movement) in SystemAPI.Query<RefRW<LocalTransform>, LinearMovementSpeed>())
            {
                transform.ValueRW.Position += transform.ValueRO.Forward() * movement.Value * deltaTime;
            }
        }
    }
}