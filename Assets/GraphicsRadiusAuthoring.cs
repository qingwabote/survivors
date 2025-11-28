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
    static private BatcherImpl<MaterialMeshInfo, BatchSorter, NoParam> s_Batcher = new();

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
            MaterialMeshInfo.Update(ref state);
            var LocalToWorld = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true);
            LocalToWorld.Update(ref state);
            var GraphicsRadius = SystemAPI.GetComponentTypeHandle<GraphicsRadius>(true);
            GraphicsRadius.Update(ref state);

            state.EntityManager.CompleteDependencyBeforeRO<LocalToWorld>();

            foreach (var chunk in SystemAPI.QueryBuilder().WithAll<MaterialMeshInfo, GraphicsRadius>().Build().ToArchetypeChunkArray(Allocator.Temp))
            {
                s_Batcher.BeginChunk(ref state, chunk);
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
                    s_Batcher.Add(i, world, mms[i]);
                }
                s_Batcher.EndChunk();
            }
            s_Batcher.Clear();
        }
    }
}
