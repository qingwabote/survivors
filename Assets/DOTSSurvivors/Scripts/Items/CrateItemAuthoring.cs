using System;
using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Tag component to identify crate entities.
    /// </summary>
    /// <remarks>
    /// Crate entities have a chance to be dropped by boss enemies for a spawn wave. When a crate is picked up by the player a sequence will play and grant the player an upgrade to a random weapon or passive ability they already have selected. If all weapons or passive items the player currently has are fully upgraded, additional money will be granted.
    /// </remarks>
    public struct CrateItemTag : IComponentData {}
    
    /// <summary>
    /// Authoring script for crate entity.
    /// </summary>
    /// <remarks>
    /// Additional authoring scripts required to add components necessary for desired behavior.
    /// </remarks>
    [RequireComponent(typeof(ItemAuthoring))]
    [RequireComponent(typeof(DestroySelfOnInteractionAuthoring))]
    public class CrateItemAuthoring : MonoBehaviour
    {
        private class Baker : Baker<CrateItemAuthoring>
        {
            public override void Bake(CrateItemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<CrateItemTag>(entity);
            }
        }
    }

    /// <summary>
    /// System containing logic for when the player collects a crate.
    /// </summary>
    /// <remarks>
    /// System is a SystemBase type as it has a System.Action event type as a member variable that is invoked when the player collects a crate.
    /// This system first determines which upgrade will be granted to the player. If all the player's current weapons and passive abilities are already at the max level, additional money will be granted to the player.
    /// </remarks>
    /// <seealso cref="CrateUIController"/>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup))]
    public partial class HandleCrateItemInteractionSystem : SystemBase
    {
        /// <summary>
        /// Event invoked when the player collects a crate. Informs <see cref="CrateUIController"/> to begin the crate opening sequence.
        /// </summary>
        public Action<CapabilityUpgradeLevel> OnBeginOpenCrate;
        
        /// <summary>
        /// Time in seconds the player will be invincible for after the crate opening dialog window is closed and gameplay is resumed.
        /// </summary>
        private const float INVINCIBILITY_DURATION = 0.15f;
        
        protected override void OnCreate()
        {
            RequireForUpdate<GameEntityTag>();
            RequireForUpdate<CrateItemTag>();

            EntityManager.AddComponent<EntityRandom>(SystemHandle);
            EntityManager.AddComponentData(SystemHandle, new InitializeEntityRandom
            {
                InitializationType = EntityRandomInitializationType.SystemMilliseconds
            });
        }
        
        protected override void OnUpdate()
        {
            var gameEntity = SystemAPI.GetSingletonEntity<GameEntityTag>();
            
            foreach (var interactionBuffer in SystemAPI.Query<DynamicBuffer<EntityInteraction>>().WithAll<CrateItemTag>())
            {
                foreach (var interaction in interactionBuffer)
                {
                    if (interaction.IsHandled) continue;
                    if (!SystemAPI.HasComponent<PlayerTag>(interaction.TargetEntity)) continue;

                    var upgradeData = EntityManager.GetComponentObject<PlayerUpgradeData>(gameEntity);

                    var nonMaxedUpgradeProperties = new List<CapabilityUpgradeLevel>();
                    
                    foreach (var weaponEntityPropertyPair in upgradeData.ActiveWeaponEntityLookup)
                    {
                        var weaponState = SystemAPI.GetComponent<WeaponState>(weaponEntityPropertyPair.Value);
                        if (weaponState.LevelIndex >= weaponEntityPropertyPair.Key.MaxLevelIndex) continue;
                        nonMaxedUpgradeProperties.Add(new CapabilityUpgradeLevel
                        {
                            UpgradeProperties = weaponEntityPropertyPair.Key,
                            NextLevelIndex = weaponState.LevelIndex + 1
                        });
                    }

                    foreach (var passiveUpgradeEntityPair in upgradeData.ActivePassiveEntityLookup)
                    {
                        var curPassiveLevelIndex = SystemAPI.GetComponent<PassiveLevelIndex>(passiveUpgradeEntityPair.Value).Value;
                        if (curPassiveLevelIndex >= passiveUpgradeEntityPair.Key.MaxLevelIndex) continue;
                        nonMaxedUpgradeProperties.Add(new CapabilityUpgradeLevel
                        {
                            UpgradeProperties = passiveUpgradeEntityPair.Key,
                            NextLevelIndex = curPassiveLevelIndex + 1
                        });
                    }

                    CapabilityUpgradeLevel randomUpgrade = default;
                    if (nonMaxedUpgradeProperties.Count > 0)
                    {
                        var random = SystemAPI.GetComponentRW<EntityRandom>(SystemHandle);
                        var randomIndex = random.ValueRW.Value.NextInt(nonMaxedUpgradeProperties.Count);
                        randomUpgrade = nonMaxedUpgradeProperties[randomIndex];
                    }
                    
                    OnBeginOpenCrate?.Invoke(randomUpgrade);

                    var invincibilityExpirationTimestamp = SystemAPI.GetComponentRW<InvincibilityExpirationTimestamp>(interaction.TargetEntity);
                    invincibilityExpirationTimestamp.ValueRW.Value = SystemAPI.Time.ElapsedTime + INVINCIBILITY_DURATION;
                    SystemAPI.SetComponentEnabled<InvincibilityExpirationTimestamp>(interaction.TargetEntity, true);
                }
            }
        }
    }
}