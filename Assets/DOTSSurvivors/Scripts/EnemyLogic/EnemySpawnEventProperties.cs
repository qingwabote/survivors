using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Defines the formation enemies for a <see cref="SpawnEvent"/> will spawn in. The formation will also determine certain aspects of their behavior in the game.
    /// </summary>
    public enum SpawnFormation : byte
    {
        None,
        LinearMoveGroup,
        EllipseAroundView,
        SineMoveVerticalLine,
        SineMoveHorizontalLine,
    }
    
    /// <summary>
    /// Authoring for spawn events. Spawn event are certain events that have a chance of occurrence within a spawn wave. When a spawn event happens, enemies will be spawned in a certain <see cref="SpawnFormation"/> and have unique behavior.
    /// </summary>
    /// <remarks>
    /// Certain SpawnFormations require additional data. Not all fields will be used for each formation
    /// Custom editor script displays relevant fields for the selected formation (SpawnEventPropertiesEditor.cs)
    /// </remarks>
    /// <seealso cref="EnemySpawnWaveProperties"/>
    /// <seealso cref="EnemySpawnSystem"/>
    /// <seealso cref="SpawnEvent"/>
    [CreateAssetMenu(fileName = "SpawnEvent-", menuName = "ScriptableObjects/Spawn Event")]
    public class EnemySpawnEventProperties : ScriptableObject
    {
        /// <summary>
        /// Arrangement the enemies will be spawned in.
        /// </summary>
        public SpawnFormation SpawnFormation;
        /// <summary>
        /// Type of enemy to be spawned for this event.
        /// </summary>
        public EnemyType EnemyType;
        /// <summary>
        /// Number of enemies to be spawned during this event.
        /// </summary>
        public int EnemyCount;
        
        /// <summary>
        /// Optional field relevant to certain spawn formations. Defines the spacing between enemies.
        /// </summary>
        public float EnemySpacing;
        /// <summary>
        /// Optional field relevant to certain spawn formations. Defines the time to live for a given enemy before it is destroyed. This field can also be used to store the test interval for the <see cref="DestroyOffCameraData"/> component.
        /// </summary>
        public float TimeToLive;
        /// <summary>
        /// Optional field relevant to certain spawn formations. Defines the movement speed for linear moving enemies (<see cref="EnemyLinearMovement"/>) and constant move speed for sine wave enemies (<see cref="EnemySineWaveMovement"/>).
        /// </summary>
        public float MoveSpeed;
        /// <summary>
        /// Optional field relevant to certain spawn formations. Defines the period for enemy sine wave movement. <see cref="EnemySineWaveMovement"/>
        /// </summary>
        public float Period;
        /// <summary>
        /// Optional field relevant to certain spawn formations. Defines the Amplitude for enemy sine wave movement. <see cref="EnemySineWaveMovement"/>
        /// </summary>
        public float Amplitude;
    }
}