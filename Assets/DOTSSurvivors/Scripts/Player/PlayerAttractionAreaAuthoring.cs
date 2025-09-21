using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Component containing the data related to the player's attraction area.
    /// </summary>
    /// <remarks>
    /// Attraction area is the area at which the player can pick up attractable items and experience gems.
    /// </remarks>
    public struct PlayerAttractionAreaData : IComponentData
    {
        /// <summary>
        /// Defines the radius of the attraction area.
        /// </summary>
        public float Radius;
        /// <summary>
        /// Collision filter determines the types of colliders that trigger events will be raised with. i.e. this should raise trigger events with items and experience gems, but not enemies or environment elements.
        /// </summary>
        public CollisionFilter CollisionFilter;
    }
    
    /// <summary>
    /// Authoring script to initialize data for the player's attraction area.
    /// </summary>
    public class PlayerAttractionAreaAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Defines the radius of the attraction area.
        /// </summary>
        public float Radius;
        /// <summary>
        /// Reference to the player entity. Used in systems such as <see cref="AttractableItemSystem"/> to determine any modifications to the attraction radius as defined by <see cref="CharacterStatModificationState.ItemAttractionRadius"/>.
        /// </summary>
        public GameObject PlayerEntity;
        
        private class Baker : Baker<PlayerAttractionAreaAuthoring>
        {
            public override void Bake(PlayerAttractionAreaAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                DependsOn(authoring.gameObject);
                var belongsToLayer = authoring.gameObject.layer;
                var belongsToLayerMask = (uint)math.pow(2, belongsToLayer);
                var collidesWithLayerMask = (uint)PhysicsHelper.GetCollisionMaskForLayer(belongsToLayer);
                AddComponent(entity, new PlayerAttractionAreaData
                {
                    Radius = authoring.Radius,
                    CollisionFilter = new CollisionFilter
                    {
                        BelongsTo = belongsToLayerMask,
                        CollidesWith = collidesWithLayerMask
                    }
                });
                AddComponent(entity, new CharacterEntity
                {
                    Value = GetEntity(authoring.PlayerEntity, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}