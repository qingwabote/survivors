using Bastard;
using TMG.DOTSSurvivors;
using Unity.Entities;
using Unity.Physics.Systems;
using UnityEngine;

struct PhysicsSystemProfiler
{
    static public int PhysicsEntry = Profile.DefineEntry("Physics");
}

[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
partial struct BeforePhysicsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        Profile.Begin(PhysicsSystemProfiler.PhysicsEntry);
    }
}

[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
partial struct AfterPhysicsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        Profile.End(PhysicsSystemProfiler.PhysicsEntry);
    }
}

partial struct FixedTimestep : ISystem
{
    static private int s_FixedDeltaTime = Profile.DefineEntry("FixedTimestep");
    static private int s_Enemies = Profile.DefineEntry("Enemies");

    public void OnUpdate(ref SystemState state)
    {
        Profile.Delta(s_FixedDeltaTime, Time.fixedDeltaTime);
        Profile.Delta(s_Enemies, SystemAPI.QueryBuilder().WithAll<EnemyTag>().Build().CalculateEntityCount());
    }
}