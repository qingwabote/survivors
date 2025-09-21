using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component to store data relevant to spawning wrench attacks.
    /// </summary>
    public struct WrenchWeaponData : IComponentData
    {
        /// <summary>
        /// An offset to apply to the angle at which a wrench attack spawns in a group. Value will be multiplied by its spawn index in the attack group.
        /// </summary>
        /// <remarks>
        /// Authored in degrees for ease of use, stored in radians as that is what unity expects for the quaternion.Euler() method
        /// </remarks>
        public float RadianAngleBetweenAttacks;

        /// <summary>
        /// Stores the angle towards the first enemy the weapon will be firing a wrench towards. Subsequent wrenches will fire at an offset from this angle.
        /// </summary>
        /// <remarks>
        /// As this value will change with each attack group, normally I would store something like this in a separate "state" component. However, as this component is only used in the <see cref="WrenchAttackSystem"/> I don't have any concern over potential data dependency issues so there is no problem having this field in here too.
        /// </remarks>
        public float RadianAngleToFirstEnemy;
    }
    
    /// <summary>
    /// Authoring script to add <see cref="WrenchWeaponData"/> to the entity.
    /// </summary>
    /// <remarks>
    /// Note that this script should be added to the weapon entity that will spawn in-world attacks, not the in-world attack entity itself.
    /// Although this entity will not be rendered in the game world, it is marked with the dynamic transform usage flags as it will be a child of the player entity.
    /// Requires the <see cref="WeaponAuthoring"/> script to ensure all components required for executing attacks are added to the entity.
    /// Requires the <see cref="EntityRandomAuthoring"/> script for random number generation in <see cref="WrenchAttackSystem"/>.
    /// </remarks>
    [RequireComponent(typeof(WeaponAuthoring))]
    [RequireComponent(typeof(EntityRandomAuthoring))]
    public class WrenchWeaponAuthoring : MonoBehaviour
    {
        /// <summary>
        /// The initial wrench attack will spawn in the direction towards a random enemy on screen. Subsequent wrenches will be offset from that initial angle by the angle defined in this field.
        /// </summary>
        /// <remarks>
        /// Authored in degrees for ease of use, stored in radians as that is what unity expects for the quaternion.Euler() method
        /// </remarks>
        public float DegreeAngleBetweenAttacks = 20f;
        
        private class Baker : Baker<WrenchWeaponAuthoring>
        {
            public override void Bake(WrenchWeaponAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new WrenchWeaponData
                {
                    RadianAngleBetweenAttacks = math.radians(authoring.DegreeAngleBetweenAttacks)
                });
            }
        }
    }

    /// <summary>
    /// System to handle instantiating <see cref="AttackPrefab"/>s of wrenches into the game world.
    /// </summary>
    /// <remarks>
    /// Wrenches are affected by modifications to the player's additional attack projectiles, damage dealt, and attack projectile speed stat modifications.
    /// The initial wrench attack will spawn in the direction towards a random enemy on screen. Subsequent wrenches will be offset from that initial angle by the angle defined in <see cref="WrenchWeaponData.RadianAngleBetweenAttacks"/>.
    /// Wrenches move in a boomerang pattern, first moving in the direction of spawn then reversing on its initial path.
    /// System executes on the persistent weapon entity responsible for spawning attacks, not the in-world attack entity itself.
    /// This system will only execute on the weapon entity once its <see cref="WeaponState.CooldownTimer"/> expires and has its <see cref="WeaponActiveFlag"/> set to true in the <see cref="WeaponActivationSystem"/>.
    /// </remarks>
    [UpdateInGroup(typeof(DS_AttackSystemGroup))]
    public partial struct WrenchAttackSystem : ISystem
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
            var deltaTime = SystemAPI.Time.DeltaTime;
            var elapsedTime = SystemAPI.Time.ElapsedTime;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (weaponState, random, wrenchData, weaponData, attackPrefab, parent, weaponActive) in SystemAPI.Query<RefRW<WeaponState>, RefRW<EntityRandom>, RefRW<WrenchWeaponData>, WeaponLevelData, AttackPrefab, Parent, EnabledRefRW<WeaponActiveFlag>>().WithNone<InitializeEntityRandom>())
            {
                weaponState.ValueRW.NextAttackTimer -= deltaTime;
                if (weaponState.ValueRO.NextAttackTimer > 0f) continue;

                var playerEntity = parent.Value;
                var playerCurrentStats = SystemAPI.GetComponent<CharacterStatModificationState>(playerEntity);
                var spawnPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;

                if (weaponState.ValueRO.AttackCount == 0)
                {
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
                    wrenchData.ValueRW.RadianAngleToFirstEnemy = math.atan2(vectorToTargetEnemy.x, vectorToTargetEnemy.y);
                }
                
                var spawnAngle = wrenchData.ValueRO.RadianAngleToFirstEnemy + weaponState.ValueRO.AttackCount * wrenchData.ValueRO.RadianAngleBetweenAttacks;
                var spawnRotation = quaternion.Euler(0f, spawnAngle, 0f);
                var spawnScale = weaponData.Area * playerCurrentStats.AttackArea;

                var damageToDeal = (int)math.ceil(weaponData.BaseHitPoints * playerCurrentStats.DamageDealt);
                var attackProjectileSpeed = weaponData.MovementSpeed * playerCurrentStats.AttackProjectileSpeed;
                
                var newWrenchEntity = ecb.Instantiate(attackPrefab.Value);
                ecb.SetComponent(newWrenchEntity, LocalTransform.FromPositionRotationScale(spawnPosition, spawnRotation, spawnScale));
                ecb.SetComponent(newWrenchEntity, new DealHitPointsOnInteraction { Value = damageToDeal });
                ecb.SetComponent(newWrenchEntity, new BoomerangMovementData
                {
                    MoveSpeed = attackProjectileSpeed,
                    StartTime = elapsedTime
                });

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