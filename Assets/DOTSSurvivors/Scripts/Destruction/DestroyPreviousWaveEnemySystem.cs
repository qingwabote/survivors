using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace TMG.DOTSSurvivors
{

    /// <summary>
    /// Shared component to store the index of the wave the enemy was spawned in.
    /// </summary>
    /// <remarks>
    /// Shared component is used so enemies in a specific wave can be queried for. This is notably used in the <see cref="DestroyPreviousWaveEnemySystem"/> system that cleans up offscreen enemies from previous waves.
    /// </remarks>
    public struct WaveIndexOfSpawn : ISharedComponentData
    {
        public int Value;
    }

    /// <summary>
    /// System to destroy off-screen enemies that were spawned in a previous wave.
    /// </summary>
    /// <remarks>
    /// This is used to ensure a fresh set of enemies are regularly spawned.
    /// </remarks>
    [UpdateInGroup(typeof(DS_DestructionSystemGroup))]
    public partial struct DestroyPreviousWaveEnemySystem : ISystem
    {
        /// <summary>
        /// Entity query for all enemies. The shared component filter will be set on this query to test for the existence of enemies in a previous wave and destroy them if they are outside the camera bounds.
        /// </summary>
        private EntityQuery _enemiesQuery;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemySpawnerState>();
            state.RequireForUpdate<CameraTarget>();
            _enemiesQuery = state.EntityManager.CreateEntityQuery(typeof(EnemyTag), typeof(WaveIndexOfSpawn));
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var curWaveIndex = SystemAPI.GetSingleton<EnemySpawnerState>().CurWaveIndex;
            var cameraReferenceEntity = SystemAPI.GetSingletonEntity<CameraTarget>();
            var cameraPosition = SystemAPI.GetComponent<LocalToWorld>(cameraReferenceEntity).Position;
            
            for (var i = 0; i < curWaveIndex; i++)
            {
                var sharedWaveIndex = new WaveIndexOfSpawn { Value = i };
                _enemiesQuery.SetSharedComponentFilter(sharedWaveIndex);
                if (_enemiesQuery.IsEmpty) continue;
                
                var enemiesInPreviousWave = _enemiesQuery.ToEntityArray(state.WorldUpdateAllocator);
                foreach (var enemy in enemiesInPreviousWave)
                {
                    var transform = SystemAPI.GetComponent<LocalTransform>(enemy);
                    var distanceFromCameraSq = math.distancesq(transform.Position.xz, cameraPosition.xz);
                    if (distanceFromCameraSq > 500)
                    {
                        state.EntityManager.AddComponent<InstantDestroyTag>(enemy);
                    }
                }
            }
        }
    }
}