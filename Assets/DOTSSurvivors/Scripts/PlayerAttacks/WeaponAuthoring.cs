using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component to store data related to player weapons.
    /// </summary>
    /// <remarks>
    /// Data in this component represents a single level for a weapon. A collection of WeaponLevelData components are stored in the <see cref="WeaponUpgradeData"/> to represent the all WeaponLevelData components in a weapon's upgrade path.
    /// Not all fields are used on all weapon types.
    /// Has the System.Serializable attribute so these values can be initialized in the editor via <see cref="WeaponUpgradeProperties"/>.
    /// </remarks>
    [Serializable]
    public struct WeaponLevelData : IComponentData
    {
        /// <summary>
        /// Cooldown for the weapon in seconds. This is the amount of time between when the final attack entity of an attack group is spawned and the first attack entity will be spawned for the next attack group.
        /// </summary>
        public float Cooldown;
        /// <summary>
        /// Time an attack entity will exist in the game world before self-destructing.
        /// </summary>
        public float TimeToLive;
        /// <summary>
        /// Interval between attack entity spawns in an attack group in seconds.
        /// </summary>
        public float IntervalBetweenAttacks;
        /// <summary>
        /// Movement speed in units per second of a moving attack entity.
        /// </summary>
        public float MovementSpeed;
        /// <summary>
        /// Number of attack entities that will be spawned in a single attack group.
        /// </summary>
        public int AttackCount;
        /// <summary>
        /// Base hit points that will be assigned to an attack entity to deal damage to an enemy entity via <see cref="EntityInteraction"/>
        /// </summary>
        public int BaseHitPoints;
        /// <summary>
        /// Scale modifier of an attack entity.
        /// </summary>
        public float Area;
        /// <summary>
        /// Maximum number of enemy entities an attack entity can collide with before self-destructing.
        /// </summary>
        public int MaxEnemyHitCount;
        /// <summary>
        /// Random chance used to determine if certain random behaviors should or should not happen.
        /// </summary>
        /// <remarks>
        /// Value will be from 0 to 100 where 0 will never occur and 100 will always occur.
        /// </remarks>
        public int RandomChance;
        /// <summary>
        /// Collision filter to be applied to attack entities.
        /// </summary>
        public CollisionFilter CollisionFilter;
    }

    /// <summary>
    /// Prefab of an in-world attack entity that will be spawned by an attack system.
    /// </summary>
    /// <remarks>
    /// Many of these in-world attacks are projectiles that collide with enemies, however some like the black hole attack have slightly different behavior.
    /// </remarks>
    public struct AttackPrefab : IComponentData
    {
        public Entity Value;
    }

    /// <summary>
    /// Data component to reference the <see cref="WeaponUpgradeProperties"/> for a weapon.
    /// </summary>
    /// <remarks>
    /// This is primarily used for UI display of weapon upgrades as runtime weapon data is stored in <see cref="WeaponUpgradeData"/>.
    /// </remarks>
    public struct WeaponUpgradePropertiesReference : IComponentData
    {
        public UnityObjectRef<WeaponUpgradeProperties> Value;
    }

    /// <summary>
    /// Data component to hold the current state of a weapon.
    /// </summary>
    /// <remarks>
    /// Not all fields are relevant to all weapon types.
    /// </remarks>
    public struct WeaponState : IComponentData
    {
        /// <summary>
        /// Timer to keep track of the time until the next attack group begins in seconds.
        /// </summary>
        public float CooldownTimer;
        /// <summary>
        /// Timer to keep track of time until the next attack entity should spawn.
        /// </summary>
        public float NextAttackTimer;
        /// <summary>
        /// Value to keep track of the number of attack entities spawned in the current attack group.
        /// </summary>
        public int AttackCount;
        /// <summary>
        /// Value to store the current level index of a weapon. Level 1 (lowest level) is stored at 0 in this field.
        /// </summary>
        public int LevelIndex;
    }
    
    /// <summary>
    /// Flag component to signify this weapon is currently active.
    /// </summary>
    /// <remarks>
    /// Enableable component will be marked as active when <see cref="WeaponState.CooldownTimer"/> is expired and the weapon should perform its attack behavior (typically spawning <see cref="AttackPrefab"/> entities).
    /// </remarks>
    public struct WeaponActiveFlag : IComponentData, IEnableableComponent {}
    
    /// <summary>
    /// Flag component to signify this weapon should be upgraded to the next <see cref="WeaponLevelData"/> level.
    /// </summary>
    /// <remarks>
    /// Enableable component will be marked as active when the weapon should be upgraded.
    /// </remarks>
    public struct UpgradeWeaponFlag : IComponentData, IEnableableComponent {}
    
    /// <summary>
    /// Data component to hold blob asset reference for blob array of <see cref="WeaponLevelData"/> components for a weapon's upgrade path.
    /// </summary>
    /// <remarks>
    /// Blob asset created during baking in <see cref="WeaponAuthoring"/>.
    /// </remarks>
    public struct WeaponUpgradeData : IComponentData
    {
        /// <summary>
        /// Reference to the blob array of <see cref="WeaponLevelData"/> for a weapon's upgrade path. Element 0 of the array corresponds to the weapon level data for level 1 of the attack, index 1 is level 2, and so on.
        /// </summary>
        public BlobAssetReference<BlobArray<WeaponLevelData>> Value;
        /// <summary>
        /// Custom indexer for easy access to the <see cref="WeaponLevelData"/> for a weapon.
        /// </summary>
        /// <param name="i">Index into the blob array. Index 0 is level 1 for a weapon, index 1 is level 2, and so on.</param>
        public WeaponLevelData this[int i] => Value.Value[i];
    }
    
    /// <summary>
    /// Base authoring script to add components all weapon entities need.
    /// </summary>
    /// <remarks>
    /// Note that this component will be attached to the base weapon entity which exists from instantiation through the remainder of a game run. This should not be attached to <see cref="AttackPrefab"/>s that spawn as visible objects in the game world.
    /// The baker of this authoring script will allocate a blob array of <see cref="WeaponLevelData"/> and populate it with data from the <see cref="WeaponUpgradeProperties"/> field of this authoring script. It is important that we include DependsOn(authoring.WeaponUpgradeProperties) so baking is re-ran when values on the ScriptableObject are changed.
    /// </remarks>
    public class WeaponAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Debug field to set the level the weapon will start on when added to the player's weapon inventory. Used for testing higher levels of a weapon without needing to upgrade in normal gameplay.
        /// </summary>
        /// <remarks>
        /// This field is for debugging purposes only and should be set to 0 before shipping. 
        /// </remarks>
        public int StartingLevel;
        /// <summary>
        /// Reference to the ScriptableObject defining properties of a weapon.
        /// </summary>
        /// <remarks>
        /// Unmanaged data from this ScriptableObject get loaded into a blob asset during baking. It is important that we include DependsOn(authoring.WeaponUpgradeProperties) so baking is re-ran when values on the ScriptableObject are changed.
        /// </remarks>
        public WeaponUpgradeProperties WeaponUpgradeProperties;
        
        private class Baker : Baker<WeaponAuthoring>
        {
            public override void Bake(WeaponAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                DependsOn(authoring.WeaponUpgradeProperties);
                DependsOn(authoring.gameObject);

                var belongsToLayer = authoring.gameObject.layer;
                var belongsToLayerMask = (uint)math.pow(2, belongsToLayer);
                var collidesWithLayerMask = (uint)PhysicsHelper.GetCollisionMaskForLayer(belongsToLayer);
                var collisionFilter = new CollisionFilter
                {
                    BelongsTo = belongsToLayerMask,
                    CollidesWith = collidesWithLayerMask
                };
                
                var builder = new BlobBuilder(Allocator.Temp);
                ref var blobArrayRoot = ref builder.ConstructRoot<BlobArray<WeaponLevelData>>();
                var arrayBuilder = builder.Allocate(ref blobArrayRoot, authoring.WeaponUpgradeProperties.LevelPropertiesArray.Length);

                for (var i = 0; i < authoring.WeaponUpgradeProperties.LevelPropertiesArray.Length; i++)
                {
                    var curUpgradeProperties = authoring.WeaponUpgradeProperties.LevelPropertiesArray[i].WeaponLevelData;
                    curUpgradeProperties.CollisionFilter = collisionFilter;
                    arrayBuilder[i] = curUpgradeProperties;
                }

                var weaponLevelDataBlobArray = builder.CreateBlobAssetReference<BlobArray<WeaponLevelData>>(Allocator.Persistent);
                AddBlobAsset(ref weaponLevelDataBlobArray, out _);
                builder.Dispose();

                AddComponent(entity, new WeaponUpgradeData { Value = weaponLevelDataBlobArray });
                var startingWeaponData = weaponLevelDataBlobArray.Value[authoring.StartingLevel];
                AddComponent(entity, startingWeaponData);
                AddComponent(entity, new WeaponUpgradePropertiesReference { Value = authoring.WeaponUpgradeProperties });
                
                var attackEntity = GetEntity(authoring.WeaponUpgradeProperties.AttackPrefab, TransformUsageFlags.Dynamic);
                AddComponent(entity, new AttackPrefab { Value = attackEntity });
                AddComponent(entity, new WeaponState
                {
                    CooldownTimer = startingWeaponData.Cooldown,
                    NextAttackTimer = 0f,
                    AttackCount = 0,
                    LevelIndex = authoring.StartingLevel
                });
                AddComponent<WeaponActiveFlag>(entity);
                SetComponentEnabled<WeaponActiveFlag>(entity, false);
                AddComponent<UpgradeWeaponFlag>(entity);
                SetComponentEnabled<UpgradeWeaponFlag>(entity, false);
                AddBuffer<Child>(entity);
            }
        }

        private void OnValidate()
        {
            StartingLevel = Mathf.Clamp(StartingLevel, 0, WeaponUpgradeProperties.LevelPropertiesArray.Length - 1);
        }
    }

    /// <summary>
    /// System to determine when a weapon should become active.
    /// </summary>
    /// <remarks>
    /// A weapon is active when its <see cref="WeaponActiveFlag"/> is enabled; at this time it will begin spawning instances of <see cref="AttackPrefab"/>s into the game world. Spawning of attack instances take place in systems unique to the weapon type.
    /// </remarks>
    [UpdateInGroup(typeof(DS_AttackSystemGroup), OrderFirst = true)]
    public partial struct WeaponActivationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            
            foreach (var (weaponState, weaponData, parent, entity) in SystemAPI.Query<RefRW<WeaponState>, WeaponLevelData, Parent>().WithNone<WeaponActiveFlag>().WithEntityAccess())
            {
                weaponState.ValueRW.CooldownTimer -= deltaTime;
                if (weaponState.ValueRO.CooldownTimer > 0f) continue;
                SystemAPI.SetComponentEnabled<WeaponActiveFlag>(entity, true);
                var cooldownModifier = SystemAPI.GetComponent<CharacterStatModificationState>(parent.Value).AttackCooldown;
                weaponState.ValueRW.CooldownTimer = weaponData.Cooldown * cooldownModifier;
            }
        }
    }

    /// <summary>
    /// System to upgrade runtime weapon data when the <see cref="UpgradeWeaponFlag"/> is set to true.
    /// </summary>
    /// <remarks>
    /// Note that the <see cref="WeaponActiveFlag"/> is not an EnabledRefRW in the foreach query. This is because this isn't guaranteed to be active during upgrading. However, it is set to false to effectively reset its state if it is in the middle of being active. Note that we also set <see cref="WeaponState.CooldownTimer"/> to a very low number so the weapon will become active almost immediately after upgrading.
    /// </remarks>
    [UpdateInGroup(typeof(DS_AttackSystemGroup))]
    public partial struct UpgradeWeaponSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (upgradeFlag, weaponData, weaponState, propertiesBlob, entity) in SystemAPI.Query<EnabledRefRW<UpgradeWeaponFlag>, RefRW<WeaponLevelData>, RefRW<WeaponState>, WeaponUpgradeData>().WithEntityAccess())
            {
                upgradeFlag.ValueRW = false;
                SystemAPI.SetComponentEnabled<WeaponActiveFlag>(entity, false);
                weaponState.ValueRW.LevelIndex += 1;
                weaponState.ValueRW.CooldownTimer = 0.15f;
                weaponState.ValueRW.NextAttackTimer = 0f;
                weaponData.ValueRW = propertiesBlob[weaponState.ValueRO.LevelIndex];
            }
        }
    }
}