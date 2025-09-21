using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Enum to define the type of bonus item. Used in <see cref="BonusItemProperties"/>.
    /// </summary>
    /// <seealso cref="BonusItemProperties"/>
    public enum BonusItemType : byte
    {
        None = 0,
        Health = 1,
        Money = 2
    }
    
    /// <summary>
    /// ScriptableObject to define properties related to bonus items. Bonus items are items shown in the level up and crate UI if no further upgrades to attacks or passive items are available to the player.
    /// </summary>
    /// <remarks>
    /// Currently bonus items can grant additional health points or money.
    /// </remarks>
    [CreateAssetMenu(fileName = "BonusItemProperties", menuName = "ScriptableObjects/BonusItemProperties")]
    public class BonusItemProperties : ScriptableObject
    {
        /// <summary>
        /// Name of the item, to be displayed in UI when bonus item is displayed.
        /// </summary>
        public string ItemName;
        /// <summary>
        /// Icon of the item, to be displayed in UI when bonus item is displayed.
        /// </summary>
        public Sprite ItemIcon;
        /// <summary>
        /// Longer description of the item, to be displayed in UI when bonus item is displayed.
        /// </summary>
        public string ItemDescription;
        /// <summary>
        /// Type of the item.
        /// </summary>
        public BonusItemType ItemType;
        /// <summary>
        /// Value of hit points or money to be granted if bonus item is selected.
        /// </summary>
        public int ItemValue;
    }
}