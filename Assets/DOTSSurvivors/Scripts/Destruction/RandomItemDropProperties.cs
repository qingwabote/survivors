using UnityEngine;
using System.Collections.Generic;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Serializable helper struct to facilitate Unity editor authoring of many of these elements contained in the <see cref="RandomItemDropProperties.ItemDrops"/> List.
    /// </summary>
    [System.Serializable]
    public struct RandomItemDropInfo
    {
        /// <summary>
        /// GameObject prefab to be dropped. In the <see cref="SpawnRandomOnDestroyAuthoring.Baker"/> this GameObject will be baked into an entity prefab.
        /// </summary>
        public GameObject Prefab;
        
        /// <summary>
        /// Determines how frequently an item will be dropped. Value of 100 gives the highest chance while value of 0 will never be chosen.
        /// </summary>
        /// <remarks>
        /// This is not a percentage chance, this is a relative chance, so if 4 elements each have a Rarity value of 100, they will each have a 25% chance of being dropped.
        /// </remarks>
        [Range(0, 100)]
        [Tooltip("Determines how frequently an item will be dropped. Value of 100 gives the highest chance while value of 0 will never be chosen.")]
        public int Rarity;
    }
    
    /// <summary>
    /// ScriptableObject to define which items can be dropped when an entity destroyed.
    /// </summary>
    /// <seealso cref="RandomItemToSpawnBlob"/>
    /// <seealso cref="DestroyEntitySystem"/>
    [CreateAssetMenu(fileName = "RandomItemDropProperties", menuName = "ScriptableObjects/Random Item Drop Properties")]
    public class RandomItemDropProperties : ScriptableObject
    {
        /// <summary>
        /// List of <see cref="RandomItemDropInfo"/> elements that are used to author which items can be dropped with their associated rarities via the Unity editor.
        /// </summary>
        public List<RandomItemDropInfo> ItemDrops;
    }
}