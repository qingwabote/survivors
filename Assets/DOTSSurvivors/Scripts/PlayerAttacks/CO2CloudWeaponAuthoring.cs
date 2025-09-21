using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Tag component to signify this entity as the weapon entity to spawn CO2 clouds into the game world.
    /// </summary>
    public struct CO2CloudWeaponTag : IComponentData {}
    
    /// <summary>
    /// Authoring script to add <see cref="CO2CloudWeaponTag"/> to the entity.
    /// </summary>
    /// <remarks>
    /// Note that this script should be added to the weapon entity that will spawn in-world attacks, not the in-world attack entity itself.
    /// Although this entity will not be rendered in the game world, it is marked with the dynamic transform usage flags as it will be a child of the player entity.
    /// Requires the <see cref="WeaponAuthoring"/> script to ensure all components required for executing attacks are added to the entity.
    /// </remarks>
    [RequireComponent(typeof(WeaponAuthoring))]
    public class CO2CloudWeaponAuthoring : MonoBehaviour
    {
        private class Baker : Baker<CO2CloudWeaponAuthoring>
        {
            public override void Bake(CO2CloudWeaponAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<CO2CloudWeaponTag>(entity);
            }
        }
    }

    /// <summary>
    /// System to handle instantiating <see cref="AttackPrefab"/>s of CO2 clouds into the game world.
    /// </summary>
    /// <remarks>
    /// CO2 clouds will exist in the transform hierarchy of the player, making them follow the player's movement for as long as they are active.
    /// CO2 clouds are affected by modifications to the player's attack area and damage dealt stats.
    /// System executes on the persistent weapon entity responsible for spawning attacks, not the in-world attack entity itself.
    /// This system will only execute on the weapon entity once its <see cref="WeaponState.CooldownTimer"/> expires and has its <see cref="WeaponActiveFlag"/> set to true in the <see cref="WeaponActivationSystem"/>.
    /// </remarks>
    [UpdateInGroup(typeof(DS_AttackSystemGroup))]
    public partial struct CO2CloudAttackSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (weaponData, attackPrefab, parent, entity) in SystemAPI.Query<WeaponLevelData, AttackPrefab, Parent>().WithAll<CO2CloudWeaponTag, WeaponActiveFlag>().WithEntityAccess())
            {
                var playerEntity = parent.Value;
                var playerCurrentStats = SystemAPI.GetComponent<CharacterStatModificationState>(playerEntity);
                var attackArea = weaponData.Area * playerCurrentStats.AttackArea;
                var damageToDeal = (int)math.ceil(weaponData.BaseHitPoints * playerCurrentStats.DamageDealt);
                
                var newCO2CloudEntity = ecb.Instantiate(attackPrefab.Value);
                ecb.SetComponent(newCO2CloudEntity, LocalTransform.FromScale(attackArea));
                ecb.SetComponent(newCO2CloudEntity, new Parent { Value = entity });
                ecb.SetComponent(newCO2CloudEntity, new DestroyAfterTime { Value = weaponData.Cooldown });
                ecb.SetComponent(newCO2CloudEntity, new DealHitPointsOnInteraction { Value = damageToDeal });
                SystemAPI.SetComponentEnabled<WeaponActiveFlag>(entity, false);
            }
            
            ecb.Playback(state.EntityManager);
        }
    }
}