using UnityEngine;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Material property override for the facing direction of the character. Setting this value will update the _FacingDirection property in the material instance of this entity. This will flip the character animation so the character faces to the left or right depending on the direction of movement.
    /// </summary>
    /// <remarks>
    /// A value of 1 makes the character face to the right and a value of -1 makes the character face to the left.
    /// Your IDE may gray out the Value field as this value is not used in our code. However, Unity uses it to apply this value to the material property defined in the MaterialProperty attribute.
    /// Be sure the MaterialProperty string exactly matches the reference string defined in the shader as this will silently fail if there is a typo.
    /// </remarks>
    /// <seealso cref="CharacterMoveSystem"/>
    [MaterialProperty("_FacingDirection")]
    public struct FacingDirectionOverride : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// Material property override for the dissolve property of the character. Setting this value will update the _Dissolve property in the material instance of this entity. This will make the character sprite appear to dissolve upon destruction.
    /// </summary>
    /// <remarks>
    /// Your IDE may gray out the Value field as this value is not used in our code. However, Unity uses it to apply this value to the material property defined in the MaterialProperty attribute.
    /// Be sure the MaterialProperty string exactly matches the reference string defined in the shader as this will silently fail if there is a typo.
    /// </remarks>
    /// <seealso cref="DissolveData"/>
    /// <seealso cref="CharacterAnimationSystem"/>
    [MaterialProperty("_Dissolve")]
    public struct DissolveOverride : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// Component data relevant to dissolve visual effect for characters upon destruction
    /// </summary>
    /// <seeaslo cref="DissolveOverride"/>
    /// <seeaslo cref="CharacterAnimationSystem"/>
    /// <seeaslo cref="DestroyEntitySystem"/>
    public struct DissolveData : IComponentData
    {
        public float StartTimestamp;
        public float Duration;
    }

    /// <summary>
    /// Authoring script to add components required for character animation.
    /// </summary>
    /// <seealso cref="PlayerAnimationAuthoring"/>
    /// <seealso cref="EnemyAnimationAuthoring"/>
    public class CharacterAnimationAuthoring : MonoBehaviour
    {
        private class Baker : Baker<CharacterAnimationAuthoring>
        {
            public override void Bake(CharacterAnimationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new FacingDirectionOverride { Value = 1f });
                AddComponent<DissolveOverride>(entity);
            }
        }
    }

    /// <summary>
    /// System to handle changes to character materials to facilitate animation visual effects.
    /// This system has 4 foreach loops that do the following:
    /// 1. Initializes <see cref="AnimationOffsetOverride"/> for enemy entities so all their animations are not synchronized.
    /// 2. Initializes the <see cref="EnhanceThicknessOverride"/> to apply a visual enhancement outline on the outside of an enemy to denote it as an enhanced entity.
    /// 3. Set which animation is currently playing for the player entity. Used to switch between walk and idle animations depending on current input (<see cref="CharacterMoveDirection"/>) and previous input (<see cref="PreviousPlayerInput"/>).
    /// 4. Plays the dissolve effect when the character is destroyed. (see <see cref="DissolveData"/> and <see cref="DissolveOverride"/>)
    /// </summary>
    /// <remarks>
    /// This system executes in the <see cref="DS_EffectsSystemGroup"/> which executes towards the end of the frame.
    /// </remarks>
    [BurstCompile]
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial struct CharacterAnimationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponent<EntityRandom>(state.SystemHandle);
            state.EntityManager.AddComponentData(state.SystemHandle, new InitializeEntityRandom
            {
                InitializationType = EntityRandomInitializationType.SystemMilliseconds
            });
        }
        
        public void OnUpdate(ref SystemState state)
        {
            var systemRandom = SystemAPI.GetComponentRW<EntityRandom>(state.SystemHandle);

            // Initialize a random value used as an offset on the animation timer so enemy animations are not all synchronized
            foreach (var (spawnTime, shouldInitialize) in SystemAPI.Query<RefRW<AnimationOffsetOverride>, EnabledRefRW<InitializeAnimationOffsetOverrideFlag>>())
            {
                // Random value between 0 and 8/12 as most enemies have 8 animation frames played at 12 frames/second.
                spawnTime.ValueRW.Value = systemRandom.ValueRW.Value.NextFloat(8f / 12f);
                shouldInitialize.ValueRW = false;
            }

            // Initialize the enhancement thickness on enhanced entities
            foreach (var (graphicsEntity, initialize) in SystemAPI.Query<GraphicsEntity, EnabledRefRW<InitializeEnhancementMaterialFlag>>())
            {
                var thickness = SystemAPI.GetComponentRW<EnhanceThicknessOverride>(graphicsEntity.Value);
                thickness.ValueRW.Value = 1f;
                initialize.ValueRW = false;
            }

            // Set which animation is currently playing. Used for changing player animation between walk and idle
            // If the player currently has positive input and the previous frame they had zero input, set the player to the moving animation.
            // If the player currently has zero input and the previous frame they had positive input, set the player to the idle animation.
            foreach (var (moveDirection, previousPlayerInput, graphicsEntity) in SystemAPI.Query<CharacterMoveDirection, PreviousPlayerInput, GraphicsEntity>().WithAll<PlayerTag>())
            {
                if (math.lengthsq(moveDirection.Value) > float.Epsilon && math.lengthsq(previousPlayerInput.PreviousInput) < float.Epsilon)
                {
                    var animationIndexOverride = SystemAPI.GetComponentRW<AnimationIndexOverride>(graphicsEntity.Value);
                    animationIndexOverride.ValueRW.Value = (int)PlayerAnimationIndex.Movement;
                }
                else if (math.lengthsq(moveDirection.Value) < float.Epsilon && math.lengthsq(previousPlayerInput.PreviousInput) > float.Epsilon)
                {
                    var animationIndexOverride = SystemAPI.GetComponentRW<AnimationIndexOverride>(graphicsEntity.Value);
                    animationIndexOverride.ValueRW.Value = (int)PlayerAnimationIndex.Idle;
                }
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            
            // Play the dissolve effect when an entity gets destroyed
            foreach (var (dissolveOverride, dissolveData, entity) in SystemAPI.Query<RefRW<DissolveOverride>, DissolveData>().WithEntityAccess())
            {
                var dissolveTime = currentTime - dissolveData.StartTimestamp;
                var t = dissolveTime / dissolveData.Duration;
                dissolveOverride.ValueRW.Value = t;
                if (t < 1f) continue;
                ecb.AddComponent<InstantDestroyTag>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}