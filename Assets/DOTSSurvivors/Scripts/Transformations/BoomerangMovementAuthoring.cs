using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data related to boomerang movement.
    /// </summary>
    /// <remarks>
    /// Used for wrench attack - <see cref="WrenchAttackSystem"/>
    /// </remarks>
    public struct BoomerangMovementData : IComponentData
    {
        public float MoveSpeed;
        public double StartTime;
    }
    
    /// <summary>
    /// Authoring script to add the <see cref="BoomerangMovementData"/> to an entity.
    /// </summary>
    /// <remarks>
    /// Note that this authoring component does not set the <see cref="BoomerangMovementData.StartTime"/> as that value would not be known at baking time. So you must initialize this value upon instantiation of the entity.
    /// </remarks>
    public class BoomerangMovementAuthoring : MonoBehaviour
    {
        public float BoomerangMoveSpeed;
        
        private class Baker : Baker<BoomerangMovementAuthoring>
        {
            public override void Bake(BoomerangMovementAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BoomerangMovementData { MoveSpeed = authoring.BoomerangMoveSpeed });
            }
        }
    }

    /// <summary>
    /// System to implement the boomerang movement.
    /// </summary>
    /// <remarks>
    /// Updates in the <see cref="DS_TranslationSystemGroup"/> which updates before Unity's TransformSystemGroup, so the LocalTransform for this entity can be safely updated.
    /// </remarks>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct BoomerangMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var currentTime = SystemAPI.Time.ElapsedTime;
            var deltaTime = SystemAPI.Time.DeltaTime;
            
            foreach (var (transform, boomerangMovement) in SystemAPI.Query<RefRW<LocalTransform>, BoomerangMovementData>())
            {
                var elapsedTime = (float)(currentTime - boomerangMovement.StartTime);
                var velocity = -1f * boomerangMovement.MoveSpeed * elapsedTime * elapsedTime + boomerangMovement.MoveSpeed;
                var velocityVector = transform.ValueRO.Forward() * velocity * deltaTime;
                transform.ValueRW.Position += velocityVector;
            }
        }
    }
}