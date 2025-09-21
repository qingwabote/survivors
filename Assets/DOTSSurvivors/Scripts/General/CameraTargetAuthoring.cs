using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Component used to initialize the <see cref="CameraTarget"/> component attached to the player entity.
    /// </summary>
    /// <remarks>
    /// In the <see cref="InitializeCameraTargetSystem"/> this component will be used to locate the <see cref="CameraTargetObject"/> and assign values in the <see cref="CameraTarget"/> component attached to the player entity.
    /// </remarks>
    public struct InitializeCameraTarget : IComponentData
    {
        /// <summary>
        /// Units along the Y-axis the camera will be offset to ensure all entities are in view.
        /// </summary>
        public float YOffset;
    }

    /// <summary>
    /// Component attached to the player entity. Used for systems that need information about the camera.
    /// </summary>
    /// <seealso cref="InitializeCameraTarget"/>
    /// <seealso cref="InitializeCameraTargetSystem"/>
    /// <seealso cref="MoveCameraSystem"/>
    public struct CameraTarget : IComponentData
    {
        public UnityObjectRef<Transform> Transform;
        public float3 HalfExtents;
        public float YOffset;
    }
        
    /// <summary>
    /// Authoring script to initialize the <see cref="InitializeCameraTarget"/> component on the player entity.
    /// </summary>
    public class CameraTargetAuthoring : MonoBehaviour
    {
        public float YOffset;
        
        private class Baker : Baker<CameraTargetAuthoring>
        {
            public override void Bake(CameraTargetAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new InitializeCameraTarget { YOffset = authoring.YOffset });
            }
        }
    }

    /// <summary>
    /// System to initialize data for the <see cref="CameraTarget"/> by locating the <see cref="CameraTargetObject"/> and using data from <see cref="InitializeCameraTarget"/>.
    /// </summary>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup))]
    public partial struct InitializeCameraTargetSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InitializeCameraTarget>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            if(CameraTargetObject.Instance == null || Camera.main == null) return;
            var cameraTargetTransform = CameraTargetObject.Instance.transform;
            var cameraVertical = Camera.main.orthographicSize;
            var cameraHorizontal = cameraVertical * Camera.main.aspect;
            
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            
            foreach (var (initializeCameraTarget, entity) in SystemAPI.Query<InitializeCameraTarget>().WithNone<CameraTarget>().WithEntityAccess())
            {
                ecb.AddComponent(entity, new CameraTarget
                {
                    Transform = new UnityObjectRef<Transform>
                    {
                        Value = cameraTargetTransform
                    },
                    HalfExtents = new float3(cameraHorizontal, 5f, cameraVertical),
                    YOffset = initializeCameraTarget.YOffset
                });
                ecb.RemoveComponent<InitializeCameraTarget>(entity);
            }
            ecb.Playback(state.EntityManager);
        }
    }

    /// <summary>
    /// This system updates the position of the GameObject camera target to match that of the player's position plus an additional offset along the y-axis.
    /// </summary>
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct MoveCameraSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (localToWorld, cameraTargetReference) in SystemAPI.Query<LocalToWorld, CameraTarget>())
            {
                cameraTargetReference.Transform.Value.position = localToWorld.Position + math.up() * cameraTargetReference.YOffset;
            }
        }
    }
}