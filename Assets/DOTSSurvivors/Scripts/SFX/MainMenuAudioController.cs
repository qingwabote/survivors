using UnityEngine;
using UnityEngine.Audio;
using Unity.Mathematics;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// MonoBehaviour for controlling audio in main menu.
    /// </summary>
    /// <seealso cref="GameAudioController"/>
    public class MainMenuAudioController : MonoBehaviour
    {
        /// <summary>
        /// Public singleton access to play audio in main menu.
        /// </summary>
        public static MainMenuAudioController Instance;
        
        /// <summary>
        /// Sound effects pool count. At the beginning of the main menu scene, this number of audio sources are spawned which can play one-shot sound effects.
        /// </summary>
        [SerializeField] private int _sfxPoolCount = 10;
        /// <summary>
        /// AudioMixer used to set audio levels for music and sound effects independently.
        /// </summary>
        [SerializeField] private AudioMixer _audioMixer;
        /// <summary>
        /// AudioMixerGroup for sound effects, used during instantiation of pool for AudioSources for sound effects.
        /// </summary>
        [SerializeField] private AudioMixerGroup _soundEffectsMixerGroup;
        
        /// <summary>
        /// Pool of audio sources to play regular sound effects.
        /// </summary>
        private AudioSource[] _sfxAudioSources;
        /// <summary>
        /// Index to keep track of the current audio source in the respective pool to play sound effects from.
        /// </summary>
        private int _sfxAudioSourceIndex;

        /// <summary>
        /// Maximum dB to play music.
        /// </summary>
        private const float MAX_MUSIC_DB = 0f;
        /// <summary>
        /// Maximum dB to play sound effects.
        /// </summary>
        private const float MAX_SFX_DB = 0f;
        /// <summary>
        /// Minimum dB to play music before totally muting music channel.
        /// </summary>
        private const float MIN_MUSIC_DB = -40f;
        /// <summary>
        /// Minimum dB to play sound effects before totally muting sound effects channel.
        /// </summary>
        private const float MIN_SFX_DB = -40f;
        /// <summary>
        /// Cutoff level for volume UI sliders. If value is below this, audio for the associated channel will be totally muted.
        /// </summary>
        private const float MIN_CUTOFF_LEVEL = 0.05f;
        /// <summary>
        /// String for the music volume parameter.
        /// </summary>
        private const string MUSIC_VOLUME_PARAMETER = "MusicVolume";
        /// <summary>
        /// String for the sound effects volume parameter.
        /// </summary>
        private const string SFX_VOLUME_PARAMETER = "SFXVolume";
        
        /// <summary>
        /// Initializes singleton instance.
        /// </summary>
        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Warning, multiple instances of MainMenuAudioController detected. Destroying new instance.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
        
        /// <summary>
        /// Instantiates and initializes sound effects pool. Loads volume levels for music and sfx channels from <see cref="SerializationController"/>.
        /// </summary>
        private void Start()
        {
            _sfxAudioSources = new AudioSource[_sfxPoolCount];
            for (var i = 0; i < _sfxPoolCount; i++)
            {
                var newSfxAudioSourceGameObject = new GameObject($"SFXAudioSource_{i}");
                newSfxAudioSourceGameObject.transform.parent = transform;
                var newSfxAudioSource = newSfxAudioSourceGameObject.AddComponent<AudioSource>();
                newSfxAudioSource.outputAudioMixerGroup = _soundEffectsMixerGroup;
                newSfxAudioSource.priority = (int)AudioPriority.Average;
                _sfxAudioSources[i] = newSfxAudioSource;
            }
            
            var savedMusicVolumeLevel = PersistentDataManager.Instance.MusicVolumeLevel;
            _audioMixer.SetFloat(MUSIC_VOLUME_PARAMETER, savedMusicVolumeLevel);

            var savedSfxVolumeLevel = PersistentDataManager.Instance.SfxVolumeLevel;
            _audioMixer.SetFloat(SFX_VOLUME_PARAMETER, savedSfxVolumeLevel);
        }
        
        /// <summary>
        /// Method for playing a normal sound effects clip.
        /// </summary>
        /// <param name="audioClip">Audio clip to play.</param>
        /// <param name="priority">Priority of the sound effect. <see cref="AudioPriority"/></param>
        public void PlaySfxAudioClip(AudioClip audioClip, int priority = (int)AudioPriority.Average)
        {
            var currentAudioSource = _sfxAudioSources[_sfxAudioSourceIndex];
            currentAudioSource.clip = audioClip;
            currentAudioSource.priority = priority;
            currentAudioSource.Play();
            _sfxAudioSourceIndex += 1;
            _sfxAudioSourceIndex %= _sfxAudioSources.Length;
        }

        /// <summary>
        /// Method to return the normalized music volume level.
        /// </summary>
        /// <remarks>
        /// If volume level is lower than the defined minimum value, 0 will be returned as the music volume can be considered muted.
        /// This is used to set the music volume slider in UI configuration based on its current values.
        /// </remarks>
        /// <returns>Normalized music volume level from 0 and 1</returns>
        public float GetNormalizedMusicLevel()
        {
            _audioMixer.GetFloat(MUSIC_VOLUME_PARAMETER, out var musicVolume);
            if (musicVolume <= MIN_MUSIC_DB)
            {
                return 0f;
            }

            return math.unlerp(MIN_MUSIC_DB, MAX_MUSIC_DB, musicVolume);
        }
        
        /// <summary>
        /// Sets the music volume based on normalized value from UI slider.
        /// </summary>
        /// <remarks>
        /// If normalized value is below the <see cref="MIN_CUTOFF_LEVEL"/>, the music volume will be set to -80dB, effectively muting the channel.
        /// </remarks>
        /// <param name="normalizedLevel">Value from 0 to 1 for the music volume.</param>
        public void SetMusicVolume(float normalizedLevel)
        {
            var newVolume = Mathf.Lerp(MIN_MUSIC_DB, MAX_MUSIC_DB, normalizedLevel);
            if (normalizedLevel < MIN_CUTOFF_LEVEL)
            {
                newVolume = -80;
            }
            _audioMixer.SetFloat(MUSIC_VOLUME_PARAMETER, newVolume);
            PersistentDataManager.Instance.SaveMusicVolume(newVolume);
        }

        /// <summary>
        /// Method to return the normalized sound effects volume level.
        /// </summary>
        /// <remarks>
        /// If volume level is lower than the defined minimum value, 0 will be returned as the sound effects volume can be considered muted.
        /// This is used to set the sound effects volume slider in UI configuration based on its current values.
        /// </remarks>
        /// <returns>Normalized sound effects volume level from 0 and 1</returns>
        public float GetNormalizedSfxLevel()
        {
            _audioMixer.GetFloat(SFX_VOLUME_PARAMETER, out var sfxVolume);
            if (sfxVolume <= MIN_SFX_DB)
            {
                return 0f;
            }

            return math.unlerp(MIN_SFX_DB, MAX_SFX_DB, sfxVolume);
        }
        
        /// <summary>
        /// Sets the sound effects volume based on normalized value from UI slider.
        /// </summary>
        /// <remarks>
        /// If normalized value is below the <see cref="MIN_CUTOFF_LEVEL"/>, the sound effects volume will be set to -80dB, effectively muting the channel.
        /// </remarks>
        /// <param name="normalizedLevel">Value from 0 to 1 for the sound effects volume.</param>
        public void SetSfxVolume(float normalizedLevel)
        {
            var newVolume = Mathf.Lerp(MIN_SFX_DB, MAX_SFX_DB, normalizedLevel);
            if (normalizedLevel < MIN_CUTOFF_LEVEL)
            {
                newVolume = -80;
            }
            _audioMixer.SetFloat(SFX_VOLUME_PARAMETER, newVolume);
            PersistentDataManager.Instance.SaveSfxVolume(newVolume);
        }
    }
}