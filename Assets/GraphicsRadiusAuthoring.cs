using Bastard;
using Graphix;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[WriteGroup(typeof(MaterialMeshInfo))]
public struct GraphicsRadius : IComponentData
{
    public float Value;
}

public class GraphicsRadiusAuthoring : MonoBehaviour
{
    public float Value;

    private class Baker : Baker<GraphicsRadiusAuthoring>
    {
        public override void Bake(GraphicsRadiusAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new GraphicsRadius { Value = authoring.Value });
        }
    }
}

[UpdateInGroup(typeof(BatchGroup))]
public partial struct GraphicsBatcher : ISystem
{
    private int m_BatchEntry;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MaterialMeshInfo>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (m_BatchEntry == 0)
        {
            m_BatchEntry = Profile.DefineEntry("GraphicsBatcher");
        }

        using (new Profile.Scope(m_BatchEntry))
        {
            var cam = Camera.main;
            var camPos = cam.transform.position;
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            var MaterialMeshInfo = SystemAPI.GetComponentTypeHandle<MaterialMeshInfo>(true);
            var LocalToWorld = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true);
            var GraphicsRadius = SystemAPI.GetComponentTypeHandle<GraphicsRadius>(true);
            var MaterialMeshArray = SystemAPI.ManagedAPI.GetSharedComponentTypeHandle<MaterialMeshArray>();

            state.EntityManager.CompleteDependencyBeforeRO<LocalToWorld>();

            var batcher = new BatcherImpl<MaterialMeshInfo, BatchProgram>(MaterialMeshArray, 128);
            foreach (var chunk in SystemAPI.QueryBuilder().WithAll<MaterialMeshInfo, GraphicsRadius>().Build().ToArchetypeChunkArray(Allocator.Temp))
            {
                batcher.BeginChunk(ref state, chunk);
                var mms = chunk.GetNativeArray(ref MaterialMeshInfo);
                var worlds = chunk.GetNativeArray(ref LocalToWorld);
                for (int i = 0; i < chunk.Count; i++)
                {
                    ref readonly var world = ref worlds.ElementAtRO(i).Value;
                    float dx = world.c3.x - camPos.x;
                    float dz = world.c3.z - camPos.z;
                    if (dx < -halfWidth || dx > halfWidth || dz < -halfHeight || dz > halfHeight)
                    {
                        continue;
                    }
                    batcher.Add(i, world, mms[i]);
                }
                batcher.EndChunk();
            }
        }
    }
}
