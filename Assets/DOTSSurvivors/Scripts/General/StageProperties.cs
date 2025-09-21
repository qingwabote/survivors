using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// ID of the stage, used for serialization.
    /// </summary>
    /// <remarks>
    /// Each stage is assigned a bit value so they can be used as bitflags in <see cref="PersistentGameData.UnlockedStageFlags"/>.
    /// </remarks>
    public enum StageID : byte
    {
        None = 0,
        Moon = 1 << 1,
        Mars = 1 << 2,
        Venus = 1 << 3,
        ArtTestScene = 1 << 4,
    }
    
    /// <summary>
    /// ScriptableObject to store properties related to stages.
    /// </summary>
    [CreateAssetMenu(fileName = "-StageProperties", menuName = "ScriptableObjects/Stage Properties")]
    public class StageProperties : ScriptableObject
    {
        /// <summary>
        /// Name of the stage that will be displayed in the stage selection UI.
        /// </summary>
        public string StageName;

        /// <summary>
        /// ID of the stage, used for serialization.
        /// </summary>
        public StageID StageID;
        /// <summary>
        /// Description of the stage that will be displayed in the stage selection UI.
        /// </summary>
        [TextArea]
        public string StageDescription;
        /// <summary>
        /// Cost to unlock the stage for playing.
        /// </summary>
        public int UnlockCost;
        /// <summary>
        /// Build index of the scene associated with this level.
        /// </summary>
        public int SceneIndex;
        /// <summary>
        /// Preview sprite shown in the stage selection UI.
        /// </summary>
        public Sprite PreviewSprite;
        /// <summary>
        /// Collection of <see cref="EnemySpawnWaveProperties"/> to define which waves will be spawned in a given level.
        /// </summary>
        public EnemySpawnWaveProperties[] EnemySpawnWaves;
        /// <summary>
        /// Interval at which the next wave of entities will be selected.
        /// </summary>
        public int WaveInterval;

        /// <summary>
        /// Time it will take to reach the final spawn wave in the level, shown in the stage selection UI.
        /// </summary>
        /// <remarks>
        /// EnemySpawnWaves.Length - 1 is used as the final stage in the game is the reaper stage that will effectively destroy the entity immediately, so this stage shouldn't be factored into the time calculation.
        /// </remarks>
        public float TimeLimit => (EnemySpawnWaves.Length - 1) * WaveInterval;
    }
}