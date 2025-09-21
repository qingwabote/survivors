using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// ScriptableObject used to author information about spawn waves.
    /// </summary>
    /// <remarks>
    /// A spawn wave defines the behavior of enemy spawning for a certain period of time (typically 1 minute) in a level.
    /// </remarks>
    [CreateAssetMenu(fileName = "EnemySpawnWave-", menuName = "Enemy Spawn Wave")]
    public class EnemySpawnWaveProperties : ScriptableObject
    {
        /// <summary>
        /// Array of enemy types that will be spawned during this wave.
        /// </summary>
        public EnemyType[] EnemyTypes;
        /// <summary>
        /// Boss enemy type for the current spawn wave.
        /// </summary>
        /// <remarks>
        /// Boss enemies have more health than normal, have an enhanced material effect, and have a chance to spawn a supply crate when defeated.
        /// There can only be 1 boss enemy per wave.
        /// Boss enemies always spawn at the start of a spawn wave.
        /// </remarks>
        public EnemyType BossType;
        /// <summary>
        /// Base hit points to be applied to the boss for this spawn wave. Boss hit points are multiplied by the player's current level for a small bit of scaling.
        /// </summary>
        public int BossBaseHitPoints;
        /// <summary>
        /// Value of the experience point gem dropped by the boss for this wave.
        /// </summary>
        public int BossExperiencePoints;
        /// <summary>
        /// Percentage chance for the boss to spawn a crate once defeated. 0 will never spawn a crate and 100 will always spawn a crate.
        /// </summary>
        [Range(0,100)]
        public int BossChanceToDropCrate;
        /// <summary>
        /// Defines the minimum number of enemies that will be present while this wave is active. If there are fewer than the minimum enemies in the world for a particular spawn wave, the number of enemies required to meet this minimum value will be spawned in 1 frame.
        /// </summary>
        public int MinEnemyCount;
        /// If the minimum enemy count is met for this wave, additional enemies will be spawned on an interval defined by this value. One enemy of each type stored in the <see cref="SpawnWaveEnemyPrefab"/> Dynamic Buffer will be spawned once the <see cref="EnemySpawnerState.WaveTimer"/> reaches 0.
        public float SpawnInterval;
        
        /// <summary>
        /// Defines the spawn events for this spawn wave. Spawn events are groups of entities that will spawn in a certain <see cref="SpawnFormation"/> with unique behavior.
        /// </summary>
        [Header("Spawn Event Settings")]
        public EnemySpawnEventProperties[] SpawnEvents;
        /// <summary>
        /// Time in seconds from the start of the wave until the first spawn event. Subsequent spawn events will be started after another interval of this delay time. Spawn events occur sequentially. It is up to the spawn event designer to ensure that all spawn events occur within the time frame of the spawn wave as spawn events that would occur after a spawn wave concludes will not occur.
        /// </summary>
        [Tooltip("Time in seconds after wave begins until event starts.")]
        public float DelayTime;
        /// <summary>
        /// Percentage chance each spawn event will occur. Each spawn event will be evaluated at its execution time to determine if it should occur or not. A value of 0 will never occur and a value of 100 will always occur.
        /// </summary>
        [Range(0,100)]
        [Tooltip("Percent chance each event has to occur")]
        public int ChanceOfOccurrence;
    }
}