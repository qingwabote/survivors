using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component to store data related to scale oscillation.
    /// Scale oscillation is when the scale of an entity's LocalTransform is scaled up and down in an sine wave pattern.
    /// </summary>
    public struct ScaleOscillationData : IComponentData
    {
        /// <summary>
        /// Period (frequency) of the sine wave used to calculate scale.
        /// </summary>
        public float Period;
        /// <summary>
        /// Amplitude (height) of the sine wave used to calculate scale.
        /// </summary>
        public float Amplitude;
        /// <summary>
        /// Offset applied along the y-axis to control the midpoint of the sine wave used to calculate scale.
        /// </summary>
        public float YOffset;
    }

    /// <summary>
    /// Timer used to evaluate the sine wave to determine scale at a given time.
    /// </summary>
    public struct ScaleOscillationTimer : IComponentData
    {
        public float Value;
    }
    
    /// <summary>
    /// Authoring script to initialize values on the <see cref="ScaleOscillationData"/> of an entity.
    /// </summary>
    /// <remarks>
    /// Authoring script uses user-friendly data values for authoring and bakes them into runtime friendly data.
    /// </remarks>
    public class ScaleOscillationAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Period (frequency) of the sine wave used to calculate scale.
        /// </summary>
        public float Period;
        /// <summary>
        /// Minimum uniform scale value the entity will be oscillating between.
        /// </summary>
        public float MinScale;
        /// <summary>
        /// Maximum uniform scale value the entity will be oscillating between.
        /// </summary>
        public float MaxScale;

        private class Baker : Baker<ScaleOscillationAuthoring>
        {
            public override void Bake(ScaleOscillationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var amplitude = (authoring.MaxScale - authoring.MinScale) / 2f;
                AddComponent(entity, new ScaleOscillationData
                {
                    Period = authoring.Period,
                    Amplitude = amplitude,
                    YOffset = authoring.MinScale + amplitude
                });
                AddComponent<ScaleOscillationTimer>(entity);
            }
        }
    }

    /// <summary>
    /// System to update the uniform scale value of an entity based on data in <see cref="ScaleOscillationData"/>.
    /// </summary>
    /// <remarks>
    /// System update in the <see cref="DS_TranslationSystemGroup"/> which updates before Unity's TransformSystemGroup so the LocalTransform component can be safely modified.
    /// </remarks>
    /// <seealso cref="ScaleOscillationTimer"/>
    /// <seealso cref="ScaleOscillationAuthoring"/>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct ScaleOscillationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (transform, timer, oscillationData) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<ScaleOscillationTimer>, ScaleOscillationData>())
            {
                timer.ValueRW.Value += deltaTime;
                var newScale = oscillationData.Amplitude * math.sin(oscillationData.Period * timer.ValueRO.Value) + oscillationData.YOffset;
                transform.ValueRW.Scale = newScale;
            }
        }
    }
}