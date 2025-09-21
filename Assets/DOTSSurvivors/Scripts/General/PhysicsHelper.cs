using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Static helper class used to implement helpers not present in Unity's API.
    /// </summary>
    public static class PhysicsHelper
    {
        /// <summary>
        /// Static method to return the 32-bit collision layer bitmask that will have bits set to 1 for the indices of layers it can collide with.
        /// </summary>
        /// <remarks>
        /// Typically used to create a CollisionFilter type which can be used in raycasts and spatial queries.
        /// </remarks>
        /// <param name="layer">Index of the layer to get the collision bitmask for.</param>
        /// <returns>Returns the 32-bit collision layer bitmask for all the layers this layer collides with.</returns>
        public static int GetCollisionMaskForLayer(int layer)
        {
            var mask = 0;
            for (var i = 0; i < 32; i++)
            {
                if (Physics.GetIgnoreLayerCollision(layer, i)) continue;
                mask |= 1 << i;
            }
            return mask;
        }
    }
}