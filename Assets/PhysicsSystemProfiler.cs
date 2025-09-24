using Bastard;
using Unity.Entities;
using Unity.Physics.Systems;

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