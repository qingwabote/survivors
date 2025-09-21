using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Tag component to signify this entity as the weapon entity to spawn drones into the game world.
    /// </summary>
    public struct DroneWeaponTag : IComponentData {}
    
    /// <summary>
    /// Authoring script to add <see cref="DroneWeaponTag"/> to the entity.
    /// </summary>
    /// <remarks>
    /// Note that this script should be added to the weapon entity that will spawn in-world attacks, not the in-world attack entity itself.
    /// Although this entity will not be rendered in the game world, it is marked with the dynamic transform usage flags as it will be a child of the player entity.
    /// Requires the <see cref="WeaponAuthoring"/> script to ensure all components required for executing attacks are added to the entity.
    /// </remarks>
    [RequireComponent(typeof(WeaponAuthoring))]
    public class DroneWeaponAuthoring : MonoBehaviour
    {
        private class Baker : Baker<DroneWeaponAuthoring>
        {
            public override void Bake(DroneWeaponAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DroneWeaponTag>(entity);
            }
        }
    }

    /// <summary>
    /// System to handle instantiating <see cref="AttackPrefab"/>s of drones into the game world.
    /// </summary>
    /// <remarks>
    /// All drones are spawned at once in a circular pattern around the player under the main weapon entity. This weapon entity will rotate its transform, thus rotating the individual drones around the player. As the drone weapon entity is a child of the player, the drones will follow the player's movement for as long as they are active.
    /// Drones are affected by modifications to the player's additional attack projectiles, damage dealt, attack duration, and attack projectile speed stat modifications.
    /// System executes on the persistent weapon entity responsible for spawning attacks, not the in-world attack entity itself.
    /// This system will only execute on the weapon entity once its <see cref="WeaponState.CooldownTimer"/> expires and has its <see cref="WeaponActiveFlag"/> set to true in the <see cref="WeaponActivationSystem"/>.
    /// This system is unique in that the attack duration time is added to the cooldown time so that the effective cooldown doesn't begin until the attack ends.
    /// </remarks>
    [UpdateInGroup(typeof(DS_AttackSystemGroup))]
    public partial struct DroneAttackSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (weaponState, weaponData, attackPrefab, parent, entity) in SystemAPI.Query<RefRW<WeaponState>, WeaponLevelData, AttackPrefab, Parent>().WithAll<DroneWeaponTag, WeaponActiveFlag>().WithEntityAccess())
            {
                var playerEntity = parent.Value;
                var playerCurrentStats = SystemAPI.GetComponent<CharacterStatModificationState>(playerEntity);
                
                var numberAttacks = weaponData.AttackCount + playerCurrentStats.AdditionalAttackProjectiles;
                var angleBetweenAttacks = math.TAU / numberAttacks;
                
                var attackDuration = weaponData.TimeToLive * playerCurrentStats.AttackDuration;
                var damageToDeal = (int)math.ceil(weaponData.BaseHitPoints * playerCurrentStats.DamageDealt);
                var attackSpeed = weaponData.MovementSpeed * playerCurrentStats.AttackProjectileSpeed;
                
                for (var i = 0; i < numberAttacks; i++)
                {
                    var newDroneEntity = ecb.Instantiate(attackPrefab.Value);
                    ecb.SetComponent(newDroneEntity, new Parent { Value = entity });
                    ecb.SetComponent(newDroneEntity, LocalTransform.FromRotation(quaternion.Euler(0f, i * angleBetweenAttacks, 0f)));
                    ecb.SetComponent(newDroneEntity, new DestroyAfterTime { Value = attackDuration });
                    ecb.SetComponent(newDroneEntity, new DealHitPointsOnInteraction { Value = damageToDeal });
                    ecb.SetComponent(newDroneEntity, new ConstantRotationData
                    {
                        EulerRadiansPerSecond = new float3(0f, attackSpeed, 0f), 
                        RotationOrder = math.RotationOrder.Default
                    });
                }

                weaponState.ValueRW.CooldownTimer += weaponData.TimeToLive;
                SystemAPI.SetComponentEnabled<WeaponActiveFlag>(entity, false);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}