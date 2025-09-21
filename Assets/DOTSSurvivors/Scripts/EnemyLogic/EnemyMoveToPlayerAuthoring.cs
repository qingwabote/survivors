using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Enemies tagged with this component will constantly update their <see cref="CharacterMoveDirection"/> to move towards the player's current position.
    /// </summary>
    /// <seealso cref="EnemyMoveToPlayerAuthoring"/>
    /// <seealso cref="EnemyMoveToPlayerSystem"/>
    /// <seealso cref="EnemyMoveToPlayerJob"/>
    /// <seealso cref="CharacterMoveSystem"/>
    public struct EnemyMoveToPlayerTag : IComponentData {}

    /// <summary>
    /// Authoring script to add <see cref="EnemyMoveToPlayerTag"/> to entity.
    /// </summary>
    /// <seealso cref="EnemyMoveToPlayerTag"/>
    /// <seealso cref="EnemyMoveToPlayerSystem"/>
    /// <seealso cref="EnemyMoveToPlayerJob"/>
    /// <seealso cref="CharacterMoveSystem"/>
    public class EnemyMoveToPlayerAuthoring : MonoBehaviour
    {
        private class Baker : Baker<EnemyMoveToPlayerAuthoring>
        {
            public override void Bake(EnemyMoveToPlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<EnemyMoveToPlayerTag>(entity);
            }
        }
    }

    /// <summary>
    /// System to schedule to <see cref="EnemyMoveToPlayerJob"/> which constantly updates the enemies' <see cref="CharacterMoveDirection"/> to move the enemy towards the player.
    /// </summary>
    /// <seealso cref="EnemyMoveToPlayerJob"/>
    /// <seealso cref="EnemyMoveToPlayerTag"/>
    /// <seealso cref="CharacterMoveSystem"/>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct EnemyMoveToPlayerSystem : ISystem, ISystemStartStop
    {
        /// <summary>
        /// Only schedule the <see cref="EnemyMoveToPlayerJob"/> when the player exists.
        /// </summary>
        /// <remarks>
        /// This is important for two reasons:
        /// 1. Without the player entity, the job will be unable to find the player's position
        /// 2. When the player entity is destroyed, we can trigger logic to stop enemies in the <see cref="EnemyMoveToPlayerSystem.OnStopRunning"/> method.
        /// </remarks>
        /// <param name="state"></param>
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
            var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
            new EnemyMoveToPlayerJob { PlayerPosition = playerPosition }.ScheduleParallel();
        }

        /// <summary>
        /// Empty method required for ISystemStartStop implementation.
        /// </summary>
        /// <param name="state"></param>
        public void OnStartRunning(ref SystemState state)
        {
            
        }

        /// <summary>
        /// When Player no longer exists, stop movement.
        /// </summary>
        /// <param name="state"></param>
        [BurstCompile]
        public void OnStopRunning(ref SystemState state)
        {
            foreach (var moveDirection in SystemAPI.Query<RefRW<CharacterMoveDirection>>().WithAll<EnemyMoveToPlayerTag>())
            {
                moveDirection.ValueRW.Value = float2.zero;
            }
        }
    }

    /// <summary>
    /// Job to calculate the direction from the enemy to the player and set the <see cref="CharacterMoveDirection"/> to move the enemy towards the player.
    /// </summary>
    /// <seealso cref="EnemyMoveToPlayerSystem"/>
    /// <seealso cref="EnemyMoveToPlayerTag"/>
    /// <seealso cref="CharacterMoveSystem"/>
    [BurstCompile]
    [WithAll(typeof(EnemyTag), typeof(EnemyMoveToPlayerTag))]
    public partial struct EnemyMoveToPlayerJob : IJobEntity
    {
        public float3 PlayerPosition;
        
        [BurstCompile]
        private void Execute(ref CharacterMoveDirection moveDirection, in LocalTransform transform)
        {
            var curMoveDirection = PlayerPosition.xz - transform.Position.xz;
            curMoveDirection = math.normalize(curMoveDirection);
            moveDirection.Value = curMoveDirection;
        }
    }
}