using Bastard;
using TMG.DOTSSurvivors;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Transforms;

struct SystemProfiler
{
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

partial struct EnemyProfiler : ISystem
{
    static private int s_Enemies = Profile.DefineEntry("Enemies");

    public void OnUpdate(ref SystemState state)
    {
        Profile.Delta(s_Enemies, SystemAPI.QueryBuilder().WithAll<EnemyTag>().Build().CalculateEntityCount());
    }
}