using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// GameObject prefab for the particle system that should play when this entity takes damage.
    /// </summary>
    /// <seealso cref="PlayParticleSystemOnDamage"/>
    public struct OnDamageParticleSystemPrefab : IComponentData
    {
        public UnityObjectRef<GameObject> Value;
    }
    
    /// <summary>
    /// Cleanup component to hold a reference to the particle system that will play when this entity takes damage.
    /// </summary>
    /// <remarks>
    /// As this is a cleanup component, this value will be initialized via the <see cref="OnDamageParticleSystemPrefab"/> as a GameObject instance will not be able to be baked into this component.
    /// </remarks>
    public struct PlayParticleSystemOnDamage : ICleanupComponentData
    {
        public UnityObjectRef<ParticleSystem> Value;
    }
    
    /// <summary>
    /// Enableable component to signify this entity should begin playing the particle system as it is taking damage.
    /// </summary>
    public struct PlayParticleSystemOnDamageFlag : IComponentData, IEnableableComponent {}

    /// <summary>
    /// Authoring script to add components necessary for playing a particle system when an entity takes damage.
    /// </summary>
    /// <remarks>
    /// Authoring script should be attached to the root character entity.
    /// </remarks>
    public class PlayParticleSystemOnDamageAuthoring : MonoBehaviour
    {
        /// <summary>
        /// GameObject prefab for the particle system that should play when this entity takes damage.
        /// </summary>
        public GameObject ParticleSystemPrefab;

        private class Baker : Baker<PlayParticleSystemOnDamageAuthoring>
        {
            public override void Bake(PlayParticleSystemOnDamageAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new OnDamageParticleSystemPrefab
                {
                    Value = authoring.ParticleSystemPrefab
                });
                AddComponent<PlayParticleSystemOnDamageFlag>(entity);
                SetComponentEnabled<PlayParticleSystemOnDamageFlag>(entity, false);
            }
        }
    }

    /// <summary>
    /// System to manage the lifecycle of particle systems that play when an entity takes damage.
    /// Has 3 foreach loops that do the following:
    /// 1. Instantiate an instance of the particle system prefab from <see cref="OnDamageParticleSystemPrefab"/> and store a reference to it in <see cref="PlayParticleSystemOnDamage"/>.
    /// 2. Sync the transform of the particle system to the entity's transform. If <see cref="PlayParticleSystemOnDamage"/> is enabled, it will play the particle system.
    /// 3. When the entity is destroyed, destroy the particle system GameObject.
    /// </summary>
    /// <remarks>
    /// This system updates in the <see cref="DS_EffectsSystemGroup"/> which updates towards the end of the frame. As such, transform is synchronized via the entity's LocalToWorld component as this will have been updated by this point in the frame.
    /// </remarks>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct PlayParticleSystemOnDamageSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            
            // Instantiate particle system
            foreach (var (particleSystemPrefab, entity) in SystemAPI.Query<OnDamageParticleSystemPrefab>().WithNone<PlayParticleSystemOnDamage>().WithEntityAccess())
            {
                var newParticleSystem = Object.Instantiate(particleSystemPrefab.Value.Value);
                var playParticleSystemOnDamage = new PlayParticleSystemOnDamage
                {
                    Value = newParticleSystem.GetComponent<ParticleSystem>()
                };
                ecb.AddComponent(entity, playParticleSystemOnDamage);
                ecb.RemoveComponent<OnDamageParticleSystemPrefab>(entity);
            }

            // Sync particle system transform and play if necessary
            foreach (var (transform, particleSystem, playParticleSystem) in SystemAPI.Query<LocalToWorld, PlayParticleSystemOnDamage, EnabledRefRW<PlayParticleSystemOnDamageFlag>>().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                particleSystem.Value.Value.transform.position = transform.Position;
                if (playParticleSystem.ValueRO)
                {
                    playParticleSystem.ValueRW = false;
                    particleSystem.Value.Value.Play();
                }
            }

            // Cleanup particle system
            foreach (var (particleSystem, entity) in SystemAPI.Query<PlayParticleSystemOnDamage>().WithNone<LocalToWorld>().WithEntityAccess())
            {
                if (particleSystem.Value.Value != null)
                {
                    Object.Destroy(particleSystem.Value.Value.gameObject);
                }
                ecb.RemoveComponent<PlayParticleSystemOnDamage>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}