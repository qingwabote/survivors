using System;
using Unity.Entities;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Component defining the value of a coin item.
    /// </summary>
    /// <seealso cref="GrantMoneyOnInteractionSystem"/>
    public struct GrantMoneyOnInteraction : IComponentData
    {
        /// <summary>
        /// Amount of money granted when the coin item is collected by the player.
        /// </summary>
        public int Value;
    }
    
    /// <summary>
    /// Authoring script to initialize the value of a coin item.
    /// </summary>
    /// <remarks>
    /// Additional authoring scripts required to add components necessary for desired behavior.
    /// </remarks>
    [RequireComponent(typeof(ItemAuthoring))]
    [RequireComponent(typeof(DestroySelfOnInteractionAuthoring))]
    public class GrantMoneyOnInteractionAuthoring : MonoBehaviour
    {
        public int CoinValue;
        
        private class Baker : Baker<GrantMoneyOnInteractionAuthoring>
        {
            public override void Bake(GrantMoneyOnInteractionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new GrantMoneyOnInteraction { Value = authoring.CoinValue });
            }
        }
    }
    
    /// <summary>
    /// System to grant money to the player when the coin item is collected.
    /// </summary>
    /// <remarks>
    /// System is a SystemBase type as it has a System.Action event type as a member variable that is invoked when the player collects a coin.
    /// </remarks>
    /// <seealso cref="GrantMoneyOnInteraction"/>
    /// <seealso cref="EntityInteraction"/>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup))]
    public partial class GrantMoneyOnInteractionSystem : SystemBase
    {
        /// <summary>
        /// Event invoked when the player collects a coin. Used to update the HUD UI with an up to date coin count.
        /// </summary>
        public Action<int> OnUpdateCoinCount;
        
        protected override void OnUpdate()
        {
            foreach (var (interactionBuffer, coinItemProperties) in SystemAPI.Query<DynamicBuffer<EntityInteraction>, GrantMoneyOnInteraction>().WithAll<ItemTag>())
            {
                foreach (var interaction in interactionBuffer)
                {
                    if (interaction.IsHandled) continue;
                    if (!SystemAPI.HasComponent<PlayerTag>(interaction.TargetEntity)) continue;
                    var coinsCollected = SystemAPI.GetSingletonRW<CoinsCollected>();
                    coinsCollected.ValueRW.Value += coinItemProperties.Value;
                    
                    OnUpdateCoinCount?.Invoke(coinsCollected.ValueRO.Value);
                }
            }
        }
    }
}