using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Enum type to define the direction of travel the enemy will move in as it also moves in a sine wave pattern.
    /// </summary>
    /// <remarks>
    /// For example, left means the character is moving from right to left at a constant move speed while moving up and down in a sine wave pattern.
    /// </remarks>
    [System.Serializable]
    public enum SineWaveMoveDirection : byte
    {
        None,
        Left,
        Right,
        Up,
        Down
    }
    
    /// <summary>
    /// Component defining the movement properties for enemy sine wave movement.
    /// </summary>
    public struct EnemySineWaveMovement : IComponentData
    {
        /// <summary>
        /// Cardinal direction specifying the direction of constant movement of the enemy.
        /// </summary>
        public SineWaveMoveDirection MoveDirection;
        /// <summary>
        /// Speed it will move along the axis of constant movement.
        /// </summary>
        public float ConstantMoveSpeed;
        /// <summary>
        /// Period of sine wave - units between peaks of waves. Essentially determines how fast it will move back and forth, perpendicular to direction of constant movement.
        /// </summary>
        public float Period;
        /// <summary>
        /// Amplitude of sine wave - height from bottom valley to top peak of sine wave.
        /// </summary>
        public float Amplitude;
    }

    /// <summary>
    /// Component containing the start time of the sine wave so the enemy can move along the wave over time.
    /// </summary>
    /// <remarks>
    /// Typically this value will be initialized to the time at which the enemy was spawned and time alive can be calculated from this value.
    /// </remarks>
    public struct EnemySineWaveStartTime : IComponentData
    {
        /// <summary>
        /// Timestamp at which the current time alive value should be calculated from.
        /// </summary>
        public float Value;
    }
    
    /// <summary>
    /// Authoring script to initialize values for <see cref="EnemySineWaveMovement"/>.
    /// </summary>
    public class EnemySineWaveMovementAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Cardinal direction specifying the direction of constant movement of the enemy.
        /// </summary>
        public SineWaveMoveDirection MoveDirection;
        /// <summary>
        /// Speed it will move along the axis of constant movement.
        /// </summary>
        public float ConstantMoveSpeed;
        /// <summary>
        /// Period of sine wave - units between peaks of waves. Essentially determines how fast it will move back and forth, perpendicular to direction of constant movement.
        /// </summary>
        public float Period;
        /// <summary>
        /// Amplitude of sine wave - height from bottom valley to top peak of sine wave.
        /// </summary>
        public float Amplitude;
        
        private class Baker : Baker<EnemySineWaveMovementAuthoring>
        {
            public override void Bake(EnemySineWaveMovementAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new EnemySineWaveMovement
                {
                    MoveDirection = authoring.MoveDirection,
                    ConstantMoveSpeed = authoring.ConstantMoveSpeed,
                    Period = authoring.Period,
                    Amplitude = authoring.Amplitude
                });
            }
        }
    }

    /// <summary>
    /// System to set the <see cref="CharacterMoveDirection"/> for an enemy entity based on values calculated using <see cref="EnemySineWaveMovement"/>.
    /// </summary>
    /// <remarks>
    /// This system will also add an <see cref="EnemySineWaveStartTime"/> component with the current game time value to any entity that has an <see cref="EnemySineWaveMovement"/> component but no <see cref="EnemySineWaveStartTime"/> component.
    /// </remarks>
    /// <seealso cref="CharacterMoveSystem"/>
    /// <seealso cref="EnemySineWaveMovement"/>
    /// <seealso cref="EnemySineWaveStartTime"/>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct EnemySineWaveMovementSystem : ISystem
    {
        private EntityQuery _initializeSineWaveMovementQuery;

        public void OnCreate(ref SystemState state)
        {
            _initializeSineWaveMovementQuery = SystemAPI.QueryBuilder().WithAll<EnemySineWaveMovement>().WithNone<EnemySineWaveStartTime>().Build();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var elapsedTime = SystemAPI.Time.ElapsedTime;

            if (!_initializeSineWaveMovementQuery.IsEmpty)
            {
                var newStartTime = new EnemySineWaveStartTime { Value = (float)elapsedTime };
                var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
                ecb.AddComponent(_initializeSineWaveMovementQuery, newStartTime);
                ecb.Playback(state.EntityManager);
            }
            
            foreach (var (characterMoveDirection, enemySineWaveMovement, startTime) in SystemAPI.Query<RefRW<CharacterMoveDirection>, EnemySineWaveMovement, EnemySineWaveStartTime>())
            {
                var timeAlive = (float)elapsedTime - startTime.Value;
                var constantMoveSpeed = enemySineWaveMovement.ConstantMoveSpeed;
                var sineMovement = enemySineWaveMovement.Amplitude * math.sin(enemySineWaveMovement.Period * timeAlive);

                var moveDirection = enemySineWaveMovement.MoveDirection switch
                {
                    SineWaveMoveDirection.Left => new float2(-1 * constantMoveSpeed, sineMovement),
                    SineWaveMoveDirection.Right => new float2(constantMoveSpeed, sineMovement),
                    SineWaveMoveDirection.Up => new float2(sineMovement, constantMoveSpeed),
                    SineWaveMoveDirection.Down => new float2(sineMovement, -1 * constantMoveSpeed),
                    _ => float2.zero
                };

                characterMoveDirection.ValueRW.Value = moveDirection;
            }
        }
    }
}