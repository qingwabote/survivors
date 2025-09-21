using System;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data struct used to store <see cref="StatModifier"/> array and description of a passive item at a single level of the item's upgrade path.
    /// </summary>
    /// <remarks>
    /// Has the System.Serializable attribute so these values can be initialized in the editor via <see cref="PassiveUpgradeProperties"/>.
    /// </remarks>
    [Serializable]
    public struct PassiveLevelInfo
    {
        /// <summary>
        /// Description of the passive item at a specific level.
        /// </summary>
        public string Description;
        /// <summary>
        /// Array of <see cref="StatModifier"/>s that will be added to the player's <see cref="CharacterStatModificationState"/> when the level of this passive item is selected.
        /// </summary>
        /// <remarks>
        /// Gets added indirectly via <see cref="ActiveStatModifierEntity"/> in <see cref="RecalculateStatsSystem"/>.
        /// </remarks>
        public StatModifier[] StatModifiers;
    }
    
    /// <summary>
    /// ScriptableObject to define the properties for a passive item and its upgrade path.
    /// </summary>
    /// <remarks>
    /// Inherits from <see cref="UpgradeProperties"/>.
    /// </remarks>
    [CreateAssetMenu(fileName = "PassiveUpgradeProperties", menuName = "ScriptableObjects/Passive Upgrade Properties")]
    public class PassiveUpgradeProperties : UpgradeProperties
    {
        /// <summary>
        /// Array to store <see cref="StatModifier"/>s and descriptions for each level in the passive item's upgrade path.
        /// </summary>
        public PassiveLevelInfo[] UpgradeProperties;
        /// <summary>
        /// Gets the description of the passive item upgrade at a given level.
        /// </summary>
        /// <param name="levelIndex">Index of the level to get the description from. Index 0 for level 1, index 1 for level 2, and so on.</param>
        /// <returns>Description of the attack upgrade at the given level.</returns>
        public override string GetDescription(int levelIndex) => UpgradeProperties[levelIndex].Description;
        /// <summary>
        /// Property to return the maximum level index for the passive item.
        /// </summary>
        public override int MaxLevelIndex => UpgradeProperties.Length - 1;
    }
}