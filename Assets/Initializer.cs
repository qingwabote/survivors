using Unity.Entities;
using UnityEngine;

struct Initializer
{
    [RuntimeInitializeOnLoadMethod]
    private static void Initialize()
    {
        var fixedGroup = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
        fixedGroup.SetRateManagerCreateAllocator(null);
    }
}