using Unity.Entities;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Authoring script to set components to the graphics entity of animated characters in the art test scene.
    /// </summary>
    public class ArtTestSceneAnimationAuthoring : MonoBehaviour
    {
        public bool IsFacingLeft;
        public bool IsIdle;
        public bool IsEnhanced;
        
        private class Baker : Baker<ArtTestSceneAnimationAuthoring>
        {
            public override void Bake(ArtTestSceneAnimationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                if (authoring.IsFacingLeft)
                {
                    AddComponent(entity, new FacingDirectionOverride { Value = -1f });
                }

                if (authoring.IsIdle)
                {
                    AddComponent(entity, new AnimationIndexOverride { Value = 1f });
                }

                if (authoring.IsEnhanced)
                {
                    AddComponent(entity, new EnhanceThicknessOverride { Value = 1f });
                }
            }
        }
    }
}