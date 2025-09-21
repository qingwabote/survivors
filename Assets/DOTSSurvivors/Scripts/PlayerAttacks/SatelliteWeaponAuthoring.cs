using Unity.Burst;
using Unity.Entities;
using UnityEngine;
using Unity.Transforms;
using Unity.Mathematics;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component to store data relevant to spawning satellite attacks.
    /// </summary>
    public struct SatelliteWeaponData : IComponentData
    {
        /// <summary>
        /// An offset to apply to the angle at which a satellite attack spawns in a group. Value will be multiplied by its spawn index in the attack group.
        /// </summary>
        /// <remarks>
        /// Authored in degrees for ease of use, stored in radians as that is what unity expects for the quaternion.Euler() method
        /// </remarks>
        public float RadianAngleBetweenAttacks;
    }
    
    /// <summary>
    /// Authoring script to add <see cref="SatelliteWeaponData"/> to the entity.
    /// </summary>
    /// <remarks>
    /// Note that this script should be added to the weapon entity that will spawn in-world attacks, not the in-world attack entity itself.
    /// Although this entity will not be rendered in the game world, it is marked with the dynamic transform usage flags as it will be a child of the player entity.
    /// Requires the <see cref="WeaponAuthoring"/> script to ensure all components required for executing attacks are added to the entity.
    /// </remarks>
    [RequireComponent(typeof(WeaponAuthoring))]
    public class SatelliteWeaponAuthoring : MonoBehaviour
    {
        /// <summary>
        /// An offset to apply to the angle at which a satellite attack spawns in a group. Value will be multiplied by its spawn index in the attack group.
        /// </summary>
        /// <remarks>
        /// Authored in degrees for ease of use, stored in radians as that is what unity expects for the quaternion.Euler() method
        /// </remarks>
        public float DegreeAngleBetweenAttacks = 20f;
        
        private class Baker : Baker<SatelliteWeaponAuthoring>
        {
            public override void Bake(SatelliteWeaponAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SatelliteWeaponData
                {
                    RadianAngleBetweenAttacks = math.radians(authoring.DegreeAngleBetweenAttacks)
                });
            }
        }
    }

    /// <summary>
    /// System to handle instantiating <see cref="AttackPrefab"/>s of satellites into the game world.
    /// </summary>
    /// <remarks>
    /// Satellites are affected by modifications to the player's additional attack projectiles, damage dealt, attack area, and attack projectile speed stat modifications.
    /// Satellites will spawn at an increasing angle towards the player's facing direction.
    /// System executes on the persistent weapon entity responsible for spawning attacks, not the in-world attack entity itself.
    /// This system will only execute on the weapon entity once its <see cref="WeaponState.CooldownTimer"/> expires and has its <see cref="WeaponActiveFlag"/> set to true in the <see cref="WeaponActivationSystem"/>.
    /// </remarks>
    [UpdateInGroup(typeof(DS_AttackSystemGroup))]
    public partial struct SatelliteAttackSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var elapsedTime = SystemAPI.Time.ElapsedTime;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (weaponState, satelliteData, weaponData, attackPrefab, parent, weaponActive) in SystemAPI.Query<RefRW<WeaponState>, SatelliteWeaponData, WeaponLevelData, AttackPrefab, Parent, EnabledRefRW<WeaponActiveFlag>>())
            {
                weaponState.ValueRW.NextAttackTimer -= deltaTime;
                if (weaponState.ValueRO.NextAttackTimer > 0f) continue;

                var playerEntity = parent.Value;
                var spawnPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
                var playerCurrentStats = SystemAPI.GetComponent<CharacterStatModificationState>(playerEntity);

                var spawnScale = playerCurrentStats.AttackArea;

                var damageToDeal = (int)math.ceil(weaponData.BaseHitPoints * playerCurrentStats.DamageDealt);
                
                var spawnAngle = (weaponState.ValueRO.AttackCount + 1) * satelliteData.RadianAngleBetweenAttacks;
                var playerFacingDirection = SystemAPI.GetComponent<PreviousPlayerInput>(playerEntity).LastFacingDirection;
                spawnAngle *= math.sign(playerFacingDirection);
                var attackProjectileSpeed = weaponData.MovementSpeed * playerCurrentStats.AttackProjectileSpeed;
                var velocity = new float2
                {
                    x = math.sin(spawnAngle),
                    y = math.cos(spawnAngle)
                };
                velocity *= attackProjectileSpeed;
                var parabolicMoveState = new ParabolicMovementState
                {
                    Velocity = velocity,
                    StartTime = elapsedTime
                };

                var satelliteEntity = ecb.Instantiate(attackPrefab.Value);
                ecb.SetComponent(satelliteEntity, LocalTransform.FromPositionRotationScale(spawnPosition, quaternion.identity, spawnScale));
                ecb.SetComponent(satelliteEntity, new DealHitPointsOnInteraction { Value = damageToDeal });
                ecb.SetComponent(satelliteEntity, parabolicMoveState);
                ecb.SetComponent(satelliteEntity, new DestroyAfterNumberHits { HitsRemaining = weaponData.MaxEnemyHitCount });

                weaponState.ValueRW.NextAttackTimer = weaponData.IntervalBetweenAttacks;
                weaponState.ValueRW.AttackCount += 1;
                var numberAttacks = weaponData.AttackCount + playerCurrentStats.AdditionalAttackProjectiles;
                if (weaponState.ValueRO.AttackCount < numberAttacks) continue;
                
                weaponState.ValueRW.NextAttackTimer = 0f;
                weaponState.ValueRW.AttackCount = 0;

                weaponActive.ValueRW = false;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}