using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Burst;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Component to hold data related to visual effect that flashes the graphics entity of a character when taking damage.
    /// </summary>
    /// <remarks>
    /// This component should go on the graphics entity of the character.
    /// Enableable component so the <see cref="FlashColorOnDamageSystem"/> will only run on entities with this component enabled.
    /// </remarks>
    /// <seealso cref="FlashColorOnDamageTimer"/>
    /// <seealso cref="FlashColorOnDamageSystem"/>
    public struct FlashColorOnDamageData : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// Color the entity will be tinted when taking damage.
        /// </summary>
        /// <remarks>
        /// This is actually the 2nd color that will flash. For the first half of the effect the entity will be all black.
        /// </remarks>
        public float4 Color;
        /// <summary>
        /// Total time of the flash effect in seconds.
        /// </summary>
        /// <remarks>
        /// The first half of this time, the entity will be all black. The second half the entity will be tinted with the color defined in the Color field.
        /// </remarks>
        public float FlashTime;
    }

    /// <summary>
    /// Timer used to display the flash effect for a certain period of time. This timer will count down from <see cref="FlashColorOnDamageData.FlashTime"/> to zero.
    /// </summary>
    /// <seealso cref="FlashColorOnDamageSystem"/>
    public struct FlashColorOnDamageTimer : IComponentData
    {
        public float Value;
    }
    
    /// <summary>
    /// Authoring script to initialize values for the <see cref="FlashColorOnDamageData"/> component and add other necessary components.
    /// </summary>
    /// <remarks>
    /// Note that this authoring script should be attached to the graphics entity which is a child of the character entity.
    /// </remarks>
    public class FlashColorOnDamageAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Color the entity will be tinted when taking damage.
        /// </summary>
        /// <remarks>
        /// This is actually the 2nd color that will flash. For the first half of the effect the entity will be all black.
        /// </remarks>
        public Color Color;
        /// <summary>
        /// Total time of the flash effect in seconds.
        /// </summary>
        /// <remarks>
        /// The first half of this time, the entity will be all black. The second half the entity will be tinted with the color defined in the Color field.
        /// </remarks>
        public float FlashTime;

        private class Baker : Baker<FlashColorOnDamageAuthoring>
        {
            public override void Bake(FlashColorOnDamageAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new FlashColorOnDamageData
                {
                    Color = (Vector4)authoring.Color,
                    FlashTime = authoring.FlashTime
                });
                SetComponentEnabled<FlashColorOnDamageData>(entity, false);
                AddComponent<FlashColorOnDamageTimer>(entity);
                AddComponent(entity, new URPMaterialPropertyBaseColor { Value = new float4(1) });
            }
        }
    }

    /// <summary>
    /// This system will flash the graphics entity black and then a color defined in <see cref="FlashColorOnDamageData.Color"/> when the parent character entity takes damage. This provides a nice visual feedback for the player when an entity takes damage.
    /// </summary>
    /// <remarks>
    /// This system updates in the <see cref="DS_EffectsSystemGroup"/> which updates towards the end of the frame.
    /// </remarks>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct FlashColorOnDamageSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            
            foreach (var (timer, baseColorProperty, flashColorOnDamage, shouldFlash) in SystemAPI.Query<RefRW<FlashColorOnDamageTimer>, RefRW<URPMaterialPropertyBaseColor>, FlashColorOnDamageData, EnabledRefRW<FlashColorOnDamageData>>())
            {
                timer.ValueRW.Value -= deltaTime;
                // First half of the flash duration, set the color of the sprite to all black
                if (timer.ValueRO.Value > flashColorOnDamage.FlashTime / 2f)
                {
                    baseColorProperty.ValueRW.Value = new float4(0, 0, 0, 1);
                }
                // 2nd half of the flash duration, apply a tint of the color specified (i.e. red)
                else if (timer.ValueRO.Value > 0f)
                {
                    baseColorProperty.ValueRW.Value = flashColorOnDamage.Color;
                }
                // Reset to normal sprite look
                else
                {
                    baseColorProperty.ValueRW.Value = new float4(1);
                    shouldFlash.ValueRW = false;
                }
            }
        }
    }
}