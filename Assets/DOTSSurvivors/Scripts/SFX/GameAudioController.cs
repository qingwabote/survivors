using UnityEngine;
using UnityEngine.Audio;
using Unity.Entities;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Enum used to set the priority of audio clips being played.
    /// </summary>
    /// <remarks>
    /// When lots of audio sources are being played at once, audio sources with a lower priority value are more likely to play than audio sources with a higher priority value.
    /// </remarks>
    /// <seealso cref="GameAudioController"/>
    /// <seealso cref="MainMenuAudioController"/>
    public enum AudioPriority : byte
    {
        Highest = 0,
        NextHighest = 1,
        VeryHigh = 16,
        High = 32,
        AboveAverage = 64,
        Average = 128,
        Low = 192,
        Lowest = 255
    }
    
    /// <summary>
    /// MonoBehaviour for controlling audio in game.
    /// </summary>
    /// <seealso cref="MainMenuAudioController"/>
    public class GameAudioController : MonoBehaviour
    {
        /// <summary>
        /// Public singleton access to play audio in game.
        /// </summary>
        public static GameAudioController Instance;
        
        /// <summary>
        /// Audio clip to play when the player is destroyed and the game is over.
        /// </summary>
        [SerializeField] private AudioClip _gameOverAudioClip;
        /// <summary>
        /// Audio source to play background music from. Used for playing/pausing background music in certain scenarios.
        /// </summary>
        [SerializeField] private AudioSource _backgroundMusicAudioSource;
        /// <summary>
        /// Sound effects pool count. At the beginning of the level, this number of audio sources are spawned which can play one-shot sound effects.
        /// </summary>
        [SerializeField] private int _sfxPoolCount = 100;
        /// <summary>
        /// Looping sound effects pool count. At the beginning of the level, this number of audio sources are spawned which can play looping sound effects.
        /// </summary>
        [SerializeField] private int _loopingPoolCount = 50;
        /// <summary>
        /// Pause resistant sound effects pool count. At the beginning of the level, this number of audio sources are spawned which can play sound effects that will not pause during pause events that would pause other sound effect types.
        /// </summary>
        [SerializeField] private int _pauseResistantPoolCount = 10;
        /// <summary>
        /// AudioMixer used to set audio levels for music and sound effects independently.
        /// </summary>
        [SerializeField] private AudioMixer _audioMixer;
        /// <summary>
        /// AudioMixerGroup for sound effects, used during instantiation of pool for AudioSources for sound effects.
        /// </summary>
        [SerializeField] private AudioMixerGroup _soundEffectsMixerGroup;
        
        /// <summary>
        /// This controller subscribes to events from ECS systems. At the beginning of the game, this controller will attempt to subscribe to the events, if it fails more than this amount of times, it is likely there is an issue with ECS initializing systems and an error will be thrown. This is used as a way to ensure events are subscribed to even if ECS systems are not yet created at when this controller is loaded.
        /// </summary>
        private const int EVENT_SCHEDULE_FAIL_COUNT = 600;

        /// <summary>
        /// Pool of audio sources to play regular sound effects.
        /// </summary>
        private AudioSource[] _sfxAudioSources;
        /// <summary>
        /// Index to keep track of the current audio source in the respective pool to play sound effects from.
        /// </summary>
        private int _sfxAudioSourceIndex;

        /// <summary>
        /// Pool of audio sources to play looping sound effects.
        /// </summary>
        private AudioSource[] _loopingAudioSources;
        /// <summary>
        /// Index to keep track of the current audio source in the respective pool to play sound effects from.
        /// </summary>
        private int _loopingAudioSourceIndex;

        /// <summary>
        /// Pool of audio sources to play pause resistant sound effects.
        /// </summary>
        private AudioSource[] _pauseResistantAudioSources;
        /// <summary>
        /// Index to keep track of the current audio source in the respective pool to play sound effects from.
        /// </summary>
        private int _pauseResistantAudioSourceIndex;
        
        /// <summary>
        /// List to keep track of sound effect clips that were paused during a pause event and should be resumed when the game resumes.
        /// </summary>
        private List<int> _pausedSfxClipIndices;
        /// <summary>
        /// List to keep track of looping sound effect clips that were paused during a pause event and should be resumed when the game resumes.
        /// </summary>
        private List<int> _pausedLoopingClipIndices;

        /// <summary>
        /// Boolean to denote the game is over and sound effects should not be resumed.
        /// </summary>
        private bool _isGameOver;
        
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
                Debug.LogWarning("Warning, multiple instances of AudioController detected. Destroying new instance");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
        
        /// <summary>
        /// Instantiates and initializes sound effects pools. Loads volume levels for music and sfx channels from <see cref="PersistentDataManager"/>.
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
            
            _loopingAudioSources = new AudioSource[_loopingPoolCount];
            for (var i = 0; i < _loopingPoolCount; i++)
            {
                var newLoopingAudioSourceGameObject = new GameObject($"LoopingAudioSource_{i}");
                newLoopingAudioSourceGameObject.transform.parent = transform;
                var newLoopingAudioSource = newLoopingAudioSourceGameObject.AddComponent<AudioSource>();
                newLoopingAudioSource.loop = true;
                newLoopingAudioSource.outputAudioMixerGroup = _soundEffectsMixerGroup;
                newLoopingAudioSource.priority = (int)AudioPriority.AboveAverage;
                _loopingAudioSources[i] = newLoopingAudioSource;
            }

            _pauseResistantAudioSources = new AudioSource[_pauseResistantPoolCount];
            for (var i = 0; i < _pauseResistantPoolCount; i++)
            {
                var newPauseResistantAudioSourceGameObject = new GameObject($"PauseResistantAudioSource_{i}");
                newPauseResistantAudioSourceGameObject.transform.parent = transform;
                var newPauseResistantAudioSource = newPauseResistantAudioSourceGameObject.AddComponent<AudioSource>();
                newPauseResistantAudioSource.outputAudioMixerGroup = _soundEffectsMixerGroup;
                newPauseResistantAudioSource.priority = (int)AudioPriority.High;
                _pauseResistantAudioSources[i] = newPauseResistantAudioSource;
            }

            var savedMusicVolumeLevel = PersistentDataManager.Instance.MusicVolumeLevel;
            _audioMixer.SetFloat(MUSIC_VOLUME_PARAMETER, savedMusicVolumeLevel);

            var savedSfxVolumeLevel = PersistentDataManager.Instance.SfxVolumeLevel;
            _audioMixer.SetFloat(SFX_VOLUME_PARAMETER, savedSfxVolumeLevel);
        }
        
        private void OnEnable()
        {
            StartCoroutine(DelayEventSubscription());
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// This controller subscribes to events from ECS systems. At the beginning of the game, this controller will attempt to subscribe to the events, if it fails more than <see cref="EVENT_SCHEDULE_FAIL_COUNT"/> times, it is likely there is an issue with ECS initializing systems and an error will be thrown. This is used as a way to ensure events are subscribed to even if ECS systems are not yet created at when this controller is loaded.
        /// </summary>
        private IEnumerator DelayEventSubscription()
        {
            yield return null;
            var failCount = 0;
            var defaultWorld = World.DefaultGameObjectInjectionWorld;
            while (defaultWorld == null)
            {
                failCount++;
                if (failCount > EVENT_SCHEDULE_FAIL_COUNT)
                {
                    Debug.LogError($"Default World was null for {EVENT_SCHEDULE_FAIL_COUNT} frames. Check to ensure ECS world is being properly initialized.");
                    yield break;
                }
                yield return null;
                defaultWorld = World.DefaultGameObjectInjectionWorld;
            }

            SubscribeToEvents(defaultWorld);
        }

        private void SubscribeToEvents(World world)
        {
            PauseManager.Instance.OnPauseGame += PauseAudioClips;
            PauseManager.Instance.OnResumeGame += ResumeAudioClips;
            
            var beginGameOverSystem = world.GetExistingSystemManaged<BeginGameOverSystem>();
            beginGameOverSystem.OnGameOver += PlayGameOverAudio;
        }

        private void UnsubscribeFromEvents()
        {
            PauseManager.Instance.OnPauseGame -= PauseAudioClips;
            PauseManager.Instance.OnResumeGame -= ResumeAudioClips;
            
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;
            
            var beginGameOverSystem = world.GetExistingSystemManaged<BeginGameOverSystem>();
            beginGameOverSystem.OnGameOver -= PlayGameOverAudio;
        }

        /// <summary>
        /// When the game is over, play the <see cref="_gameOverAudioClip"/>.
        /// </summary>
        private void PlayGameOverAudio()
        {
            _isGameOver = true;
            _backgroundMusicAudioSource.Stop();
            PlayPauseResistantAudioClip(_gameOverAudioClip);
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
        /// Method for playing a looping sound effects clip.
        /// </summary>
        /// <param name="audioClip">Audio clip to play.</param>
        /// <param name="priority">Priority of the sound effect. <see cref="AudioPriority"/></param>
        /// <returns>Returns the index in the <see cref="_loopingAudioSources"/> of the audio source this effect is playing out of. This is used so <see cref="LoopingAudioSourceIndex"/> can stop the looping audio source when it should no longer play.</returns>
        public int PlayLoopingAudioClip(AudioClip audioClip, int priority = (int)AudioPriority.AboveAverage)
        {
            var audioSourceIndex = _loopingAudioSourceIndex;
            var currentAudioSource = _loopingAudioSources[audioSourceIndex];
            currentAudioSource.clip = audioClip;
            currentAudioSource.priority = priority;
            currentAudioSource.Play();
            _loopingAudioSourceIndex += 1;
            _loopingAudioSourceIndex %= _loopingAudioSources.Length;
            return audioSourceIndex;
        }

        /// <summary>
        /// Method for stopping a looping audio source.
        /// </summary>
        /// <param name="index">Index in the <see cref="_loopingAudioSources"/> of the audio source to stop playing.</param>
        /// <seealso cref="LoopingAudioSourceIndex"/>
        public void StopLoopingAudioClip(int index)
        {
            var currentAudioClip = _loopingAudioSources[index];
            currentAudioClip.Stop();
        }

        /// <summary>
        /// Method for playing a pause resistant sound effects clip.
        /// </summary>
        /// <remarks>
        /// This is typically used for sound effects that must play when the game is in a paused state. i.e. level up sound effect.
        /// </remarks>
        /// <param name="audioClip">Audio clip to play.</param>
        /// <param name="priority">Priority of the sound effect. <see cref="AudioPriority"/></param>
        public void PlayPauseResistantAudioClip(AudioClip audioClip, int priority = (int)AudioPriority.High)
        {
            var currentAudioSource = _pauseResistantAudioSources[_pauseResistantAudioSourceIndex];
            currentAudioSource.clip = audioClip;
            currentAudioSource.priority = priority;
            currentAudioSource.Play();
            _pauseResistantAudioSourceIndex += 1;
            _pauseResistantAudioSourceIndex %= _pauseResistantAudioSources.Length;
        }

        /// <summary>
        /// Method for toggling the background music between play and pause state.
        /// </summary>
        /// <remarks>
        /// Certain events such as picking up a crate should pause background music. It should resume once the crate UI is closed.
        /// </remarks>
        public void PlayPauseBackgroundMusic()
        {
            if (_backgroundMusicAudioSource.isPlaying)
            {
                _backgroundMusicAudioSource.Pause();
            }
            else
            {
                _backgroundMusicAudioSource.Play();
            }
        }

        /// <summary>
        /// Method to pause audio clips.
        /// </summary>
        /// <remarks>
        /// When game pauses for various reasons, normal sound effects and looping sound effects should be paused.
        /// Note that this will not pause sound effects that are pause resistant.
        /// </remarks>
        private void PauseAudioClips()
        {
            _pausedSfxClipIndices = new List<int>();
            for (var i = 0; i < _sfxAudioSources.Length; i++)
            {
                var currentAudioSource = _sfxAudioSources[i];
                if (!currentAudioSource.isPlaying) continue;
                currentAudioSource.Pause();
                _pausedSfxClipIndices.Add(i);
            }
            
            _pausedLoopingClipIndices = new List<int>();
            for (var i = 0; i < _loopingAudioSources.Length; i++)
            {
                var currentAudioSource = _loopingAudioSources[i];
                if (!currentAudioSource.isPlaying) continue;
                currentAudioSource.Pause();
                _pausedLoopingClipIndices.Add(i);
            }
        }

        /// <summary>
        /// Method for resuming audio clips when the game returns to a playing state from a paused state.
        /// </summary>
        private void ResumeAudioClips()
        {
            if (_isGameOver) return;
            if (_pausedSfxClipIndices != null)
            {
                foreach (var pausedClipIndex in _pausedSfxClipIndices)
                {
                    _sfxAudioSources[pausedClipIndex].Play();
                }

                _pausedSfxClipIndices.Clear();
            }

            if (_pausedLoopingClipIndices != null)
            {
                foreach (var pausedClipIndex in _pausedLoopingClipIndices)
                {
                    _loopingAudioSources[pausedClipIndex].Play();
                }

                _pausedLoopingClipIndices.Clear();
            }
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