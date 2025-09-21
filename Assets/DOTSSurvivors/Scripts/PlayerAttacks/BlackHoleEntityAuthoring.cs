using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component to store information about the current state of the in-world black hole entity.
    /// </summary>
    public struct BlackHoleState : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// Timer to determine how long until the black hole should show its graphic.
        /// </summary>
        /// <remarks>
        /// This is used as a nice visual effect that synchronizes with the audio for the black hole. The audio begins to play for approx. 2 seconds as a bit of a warm-up for the attack, this is timed with a beat in the audio sound effect so it looks/sounds nice in the game when this attack executes.
        /// </remarks>
        public float SpawnGraphicTimer;
        /// <summary>
        /// Timer to determine the length of time from when the black hole graphic is shown until damage is actually applied.
        /// </summary>
        /// <remarks>
        /// The reason for this timer is also primarily for a visual effect. The black hole attack instantly destroys all enemies, gems, and items on screen and this timer is required so that actual destruction occurs when the black hole is covering the entirety of the screen at max opacity - this effectively hides the destruction of entities so they don't all just blink out of existence at once.
        /// </remarks>
        public float DamageTimer;
    }
    
    /// <summary>
    /// Data component to store persistent data for black hole attacks.
    /// </summary>
    public struct BlackHoleData : IComponentData
    {
        /// <summary>
        /// Collision filter used for detecting entities to be destroyed on screen.
        /// </summary>
        /// <remarks>
        /// The black hole in-world entity does not have a collider so this stores a collision filter used to find all enemies, gems, and items on screen when the attack executes in the <see cref="BlackHoleSystem"/>.
        /// </remarks>
        public CollisionFilter CollisionFilter;
        /// <summary>
        /// Boolean to determine if items should not be consumed by the black hole attack.
        /// </summary>
        /// <remarks>
        /// A random chance is evaluated in the <see cref="BlackHoleAttackSystem"/> to determine the result of this value.
        /// If true, only enemies will be consumed by the black hole. If false, items, experience point gems, and crates will be consumed by the black hole.
        /// </remarks>
        public bool SaveItems;
    }
    
    /// <summary>
    /// Authoring script to add components necessary for black hole in-world attack entity to function.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="EntityInteractionAuthoring"/>, <see cref="DestructibleEntityAuthoring"/>, and <see cref="InstantDestroyOnInteractionAuthoring"/> to ensure additional components are added to perform desired behaviors.
    /// </remarks>
    /// <seealso cref="BlackHoleSystem"/>
    /// <seealso cref="DestroyAfterTime"/>
    /// <seealso cref="BlackHoleData"/>
    /// <seealso cref="GraphicsEntity"/>
    [RequireComponent(typeof(EntityInteractionAuthoring))]
    [RequireComponent(typeof(DestructibleEntityAuthoring))]
    [RequireComponent(typeof(InstantDestroyOnInteractionAuthoring))]
    public class BlackHoleEntityAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Timer to determine how long until the black hole should show its graphic.
        /// </summary>
        /// <remarks>
        /// This is used as a nice visual effect that synchronizes with the audio for the black hole. The audio begins to play for approx. 2 seconds as a bit of a warm-up for the attack, this is timed with a beat in the audio sound effect so it looks/sounds nice in the game when this attack executes.
        /// </remarks>
        public float SpawnGraphicTimer;
        /// <summary>
        /// Timer to determine the length of time from when the black hole graphic is shown until damage is actually applied.
        /// </summary>
        /// <remarks>
        /// The reason for this timer is also primarily for a visual effect. The black hole attack instantly destroys all enemies, gems, and items on screen and this timer is required so that actual destruction occurs when the black hole is covering the entirety of the screen at max opacity - this effectively hides the destruction of entities so they don't all just blink out of existence at once.
        /// </remarks>
        public float DamageTimer;
        /// <summary>
        /// Reference to the graphical portion of the in-world black hole attack.
        /// </summary>
        /// <remarks>
        /// This is required because the graphics entity will be disabled by default and will become enabled once <see cref="BlackHoleState.SpawnGraphicTimer"/> expires.
        /// </remarks>
        public GameObject GraphicsEntity;
        
        private class Baker : Baker<BlackHoleEntityAuthoring>
        {
            public override void Bake(BlackHoleEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DestroyAfterTime>(entity);
                AddComponent(entity, new BlackHoleState
                {
                    SpawnGraphicTimer = authoring.SpawnGraphicTimer,
                    DamageTimer = authoring.DamageTimer
                });
                AddComponent<BlackHoleData>(entity);
                var graphicsEntity = GetEntity(authoring.GraphicsEntity, TransformUsageFlags.Dynamic);
                AddComponent(entity, new GraphicsEntity
                {
                    Value = graphicsEntity
                });
            }
        }
    }

    /// <summary>
    /// System that controls the behavior of the black hole in-world attack entity.
    /// </summary>
    /// <remarks>
    /// Once a black hole entity is spawned into the world via the <see cref="BlackHoleAttackSystem"/>, this system will decrement timers stored in the <see cref="BlackHoleState"/> of the black hole in-world entity to determine when the graphics should begin to be shown, and when to evaluate the screen for enemies, gems, and items to be consumed by the black hole.
    /// Note that due to how the graphics entity is re-enabled by this system (removing the Disabled component), this will also start other effects applied to the graphics entity such as <see cref="LinearScaleTransformationSystem"/>, <see cref="ConstantRotationSystem"/>, and <see cref="FadeAttackInOutSystem"/>.
    /// </remarks>
    [UpdateInGroup(typeof(DS_AttackSystemGroup))]
    public partial struct BlackHoleSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<CameraTarget>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var (blackHoleState, transform, graphicsEntity, entityInteractions, blackHoleData, blackHoleActive) in SystemAPI.Query<RefRW<BlackHoleState>, RefRW<LocalTransform>, GraphicsEntity, DynamicBuffer<EntityInteraction>, BlackHoleData, EnabledRefRW<BlackHoleState>>())
            {
                blackHoleState.ValueRW.SpawnGraphicTimer -= deltaTime;
                if (blackHoleState.ValueRO.SpawnGraphicTimer > 0f) continue;

                ecb.RemoveComponent<Disabled>(graphicsEntity.Value);
                
                // Follow the player's position so the black hole remains centered at the player's position.
                var playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
                var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;
                transform.ValueRW.Position = playerPosition + math.up();

                blackHoleState.ValueRW.DamageTimer -= deltaTime;
                if (blackHoleState.ValueRO.DamageTimer > 0f) continue;

                var pSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
                var cameraHalfExtents = SystemAPI.GetSingleton<CameraTarget>().HalfExtents;
                
                var overlapHits = new NativeList<int>(state.WorldUpdateAllocator);
                var minPosition = playerPosition - cameraHalfExtents;
                var maxPosition = playerPosition + cameraHalfExtents;
                var aabbInput = new OverlapAabbInput
                {
                    Aabb = new Aabb
                    {
                        Min = minPosition,
                        Max = maxPosition
                    },
                    Filter = blackHoleData.CollisionFilter
                };

                if (!pSingleton.OverlapAabb(aabbInput, ref overlapHits)) continue;
                foreach (var overlapHit in overlapHits)
                {
                    var targetEntity = pSingleton.Bodies[overlapHit].Entity;
                    // Skip adding interaction if enemy is resistant to Black Hole attack
                    if (SystemAPI.HasComponent<EnemyBlackHoleResistTag>(targetEntity)) continue;
                    
                    // Skip adding interaction if entity is not an enemy and if items should not be destroyed.
                    if (blackHoleData.SaveItems && !SystemAPI.HasComponent<EnemyTag>(targetEntity)) continue;
                    
                    entityInteractions.Add(new EntityInteraction
                    {
                        IsHandled = false,
                        TargetEntity = targetEntity
                    });
                }

                blackHoleActive.ValueRW = false;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}