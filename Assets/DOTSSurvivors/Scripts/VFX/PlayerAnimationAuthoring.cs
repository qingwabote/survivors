using UnityEngine;
using Unity.Entities;
using Unity.Rendering;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Enum to define the indices for player animations.
    /// </summary>
    public enum PlayerAnimationIndex : byte
    {
        /// <summary>
        /// Animation to play when player is moving - i.e. walk cycle
        /// </summary>
        Movement = 0,
        /// <summary>
        /// Animation to player when character is not moving - i.e. idle animation
        /// </summary>
        Idle = 1,
        /// <summary>
        /// Invalid animation
        /// </summary>
        None = byte.MaxValue
    }

    /// <summary>
    /// Material property override for the animation index. Setting this value will set the animation index on the material instance for this entity.
    /// </summary>
    /// <remarks>
    /// Your IDE may gray out the Value field as this value is not used in our code. However, Unity uses it to apply this value to the material property defined in the MaterialProperty attribute.
    /// Be sure the MaterialProperty string exactly matches the reference string defined in the shader as this will silently fail if there is a typo.
    /// </remarks>
    [MaterialProperty("_AnimationIndex")]
    public struct AnimationIndexOverride : IComponentData
    {
        public float Value;
    }
    
    /// <summary>
    /// Authoring script to initialize and add components required for player character animation.
    /// </summary>
    /// <remarks>
    /// This authoring script should be attached to the graphics entity which is a child of the main player entity.
    /// <see cref="CharacterAnimationAuthoring"/> is a required component to ensure components necessary for character animation are applied to the entity.
    /// </remarks>
    /// <seealso cref="AnimationIndexOverride"/>
    [RequireComponent(typeof(CharacterAnimationAuthoring))]
    public class PlayerAnimationAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PlayerAnimationAuthoring>
        {
            public override void Bake(PlayerAnimationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new AnimationIndexOverride { Value = (int)PlayerAnimationIndex.Idle });
            }
        }
    }
}