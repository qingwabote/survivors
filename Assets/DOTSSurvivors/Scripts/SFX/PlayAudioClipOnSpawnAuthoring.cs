using UnityEngine;
using Unity.Entities;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component holding the audio clip that will be played when the associated entity is spawned.
    /// </summary>
    public struct PlayAudioClipOnSpawnData : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// Priority of the audio clip to play. Lower values have a higher priority
        /// </summary>
        /// <seeaslo cref="AudioPriority"/>
        public byte Priority;
        /// <summary>
        /// Audio clip to play when this entity is spawned.
        /// </summary>
        public UnityObjectRef<AudioClip> AudioClip;
    }
    
    /// <summary>
    /// Tag component to signify this entity should loop their associated audio clip for its full lifecycle.
    /// </summary>
    /// <seealso cref="PlayAudioClipOnSpawnData"/>
    public struct ShouldLoopAudioClipTag : IComponentData {}
    
    /// <summary>
    /// Component to store the index of the looping audio source in <see cref="GameAudioController._loopingAudioSources"/>.
    /// </summary>
    /// <remarks>
    /// Cleanup component will call <see cref="GameAudioController.StopLoopingAudioClip"/> when the associated entity is destroyed to stop the looping sound effect.
    /// </remarks>
    public struct LoopingAudioSourceIndex : ICleanupComponentData
    {
        public int AudioClipIndex;
    }
    
    /// <summary>
    /// Authoring script to add necessary components for playing audio clips when the entity is spawned.
    /// </summary>
    public class PlayAudioClipOnSpawnAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Priority of the audio clip to play. Lower values have a higher priority.
        /// </summary>
        public AudioPriority Priority = AudioPriority.Average;
        /// <summary>
        /// Audio clip to play when this entity is spawned.
        /// </summary>
        public AudioClip AudioClip;
        /// <summary>
        /// Boolean to denote this entity as an entity that should loop its audio for the duration of its lifecycle.
        /// </summary>
        public bool ShouldLoop;

        private class Baker : Baker<PlayAudioClipOnSpawnAuthoring>
        {
            public override void Bake(PlayAudioClipOnSpawnAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PlayAudioClipOnSpawnData
                {
                    Priority = (byte)authoring.Priority,
                    AudioClip = authoring.AudioClip
                });

                if (authoring.ShouldLoop)
                {
                    AddComponent<ShouldLoopAudioClipTag>(entity);
                }
            }
        }
    }

    /// <summary>
    /// System to play audio clips when an entity is spawned.
    /// </summary>
    /// <remarks>
    /// Entities can be tagged with a <see cref="ShouldLoopAudioClipTag"/> which will loop the audio clip for the duration of its lifecycle.
    /// </remarks>
    /// <seealso cref="PlayAudioClipOnSpawnData"/>
    /// <seealso cref="ShouldLoopAudioClipTag"/>
    /// <seealso cref="LoopingAudioSourceIndex"/>
    /// <seealso cref="GameAudioController"/>
    /// <seealso cref="DestroyEntitySystem"/>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct PlayAudioClipOnSpawnSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            // Plays non-looping audio clips when spawned.
            foreach (var (audioClip, audioClipEnabled) in SystemAPI.Query<PlayAudioClipOnSpawnData, EnabledRefRW<PlayAudioClipOnSpawnData>>().WithNone<ShouldLoopAudioClipTag, LoopingAudioSourceIndex>())
            {
                GameAudioController.Instance.PlaySfxAudioClip(audioClip.AudioClip.Value, audioClip.Priority);
                audioClipEnabled.ValueRW = false;
            }

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            
            // Plays looping audio clips when spawned.
            foreach (var (audioClip, audioClipEnabled, entity) in SystemAPI.Query<PlayAudioClipOnSpawnData, EnabledRefRW<PlayAudioClipOnSpawnData>>().WithAll<ShouldLoopAudioClipTag>().WithNone<LoopingAudioSourceIndex>().WithEntityAccess())
            {
                var audioClipIndex = GameAudioController.Instance.PlayLoopingAudioClip(audioClip.AudioClip.Value, audioClip.Priority);
                audioClipEnabled.ValueRW = false;
                ecb.AddComponent(entity, new LoopingAudioSourceIndex { AudioClipIndex = audioClipIndex });
            }

            // Stops looping audio clips when the entity is destroyed.
            foreach (var (loopAudioClip, entity) in SystemAPI.Query<LoopingAudioSourceIndex>().WithNone<ShouldLoopAudioClipTag>().WithEntityAccess())
            {
                GameAudioController.Instance.StopLoopingAudioClip(loopAudioClip.AudioClipIndex);
                ecb.RemoveComponent<LoopingAudioSourceIndex>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}