using Unity.Burst;
using Unity.Entities;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// When this component is added to an entity, it will be destroyed at the end of the frame, ignoring any additional destruction logic that would normally be triggered in the <see cref="DestroyEntitySystem"/>
    /// </summary>
    /// <seealso cref="InstantDestroyEntitySystem"/>
    /// <seealso cref="DestroyEntitySystem"/>
    public struct InstantDestroyTag : IComponentData {}

    /// <summary>
    /// System responsible for immediately destroying entities with the <see cref="InstantDestroyTag"/>. This system will execute at the end of the <see cref="DS_DestructionSystemGroup"/> to ensure all systems that may add this tag have executed but before the <see cref="DestroyEntitySystem"/> to avoid the possibility of executing any additional destruction logic in that system.
    /// </summary>
    /// <seealso cref="InstantDestroyTag"/>
    /// <seealso cref="DestroyEntitySystem"/>
    [UpdateInGroup(typeof(DS_DestructionSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(DestroyEntitySystem))]
    public partial struct InstantDestroyEntitySystem : ISystem
    {
        /// <summary>
        /// Entity query for all entities with the <see cref="PlayAudioClipOnDestroyData"/> component.
        /// </summary>
        /// <remarks>
        /// Used to remove the <see cref="PlayAudioClipOnDestroyData"/> cleanup component from entities that are set to be instantly destroyed as audio clips should not play when entities are destroyed in this manner.
        /// </remarks>
        private EntityQuery _playAudioClipOnDestroyQuery;
        /// <summary>
        /// Entity query for all entities with the <see cref="InstantDestroyTag"/>.
        /// </summary>
        private EntityQuery _instantDestroyQuery;

        public void OnCreate(ref SystemState state)
        {
            _playAudioClipOnDestroyQuery = SystemAPI.QueryBuilder().WithAll<PlayAudioClipOnDestroyData, InstantDestroyTag>().Build();
            _instantDestroyQuery = SystemAPI.QueryBuilder().WithAll<InstantDestroyTag>().Build();
            state.RequireForUpdate(_instantDestroyQuery);
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.EntityManager.RemoveComponent<PlayAudioClipOnDestroyData>(_playAudioClipOnDestroyQuery);
            // Note: must convert query to entity array so entities within linked entity group are destroyed as well.
            var entitiesToDestroy = _instantDestroyQuery.ToEntityArray(state.WorldUpdateAllocator);
            state.EntityManager.DestroyEntity(entitiesToDestroy);
        }
    }
}