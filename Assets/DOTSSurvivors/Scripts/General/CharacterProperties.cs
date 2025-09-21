using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// ID of the character, used for serialization.
    /// </summary>
    /// <remarks>
    /// Each character is assigned a bit value so they can be used as bitflags in <see cref="PersistentGameData.UnlockedStageFlags"/>.
    /// </remarks>
    public enum CharacterID : byte
    {
        None = 0,
        Turbo = 1 << 0,
        Mockarutan = 1 << 1,
        Daxode = 1 << 2,
        Hazel = 1 << 3,
        Felikss = 1 << 4,
        Zenna = 1 << 5,
    }
    
    /// <summary>
    /// ScriptableObject to define properties for player characters. Certain fields are used just for UI display, while others affect gameplay.
    /// </summary>
    [CreateAssetMenu(fileName = "-CharacterProperties", menuName = "ScriptableObjects/Character Properties")]
    public class CharacterProperties : ScriptableObject
    {
        /// <summary>
        /// Name of the character.
        /// </summary>
        public string CharacterName;
        /// <summary>
        /// ID of the character, used for serialization.
        /// </summary>
        public CharacterID CharacterID;
        /// <summary>
        /// Sprite of the character to be shown in the character selection UI.
        /// </summary>
        public Sprite CharacterSprite;
        /// <summary>
        /// Sprite of the space agency flag to be displayed in the character selection UI and on the flagpole near the spawn point of the character.
        /// </summary>
        public Sprite SpaceAgencySprite;
        /// <summary>
        /// Description of the initial buff(s) for the character to be displayed in the character selection UI.
        /// </summary>
        [TextArea]
        public string BuffDescription;
        /// <summary>
        /// Cost to unlock this character.
        /// </summary>
        public int UnlockCost;

        /// <summary>
        /// Base hit points of the character.
        /// </summary>
        public int HitPoints = 100;
        /// <summary>
        /// Initial stat modifications the player spawns with.
        /// </summary>
        public StatModifierInfo[] StatOverrides;
        /// <summary>
        /// Starting weapon the player spawns with.
        /// </summary>
        public WeaponUpgradeProperties StartingWeapon;
        /// <summary>
        /// Material of the character that references the appropriate sprite sheet.
        /// </summary>
        public Material CharacterMaterial;
        
        /// <summary>
        /// Method used to get the current value of a given <see cref="StatModifierType"/>. Only used in the character selection panel to display stats unique to this character.
        /// </summary>
        /// <param name="statModifierType">Requested stat modification type.</param>
        /// <param name="defaultValue">Default value of this stat modification. Used so differences can be added to default value.</param>
        /// <returns>Value of the given stat modification type for this character.</returns>
        public float GetCurrentValue(StatModifierType statModifierType, float defaultValue)
        {
            var currentValue = defaultValue;
            if (statModifierType == StatModifierType.AdditionalHitPoints)
            {
                currentValue = HitPoints;
            }
            
            foreach (var statOverride in StatOverrides)
            {
                if (statOverride.Type != statModifierType) continue;
                currentValue += statOverride.Value;
            }

            return currentValue;
        }
    }
}