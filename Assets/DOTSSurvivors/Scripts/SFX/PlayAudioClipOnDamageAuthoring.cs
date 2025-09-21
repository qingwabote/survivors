using UnityEngine;
using Unity.Entities;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component for defining the audio clip to play when the associated entity takes damage.
    /// </summary>
    /// <remarks>
    /// Enableable component - when enabled, the audio clip will play.
    /// </remarks>
    /// <seealso cref="PlayAudioClipOnDamageSystem"/>
    public struct PlayAudioClipOnDamageData : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// Priority of the audio clip to play. Lower values have a higher priority
        /// </summary>
        /// <seeaslo cref="AudioPriority"/>
        public byte Priority;
        /// <summary>
        /// Audio clip to play when this entity takes damage.
        /// </summary>
        public UnityObjectRef<AudioClip> AudioClip;
    }
    
    /// <summary>
    /// Authoring script to add components to entity required for playing audio clips when taking damage.
    /// </summary>
    public class PlayAudioClipOnDamageAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Priority of the audio clip to play. Lower values have a higher priority.
        /// </summary>
        public AudioPriority Priority = AudioPriority.Average;
        /// <summary>
        /// Audio clip to play when this entity takes damage.
        /// </summary>
        public AudioClip AudioClip;

        private class Baker : Baker<PlayAudioClipOnDamageAuthoring>
        {
            public override void Bake(PlayAudioClipOnDamageAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PlayAudioClipOnDamageData
                {
                    AudioClip = authoring.AudioClip,
                    Priority = (byte)authoring.Priority
                });
                SetComponentEnabled<PlayAudioClipOnDamageData>(entity, false);
            }
        }
    }

    /// <summary>
    /// System for playing audio clips when an entity takes damage.
    /// </summary>
    /// <remarks>
    /// <see cref="ProcessDamageThisFrameSystem"/> will enable the <see cref="PlayAudioClipOnDamageData"/> component if the entity takes damage.
    /// System updates in the <see cref="DS_EffectsSystemGroup"/> which executes towards the end of the frame after <see cref="ProcessDamageThisFrameSystem"/> has ran for the frame.
    /// </remarks>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct PlayAudioClipOnDamageSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (audioClip, audioClipEnabled) in SystemAPI.Query<PlayAudioClipOnDamageData, EnabledRefRW<PlayAudioClipOnDamageData>>())
            {
                GameAudioController.Instance.PlaySfxAudioClip(audioClip.AudioClip.Value, audioClip.Priority);
                audioClipEnabled.ValueRW = false;
            }
        }
    }
}