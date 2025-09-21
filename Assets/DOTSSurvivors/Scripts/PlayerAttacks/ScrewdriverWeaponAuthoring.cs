using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Tag component to signify this entity as the weapon entity to spawn screwdrivers into the game world.
    /// </summary>
    public struct ScrewdriverWeaponTag : IComponentData {}
    
    /// <summary>
    /// Authoring script to add <see cref="ScrewdriverWeaponTag"/> to the entity.
    /// </summary>
    /// <remarks>
    /// Note that this script should be added to the weapon entity that will spawn in-world attacks, not the in-world attack entity itself.
    /// Although this entity will not be rendered in the game world, it is marked with the dynamic transform usage flags as it will be a child of the player entity.
    /// Requires the <see cref="WeaponAuthoring"/> script to ensure all components required for executing attacks are added to the entity.
    /// </remarks>
    [RequireComponent(typeof(WeaponAuthoring))]
    public class ScrewdriverWeaponAuthoring : MonoBehaviour
    {
        private class Baker : Baker<ScrewdriverWeaponAuthoring>
        {
            public override void Bake(ScrewdriverWeaponAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ScrewdriverWeaponTag>(entity);
            }
        }
    }

    /// <summary>
    /// System to handle instantiating <see cref="AttackPrefab"/>s of  into the game world.
    /// </summary>
    /// <remarks>
    /// Screwdrivers are affected by modifications to the player's additional attack projectiles, damage dealt, attack speed, and attack duration stat modifications.
    /// Screwdrivers shoot in the most recent, non-zero directional input from the player.
    /// System executes on the persistent weapon entity responsible for spawning attacks, not the in-world attack entity itself.
    /// This system will only execute on the weapon entity once its <see cref="WeaponState.CooldownTimer"/> expires and has its <see cref="WeaponActiveFlag"/> set to true in the <see cref="WeaponActivationSystem"/>.
    /// </remarks>
    [UpdateInGroup(typeof(DS_AttackSystemGroup))]
    public partial struct ScrewdriverAttackSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (weaponState, weaponData, attackPrefab, parent, weaponActive) in SystemAPI.Query<RefRW<WeaponState>, WeaponLevelData, AttackPrefab, Parent, EnabledRefRW<WeaponActiveFlag>>().WithAll<ScrewdriverWeaponTag>())
            {
                weaponState.ValueRW.NextAttackTimer -= deltaTime;
                if (weaponState.ValueRO.NextAttackTimer > 0f) continue;

                var playerEntity = parent.Value;
                var playerCurrentStats = SystemAPI.GetComponent<CharacterStatModificationState>(playerEntity);
                var spawnPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;

                var playerFacingDirection = SystemAPI.GetComponent<PreviousPlayerInput>(playerEntity).LastPositiveInput;
                var angle = math.atan2(playerFacingDirection.x, playerFacingDirection.y);
                var spawnRotation = quaternion.Euler(0f, angle, 0f);

                var attackTimeToLive = weaponData.TimeToLive * playerCurrentStats.AttackDuration;
                var damageToDeal = (int)math.ceil(weaponData.BaseHitPoints * playerCurrentStats.DamageDealt);
                var attackSpeed = weaponData.MovementSpeed * playerCurrentStats.AttackProjectileSpeed;

                var newScrewdriverEntity = ecb.Instantiate(attackPrefab.Value);
                ecb.SetComponent(newScrewdriverEntity, LocalTransform.FromPositionRotation(spawnPosition, spawnRotation));
                ecb.SetComponent(newScrewdriverEntity, new DestroyAfterTime { Value = attackTimeToLive });
                ecb.SetComponent(newScrewdriverEntity, new DestroyAfterNumberHits { HitsRemaining = weaponData.MaxEnemyHitCount });
                ecb.SetComponent(newScrewdriverEntity, new DealHitPointsOnInteraction { Value = damageToDeal });
                ecb.SetComponent(newScrewdriverEntity, new LinearMovementSpeed { Value = attackSpeed });

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