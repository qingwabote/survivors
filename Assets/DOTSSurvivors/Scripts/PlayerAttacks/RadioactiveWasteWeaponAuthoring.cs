using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component to store data relevant to spawning radioactive waste drops attacks.
    /// </summary>
    public struct RadioactiveWasteWeaponData : IComponentData
    {
        /// <summary>
        /// Radioactive waste drops after the first will spawn at a random position within a range of the player. This value denotes the minimum offset from the player the random position will be selected in.
        /// </summary>
        public float3 MinSpawnOffset;
        /// <summary>
        /// Radioactive waste drops after the first will spawn at a random position within a range of the player. This value denotes the maximum offset from the player the random position will be selected in.
        /// </summary>
        public float3 MaxSpawnOffset;
    }
    
    /// <summary>
    /// Authoring script to add <see cref="RadioactiveWasteWeaponData"/> to the entity.
    /// </summary>
    /// <remarks>
    /// Note that this script should be added to the weapon entity that will spawn in-world attacks, not the in-world attack entity itself.
    /// Although this entity will not be rendered in the game world, it is marked with the dynamic transform usage flags as it will be a child of the player entity.
    /// Requires the <see cref="WeaponAuthoring"/> script to ensure all components required for executing attacks are added to the entity.
    /// Requires the <see cref="EntityRandomAuthoring"/> script for random number generation in <see cref="RadioactiveWasteAttackSystem"/>.
    /// </remarks>
    [RequireComponent(typeof(WeaponAuthoring))]
    [RequireComponent(typeof(EntityRandomAuthoring))]
    public class RadioactiveWasteWeaponAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Radioactive waste drops after the first will spawn at a random position within a range of the player. This value denotes the minimum offset from the player the random position will be selected in.
        /// </summary>
        public float3 MinSpawnOffset;
        /// <summary>
        /// Radioactive waste drops after the first will spawn at a random position within a range of the player. This value denotes the maximum offset from the player the random position will be selected in.
        /// </summary>
        public float3 MaxSpawnOffset;
        
        private class Baker : Baker<RadioactiveWasteWeaponAuthoring>
        {
            public override void Bake(RadioactiveWasteWeaponAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new RadioactiveWasteWeaponData
                {
                    MinSpawnOffset = authoring.MinSpawnOffset,
                    MaxSpawnOffset = authoring.MaxSpawnOffset
                });
            }
        }
    }
    
    /// <summary>
    /// System to handle instantiating <see cref="AttackPrefab"/>s of radioactive waste drops into the game world.
    /// </summary>
    /// <remarks>
    /// Radioactive waste drops are affected by modifications to the player's additional attack projectiles, damage dealt, attack area, and attack duration stat modifications.
    /// The first radioactive waste drop will drop from the top of the screen towards the closest enemy to the player. Subsequent drops will spawn at a random location within a certain distance from the player.
    /// Radioactive waste drops do not deal damage themselves, however they will spawn a radioactive waste spill which has an area of effect damage and will stay in the game world for a certain period of time. As such certain data will be set on the drop entity and certain data will be set on the spill prefab entity to be spawned on destruction.
    /// System executes on the persistent weapon entity responsible for spawning attacks, not the in-world attack entity itself.
    /// This system will only execute on the weapon entity once its <see cref="WeaponState.CooldownTimer"/> expires and has its <see cref="WeaponActiveFlag"/> set to true in the <see cref="WeaponActivationSystem"/>.
    /// </remarks>
    [UpdateInGroup(typeof(DS_AttackSystemGroup))]
    public partial struct RadioactiveWasteAttackSystem : ISystem
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

            foreach (var (weaponState, random, radioactiveWasteData, weaponData, attackPrefab, parent, weaponActive) in SystemAPI.Query<RefRW<WeaponState>, RefRW<EntityRandom>, RadioactiveWasteWeaponData, WeaponLevelData, AttackPrefab, Parent, EnabledRefRW<WeaponActiveFlag>>().WithNone<InitializeEntityRandom>())
            {
                weaponState.ValueRW.NextAttackTimer -= deltaTime;
                if (weaponState.ValueRO.NextAttackTimer > 0f) continue;
                
                var playerEntity = parent.Value;
                var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
                var spawnPosition = playerPosition + math.forward() * 11f;
                var playerCurrentStats = SystemAPI.GetComponent<CharacterStatModificationState>(playerEntity);

                var attackDestinationPosition = float3.zero;
                
                if (weaponState.ValueRO.AttackCount == 0)
                {
                    var pSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
                    var overlapHits = new NativeList<int>(state.WorldUpdateAllocator);
                    var minPosition = playerPosition - cameraHalfExtents;
                    var maxPosition = playerPosition + cameraHalfExtents;
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
                    foreach (var overlapHit in overlapHits)
                    {
                        var otherPosition = pSingleton.Bodies[overlapHit].WorldFromBody.pos;
                        var distanceToPlayerSq = math.distancesq(playerPosition.xz, otherPosition.xz);
                        if (distanceToPlayerSq < maxDistanceSq)
                        {
                            maxDistanceSq = distanceToPlayerSq;
                            attackDestinationPosition = otherPosition;
                        }
                    }
                }
                else
                {
                    var randomMin = playerPosition + radioactiveWasteData.MinSpawnOffset;
                    var randomMax = playerPosition + radioactiveWasteData.MaxSpawnOffset;
                    attackDestinationPosition = random.ValueRW.Value.NextFloat3(randomMin, randomMax);
                }

                var vectorToAttackDestination = attackDestinationPosition.xz - spawnPosition.xz;
                var angle = math.atan2(vectorToAttackDestination.x, vectorToAttackDestination.y);
                var spawnRotation = quaternion.Euler(0f, angle, 0f);
                
                var newRadioactiveWasteDropEntity = ecb.Instantiate(attackPrefab.Value);
                ecb.SetComponent(newRadioactiveWasteDropEntity, LocalTransform.FromPositionRotation(spawnPosition, spawnRotation));
                ecb.SetComponent(newRadioactiveWasteDropEntity, new DestroyAtPosition { TargetPosition = attackDestinationPosition, LastDistanceSq = float.MaxValue });
                ecb.SetComponent(newRadioactiveWasteDropEntity, new LinearMovementSpeed { Value = weaponData.MovementSpeed });
                
                var damageToDeal = (int) math.ceil(weaponData.BaseHitPoints * playerCurrentStats.DamageDealt);
                var attackTimeToLive = weaponData.TimeToLive * playerCurrentStats.AttackDuration;
                var attackArea = weaponData.Area * playerCurrentStats.AttackArea;
                
                var radioactiveWasteSpillPrefab = SystemAPI.GetComponent<SpawnOnDestroy>(attackPrefab.Value).Value;
                ecb.SetComponent(radioactiveWasteSpillPrefab, new DealHitPointsOnInteraction { Value = damageToDeal });
                ecb.SetComponent(radioactiveWasteSpillPrefab, new DestroyAfterTime { Value = attackTimeToLive });
                ecb.SetComponent(radioactiveWasteSpillPrefab, LocalTransform.FromScale(attackArea));
                
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