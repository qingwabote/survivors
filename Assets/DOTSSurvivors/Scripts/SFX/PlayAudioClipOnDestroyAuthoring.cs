using Unity.Entities;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component to initialize <see cref="PlayAudioClipOnDestroyData"/>.
    /// </summary>
    /// <remarks>
    /// As <see cref="PlayAudioClipOnDestroyData"/> is a cleanup component, it cannot be added to the entity through baking so this initialization component is used to pass these values to the instance.
    /// </remarks>
    public struct InitializePlayAudioClipOnDestroyData : IComponentData
    {
        /// <summary>
        /// Priority of the audio clip to play. Lower values have a higher priority.
        /// </summary>
        /// <seeaslo cref="AudioPriority"/>
        public byte Priority;
        /// <summary>
        /// Audio clip to play when this entity is destroyed.
        /// </summary>
        public UnityObjectRef<AudioClip> AudioClip;
    }
    
    /// <summary>
    /// Data component holding the audio clip that will be played when the associated entity is destroyed.
    /// </summary>
    /// <seealso cref="DestroyEntitySystem"/>
    /// <remarks>
    /// As this is a cleanup component, it cannot be added to the entity through baking, so the <see cref="InitializePlayAudioClipOnDestroyData"/> is used to set this up.
    /// </remarks>
    public struct PlayAudioClipOnDestroyData : ICleanupComponentData
    {
        /// <summary>
        /// Priority of the audio clip to play. Lower values have a higher priority.
        /// </summary>
        /// <seeaslo cref="AudioPriority"/>
        public byte Priority;
        /// <summary>
        /// Audio clip to play when this entity is destroyed.
        /// </summary>
        public UnityObjectRef<AudioClip> AudioClip;
    }

    /// <summary>
    /// Authoring script to initialize <see cref="PlayAudioClipOnDestroyData"/>.
    /// </summary>
    /// <seealso cref="DestroyEntitySystem"/>
    public class PlayAudioClipOnDestroyAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Priority of the audio clip to play. Lower values have a higher priority.
        /// </summary>
        public AudioPriority Priority = AudioPriority.Average;
        /// <summary>
        /// Audio clip to play when this entity is destroyed.
        /// </summary>
        public AudioClip AudioClip;

        private class Baker : Baker<PlayAudioClipOnDestroyAuthoring>
        {
            public override void Bake(PlayAudioClipOnDestroyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new InitializePlayAudioClipOnDestroyData
                {
                    Priority = (byte)authoring.Priority,
                    AudioClip = authoring.AudioClip
                });
            }
        }
    }

    /// <summary>
    /// System to handle playing audio clip when entity is destroyed.
    /// </summary>
    /// <remarks>
    /// As <see cref="PlayAudioClipOnDestroyData"/> is a cleanup component, it cannot be baked into a prefab, so we also need to initialize this component with <see cref="InitializePlayAudioClipOnDestroyData"/> shortly after the entity is spawned.
    /// </remarks>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct PlayAudioClipOnDestroySystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            
            foreach (var (initializationData, entity) in SystemAPI.Query<InitializePlayAudioClipOnDestroyData>().WithNone<PlayAudioClipOnDestroyData>().WithEntityAccess())
            {
                ecb.AddComponent(entity, new PlayAudioClipOnDestroyData
                {
                    Priority = initializationData.Priority,
                    AudioClip = initializationData.AudioClip
                });
                ecb.RemoveComponent<InitializePlayAudioClipOnDestroyData>(entity);
            }

            foreach (var (audioData, entity) in SystemAPI.Query<PlayAudioClipOnDestroyData>().WithNone<Simulate>().WithEntityAccess())
            {
                GameAudioController.Instance.PlaySfxAudioClip(audioData.AudioClip.Value, audioData.Priority);
                ecb.RemoveComponent<PlayAudioClipOnDestroyData>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}