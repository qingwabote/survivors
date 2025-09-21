using System;
using System.IO;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Persistent game data that will be saved to disk.
    /// </summary>
    [Serializable]
    public struct PersistentGameData
    {
        /// <summary>
        /// Version number of the save file.
        /// </summary>
        /// <remarks>
        /// The <see cref="PersistentDataManager"/> can use this version number to upgrade the save file to the current version.
        /// </remarks>
        public int PersistentGameDataVersion;
        /// <summary>
        /// Running count of coins the player has access to. Can gain coins through gameplay and spend coins on characters and stages.
        /// </summary>
        public int CurrentCoinCount;
        /// <summary>
        /// Total number of enemies defeated.
        /// </summary>
        /// <remarks>
        /// This value isn't currently used, but it is still kept track of in case we decide to expose that to the player later or integrate with achievements.
        /// </remarks>
        public int TotalEnemiesDefeated;
        /// <summary>
        /// Total time in seconds the player has survived across all gameplay sessions.
        /// </summary>
        /// <remarks>
        /// This value isn't currently used, but it is still kept track of in case we decide to expose that to the player later or integrate with achievements.
        /// </remarks>
        public float TotalTimeSurvived;
        /// <summary>
        /// Byte flags to track which characters have been unlocked.
        /// </summary>
        /// <remarks>
        /// Each character is assigned a bit that is assigned in <see cref="CharacterProperties.CharacterID"/>.
        /// </remarks>
        public byte UnlockedCharacterFlags;
        /// <summary>
        /// Byte flags to track which stages have been unlocked.
        /// </summary>
        /// <remarks>
        /// Each stage is assigned a bit that is assigned in <see cref="StageProperties.StageID"/>.
        /// </remarks>
        public byte UnlockedStageFlags;
        /// <summary>
        /// Current music volume level stored in dB.
        /// </summary>
        public float MusicVolumeLevel;
        /// <summary>
        /// Current SFX volume level stored in dB.
        /// </summary>
        public float SfxVolumeLevel;
    }
    
    /// <summary>
    /// Singleton MonoBehaviour to handle saving, loading, and processing, persistent data relevant to gameplay.
    /// </summary>
    public class PersistentDataManager : MonoBehaviour
    {
        /// <summary>
        /// Public singleton access to this MonoBehaviour.
        /// </summary>
        public static PersistentDataManager Instance;

        /// <summary>
        /// The current version of the most recent save file.
        /// </summary>
        /// <remarks>
        /// This value is compared with the <see cref="PersistentGameData.PersistentGameDataVersion"/> for a given save file to determine if it is on the current version. If it is not, some steps to upgrade the save file to the current version can be implemented.
        /// </remarks>
        private const int PERSISTENT_GAME_DATA_VERSION = 1;
        /// <summary>
        /// The file path of the save file that will be used.
        /// </summary>
        /// <remarks>
        /// This string will be appended to Application.PersistentDataPath to determine the full file path.
        /// </remarks>
        private const string PERSISTENT_FILE_PATH = "/PlayerSave.bin";
        
        /// <summary>
        /// Event to be invoked when the player's coin count is updated. This will trigger UI elements to update with the appropriate coin count.
        /// </summary>
        public Action<int> OnUpdateCoinCount;
        
        /// <summary>
        /// When starting the game for the first time or deleting all save data this is the initial character that will be unlocked and available to the player.
        /// </summary>
        [SerializeField] private CharacterProperties _initialUnlockedCharacter;
        /// <summary>
        /// When starting the game for the first time or deleting all save data this is the initial stage that will be unlocked and available to the player.
        /// </summary>
        [SerializeField] private StageProperties _initialUnlockedStage;

        /// <summary>
        /// Persistent game data that will be saved to disk.
        /// </summary>
        private PersistentGameData _persistentGameData;

        /// <summary>
        /// Running count of coins the player has access to. Can gain coins through gameplay and spend coins on characters and stages.
        /// </summary>
        public int CurrentCoinCount => _persistentGameData.CurrentCoinCount;

        /// <summary>
        /// Property to access the current music volume level in dB.
        /// </summary>
        public float MusicVolumeLevel => _persistentGameData.MusicVolumeLevel;
        /// <summary>
        /// Property to access the current sfx volume level in dB.
        /// </summary>
        public float SfxVolumeLevel => _persistentGameData.SfxVolumeLevel;
        
        /// <summary>
        /// Setup singleton instance and mark as don't destroy on load.
        /// </summary>
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadPersistentGameData();
            _persistentGameData.UnlockedCharacterFlags |= (byte)_initialUnlockedCharacter.CharacterID;
            _persistentGameData.UnlockedStageFlags |= (byte)_initialUnlockedStage.StageID;
            SavePersistentGameData();
        }

        private void Start()
        {
            OnUpdateCoinCount?.Invoke(CurrentCoinCount);
        }

        /// <summary>
        /// Loads the saved <see cref="PersistentGameData"/> from the save file into memory.
        /// </summary>
        /// <remarks>
        /// Stored in a binary file, read one field at a time.
        /// </remarks>
        private void LoadPersistentGameData()
        {
            var fullPath = Application.persistentDataPath + PERSISTENT_FILE_PATH;
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("Save file not found: " + fullPath);
                return;
            }

            using var fileStream = new FileStream(fullPath, FileMode.Open);
            using var reader = new BinaryReader(fileStream);
            
            _persistentGameData = new PersistentGameData
            {
                PersistentGameDataVersion = reader.ReadInt32(),
                CurrentCoinCount = reader.ReadInt32(),
                TotalEnemiesDefeated = reader.ReadInt32(),
                TotalTimeSurvived = reader.ReadSingle(),
                UnlockedCharacterFlags = reader.ReadByte(),
                UnlockedStageFlags = reader.ReadByte(),
                MusicVolumeLevel = reader.ReadSingle(),
                SfxVolumeLevel = reader.ReadSingle()
            };

            if (_persistentGameData.PersistentGameDataVersion < PERSISTENT_GAME_DATA_VERSION)
            {
                Debug.LogWarning($"Loaded persistent save data from previous version: {_persistentGameData.PersistentGameDataVersion} while current version is: {PERSISTENT_GAME_DATA_VERSION}");
                
                // Resolve any save data version conflicts here.

                _persistentGameData.PersistentGameDataVersion = PERSISTENT_GAME_DATA_VERSION;
            }
        }

        /// <summary>
        /// Saves the current <see cref="PersistentGameData"/> struct to disk.
        /// </summary>
        /// <remarks>
        /// Stored in a binary file, written one field at a time.
        /// </remarks>
        private void SavePersistentGameData()
        {
            var fullPath = Application.persistentDataPath + PERSISTENT_FILE_PATH;

            using var fileStream = new FileStream(fullPath, FileMode.Create);
            using var writer = new BinaryWriter(fileStream);

            writer.Write(PERSISTENT_GAME_DATA_VERSION);
            writer.Write(_persistentGameData.CurrentCoinCount);
            writer.Write(_persistentGameData.TotalEnemiesDefeated);
            writer.Write(_persistentGameData.TotalTimeSurvived);
            writer.Write(_persistentGameData.UnlockedCharacterFlags);
            writer.Write(_persistentGameData.UnlockedStageFlags);
            writer.Write(_persistentGameData.MusicVolumeLevel);
            writer.Write(_persistentGameData.SfxVolumeLevel);
        }

        /// <summary>
        /// Method to attempt to purchase access to a character.
        /// </summary>
        /// <param name="cost">Cost of the character.</param>
        /// <param name="characterID">ID of the character, used to set the bitflag for the character on <see cref="PersistentGameData.UnlockedCharacterFlags"/> if the character becomes unlocked.</param>
        /// <returns>True if the character was successfully purchased, false if the player doesn't have enough money to purchase the character.</returns>
        /// <remarks>
        /// If the character was successfully purchased, persistent fields for <see cref="PersistentGameData.CurrentCoinCount"/> and <see cref="PersistentGameData.UnlockedCharacterFlags"/> will be updated accordingly.
        /// </remarks>
        public bool TryBuyCharacter(int cost, CharacterID characterID)
        {
            if (cost > CurrentCoinCount) return false;
            _persistentGameData.CurrentCoinCount -= cost;
            _persistentGameData.UnlockedCharacterFlags |= (byte)characterID;
            SavePersistentGameData();
            OnUpdateCoinCount?.Invoke(CurrentCoinCount);
            return true;
        }

        /// <summary>
        /// Method to determine if a character is unlocked.
        /// </summary>
        /// <param name="characterID">ID of the character used to look at the bitflags in <see cref="PersistentGameData.UnlockedCharacterFlags"/> to determine if the character is unlocked.</param>
        /// <returns>True if the character is unlocked, false if the character has not yet been unlocked by the player.</returns>
        public bool IsCharacterUnlocked(CharacterID characterID)
        {
            return (_persistentGameData.UnlockedCharacterFlags & (byte)characterID) != 0;
        }
        
        /// <summary>
        /// Method to attempt to purchase access to a stage.
        /// </summary>
        /// <param name="cost">Cost of the stage.</param>
        /// <param name="stageID">ID of the stage, used to set the bitflag for the stage on <see cref="PersistentGameData.UnlockedStageFlags"/> if the stage becomes unlocked.</param>
        /// <returns>True if the stage was successfully purchased, false if the player doesn't have enough money to purchase the stage.</returns>
        /// <remarks>
        /// If the stage was successfully purchased, persistent fields for <see cref="PersistentGameData.CurrentCoinCount"/> and <see cref="PersistentGameData.UnlockedStageFlags"/> will be updated accordingly.
        /// </remarks>
        public bool TryBuyStage(int cost, StageID stageID)
        {
            if (cost > CurrentCoinCount) return false;
            _persistentGameData.CurrentCoinCount -= cost;
            _persistentGameData.UnlockedStageFlags |= (byte)stageID;
            SavePersistentGameData();
            OnUpdateCoinCount?.Invoke(CurrentCoinCount);
            return true;
        }

        public void UnlockArtTestScene()
        {
            _persistentGameData.UnlockedStageFlags |= (byte)StageID.ArtTestScene;
            SavePersistentGameData();
        }
        
        /// <summary>
        /// Method to determine if a stage is unlocked.
        /// </summary>
        /// <param name="stageID">ID of the stage used to look at the bitflags in <see cref="PersistentGameData.UnlockedStageFlags"/> to determine if the stage is unlocked.</param>
        /// <returns>True if the stage is unlocked, false if the stage has not yet been unlocked by the player.</returns>
        public bool IsStageUnlocked(StageID stageID)
        {
            return (_persistentGameData.UnlockedStageFlags & (byte)stageID) != 0;
        }

        /// <summary>
        /// Adds money to the player's <see cref="CurrentCoinCount"/>
        /// </summary>
        /// <param name="moneyToAdd">Amount of money to add.</param>
        public void AddMoney(int moneyToAdd)
        {
            _persistentGameData.CurrentCoinCount += moneyToAdd;
            SavePersistentGameData();
            OnUpdateCoinCount?.Invoke(CurrentCoinCount);
        }

        /// <summary>
        /// Adds to the player's <see cref="PersistentGameData.TotalEnemiesDefeated"/> count.
        /// </summary>
        /// <param name="additionalEnemiesDefeated">Additional number of enemies to add to the running total.</param>
        public void AddEnemiesDefeated(int additionalEnemiesDefeated)
        {
            _persistentGameData.TotalEnemiesDefeated += additionalEnemiesDefeated;
            SavePersistentGameData();
        }

        /// <summary>
        /// Adds to the player's <see cref="PersistentGameData.TotalTimeSurvived"/> variable.
        /// </summary>
        /// <param name="additionalTimeSurvived">Time in seconds the player has survived to add to the running total.</param>
        public void AddTimeSurvived(float additionalTimeSurvived)
        {
            _persistentGameData.TotalTimeSurvived += additionalTimeSurvived;
            SavePersistentGameData();
        }

        /// <summary>
        /// Saves the music volume level, stored in dB.
        /// </summary>
        /// <param name="volumeLevel">Volume level to save in dB.</param>
        public void SaveMusicVolume(float volumeLevel)
        {
            _persistentGameData.MusicVolumeLevel = volumeLevel;
            SavePersistentGameData();
        }
        
        /// <summary>
        /// Saves the sound effects volume level, stored in dB.
        /// </summary>
        /// <param name="volumeLevel">Volume level to save in dB.</param>
        public void SaveSfxVolume(float volumeLevel)
        {
            _persistentGameData.SfxVolumeLevel = volumeLevel;
            SavePersistentGameData();
        }

        /// <summary>
        /// Deletes all persistent data and restores the game to the point as if the player just opened the game for the first time.
        /// </summary>
        /// <remarks>
        /// This is a destructive, irreversible action and the player is warned in the UI of such.
        /// </remarks>
        public void DeleteData()
        {
            _persistentGameData = new PersistentGameData
            {
                PersistentGameDataVersion = PERSISTENT_GAME_DATA_VERSION,
                CurrentCoinCount = 0,
                TotalEnemiesDefeated = 0,
                TotalTimeSurvived = 0,
                UnlockedCharacterFlags = (byte)_initialUnlockedCharacter.CharacterID,
                UnlockedStageFlags = (byte)_initialUnlockedStage.StageID,
                MusicVolumeLevel = 0f,
                SfxVolumeLevel = 0f
            };
            SavePersistentGameData();
            OnUpdateCoinCount?.Invoke(CurrentCoinCount);
        }
    }
}