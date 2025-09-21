using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Component to hold the timestamp for when the player can be damaged again by the <see cref="DamagePlayerOutsideSafeBoundsSystem"/>.
    /// </summary>
    /// <remarks>
    /// Component gets added to the <see cref="DamagePlayerOutsideSafeBoundsSystem"/> SystemHandle entity.
    /// </remarks>
    public struct DamagePlayerOutsideSafeBoundsTimestamp : IComponentData
    {
        public float Value;
    }
    
    /// <summary>
    /// System to apply damage to the player when it is outside the <see cref="LevelSafeBounds"/> for the level.
    /// </summary>
    /// <remarks>
    /// Level safe bounds are identified by a caution tape graphic around the edge of the safe bounds.
    /// </remarks>
    public partial struct DamagePlayerOutsideSafeBoundsSystem : ISystem
    {
        private const float DAMAGE_DELAY_TIME = 0.25f;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LevelSafeBounds>();
            state.EntityManager.AddComponent<DamagePlayerOutsideSafeBoundsTimestamp>(state.SystemHandle);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var safeBounds = SystemAPI.GetSingleton<LevelSafeBounds>();
            
            foreach (var (transform, damageThisFrame) in SystemAPI.Query<LocalTransform, DynamicBuffer<DamageThisFrame>>().WithAll<PlayerTag>())
            {
                if (safeBounds.InsideSafeBounds(transform.Position.xz)) continue;
                var damageTimestamp = SystemAPI.GetComponentRW<DamagePlayerOutsideSafeBoundsTimestamp>(state.SystemHandle);
                if(damageTimestamp.ValueRO.Value <= (float)SystemAPI.Time.ElapsedTime)
                {
                    damageThisFrame.Add(new DamageThisFrame { Value = 10 });
                    damageTimestamp.ValueRW.Value = (float)SystemAPI.Time.ElapsedTime + DAMAGE_DELAY_TIME;
                }
            }
        }
    }
}