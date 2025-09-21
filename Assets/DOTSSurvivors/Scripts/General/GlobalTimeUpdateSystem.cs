using UnityEngine;
using Unity.Entities;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// System to update the global time shader property. This is required so that when the game pauses, any animated shaders that rely on this property will also pause as this system is part of a group that will be disabled when the game is paused due to level up or pressing the ESC key (<see cref="PauseManager"/>).
    /// </summary>
    /// <remarks>
    /// System updates in Unity's InitializationSystemGroup and after their UpdateWorldTimeSystem to ensure that SystemAPI.Time.ElapsedTime is accurate for the current frame.
    /// </remarks>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(UpdateWorldTimeSystem))]
    public partial class GlobalTimeUpdateSystem : SystemBase
    {
        /// <summary>
        /// Cached integer of the shader property's ID, used for more efficient setting of shader property.
        /// </summary>
        private static int _globalTimeShaderPropertyID;

        protected override void OnCreate()
        {
            _globalTimeShaderPropertyID = Shader.PropertyToID("_GlobalTime");           
        }
        
        protected override void OnUpdate()
        {
            Shader.SetGlobalFloat(_globalTimeShaderPropertyID, (float)SystemAPI.Time.ElapsedTime);
        }
    }
}