using Bastard;
using TMG.DOTSSurvivors;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
partial struct ObstacleFilter : ISystem
{
    static private int s_Profile = Profile.DefineEntry("ObstacleFilter");

    public void OnUpdate(ref SystemState state)
    {
        var cam = Camera.main;
        var camPos = cam.transform.position;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        using (new Profile.Scope(s_Profile))
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (local, entity) in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<PhysicsCollider, EnvironmentTag, Disabled>().WithEntityAccess())
            {
                var pos = local.ValueRO.Position;
                float dx = pos.x - camPos.x;
                float dz = pos.z - camPos.z;
                if (dx > -halfWidth && dx < halfWidth && dz > -halfHeight && dz < halfHeight)
                {
                    ecb.SetEnabled(entity, true);
                }
            }
            foreach (var (local, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PhysicsCollider, EnvironmentTag>().WithEntityAccess())
            {
                var pos = local.ValueRO.Position;
                float dx = pos.x - camPos.x;
                float dz = pos.z - camPos.z;
                if (dx > -halfWidth && dx < halfWidth && dz > -halfHeight && dz < halfHeight)
                {
                    continue;
                }
                ecb.SetEnabled(entity, false);
            }
            ecb.Playback(state.EntityManager);
        }
    }
}