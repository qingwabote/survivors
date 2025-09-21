using UnityEngine;
using Unity.Entities;
using Unity.Rendering;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Material property override for the animation offset of the enemy. Setting this value will update the _SpawnTime property in the material instance of this entity. This provides an offset for the animation so all enemies can be on their own unique animation frame and will not appear to be synchronized.
    /// </summary>
    /// <remarks>
    /// Your IDE may gray out the Value field as this value is not used in our code. However, Unity uses it to apply this value to the material property defined in the MaterialProperty attribute.
    /// Be sure the MaterialProperty string exactly matches the reference string defined in the shader as this will silently fail if there is a typo.
    /// </remarks>
    /// <seealso cref="InitializeAnimationOffsetOverrideFlag"/>
    /// <seealso cref="CharacterAnimationSystem"/>
    [MaterialProperty("_SpawnTime")]
    public struct AnimationOffsetOverride : IComponentData
    {
        public float Value;
    }
    
    /// <summary>
    /// Flag component to signify this entity needs its <see cref="AnimationOffsetOverride"/> component initialized.
    /// </summary>
    /// <seealso cref="CharacterAnimationSystem"/>
    public struct InitializeAnimationOffsetOverrideFlag : IComponentData, IEnableableComponent {}
    
    /// <summary>
    /// Material property override for the spawn time of the enemy. Setting this value will update the _SpawnTime property in the material instance of this entity. This provides an offset for the animation so all enemies can be on their own unique animation frame and will not appear to be synchronized.
    /// </summary>
    /// <remarks>
    /// Your IDE may gray out the Value field as this value is not used in our code. However, Unity uses it to apply this value to the material property defined in the MaterialProperty attribute.
    /// Be sure the MaterialProperty string exactly matches the reference string defined in the shader as this will silently fail if there is a typo.
    /// </remarks>
    /// <seealso cref="InitializeAnimationOffsetOverrideFlag"/>
    /// <seealso cref="CharacterAnimationSystem"/>
    [MaterialProperty("_EnhanceThickness")]
    public struct EnhanceThicknessOverride : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// Flag component to signify the <see cref="EnhanceThicknessOverride"/> component should be enabled.
    /// </summary>
    /// <remarks>
    /// Note that this component will be added to the main enemy entity when spawning an enhanced entity. However, the <see cref="EnhanceThicknessOverride"/> component will be added to the child graphics entity in <see cref="EnemyAnimationAuthoring"/>.
    /// </remarks>
    /// <seealso cref="CharacterAnimationSystem"/>
    public struct InitializeEnhancementMaterialFlag : IComponentData, IEnableableComponent {}

    /// <summary>
    /// Authoring script to add components necessary for enemy character animation.
    /// </summary>
    /// <remarks>
    /// This authoring script should be attached to the graphics entity which is a child of the main enemy entity.
    /// <see cref="CharacterAnimationAuthoring"/> is a required component to ensure components necessary for character animation are applied to the entity.
    /// </remarks>
    [RequireComponent(typeof(CharacterAnimationAuthoring))]
    public class EnemyAnimationAuthoring : MonoBehaviour
    {
        private class Baker : Baker<EnemyAnimationAuthoring>
        {
            public override void Bake(EnemyAnimationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<AnimationOffsetOverride>(entity);
                AddComponent<InitializeAnimationOffsetOverrideFlag>(entity);
                AddComponent<EnhanceThicknessOverride>(entity);
            }
        }
    }
}