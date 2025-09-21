using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Component to hold data related to displaying damage numbers above characters when taking damage.
    /// </summary>
    /// <remarks>
    /// Enableable component - when enabled it signifies that this entity should display a damage number above it.
    /// </remarks>
    public struct ShowDamageNumberOnDamage : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// Damage taken this frame. Number to be shown in world UI.
        /// </summary>
        public int DamageThisFrame;
        /// <summary>
        /// The initial offset of the first damage number so it displays properly above the character
        /// </summary>
        public float3 BaseOffset;
    }
    
    /// <summary>
    /// Authoring script to initialize components necessary for displaying damage numbers above the entity.
    /// </summary>
    /// <remarks>
    /// Authoring script should be attached to the base character entity.
    /// </remarks>
    public class ShowDamageNumberOnDamageAuthoring : MonoBehaviour
    {
        /// <summary>
        /// The initial offset of the first damage number so it displays properly above the character
        /// </summary>
        public Vector3 BaseOffset;
        
        private class Baker : Baker<ShowDamageNumberOnDamageAuthoring>
        {
            public override void Bake(ShowDamageNumberOnDamageAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ShowDamageNumberOnDamage
                {
                    BaseOffset = authoring.BaseOffset
                });
                SetComponentEnabled<ShowDamageNumberOnDamage>(entity, false);
            }
        }
    }

    /// <summary>
    /// System to display damage numbers above entities when taking damage. Only will run on entities with their <see cref="ShowDamageNumberOnDamage"/> component enabled.
    /// </summary>
    /// <remarks>
    /// This system updates in the <see cref="DS_EffectsSystemGroup"/> which updates towards the end of the frame. As such base transform position comes from the entity's LocalToWorld component which will have been updated by this point in the frame.
    /// </remarks>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct ShowDamageNumberOnDamageSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (damageNumberProperties, transform, showDamageNumber) in SystemAPI.Query<ShowDamageNumberOnDamage, LocalToWorld, EnabledRefRW<ShowDamageNumberOnDamage>>())
            {
                var startPosition = transform.Position + damageNumberProperties.BaseOffset;
                WorldUICanvasController.Instance.DisplayDamageNumber(damageNumberProperties.DamageThisFrame, startPosition);
                showDamageNumber.ValueRW = false;
            }
        }
    }
}