using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component to store data relevant to spawning saw blade attacks.
    /// </summary>
    public struct SawBladeWeaponData : IComponentData
    {
        /// <summary>
        /// An angle offset will be applied to the spawn direction of a saw blade so that they all spawn in an arc pattern.
        /// </summary>
        /// <remarks>
        /// Authored in degrees for ease of use, stored in radians as that is what unity expects for the quaternion.Euler() method
        /// </remarks>
        public float RadianAngleBetweenAttacks;
    }
    
    /// <summary>
    /// Authoring script to add <see cref="SawBladeWeaponData"/> to the entity.
    /// </summary>
    /// <remarks>
    /// Note that this script should be added to the weapon entity that will spawn in-world attacks, not the in-world attack entity itself.
    /// Although this entity will not be rendered in the game world, it is marked with the dynamic transform usage flags as it will be a child of the player entity.
    /// Requires the <see cref="WeaponAuthoring"/> script to ensure all components required for executing attacks are added to the entity.
    /// Requires the <see cref="EntityRandomAuthoring"/> script for random number generation in <see cref="SawBladeAttackSystem"/>.
    /// </remarks>
    [RequireComponent(typeof(WeaponAuthoring))]
    [RequireComponent(typeof(EntityRandomAuthoring))]
    public class SawBladeWeaponAuthoring : MonoBehaviour
    {
        /// <summary>
        /// An angle offset will be applied to the spawn direction of a saw blade so that they all spawn in an arc pattern.
        /// </summary>
        /// <remarks>
        /// Authored in degrees for ease of use, stored in radians as that is what unity expects for the quaternion.Euler() method
        /// </remarks>
        public float DegreeAngleBetweenAttacks = 10f;
        
        private class Baker : Baker<SawBladeWeaponAuthoring>
        {
            public override void Bake(SawBladeWeaponAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SawBladeWeaponData
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
    /// Saw blades are affected by modifications to the player's additional attack projectiles, damage dealt, attack area, attack duration, and attack projectile speed stat modifications.
    /// System will target a random entity on screen and fire an arc volley of saw blades towards it.
    /// System executes on the persistent weapon entity responsible for spawning attacks, not the in-world attack entity itself.
    /// This system will only execute on the weapon entity once its <see cref="WeaponState.CooldownTimer"/> expires and has its <see cref="WeaponActiveFlag"/> set to true in the <see cref="WeaponActivationSystem"/>.
    /// </remarks>
    [UpdateInGroup(typeof(DS_AttackSystemGroup))]
    public partial struct SawBladeAttackSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CameraTarget>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var cameraHalfExtents = SystemAPI.GetSingleton<CameraTarget>().HalfExtents;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (weaponState, random, sawBladeData, weaponData, attackPrefab, parent, weaponActive) in SystemAPI.Query<RefRW<WeaponState>, RefRW<EntityRandom>, SawBladeWeaponData, WeaponLevelData, AttackPrefab, Parent, EnabledRefRW<WeaponActiveFlag>>().WithNone<InitializeEntityRandom>())
            {
                var playerEntity = parent.Value;
                var spawnPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
                var playerCurrentStats = SystemAPI.GetComponent<CharacterStatModificationState>(playerEntity);

                var pSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
                var overlapHits = new NativeList<int>(state.WorldUpdateAllocator);
                var minPosition = spawnPosition - cameraHalfExtents;
                var maxPosition = spawnPosition + cameraHalfExtents;
                var aabbInput = new OverlapAabbInput
                {
                    Aabb = new Aabb
                    {
                        Min = minPosition,
                        Max = maxPosition
                    },
                    Filter = weaponData.CollisionFilter
                };

                if (!pSingleton.OverlapAabb(aabbInput, ref overlapHits))
                {
                    // No enemies within detection radius.
                    // Timer doesn't reset so this will check again each frame until an enemy is within range.
                    continue;
                }
                
                var randomIndex = random.ValueRW.Value.NextInt(overlapHits.Length);
                var randomBodyIndex = overlapHits[randomIndex];
                var randomEnemyPosition = pSingleton.Bodies[randomBodyIndex].WorldFromBody.pos;
                var vectorToTargetEnemy = randomEnemyPosition.xz - spawnPosition.xz;
                var baseAngle = math.atan2(vectorToTargetEnemy.x, vectorToTargetEnemy.y);

                var numberAttacks = weaponData.AttackCount + playerCurrentStats.AdditionalAttackProjectiles;
                for (var i = 0; i < numberAttacks; i++)
                {
                    var leftRightModifier = i % 2 == 0 ? 1 : -1;
                    var angleDifference = math.ceil(i / 2f) * leftRightModifier * sawBladeData.RadianAngleBetweenAttacks;
                    var angle = baseAngle + angleDifference;
                    var spawnAngle = quaternion.Euler(0f, angle, 0f);
                    
                    var attackArea = weaponData.Area * playerCurrentStats.AttackArea;
                    var duration = weaponData.TimeToLive * playerCurrentStats.AttackDuration;
                    var damageToDeal = (int)math.ceil(weaponData.BaseHitPoints * playerCurrentStats.DamageDealt);
                    var attackSpeed = weaponData.MovementSpeed * playerCurrentStats.AttackProjectileSpeed;
                    
                    var newSawBladeEntity = ecb.Instantiate(attackPrefab.Value);
                    ecb.SetComponent(newSawBladeEntity, LocalTransform.FromPositionRotationScale(spawnPosition, spawnAngle, attackArea));
                    ecb.SetComponent(newSawBladeEntity, new DestroyAfterTime { Value = duration });
                    ecb.SetComponent(newSawBladeEntity, new DealHitPointsOnInteraction { Value = damageToDeal });
                    ecb.SetComponent(newSawBladeEntity, new DestroyAfterNumberHits { HitsRemaining = weaponData.MaxEnemyHitCount });
                    ecb.SetComponent(newSawBladeEntity, new LinearMovementSpeed { Value = attackSpeed });
                }

                weaponState.ValueRW.NextAttackTimer = 0f;
                weaponState.ValueRW.AttackCount = 0;

                weaponActive.ValueRW = false;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}