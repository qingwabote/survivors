using System;
using Unity.Entities;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Tag component to signify the <see cref="BeginGameOverSystem"/> should begin execution.
    /// </summary>
    public struct BeginGameOverTag : IComponentData {}
    
    /// <summary>
    /// System to invoke the begin game over sequence.
    /// </summary>
    /// <remarks>
    /// This system is required so that <see cref="DestroyEntitySystem"/> can be burst compiled as it moves the Action event managed type out of that system.
    /// </remarks>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup))]
    public partial class BeginGameOverSystem : SystemBase
    {
        /// <summary>
        /// This event is invoked as soon as the player is destroyed to trigger the Game Over sequence.
        /// </summary>
        /// <seeaslo cref="GameOverUIController"/>
        public Action OnGameOver;
        
        protected override void OnCreate()
        {
            RequireForUpdate<BeginGameOverTag>();
        }

        protected override void OnStartRunning()
        {
            OnGameOver?.Invoke();
            EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<BeginGameOverTag>());
        }

        protected override void OnUpdate()
        {
            
        }
    }
}