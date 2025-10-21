using Bastard;
using TMG.DOTSSurvivors;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

struct SystemProfiler
{
    static public int Transform = Profile.DefineEntry("Transform");
    static public int Physics = Profile.DefineEntry("Physics");
}

[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
partial struct BeforePhysicsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        Profile.Begin(SystemProfiler.Physics);
    }
}

[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
partial struct AfterPhysicsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        Profile.End(SystemProfiler.Physics);
    }
}

[UpdateInGroup(typeof(TransformSystemGroup)), UpdateBefore(typeof(LocalToWorldSystem))]
partial struct BeforeLocalToWorldSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        Profile.Begin(SystemProfiler.Transform);
    }
}

[UpdateInGroup(typeof(TransformSystemGroup)), UpdateAfter(typeof(LocalToWorldSystem))]
partial struct AfterLocalToWorldSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        Profile.End(SystemProfiler.Transform);
    }
}

partial struct FixedTimestep : ISystem
{
    static private int s_FixedDeltaTime = Profile.DefineEntry("FixedTimestep");
    static private int s_Enemies = Profile.DefineEntry("Enemies");

    public void OnUpdate(ref SystemState state)
    {
        var fixedGroup = state.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
        fixedGroup.Timestep = Time.fixedDeltaTime;

        Profile.Delta(s_FixedDeltaTime, Time.fixedDeltaTime);
        Profile.Delta(s_Enemies, SystemAPI.QueryBuilder().WithAll<EnemyTag>().Build().CalculateEntityCount());
    }
}