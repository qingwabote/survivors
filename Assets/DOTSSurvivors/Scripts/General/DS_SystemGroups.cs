using Unity.Entities;
using Unity.Transforms;
using Unity.Physics.Systems;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// System group containing systems related to player attacks.
    /// </summary>
    /// <remarks>
    /// Updates in Unity's main SimulationSystemGroup. Must update before Unity's TransformSystemGroup and <see cref="DS_InteractionSystemGroup"/> as systems in those groups depend on the completion of systems in this group.
    /// </remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(DS_InteractionSystemGroup))]
    public partial class DS_AttackSystemGroup : ComponentSystemGroup
    {
        
    }
    
    /// <summary>
    /// System group containing systems related to entity interactions.
    /// </summary>
    /// <remarks>
    /// Updates in Unity's main SimulationSystemGroup. Must update after Unity's FixedStepSimulationSystemGroup and <see cref="DS_AttackSystemGroup"/> as systems in those groups raise entity interactions handled by systems in this group.
    /// </remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DS_AttackSystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public partial class DS_InteractionSystemGroup : ComponentSystemGroup
    {
        
    }
    
    /// <summary>
    /// System group containing systems related to initialization logic that should be executed towards the beginning of the frame.
    /// </summary>
    /// <remarks>
    /// Updates at the end of Unity's InitializationSystemGroup to ensure that logic has completed, however it is before the command buffer system at the end of that group in case anything needs to be scheduled in that ECB.
    /// </remarks>
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndInitializationEntityCommandBufferSystem))]
    public partial class DS_InitializationSystemGroup : ComponentSystemGroup
    {
        
    }

    /// <summary>
    /// System group containing systems that deal with physics such as scheduling collision/trigger event jobs and executing physics overlap queries.
    /// </summary>
    /// <remarks>
    /// Updates in Unity's PhysicsSystemGroup (which is part of Unity's FixedStepSimulationSystemGroup that updates on a fixed tick). Will update after Unity's PhysicsSimulationGroup and before their AfterPhysicsSystemGroup to ensure the appropriate place in the frame for looking at collision/trigger events and overlap queries for the current physics step.
    /// </remarks>
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    [UpdateBefore(typeof(AfterPhysicsSystemGroup))]
    public partial class DS_PhysicsSystemGroup : ComponentSystemGroup
    {
        
    }

    /// <summary>
    /// System group containing systems that deal with moving entities via their LocalTransform component.
    /// </summary>
    /// <remarks>
    /// Updates in Unity's main SimulationSystemGroup and before their TransformSystemGroup to ensure that any changes to LocalTransform are made before they are transferred to the LocalToWorld component which is used to tell the GPU where to render the entity for the current frame.
    /// </remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial class DS_TranslationSystemGroup : ComponentSystemGroup
    {
        
    }

    /// <summary>
    /// System group containing systems that deal with triggering visual and audio effects.
    /// </summary>
    /// <remarks>
    /// Updates at the end of Unity's main SimulationSystemGroup to ensure any effects that need to be triggered are already known about (typically via enableable component) before executing effects systems. Updates before the <see cref="DS_DestructionSystemGroup"/> to ensure an entity isn't destroyed before it plays necessary effects for the current frame.
    /// </remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(DS_DestructionSystemGroup))]
    public partial class DS_EffectsSystemGroup : ComponentSystemGroup
    {
        
    }

    /// <summary>
    /// System group containing systems related to entity destruction.
    /// </summary>
    /// <remarks>
    /// Updates at the end of Unity's main SimulationSystemGroup to ensure all entities marked for destruction in various earlier systems are accounted for and to ensure entities aren't destroyed prematurely. Updates before Unity's EndSimulationEntityCommandBufferSystem as systems in this group schedule things in that entity command buffer.
    /// </remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
    public partial class DS_DestructionSystemGroup : ComponentSystemGroup
    {
        
    }
}