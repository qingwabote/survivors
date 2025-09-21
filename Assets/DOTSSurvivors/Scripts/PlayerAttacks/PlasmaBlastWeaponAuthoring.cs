using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Unity.Collections;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Tag component to signify this entity as the weapon entity to spawn plasma blasts into the game world.
    /// </summary>
    public struct PlasmaBlastWeaponTag : IComponentData {}
    
    /// <summary>
    /// Authoring script to add <see cref="PlasmaBlastWeaponTag"/> to the entity.
    /// </summary>
    /// <remarks>
    /// Note that this script should be added to the weapon entity that will spawn in-world attacks, not the in-world attack entity itself.
    /// Although this entity will not be rendered in the game world, it is marked with the dynamic transform usage flags as it will be a child of the player entity.
    /// Requires the <see cref="WeaponAuthoring"/> script to ensure all components required for executing attacks are added to the entity.
    /// </remarks>
    [RequireComponent(typeof(WeaponAuthoring))]
    public class PlasmaBlastWeaponAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PlasmaBlastWeaponAuthoring>
        {
            public override void Bake(PlasmaBlastWeaponAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlasmaBlastWeaponTag>(entity);
            }
        }
    }

    /// <summary>
    /// System to handle instantiating <see cref="AttackPrefab"/>s of plasma blasts into the game world.
    /// </summary>
    /// <remarks>
    /// Plasma blasts are affected by modifications to the player's additional attack projectiles, damage dealt, attack speed, and attack duration stat modifications.
    /// Plasma blasts shoot towards the closest enemy to the player.
    /// System executes on the persistent weapon entity responsible for spawning attacks, not the in-world attack entity itself.
    /// This system will only execute on the weapon entity once its <see cref="WeaponState.CooldownTimer"/> expires and has its <see cref="WeaponActiveFlag"/> set to true in the <see cref="WeaponActivationSystem"/>.
    /// </remarks>
    [UpdateInGroup(typeof(DS_AttackSystemGroup))]
    public partial struct PlasmaBlastAttackSystem : ISystem
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
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (weaponState, weaponData, attackPrefab, parent, weaponActive) in SystemAPI.Query<RefRW<WeaponState>, WeaponLevelData, AttackPrefab, Parent, EnabledRefRW<WeaponActiveFlag>>().WithAll<PlasmaBlastWeaponTag>())
            {
                weaponState.ValueRW.NextAttackTimer -= deltaTime;
                if (weaponState.ValueRO.NextAttackTimer > 0f) continue;
                
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

                var maxDistanceSq = float.MaxValue;
                var closestEnemyPosition = float3.zero;
                foreach (var overlapHit in overlapHits)
                {
                    var otherPosition = pSingleton.Bodies[overlapHit].WorldFromBody.pos;
                    var distanceToPlayerSq = math.distancesq(spawnPosition.xz, otherPosition.xz);
                    if (distanceToPlayerSq < maxDistanceSq)
                    {
                        maxDistanceSq = distanceToPlayerSq;
                        closestEnemyPosition = otherPosition;
                    }
                }

                var vectorToClosestEnemy = closestEnemyPosition.xz - spawnPosition.xz;
                var angle = math.atan2(vectorToClosestEnemy.x, vectorToClosestEnemy.y);
                var spawnRotation = quaternion.Euler(0f, angle, 0f);
                
                var duration = weaponData.TimeToLive * playerCurrentStats.AttackDuration;
                var damageToDeal = (int) math.ceil(weaponData.BaseHitPoints * playerCurrentStats.DamageDealt);
                var attackSpeed = weaponData.MovementSpeed * playerCurrentStats.AttackProjectileSpeed;
                
                var plasmaBlastEntity = ecb.Instantiate(attackPrefab.Value);
                ecb.SetComponent(plasmaBlastEntity, LocalTransform.FromPositionRotation(spawnPosition, spawnRotation));
                ecb.SetComponent(plasmaBlastEntity, new DestroyAfterTime { Value = duration });
                ecb.SetComponent(plasmaBlastEntity, new DealHitPointsOnInteraction { Value = damageToDeal });
                ecb.SetComponent(plasmaBlastEntity, new LinearMovementSpeed { Value = attackSpeed });
                ecb.SetComponent(plasmaBlastEntity, new DestroyAfterNumberHits { HitsRemaining = weaponData.MaxEnemyHitCount });
                
                weaponState.ValueRW.NextAttackTimer = weaponData.IntervalBetweenAttacks;
                weaponState.ValueRW.AttackCount += 1;
                var numberAttacks = weaponData.AttackCount + playerCurrentStats.AdditionalAttackProjectiles;
                if (weaponState.ValueRW.AttackCount < numberAttacks) continue;
                
                weaponState.ValueRW.NextAttackTimer = 0f;
                weaponState.ValueRW.AttackCount = 0;

                weaponActive.ValueRW = false;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}