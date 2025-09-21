using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component to hold current state of parabolic movement.
    /// </summary>
    public struct ParabolicMovementState : IComponentData
    {
        /// <summary>
        /// Holds the velocity of the object. Value along the x-axis will remain constant through the lifetime of the entity. Value along the y-axis will update with the current velocity so velocity for the next frame can be calculated.
        /// </summary>
        public float2 Velocity;
        /// <summary>
        /// Holds the start timestamp of when the entity was spawned into the world. Used to calculate the velocity parabola the entity will follow.
        /// </summary>
        public double StartTime;
    }

    /// <summary>
    /// Authoring script to define initial values for <see cref="ParabolicMovementState"/>.
    /// </summary>
    /// <remarks>
    /// Note that this script does not initialize <see cref="ParabolicMovementState.StartTime"/> as this value will not be known at bake time, so it is important that this value is set during instantiation of the entity.
    /// </remarks>
    public class ParabolicMovementAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Starting angle in degrees of the parabolic movement.
        /// From this a float2 value for initial velocity will be calculated.
        /// </summary>
        public float StartingAngle;
        /// <summary>
        /// Additional force to be applied to the initial velocity.
        /// </summary>
        public float BaseForce;
        
        private class Baker : Baker<ParabolicMovementAuthoring>
        {
            public override void Bake(ParabolicMovementAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var initialVelocity = new float2
                {
                    x = math.sin(math.radians(authoring.StartingAngle)),
                    y = math.cos(math.radians(authoring.StartingAngle))
                };
                initialVelocity *= authoring.BaseForce;
                AddComponent(entity, new ParabolicMovementState
                {
                    Velocity = initialVelocity
                });
            }
        }
    }

    /// <summary>
    /// System to apply parabolic movement to entities. Parabolic movement gives the effect that an entity will move towards the bottom of the screen as if screen-space gravity is being applied.
    /// </summary>
    /// <remarks>
    /// System update in the <see cref="DS_TranslationSystemGroup"/> which updates before Unity's TransformSystemGroup so the LocalTransform component can be safely modified.
    /// </remarks>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct ParabolicMovementSystem : ISystem
    {
        /// <summary>
        /// Constant gravity force used to calculate parabola.
        /// </summary>
        private const float GRAVITY_FORCE = -9f;
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var elapsedTime = SystemAPI.Time.ElapsedTime;
            var deltaTime = SystemAPI.Time.DeltaTime;
            
            foreach (var (transform, parabolicMovement) in SystemAPI.Query<RefRW<LocalTransform>, ParabolicMovementState>())
            {
                var activeTime = (float)(elapsedTime - parabolicMovement.StartTime);
                var yVelocity = GRAVITY_FORCE * activeTime * activeTime;
                yVelocity += parabolicMovement.Velocity.y;
                transform.ValueRW.Position.xz += new float2(parabolicMovement.Velocity.x, yVelocity) * deltaTime;
            }
        }
    }
}