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
    /// Data component containing data related to the laser strike weapon attack.
    /// </summary>
    public struct LaserStrikeWeaponData : IComponentData
    {
        /// <summary>
        /// This value is used when determining a random enemy to strike in the <see cref="LaserStrikeAttackSystem"/> as a way to select targets inside the camera bounds plus some additional inner padding. This is so laser strikes occur fully on screen and not cut off by screen edges.
        /// </summary>
        public float InnerPadding;
    }

    /// <summary>
    /// Dynamic buffer to store entities that have already been struck by the current group of laser strikes.
    /// </summary>
    /// <remarks>
    /// This collection is used to determine if a randomly selected enemy has already been struck so that it does not strike an enemy twice in a single group of attacks.
    /// As the laser strike weapon entity sits alone it its own chunk so a large internal buffer capacity can be allocated without issue. This buffer is larger than the theoretical worst case.
    /// </remarks>
    [InternalBufferCapacity(16)]
    public struct AlreadyStruckEntity : IBufferElementData
    {
        public Entity Value;
    }
    
    /// <summary>
    /// Authoring script to add components necessary for executing laser strike attacks.
    /// </summary>
    /// <remarks>
    /// Note that this script should be added to the weapon entity that will spawn in-world attacks, not the in-world attack entity itself.
    /// Although this entity will not be rendered in the game world, it is marked with the dynamic transform usage flags as it will be a child of the player entity.
    /// Requires the <see cref="WeaponAuthoring"/> script to ensure all components required for executing attacks are added to the entity.
    /// Requires the <see cref="EntityRandomAuthoring"/> script for random number generation in <see cref="JetpackAttackSystem"/>.
    /// </remarks>
    [RequireComponent(typeof(WeaponAuthoring))]
    [RequireComponent(typeof(EntityRandomAuthoring))]
    public class LaserStrikeWeaponAuthoring : MonoBehaviour
    {
        /// <summary>
        /// This value is used when determining a random enemy to strike in the <see cref="LaserStrikeAttackSystem"/> as a way to select targets inside the camera bounds plus some additional inner padding. This is so laser strikes occur fully on screen and not cut off by screen edges.
        /// </summary>
        public float InnerPadding;
        
        private class Baker : Baker<LaserStrikeWeaponAuthoring>
        {
            public override void Bake(LaserStrikeWeaponAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new LaserStrikeWeaponData { InnerPadding = authoring.InnerPadding });
                AddBuffer<AlreadyStruckEntity>(entity);
            }
        }
    }

    /// <summary>
    /// System to handle instantiating <see cref="AttackPrefab"/>s of laser strikes into the game world.
    /// </summary>
    /// <remarks>
    /// Laser strikes are affected by modifications to the player's additional attack projectiles, damage dealt, and attack area stat modifications.
    /// Laser strikes have an area of effect damaging radius indicated by the circular shaped strike at the bottom of the attack. The vertical laser column is for visual purposes only.
    /// System will target a random entity on screen and strike it with a laser. If the randomly selected enemy has already been hit with a laser during a group of laser strikes, a new random enemy will be selected.
    /// System executes on the persistent weapon entity responsible for spawning attacks, not the in-world attack entity itself.
    /// This system will only execute on the weapon entity once its <see cref="WeaponState.CooldownTimer"/> expires and has its <see cref="WeaponActiveFlag"/> set to true in the <see cref="WeaponActivationSystem"/>.
    /// The idiomatic foreach in this system is unique as it has a large number of component types in the query - it has 7 which is the limit for these types of queries. As such, rather than using EnabledRefRW with type WeaponActiveFlag as I do in many other attack systems, I've moved the WeaponActiveFlag to the WithAll portion of the query, included WithEntityAccess, then disable the component using SystemAPI at the end of the foreach loop.
    /// </remarks>
    [UpdateInGroup(typeof(DS_AttackSystemGroup))]
    public partial struct LaserStrikeAttackSystem : ISystem
    {
        /// <summary>
        /// This system will randomly select enemies on screen. The <see cref="AlreadyStruckEntity"/> buffer is used to ensure the randomly selected entity has not already been struck by this group of attacks. This constant value is used as a fallback in case there are few enemies on screen, in which case it is acceptable for an enemy to get struck multiple times.
        /// </summary>
        private const int MAX_SELECT_RANDOM_ENEMY_COUNT = 50;
        
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

            foreach (var (weaponState, random, laserData, weaponData, attackPrefab, alreadyStruckEntities, parent, entity) in SystemAPI.Query<RefRW<WeaponState>, RefRW<EntityRandom>, LaserStrikeWeaponData, WeaponLevelData, AttackPrefab, DynamicBuffer<AlreadyStruckEntity>, Parent>().WithAll<WeaponActiveFlag>().WithNone<InitializeEntityRandom>().WithEntityAccess())
            {
                weaponState.ValueRW.NextAttackTimer -= deltaTime;
                if (weaponState.ValueRO.NextAttackTimer > 0f) continue;

                var playerEntity = parent.Value;
                var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
                var playerCurrentStats = SystemAPI.GetComponent<CharacterStatModificationState>(playerEntity);
                
                var pSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
                var overlapHits = new NativeList<int>(state.WorldUpdateAllocator);
                var innerPadding = new float3(laserData.InnerPadding, 0f, laserData.InnerPadding);
                var minPosition = playerPosition - cameraHalfExtents + innerPadding;
                var maxPosition = playerPosition + cameraHalfExtents - innerPadding;
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

                var getNewRandomEnemy = true;
                var randomEnemy = Entity.Null;
                var getRandomEnemyCount = 0;
                while (getNewRandomEnemy && getRandomEnemyCount < MAX_SELECT_RANDOM_ENEMY_COUNT)
                {
                    getNewRandomEnemy = false;
                    getRandomEnemyCount += 1;
                    
                    var randomIndex = random.ValueRW.Value.NextInt(overlapHits.Length);
                    var randomBodyIndex = overlapHits[randomIndex];
                    randomEnemy = pSingleton.Bodies[randomBodyIndex].Entity;
                    if (!SystemAPI.Exists(randomEnemy)) continue;
                    
                    foreach (var alreadyStruckEntity in alreadyStruckEntities)
                    {
                        if (alreadyStruckEntity.Value == randomEnemy)
                        {
                            getNewRandomEnemy = true;
                            break;
                        }
                    }
                }
                
                if (!SystemAPI.Exists(randomEnemy) || !SystemAPI.HasComponent<LocalTransform>(randomEnemy)) return;
                alreadyStruckEntities.Add(new AlreadyStruckEntity { Value = randomEnemy });

                var randomEnemyPosition = SystemAPI.GetComponent<LocalTransform>(randomEnemy).Position;
                var attackArea = weaponData.Area * playerCurrentStats.AttackArea;
                var damageToDeal = (int)math.ceil(weaponData.BaseHitPoints * playerCurrentStats.DamageDealt);
                
                var newLaserStrikeEntity = ecb.Instantiate(attackPrefab.Value);
                ecb.SetComponent(newLaserStrikeEntity, LocalTransform.FromPositionRotationScale(randomEnemyPosition, quaternion.identity, attackArea));
                ecb.SetComponent(newLaserStrikeEntity, new DealHitPointsOnInteraction { Value = damageToDeal });
                ecb.SetComponent(newLaserStrikeEntity, new DestroyAfterTime { Value = weaponData.TimeToLive });
                    
                weaponState.ValueRW.NextAttackTimer = weaponData.IntervalBetweenAttacks;
                weaponState.ValueRW.AttackCount += 1;
                var numberAttacks = weaponData.AttackCount + playerCurrentStats.AdditionalAttackProjectiles;
                if (weaponState.ValueRO.AttackCount < numberAttacks) continue;
                
                alreadyStruckEntities.Clear();
                weaponState.ValueRW.NextAttackTimer = 0f;
                weaponState.ValueRW.AttackCount = 0;

                SystemAPI.SetComponentEnabled<WeaponActiveFlag>(entity, false);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}