using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Enableable component used to inform the <see cref="EnemyLinearMovementSystem"/> to update the <see cref="CharacterMoveDirection"/> to a new value. Used to improve efficiency so CharacterMoveDirection is only set when needed.
    /// </summary>
    /// <seealso cref="EnemyLinearMovement"/>
    /// <seealso cref="EnemyLinearMovementSystem"/>
    /// <seealso cref="EnemyLinearMovementAuthoring"/>
    public struct SetEnemyLinearMovementFlag : IComponentData, IEnableableComponent {}
    
    /// <summary>
    /// Data component defining the angle in radians the player will move.
    /// </summary>
    /// <seealso cref="SetEnemyLinearMovementFlag"/>
    /// <seealso cref="EnemyLinearMovementSystem"/>
    /// <seealso cref="EnemyLinearMovementAuthoring"/>
    public struct EnemyLinearMovement: IComponentData
    {
        /// <summary>
        /// Angle in radians of direction of travel.
        /// </summary>
        /// <remarks>
        /// Degree 0 will move enemy from bottom to top of screen, degree 90 will move enemy from left of right of screen.
        /// </remarks>
        public float Angle;
    }
    
    /// <summary>
    /// Authoring script to initialize the angle of travel for <see cref="EnemyLinearMovement"/>. Sets <see cref="SetEnemyLinearMovementFlag"/> to true to initialize <see cref="CharacterMoveDirection"/>.
    /// </summary>
    /// <remarks>
    /// Value authored in degrees for user-friendly editing. Stored in radians for efficient math calculations.
    /// </remarks>
    /// <seealso cref="EnemyLinearMovement"/>
    /// <seealso cref="SetEnemyLinearMovementFlag"/>
    /// <seealso cref="EnemyLinearMovementSystem"/>
    public class EnemyLinearMovementAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Angle in degrees for initial direction of travel.
        /// </summary>
        /// <remarks>
        /// Degree 0 will move enemy from bottom to top of screen, degree 90 will move enemy from left of right of screen.
        /// </remarks>
        public float Angle;

        private class Baker : Baker<EnemyLinearMovementAuthoring>
        {
            public override void Bake(EnemyLinearMovementAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new EnemyLinearMovement { Angle = math.radians(authoring.Angle) });
                AddComponent<SetEnemyLinearMovementFlag>(entity);
            }
        }
    }

    /// <summary>
    /// System to set <see cref="CharacterMoveDirection"/> based off angle stored in <see cref="EnemyLinearMovement"/> when <see cref="SetEnemyLinearMovementFlag"/> is enabled for an entity.
    /// </summary>
    /// <seealso cref="EnemyLinearMovement"/>
    /// <seealso cref="SetEnemyLinearMovementFlag"/>
    /// <seealso cref="EnemyLinearMovementAuthoring"/>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct EnemyLinearMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (characterMoveDirection, enemyLinearMovement, initializationFlag) in SystemAPI.Query<RefRW<CharacterMoveDirection>, EnemyLinearMovement, EnabledRefRW<SetEnemyLinearMovementFlag>>())
            {
                characterMoveDirection.ValueRW.Value = new float2
                {
                    x = math.sin(enemyLinearMovement.Angle),
                    y = math.cos(enemyLinearMovement.Angle)
                };
                initializationFlag.ValueRW = false;
            }
        }
    }
}