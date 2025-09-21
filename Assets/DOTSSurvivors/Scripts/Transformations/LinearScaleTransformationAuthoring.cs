using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component to store data related to linear scale transformation. Linear scale transformation is when the scale of an entity changes from the start scale to the end scale in the duration listed.
    /// </summary>
    /// <seealso cref="LinearScaleTransformationEndTimestamp"/>
    /// <seealso cref="LinearScaleTransformationSystem"/>
    /// <seealso cref="LinearScaleTransformationAuthoring"/>
    public struct LinearScaleTransformationData : IComponentData
    {
        /// <summary>
        /// Duration the scale transformation will take to complete in seconds.
        /// </summary>
        public float Duration;
        /// <summary>
        /// Starting uniform scale value.
        /// </summary>
        public float StartScale;
        /// <summary>
        /// End uniform scale value.
        /// </summary>
        public float EndScale;
    }

    /// <summary>
    /// Data component to hold the end timestamp of when the linear scale transformation will end.
    /// </summary>
    /// <seealso cref="LinearScaleTransformationData"/>
    /// <seealso cref="LinearScaleTransformationSystem"/>
    public struct LinearScaleTransformationEndTimestamp : IComponentData, IEnableableComponent
    {
        public float Value;
    }
    
    /// <summary>
    /// Authoring script to add <see cref="LinearScaleTransformationData"/> to an entity.
    /// </summary>
    /// <remarks>
    /// Note that this does not add the <see cref="LinearScaleTransformationEndTimestamp"/> component which is also required to perform the transformation. This will be initialized and added in the <see cref="LinearScaleTransformationSystem"/>
    /// </remarks>
    public class LinearScaleTransformationAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Duration the scale transformation will take to complete in seconds.
        /// </summary>
        public float Duration;
        /// <summary>
        /// Starting uniform scale value.
        /// </summary>
        public float StartScale;
        /// <summary>
        /// End uniform scale value.
        /// </summary>
        public float EndScale;

        private class Baker : Baker<LinearScaleTransformationAuthoring>
        {
            public override void Bake(LinearScaleTransformationAuthoring transformationAuthoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new LinearScaleTransformationData
                {
                    Duration = transformationAuthoring.Duration,
                    StartScale = transformationAuthoring.StartScale,
                    EndScale = transformationAuthoring.EndScale
                });
            }
        }
    }

    /// <summary>
    /// System to perform the linear transformation from the data defined in <see cref="LinearScaleTransformationData"/>.
    /// </summary>
    /// <remarks>
    /// This system will first initialize and add the <see cref="LinearScaleTransformationEndTimestamp"/> component as that is required to calculate the scale transformation.
    /// </remarks>
    /// <seealso cref="LinearScaleTransformationEndTimestamp"/>
    /// <seealso cref="LinearScaleTransformationAuthoring"/>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct LinearScaleTransformationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var elapsedTime = (float)SystemAPI.Time.ElapsedTime;

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            
            foreach (var (scaleData, entity) in SystemAPI.Query<LinearScaleTransformationData>().WithAbsent<LinearScaleTransformationEndTimestamp>().WithEntityAccess())
            {
                ecb.AddComponent(entity, new LinearScaleTransformationEndTimestamp
                {
                    Value = elapsedTime + scaleData.Duration
                });
            }

            ecb.Playback(state.EntityManager);
            
            foreach (var (transform, scaleData, endTimestamp, shouldChangeScale) in SystemAPI.Query<RefRW<LocalTransform>, LinearScaleTransformationData, LinearScaleTransformationEndTimestamp, EnabledRefRW<LinearScaleTransformationEndTimestamp>>())
            {
                var timeRemaining = endTimestamp.Value - elapsedTime;
                if (timeRemaining <= 0f)
                {
                    transform.ValueRW.Scale = scaleData.EndScale;
                    shouldChangeScale.ValueRW = false;
                    continue;
                }

                var t = 1 - timeRemaining / scaleData.Duration;
                var curScale = math.lerp(scaleData.StartScale, scaleData.EndScale, t);
                transform.ValueRW.Scale = curScale;
            }
        }
    }
}