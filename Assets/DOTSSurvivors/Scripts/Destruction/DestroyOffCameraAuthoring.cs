using System;
using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Byte flag definition for camera direction.
    /// </summary>
    /// <seealso cref="DestroyOffCameraData"/>
    /// <seealso cref="DestroyOffCameraSystem"/>
    [Flags]
    public enum CameraDirection : byte
    {
        None = 0,
        Up = 1,
        Right = 2,
        Down = 4,
        Left = 8
    }
    
    /// <summary>
    /// Entities with this component will be destroyed when the entity has left the camera bounds on one (or more) of the specified sides.
    /// </summary>
    /// <seealso cref="DestroyOffCameraSystem"/>
    /// <seealso cref="DestroyOffCameraAuthoring"/>
    /// <seealso cref="CameraDirection"/>
    public struct DestroyOffCameraData : IComponentData
    {
        /// <summary>
        /// Byte flag for defining which directions offscreen should be tested when determining if the entity should be destroyed.
        /// </summary>
        /// <remarks>
        /// Example - if Up bit is not set, but down bit is set, then the entity will not be destroyed if the entity goes upwards offscreen, but will be destroyed if the entity goes downwards offscreen.
        /// </remarks>
        /// <seealso cref="CameraDirection"/>
        public CameraDirection Direction;
        
        /// <summary>
        /// Additional padding applied to testing if the entity is offscreen. An entity will be considered offscreen when its world transform position is outside the screen bounds plus the padding value.
        /// </summary>
        /// <remarks>
        /// Used so that the entity isn't destroyed when just its origin is offscreen, but some graphics are still on screen.
        /// </remarks>
        public float Padding;
    }
    
    /// <summary>
    /// Authoring script to initialize values for <see cref="DestroyOffCameraData"/>
    /// </summary>
    /// <seealso cref="DestroyOffCameraSystem"/>
    /// <seealso cref="DestroyOffCameraData"/>
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    public class DestroyOffCameraAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Byte flag for defining which directions off screen should be tested when determining if the entity should be destroyed.
        /// </summary>
        /// <remarks>
        /// Example - if Up bit is not set, but down bit is set, then the entity will not be destroyed if the entity goes upwards offscreen, but will be destroyed if the entity goes downwards offscreen.
        /// </remarks>
        /// <seealso cref="CameraDirection"/>
        public CameraDirection Direction;
        
        /// <summary>
        /// Additional padding applied to testing if the entity is offscreen. Used so that the entity isn't destroyed when just its origin is offscreen, but some graphics are still on screen.
        /// </summary>
        public float Padding;

        private class Baker : Baker<DestroyOffCameraAuthoring>
        {
            public override void Bake(DestroyOffCameraAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new DestroyOffCameraData
                {
                    Direction = authoring.Direction,
                    Padding = authoring.Padding
                });
            }
        }
    }

    /// <summary>
    /// System that will enable the <ref cref="DestroyEntityFlag"/> to destroy the entity at the end of the frame if the entity is offscreen. <see cref="DestroyOffCameraData.Direction"/> can be used to specify the directions off camera the entity will be cleaned up.
    /// </summary>
    /// <remarks>
    /// Example - if Up bit is not set, but down bit is set, then the entity will not be destroyed if the entity goes upwards offscreen, but will be destroyed if the entity goes downwards offscreen.
    /// </remarks>
    /// <seealso cref="DestroyOffCameraData"/>
    /// <seealso cref="DestroyOffCameraAuthoring"/>
    [UpdateInGroup(typeof(DS_DestructionSystemGroup))]
    public partial struct DestroyOffCameraSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CameraTarget>();
            state.RequireForUpdate<DestroyOffCameraData>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var cameraReferenceEntity = SystemAPI.GetSingletonEntity<CameraTarget>();
            var cameraPosition = SystemAPI.GetComponent<LocalToWorld>(cameraReferenceEntity).Position;
            var cameraHalfExtents = SystemAPI.GetComponent<CameraTarget>(cameraReferenceEntity).HalfExtents;
            var minPosition = cameraPosition - cameraHalfExtents;
            var maxPosition = cameraPosition + cameraHalfExtents;

            foreach (var (destroyOffCamera, localToWorld, entity) in SystemAPI.Query<DestroyOffCameraData, LocalToWorld>().WithEntityAccess())
            {
                if ((destroyOffCamera.Direction & CameraDirection.Up) != 0 && localToWorld.Position.z > maxPosition.z + destroyOffCamera.Padding)
                {
                    SystemAPI.SetComponentEnabled<DestroyEntityFlag>(entity, true);
                }
                if ((destroyOffCamera.Direction & CameraDirection.Right) != 0 && localToWorld.Position.x > maxPosition.x + destroyOffCamera.Padding)
                {
                    SystemAPI.SetComponentEnabled<DestroyEntityFlag>(entity, true);
                }
                if ((destroyOffCamera.Direction & CameraDirection.Down) != 0 && localToWorld.Position.z < minPosition.z - destroyOffCamera.Padding)
                {
                    SystemAPI.SetComponentEnabled<DestroyEntityFlag>(entity, true);
                }
                if ((destroyOffCamera.Direction & CameraDirection.Left) != 0 && localToWorld.Position.x < minPosition.x - destroyOffCamera.Padding)
                {
                    SystemAPI.SetComponentEnabled<DestroyEntityFlag>(entity, true);
                }
            }
        }
    }
}