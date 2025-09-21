using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// ScriptableObject to author properties related to stat modifiers. Certain properties are used for runtime data and others are used for displaying information in UI elements.
    /// </summary>
    /// <remarks>
    /// These properties should be stored in the Resources/ScriptableObjects/StatModifierProperties directory.
    /// Only one StatModifierProperties ScriptableObject should exist in the above directory else the <see cref="StatModifierController"/> will throw an error when trying to load these ScriptableObjects.
    /// </remarks>
    [CreateAssetMenu(fileName = "-Properties", menuName = "ScriptableObjects/Stat Modifier Properties")]
    public class StatModifierProperties : ScriptableObject
    {
        /// <summary>
        /// Type of stat modifier.
        /// </summary>
        public StatModifierType ModifierType;
        /// <summary>
        /// Calculation type for the stat modifier.
        /// </summary>
        public StatModifierCalculationType CalculationType;
        /// <summary>
        /// Title of the stat modifier to be displayed in the UI.
        /// </summary>
        public string Title;
        /// <summary>
        /// Sprite icon of the stat modifier to be displayed in the UI.
        /// </summary>
        public Sprite Icon;
        /// <summary>
        /// Minimum value for the stat modification. Used when clamping the value to ensure it stays within a range to facilitate game balance.
        /// </summary>
        public float MinimumModificationValue;
        /// <summary>
        /// Minimum value for the stat modification. Used when clamping the value to ensure it stays within a range to facilitate game balance.
        /// </summary>
        public float MaximumModificationValue;
    }
}