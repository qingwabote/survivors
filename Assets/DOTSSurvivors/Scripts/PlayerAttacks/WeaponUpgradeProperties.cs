using System;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Defines the type of the weapon. Often used as a key to obtain types relevant to the weapon in managed collections.
    /// </summary>
    public enum WeaponType : byte
    {
        None = 0,
        BlackHole = 1,
        CO2Cloud = 2,
        DroneSwarm = 3,
        Jetpack = 4,
        LaserStrike = 5,
        OxygenHose = 6,
        PlasmaBlast = 7,
        RadioactiveWaste = 8,
        SatelliteToss = 9,
        SawBlade = 10,
        Screwdriver = 11,
        Wrench = 12,
    }
    
    /// <summary>
    /// Data struct used to store weapon data and a description string for a single level of a weapon's upgrade path.
    /// </summary>
    /// <remarks>
    /// Has the System.Serializable attribute so these values can be initialized in the editor via <see cref="WeaponUpgradeProperties"/>.
    /// </remarks>
    [Serializable]
    public struct WeaponLevelInfo
    {
        /// <summary>
        /// Description of changes to <see cref="WeaponLevelData"/> to be shown to the player in the level up UI.
        /// </summary>
        public string Description;
        /// <summary>
        /// <see cref="WeaponLevelData"/> associated with a specific level of a weapon.
        /// </summary>
        public WeaponLevelData WeaponLevelData;
    }

    /// <summary>
    /// Abstract class of a ScriptableObject to define the upgradable properties for weapons and passive items that can be upgraded during gameplay.
    /// </summary>
    public abstract class UpgradeProperties : ScriptableObject
    {
        /// <summary>
        /// Determines how frequently an item will be shown for upgrade during the level up sequence. Value of 100 gives the highest chance while value of 0 will never be chosen.
        /// </summary>
        [Range(0, 100)]
        [Tooltip("Determines how frequently an item will be shown for upgrade. Value of 100 gives the highest chance while value of 0 will never be chosen.")]
        [SerializeField] private int _rarity = 100;
        /// <summary>
        /// Debug boolean to prioritize the upgrade.
        /// </summary>
        /// <remarks>
        /// Selecting this flag will change the rarity value to 5000 making it extremely likely to be chosen as an upgrade in the level up sequence. This can be used if you want to test out a specific weapon or passive item during development.
        /// For builds rarity will always be what is set in the <see cref="_rarity"/> field due to the UNITY_EDITOR directive on <see cref="Rarity"/>.
        /// </remarks>
        [Tooltip("DEBUG ONLY - Makes this weapon extremely likely to be chosen for upgrade on the level up screen. Useful for testing certain attacks and passive abilities.")]
        [SerializeField] private bool _prioritizeUpgrade;

        /// <summary>
        /// Name of the upgradeable item to be displayed in the UI.
        /// </summary>
        public string Name;
        /// <summary>
        /// Icon of the upgradeable item to be displayed in the UI.
        /// </summary>
        public Sprite Icon;
#if UNITY_EDITOR
        /// <summary>
        /// In editor rarity property. Will typically be set to <see cref="_rarity"/> unless <see cref="_prioritizeUpgrade"/> is set to true, where this property will return a large value making it extremely likely this weapon or passive item is selected during the level up sequence.
        /// </summary>
        public int Rarity => _prioritizeUpgrade ? 5000 : _rarity;
#else
        /// <summary>
        /// Rarity property for builds to return the <see cref="_rarity"/> field.
        /// </summary>
        public int Rarity => _rarity;
#endif
        /// <summary>
        /// Virtual method to get the description of an upgrade at a given level index.
        /// </summary>
        /// <remarks>
        /// This method will be overloaded in child classes to return the description at a specific index in the upgrade properties array.
        /// </remarks>
        /// <param name="levelIndex">Level index to get the description of. Index 0 for level 1, index 1 for level 2, and so on.</param>
        /// <returns>Description of changes to the item granted by this upgrade shown to the player in the level up UI.</returns>
        public virtual string GetDescription(int levelIndex) => "";
        /// <summary>
        /// Virtual property to return the maximum level index for the upgradable item.
        /// </summary>
        public virtual int MaxLevelIndex => 0;
    }
    
    /// <summary>
    /// ScriptableObject to define the properties for a weapon and its upgrade path.
    /// </summary>
    /// <remarks>
    /// Inherits from <see cref="UpgradeProperties"/>.
    /// </remarks>
    [CreateAssetMenu(fileName = "WeaponUpgradeProperties", menuName = "ScriptableObjects/Weapon Upgrade Properties")]
    public class WeaponUpgradeProperties : UpgradeProperties
    {
        /// <summary>
        /// Defines the type of the weapon. Often used as a key to obtain types relevant to the weapon in managed collections.
        /// </summary>
        public WeaponType WeaponType;
        /// <summary>
        /// GameObject prefab that will be spawned when the weapon is active via <see cref="WeaponActiveFlag"/>.
        /// This GameObject will be baked into an Entity in the <see cref="WeaponAuthoring"/> script and stored in the <see cref="AttackPrefab"/> on the associated weapon entity.
        /// </summary>
        public GameObject AttackPrefab;
        /// <summary>
        /// Array to store <see cref="WeaponLevelData"/> and descriptions for each level in the weapon's upgrade path.
        /// </summary>
        public WeaponLevelInfo[] LevelPropertiesArray;
        /// <summary>
        /// Gets the description of the weapon upgrade at a given level.
        /// </summary>
        /// <param name="levelIndex">Index of the level to get the description from. Index 0 for level 1, index 1 for level 2, and so on.</param>
        /// <returns>Description of the weapon upgrade at the given level to be shown to the player in the level up UI.</returns>
        public override string GetDescription(int levelIndex) => LevelPropertiesArray[levelIndex].Description;
        /// <summary>
        /// Property to return the maximum level index for the weapon.
        /// </summary>
        public override int MaxLevelIndex => LevelPropertiesArray.Length - 1;
    }
}