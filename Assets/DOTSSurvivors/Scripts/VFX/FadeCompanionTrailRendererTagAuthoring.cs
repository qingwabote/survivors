using Unity.Entities;
using UnityEngine;
using Unity.Transforms;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Component to signify the associated entity needs its <see cref="FadeCompanionTrailRenderer"/> component initialized.
    /// </summary>
    /// <remarks>
    /// This initialization step is required as <see cref="FadeCompanionTrailRenderer"/> is an enableable component which cannot be baked onto an entity.
    /// </remarks>
    public struct InitializeFadeCompanionTrailRenderer : IComponentData {}

    /// <summary>
    /// Cleanup component to call the <see cref="JetpackTrailRendererController.EndTrailRenderer"/> method when the main jetpack entity is destroyed. This provides a nice visual fade out of the trail renderer on jetpacks.
    /// </summary>
    public struct FadeCompanionTrailRenderer : ICleanupComponentData
    {
        public UnityObjectRef<JetpackTrailRendererController> Value;
    }
    
    /// <summary>
    /// Authoring script to add the <see cref="InitializeFadeCompanionTrailRenderer"/> component to the entity.
    /// </summary>
    /// <remarks>
    /// Requires the <see cref="CompanionGameObjectAuthoring"/> component to ensure all companion elements are added to the entity.
    /// This authoring script should be added to the attack graphics entity which is a direct child of the the main attack prefab.
    /// </remarks>
    /// <seealso cref="FadeCompanionTrailRenderer"/>
    /// <seealso cref="FadeCompanionTrailRendererSystem"/>
    [RequireComponent(typeof(CompanionGameObjectAuthoring))]
    public class FadeCompanionTrailRendererTagAuthoring : MonoBehaviour
    {
        private class Baker : Baker<FadeCompanionTrailRendererTagAuthoring>
        {
            public override void Bake(FadeCompanionTrailRendererTagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<InitializeFadeCompanionTrailRenderer>(entity);
            }
        }
    }

    /// <summary>
    /// System to initialize the <see cref="FadeCompanionTrailRenderer"/> cleanup component. Also upon entity destruction, this system will call the <see cref="JetpackTrailRendererController.EndTrailRenderer"/> method which provides a nice fade out effect of the trail renderer.
    /// </summary>
    /// <remarks>
    /// This system updates in the <see cref="DS_EffectsSystemGroup"/> which updates towards the end of the frame.
    /// </remarks>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct FadeCompanionTrailRendererSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (companionGameObject, entity) in SystemAPI.Query<CompanionGameObjectData>().WithAll<InitializeFadeCompanionTrailRenderer>().WithNone<FadeCompanionTrailRenderer>().WithEntityAccess())
            {
                if (!companionGameObject.Transform.Value.gameObject.TryGetComponent<JetpackTrailRendererController>(out var trailRendererController))
                {
                    Debug.LogError($"Error: companion GameObject of Entity: {entity.ToString()} has no JetpackTrailRendererController component");
                    continue;
                }

                ecb.AddComponent(entity, new FadeCompanionTrailRenderer { Value = trailRendererController });
                ecb.RemoveComponent<InitializeFadeCompanionTrailRenderer>(entity);
            }

            foreach (var (companionTrailRenderer, entity) in SystemAPI.Query<FadeCompanionTrailRenderer>().WithNone<LocalToWorld>().WithEntityAccess())
            {
                companionTrailRenderer.Value.Value.EndTrailRenderer();
                ecb.RemoveComponent<FadeCompanionTrailRenderer>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}