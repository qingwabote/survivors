using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Rendering;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data related to experience gems that have a chance to be dropped when an enemy is destroyed.
    /// </summary>
    public struct ExperienceGemItemData : IComponentData
    {
        /// <summary>
        /// Number of experience points to grant the player when the player picks up the gem.
        /// </summary>
        public int Value;
    }
    
    /// <summary>
    /// Flag to initialize the experience gem.
    /// </summary>
    /// <remarks>
    /// Initialization sets the color of the gem based on its value. Value to color mapping is authored in <see cref="GameAuthoring.ExperienceGemColorLookup"/> and stored in <see cref="ManagedGameData.ExperienceGemColorLookup"/>
    /// </remarks>
    public struct InitializeExperienceGemFlag : IComponentData, IEnableableComponent {}
    
    /// <summary>
    /// Authoring script to set initial experience point value for an experience point gem.
    /// </summary>
    [RequireComponent(typeof(ItemAuthoring))]
    [RequireComponent(typeof(GraphicsEntityAuthoring))]
    [RequireComponent(typeof(DestroySelfOnInteractionAuthoring))]
    public class ExperienceGemItemAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Number of experience points to grant the player when the gem is collected.
        /// </summary>
        public int ExperiencePointValue;
        
        private class Baker : Baker<ExperienceGemItemAuthoring>
        {
            public override void Bake(ExperienceGemItemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ExperienceGemItemData
                {
                    Value = authoring.ExperiencePointValue
                });
                AddComponent<InitializeExperienceGemFlag>(entity);
            }
        }
    }

    /// <summary>
    /// System to add player experience points when the player collects an experience point gem.
    /// </summary>
    [UpdateInGroup(typeof(DS_InteractionSystemGroup))]
    public partial struct HandleExperienceGemInteractionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (interactionBuffer, experiencePointItemProperties) in SystemAPI.Query<DynamicBuffer<EntityInteraction>, ExperienceGemItemData>().WithAll<ItemTag>())
            {
                for (var i = 0; i < interactionBuffer.Length; i++)
                {
                    var interaction = interactionBuffer[i];
                    if (interaction.IsHandled) continue;

                    var playerExperienceThisFrame = SystemAPI.GetBuffer<PlayerExperienceThisFrame>(interaction.TargetEntity);
                    playerExperienceThisFrame.Add(new PlayerExperienceThisFrame { Value = experiencePointItemProperties.Value });
                }
            }
        }
    }

    /// <summary>
    /// System to initialize the color of the experience point gem based on its value.
    /// </summary>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup))]
    public partial struct InitializeExperienceGemSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameEntityTag>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            var gameControllerEntity = SystemAPI.GetSingletonEntity<GameEntityTag>();
            var experienceGemColorLookup = state.EntityManager.GetComponentObject<ManagedGameData>(gameControllerEntity).ExperienceGemColorLookup;
            
            foreach (var (graphicsEntity, gemData, shouldInitialize) in SystemAPI.Query<GraphicsEntity, ExperienceGemItemData, EnabledRefRW<InitializeExperienceGemFlag>>())
            {
                var experiencePointColor = new float4(1);
                for (var i = experienceGemColorLookup.Count - 1; i >= 0; i--)
                {
                    if (gemData.Value > experienceGemColorLookup[i].Item1)
                    {
                        experiencePointColor = experienceGemColorLookup[i].Item2;
                        break;
                    }
                }

                if (SystemAPI.HasComponent<URPMaterialPropertyBaseColor>(graphicsEntity.Value))
                {
                    SystemAPI.SetComponent(graphicsEntity.Value, new URPMaterialPropertyBaseColor { Value = experiencePointColor });
                }

                shouldInitialize.ValueRW = false;
            }
        }
    }
}