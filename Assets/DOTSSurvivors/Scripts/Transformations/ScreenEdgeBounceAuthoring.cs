using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Tag component to signify this entity should bounce once it reaches the edge of the screen.
    /// </summary>
    /// <seeaslo cref="ScreenEdgeBounceSystem"/>
    /// <seeaslo cref="ScreenEdgeBounceAuthoring"/>
    public struct ScreenEdgeBounceTag : IComponentData {}
    
    /// <summary>
    /// Authoring script to add <see cref="ScreenEdgeBounceTag"/> to entity.
    /// </summary>
    /// <seeaslo cref="ScreenEdgeBounceSystem"/>
    public class ScreenEdgeBounceAuthoring : MonoBehaviour
    {
        private class Baker : Baker<ScreenEdgeBounceAuthoring>
        {
            public override void Bake(ScreenEdgeBounceAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ScreenEdgeBounceTag>(entity);
            }
        }
    }

    /// <summary>
    /// System to evaluate the position of entities tagged with a <see cref="ScreenEdgeBounceTag"/> and bounce them in a similar effect to a DVD screensaver.
    /// </summary>
    /// <remarks>
    /// System update in the <see cref="DS_TranslationSystemGroup"/> which updates before Unity's TransformSystemGroup so the LocalTransform component can be safely modified.
    /// </remarks>
    [UpdateInGroup(typeof(DS_TranslationSystemGroup))]
    public partial struct ScreenEdgeBounceSystem : ISystem
    {
        /// <summary>
        /// Constant padding value is used to slightly shift the entity away from the screen edge so multiple bounces on a single edge aren't done in quick succession.
        /// </summary>
        private const float BOUNCE_PADDING = 0.15f;
        
        /// <summary>
        /// As a slight optimization, this system will only update when an entity with the <see cref="ScreenEdgeBounceTag"/> actually exists in the game world.
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CameraTarget>();
            state.RequireForUpdate<ScreenEdgeBounceTag>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var cameraReferenceEntity = SystemAPI.GetSingletonEntity<CameraTarget>();
            var cameraPosition = SystemAPI.GetComponent<LocalToWorld>(cameraReferenceEntity).Position;
            var cameraHalfExtents = SystemAPI.GetComponent<CameraTarget>(cameraReferenceEntity).HalfExtents;
            var minPosition = cameraPosition - cameraHalfExtents;
            var maxPosition = cameraPosition + cameraHalfExtents;
            
            foreach (var transform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<ScreenEdgeBounceTag>())
            {
                if (transform.ValueRO.Position.x < minPosition.x)
                {
                    var reflectedDirection = math.reflect(transform.ValueRO.Forward(), math.right());
                    var position = transform.ValueRO.Position;
                    position.x = minPosition.x + BOUNCE_PADDING;
                    transform.ValueRW.Position = position;
                    transform.ValueRW.Rotation = quaternion.LookRotation(reflectedDirection, math.up());
                }
                else if (transform.ValueRO.Position.x > maxPosition.x)
                {
                    var reflectedDirection = math.reflect(transform.ValueRO.Forward(), math.left());
                    var position = transform.ValueRO.Position;
                    position.x = maxPosition.x - BOUNCE_PADDING;
                    transform.ValueRW.Position = position;
                    transform.ValueRW.Rotation = quaternion.LookRotation(reflectedDirection, math.up());
                }
                
                if (transform.ValueRO.Position.z < minPosition.z)
                {
                    var reflectedDirection = math.reflect(transform.ValueRO.Forward(), math.back());
                    var position = transform.ValueRO.Position;
                    position.z = minPosition.z + BOUNCE_PADDING;
                    transform.ValueRW.Position = position;
                    transform.ValueRW.Rotation = quaternion.LookRotation(reflectedDirection, math.up());
                }
                else if (transform.ValueRO.Position.z > maxPosition.z)
                {
                    var reflectedDirection = math.reflect(transform.ValueRO.Forward(), math.forward());
                    var position = transform.ValueRO.Position;
                    position.z = maxPosition.z - BOUNCE_PADDING;
                    transform.ValueRW.Position = position;
                    transform.ValueRW.Rotation = quaternion.LookRotation(reflectedDirection, math.up());
                }
            }
        }
    }
}