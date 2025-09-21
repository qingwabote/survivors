using System;
using UnityEngine;
using Unity.Entities;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Singleton MonoBehaviour to control the pause state of the game.
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        /// <summary>
        /// Public singleton access to this MonoBehaviour.
        /// </summary>
        public static PauseManager Instance;

        /// <summary>
        /// Event to be invoked when the game enters the paused state.
        /// </summary>
        /// <remarks>
        /// This triggers certain effects (i.e. sound effects) to enter a paused state as well.
        /// </remarks>
        public Action OnPauseGame;
        /// <summary>
        /// Event to be invoked when the game exits the paused state and resumes the game.
        /// </summary>
        /// <remarks>
        /// This triggers certain effects (i.e. sound effects) to exit the paused state and resume their effects.
        /// </remarks>
        public Action OnResumeGame;

        /// <summary>
        /// Boolean to determine if the game is currently in a paused state.
        /// </summary>
        public bool IsPaused { get; private set; }
        
        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Warning multiple instances of PauseManager detected. Destroying new one.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// Pauses the game.
        /// </summary>
        /// <remarks>
        /// Disabled Unity's top-level system groups of InitializationSystemGroup and SimulationSystemGroup that all gameplay systems exist under, effectively pausing the game.
        /// </remarks>
        public void PauseGame()
        {
            var initializationSystemGroup = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<InitializationSystemGroup>();
            initializationSystemGroup.Enabled = false;
            var simulationSystemGroup = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SimulationSystemGroup>();
            simulationSystemGroup.Enabled = false;
            OnPauseGame?.Invoke();
            IsPaused = true;
        }

        /// <summary>
        /// Resumes the game.
        /// </summary>
        /// <remarks>
        /// Enables Unity's top-level system groups of InitializationSystemGroup and SimulationSystemGroup that all gameplay systems exist under, effectively resuming the game.
        /// </remarks>
        public void ResumeGame()
        {
            var initializationSystemGroup = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<InitializationSystemGroup>();
            initializationSystemGroup.Enabled = true;
            var simulationSystemGroup = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SimulationSystemGroup>();
            simulationSystemGroup.Enabled = true;
            OnResumeGame?.Invoke();
            IsPaused = false;
        }
    }
}