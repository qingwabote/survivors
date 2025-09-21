using System;
using UnityEngine;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Enum denoting the type of initialization that should be used when determining the initial seed of <see cref="EntityRandom"/>.
    /// </summary>
    public enum EntityRandomInitializationType : byte
    {
        /// <summary>
        /// None value generally should not be used. Will initialize <see cref="EntityRandom"/> using index 0 during baking in <see cref="EntityRandomAuthoring.Baker.Bake"/>.
        /// </summary>
        None = 0,
        /// <summary>
        /// Manual override value will initialize <see cref="EntityRandom"/> using index <see cref="EntityRandomAuthoring.ManualOverrideValue"/> during baking in <see cref="EntityRandomAuthoring.Baker.Bake"/>. This can be helpful for debugging to get consistent random results.
        /// </summary>
        ManualOverride = 1,
        /// <summary>
        /// System milliseconds value will initialize <see cref="EntityRandom"/> using index from the system's millisecond time value at the time of initialization in the <see cref="InitializeEntityRandomSystem"/>
        /// </summary>
        SystemMilliseconds = 2
    }
    
    /// <summary>
    /// Helper data component to signify this entity needs its <see cref="EntityRandom"/> component initialized in the <see cref="InitializeEntityRandomSystem"/>.
    /// </summary>
    /// <remarks>
    /// Enableable component will be disabled once <see cref="EntityRandom"/> is initialized.
    /// </remarks>
    public struct InitializeEntityRandom : IComponentData, IEnableableComponent
    {
        public EntityRandomInitializationType InitializationType;
    }
    
    /// <summary>
    /// Component to store Unity.Mathematics.Random type used for random number generation to be held by a specific entity.
    /// </summary>
    /// <remarks>
    /// An entity having its own random number generator can be helpful for instances where unique random numbers need to be generated on separate threads in parallel.
    /// </remarks>
    public struct EntityRandom : IComponentData
    {
        public Random Value;
    }

    /// <summary>
    /// Authoring script to add <see cref="EntityRandom"/> component to an entity. This can also add the <see cref="InitializeEntityRandom"/> component to trigger initialization of random seed that cannot be set during baking i.e. using milliseconds of the system when the entity spawns.
    /// </summary>
    public class EntityRandomAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Sets the initialization type to be used when setting the initial seed of <see cref="EntityRandom"/>.
        /// </summary>
        public EntityRandomInitializationType RandomInitializationType = EntityRandomInitializationType.SystemMilliseconds;
        /// <summary>
        /// If <see cref="RandomInitializationType"/> is set to <see cref="EntityRandomInitializationType.ManualOverride"/>, this is the value that will be set as the initial seed during baking.
        /// </summary>
        public uint ManualOverrideValue;

        private class Baker : Baker<EntityRandomAuthoring>
        {
            public override void Bake(EntityRandomAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                
                switch (authoring.RandomInitializationType)
                {
                    case EntityRandomInitializationType.None:
                        AddComponent(entity, new EntityRandom { Value = Random.CreateFromIndex(0) });
                        break;
                    case EntityRandomInitializationType.ManualOverride:
                        AddComponent(entity, new EntityRandom { Value = Random.CreateFromIndex(authoring.ManualOverrideValue) });
                        break;
                    case EntityRandomInitializationType.SystemMilliseconds:
                        AddComponent<EntityRandom>(entity);
                        AddComponent(entity, new InitializeEntityRandom { InitializationType = EntityRandomInitializationType.SystemMilliseconds });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// System to initialize <see cref="EntityRandom"/> based on the <see cref="EntityRandomInitializationType"/> stored in <see cref="InitializeEntityRandom"/>.
    /// </summary>
    /// <remarks>
    /// This system only sets initial seed for initialization types of <see cref="EntityRandomInitializationType.SystemMilliseconds"/>. For other initialization types, it is assumed <see cref="EntityRandom"/> is already initialized during baking or instantiation. An additional index is added to the system's milliseconds value so that multiple entities that are initialized on the same frame do not start with the same random seed.
    /// System updates in the <see cref="DS_InitializationSystemGroup"/> before the <see cref="EnemyWaveSystem"/> to ensure <see cref="EntityRandom"/> is initialized before other systems need access to it.
    /// </remarks>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(EnemyWaveSystem))]
    public partial struct InitializeEntityRandomSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var systemMilliseconds = (uint)DateTime.Now.Millisecond;

            var index = 0u;
            foreach (var (random, initializationType, shouldInitialize) in SystemAPI.Query<RefRW<EntityRandom>, InitializeEntityRandom, EnabledRefRW<InitializeEntityRandom>>().WithOptions(EntityQueryOptions.IncludeSystems))
            {
                if (initializationType.InitializationType == EntityRandomInitializationType.SystemMilliseconds)
                {
                    random.ValueRW.Value = Random.CreateFromIndex(systemMilliseconds + index);
                    index += 1;
                }
                
                shouldInitialize.ValueRW = false;
            }
        }
    }
}