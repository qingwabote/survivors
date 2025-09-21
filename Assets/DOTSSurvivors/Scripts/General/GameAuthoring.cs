using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;
using Random = Unity.Mathematics.Random;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Tag component to signify this entity as the entity containing data related to gameplay.
    /// </summary>
    /// <remarks>
    /// This is often used when attempting to access managed data components on this entity. First a reference to the entity must be obtained (using SystemAPI.GetSingletonEntity or otherwise) then the managed component can be grabbed from the entity.
    /// </remarks>
    public struct GameEntityTag : IComponentData {}
    
    /// <summary>
    /// Component to signify the game needs to be initialized.
    /// </summary>
    /// <remarks>
    /// The reason this is a tag component and not an enableable flag component is because the <see cref="GameStartSystem"/> will initialize the game in its OnUpdate() method. As such this tag component is a required component for updating the system - enableable components cannot be added to the RequireForUpdate of a system. This tag component will be removed after initialization so that this system will not run again throughout the duration of the game.
    /// </remarks>
    public struct InitializeGameTag : IComponentData {}
    
    /// <summary>
    /// Data component to entity prefabs utilized throughout the game.
    /// </summary>
    public struct GameEntityPrefabs : IComponentData
    {
        /// <summary>
        /// Prefab of the player entity.
        /// </summary>
        public Entity PlayerEntityPrefab;
        /// <summary>
        /// Prefab of a passive item the player can obtain through leveling up.
        /// </summary>
        public Entity PassivePrefab;
        /// <summary>
        /// Prefab of an experience gem dropped by enemies.
        /// </summary>
        public Entity ExperienceGemPrefab;
        /// <summary>
        /// Prefab of a crate dropped by boss enemies.
        /// </summary>
        public Entity CratePrefab;
    }
    
    /// <summary>
    /// Data component to store the running count of aliens defeated in a run.
    /// </summary>
    public struct AliensDefeatedCount : IComponentData
    {
        public int Value;
    }
    
    /// <summary>
    /// Data component to store the running count of coins collected in a run.
    /// </summary>
    public struct CoinsCollected : IComponentData
    {
        public int Value;
    }

    /// <summary>
    /// Data component to store the current game time in seconds.
    /// </summary>
    public struct GameTime : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// Data struct used to assist with authoring experience point gem colors in the Unity editor.
    /// </summary>
    [Serializable]
    public struct ExperiencePointColor
    {
        /// <summary>
        /// Minimum experience point value for the associated color.
        /// </summary>
        public int ExperiencePointValue;
        /// <summary>
        /// Color of the experience gem if the experience point value is greater than or equal to the specified ExperiencePointValue.
        /// </summary>
        public Color Color;
    }
    
    /// <summary>
    /// Managed data component to store managed types required for gameplay.
    /// </summary>
    public class ManagedGameData : IComponentData
    {
        /// <summary>
        /// GameObject prefab of the world space UI canvas that should follow the player to display their health bar.
        /// </summary>
        public GameObject PlayerWorldUICanvasPrefab;
        /// <summary>
        /// Dictionary lookup to retrieve the entity prefab of a weapon by its <see cref="WeaponType"/> enum.
        /// </summary>
        public Dictionary<WeaponType, Entity> WeaponEntityPrefabLookup;
        /// <summary>
        /// Dictionary lookup to retrieve the entity prefab of an enemy by its <see cref="EnemyType"/>.
        /// </summary>
        public Dictionary<EnemyType, Entity> EnemyEntityPrefabLookup;
        /// <summary>
        /// List of (int, float4) to look up the color of an experience gem at a given experience point value.
        /// </summary>
        /// <remarks>
        /// Similar to <see cref="ExperiencePointColor"/>, int is the minimum experience point value for the associated color, and float4 is the color value.
        /// </remarks>
        public List<(int, float4)> ExperienceGemColorLookup;
        /// <summary>
        /// <see cref="BonusItemProperties"/> for granting the player money.
        /// </summary>
        public BonusItemProperties MoneyBonusItem;
        /// <summary>
        /// <see cref="BonusItemProperties"/> for granting the player health.
        /// </summary>
        public BonusItemProperties HealthBonusItem;
    }
    
    /// <summary>
    /// Data component to store a reference to the <see cref="CharacterProperties"/> of the player to be spawned.
    /// </summary>
    /// <remarks>
    /// This reference can be assigned in two ways - 1. through selecting a character in the title screen or 2. by assigning a value to the <see cref="GameAuthoring.StartingPlayerProperties"/>. Through normal gameplay, this will always be set by the character selected in the title screen. However during debugging in the editor, if the developer enters play mode in a level scene, the value stored in the GameAuthoring will be used as the player.
    /// </remarks>
    public struct SelectedCharacterReference : IComponentData
    {
        public UnityObjectRef<CharacterProperties> Value;
    }

    /// <summary>
    /// Data component to store information related to the amount of experience points required for the player to level up to the next level.
    /// </summary>
    /// <remarks>
    /// The amount of experience to reach the next level is equal to the amount of experience to reach the current level plus a fixed value. Once the player reaches a certain level, this fixed value between levels increases. This increase happens multiple times throughout gameplay.
    /// </remarks>
    public struct ExperiencePointTable : IComponentData
    {
        /// <summary>
        /// Experience points required to level up for the first time.
        /// </summary>
        public int InitialExperiencePoints;
        /// <summary>
        /// Collection of integers storing the level indices at which the differential between experience points required to reach the next level increases.
        /// </summary>
        /// <remarks>
        /// Using int3 type like an array as only 3 values need to be stored. Can access x, y, z values with 0, 1, 2 indices.
        /// </remarks>
        public int3 LevelAtExperienceIncrease;
        /// <summary>
        /// Collection of integers storing the differential between experience points required to reach the next level.
        /// </summary>
        /// <remarks>
        /// Using int3 type like an array as only 3 values need to be stored. Can access x, y, z values with 0, 1, 2 indices.
        /// </remarks>
        public int3 LevelExperienceIncreases;

        /// <summary>
        /// Custom indexer to easily access the experience points required to reach a certain level.
        /// </summary>
        /// <param name="i">Index of the level to retrieve experience points required to reach.</param>
        public int this[int i] => GetExperienceForLevel(i);
        
        /// <summary>
        /// Method to calculate the experience points required to reach a certain level.
        /// </summary>
        /// <param name="level">Index of the level to calculate experience points to reach.</param>
        /// <returns>Experience points required to reach the level.</returns>
        private int GetExperienceForLevel(int level)
        {
            if (level <= 0) return InitialExperiencePoints;
            var experience = InitialExperiencePoints;

            if (level < LevelAtExperienceIncrease[1])
            {
                experience += (level - 1) * LevelExperienceIncreases[0];
            }
            else if (level < LevelAtExperienceIncrease[2])
            {
                experience += (LevelAtExperienceIncrease[1] - 2) * LevelExperienceIncreases[0];
                experience += (level - LevelAtExperienceIncrease[1] + 1) * LevelExperienceIncreases[1];
            }
            else
            {
                experience += (LevelAtExperienceIncrease[1] - 2) * LevelExperienceIncreases[0];
                experience += (LevelAtExperienceIncrease[2] - LevelAtExperienceIncrease[1]) * LevelExperienceIncreases[1];
                experience += (level - LevelAtExperienceIncrease[2] + 1) * LevelExperienceIncreases[2];
            }
            
            return experience;
        }
    }
    
    /// <summary>
    /// Helper struct used for a pairing of <see cref="UpgradeProperties"/> with a weighted random value. Used in the <see cref="PlayerUpgradeData"/> to retrieve a random upgrade.
    /// </summary>
    public struct WeightedUpgradeProperties
    {
        /// <summary>
        /// Upgrade Properties associated with this weighted random value.
        /// </summary>
        public UpgradeProperties UpgradeProperties;

        /// <summary>
        /// Highest random value associated with this UpgradeProperties instance.
        /// </summary>
        /// <remarks>
        /// Weighted random value is assigned in the <see cref="GameStartSystem"/>. During initialization, all <see cref="UpgradeProperties"/> ScriptableObjects are iterated, this weighted random value is calculated by adding the running total of all weighted random values to the <see cref="UpgradeProperties.Rarity"/> property. When determining which upgrades should be shown during level up, a random value between 0 and the maximum weighted random value is chosen; each element of <see cref="PlayerUpgradeData.WeightedUpgradeProperties"/> is iterated to see if that number is less than its assigned weighted random value. If it is, then the upgrade is selected to be shown.
        /// </remarks>
        public int WeightedRandomValue;
    }

    /// <summary>
    /// Managed data component to store data related to player upgrades.
    /// </summary>
    public class PlayerUpgradeData : IComponentData
    {
        /// <summary>
        /// Collection of <see cref="WeightedUpgradeProperties"/>. Used for selecting a random upgrade during level up and chest opening events.
        /// </summary>
        public WeightedUpgradeProperties[] WeightedUpgradeProperties;

        /// <summary>
        /// Dictionary to lookup active weapon entities by <see cref="WeaponUpgradeProperties"/>.
        /// </summary>
        /// <remarks>
        /// This is the weapon entity that is persistent in the world and spawns <see cref="AttackPrefab"/> instances. Used to determine if the player already has a weapon, and if so what level it is on.
        /// </remarks>
        public Dictionary<WeaponUpgradeProperties, Entity> ActiveWeaponEntityLookup;

        /// <summary>
        /// Dictionary to lookup active passive item entities by <see cref="PassiveUpgradeProperties"/>.
        /// </summary>
        /// <remarks>
        /// This is the passive item entity that is persistent in the world. Used to determine if the player already has a passive item, and if so what level it is on.
        /// </remarks>
        public Dictionary<PassiveUpgradeProperties, Entity> ActivePassiveEntityLookup;

        /// <summary>
        /// Maximum number of weapons the player is allowed to have.
        /// </summary>
        public int MaxWeaponCount;

        /// <summary>
        /// Maximum number of passive items the player is allowed to have.
        /// </summary>
        public int MaxPassiveCount;

        /// <summary>
        /// Total random points allocated for random selection of upgrades.
        /// </summary>
        /// <remarks>
        /// This is effectively the highest <see cref="WeightedUpgradeProperties.WeightedRandomValue"/> and could also be calculated by summing all <see cref="UpgradeProperties.Rarity"/> values.
        /// </remarks>
        public int TotalRandomPoints;

        /// <summary>
        /// Helper method to select a random <see cref="UpgradeProperties"/> in the <see cref="PlayerUpgradeData.WeightedUpgradeProperties"/> array.
        /// </summary>
        /// <param name="random">Takes in a Unity.Mathematics.Random type used for random number generation. Passed in by reference so internal state can be updated to continually generate random values.</param>
        /// <remarks>
        /// First selects a random value between 0 and <see cref="TotalRandomPoints"/>. Next, each element of <see cref="PlayerUpgradeData.WeightedUpgradeProperties"/> is iterated. The <see cref="WeightedUpgradeProperties.WeightedRandomValue"/> is compared against the random value generated. If the random value is lower than current weighted value, the current <see cref="WeightedUpgradeProperties.UpgradeProperties"/> will be returned as the random upgrade.
        /// </remarks>
        /// <returns>The selected random upgrade property.</returns>
        public UpgradeProperties GetRandomUpgradeProperties(ref Random random)
        {
            var randomIndex = random.NextInt(0, TotalRandomPoints);
            for (var i = 0; i < WeightedUpgradeProperties.Length; i++)
            {
                var weightedRandomValue = WeightedUpgradeProperties[i].WeightedRandomValue;
                if (randomIndex < weightedRandomValue)
                {
                    return WeightedUpgradeProperties[i].UpgradeProperties;
                }
            }

            Debug.LogError("Error, unable to get random upgrade property.");
            return null;
        }

        /// <summary>
        /// Helper property to determine if the player has any free weapon slots.
        /// </summary>
        /// <remarks>
        /// If true, the player can obtain new weapons. If false, the player cannot obtain any new weapons as all the slots are full.
        /// </remarks>
        public bool HasFreeWeaponSlots => ActiveWeaponEntityLookup.Count < MaxWeaponCount;

        /// <summary>
        /// Helper method to determine if the player has any free passive item slots.
        /// </summary>
        /// <remarks>
        /// If true, the player can obtain new passive items. If false, the player cannot obtain any new passive items as all the slots are full.
        /// </remarks>
        public bool HasFreePassiveSlots => ActivePassiveEntityLookup.Count < MaxPassiveCount;
    }

    /// <summary>
    /// Authoring script to add components necessary for controlling the ECS side of the game.
    /// </summary>
    /// <remarks>
    /// Requires the <see cref="EntityRandomAuthoring"/> script to add <see cref="EntityRandom"/> component used for random number generation.
    /// </remarks>
    [RequireComponent(typeof(EntityRandomAuthoring))]
    public class GameAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Debug-only setting - when entering playmode while in a level, this character will be spawned into the world. Otherwise, if entering the game through the title screen, the selected character will be used.
        /// </summary>
        public CharacterProperties StartingPlayerProperties;
        /// <summary>
        /// GameObject prefab of the player which will be converted to an entity.
        /// </summary>
        [Header("Entity Prefabs")]
        public GameObject PlayerPrefab;
        /// <summary>
        /// GameObject prefab of the passive item which will be converted to an entity.
        /// </summary>
        public GameObject PassivePrefab;
        /// <summary>
        /// GameObject prefab of the experience point gem which will be converted to an entity.
        /// </summary>
        public GameObject ExperienceGemPrefab;
        /// <summary>
        /// GameObject prefab of the supply crate which will be converted to an entity.
        /// </summary>
        public GameObject CratePrefab;
        
        /// <summary>
        /// GameObject prefab of the world UI canvas which will follow the player and display their health bar.
        /// </summary>
        [Header("Managed Elements")]
        public GameObject PlayerWorldUICanvasPrefab;
        /// <summary>
        /// List to store the mapping of experience point values to the color of gems they will spawn.
        /// </summary>
        public List<ExperiencePointColor> ExperienceGemColorLookup;
        /// <summary>
        /// <see cref="BonusItemProperties"/> for granting the player money.
        /// </summary>
        public BonusItemProperties MoneyBonusItem;
        /// <summary>
        /// <see cref="BonusItemProperties"/> for granting the player health.
        /// </summary>
        public BonusItemProperties HealthBonusItem;
        
        /// <summary>
        /// Experience points required to level up for the first time.
        /// </summary>
        [Header("Experience Point Data")]
        public int InitialExperiencePoints;
        /// <summary>
        /// Collection of integers storing the level indices at which the differential between experience points required to reach the next level increases.
        /// </summary>
        /// <remarks>
        /// Array should be size of 3 as they are stored in an int3 value in the data component.
        /// </remarks>
        public int[] LevelAtExperienceIncrease;
        /// <summary>
        /// Collection of integers storing the differential between experience points required to reach the next level.
        /// </summary>
        /// <remarks>
        /// Array should be size of 3 as they are stored in an int3 value in the data component.
        /// </remarks>
        public int[] LevelExperienceIncrease;
        
        /// <summary>
        /// Maximum number of weapons the player is allowed to have.
        /// </summary>
        [Header("Player Capability Data")]
        public int MaxWeaponCount = 5;

        /// <summary>
        /// Maximum number of passive items the player is allowed to have.
        /// </summary>
        public int MaxPassiveCount = 5;

        private class Baker : Baker<GameAuthoring>
        {
            public override void Bake(GameAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<GameEntityTag>(entity);
                AddComponent<InitializeGameTag>(entity);

                DependsOn(authoring.PlayerPrefab);
                DependsOn(authoring.PassivePrefab);
                DependsOn(authoring.ExperienceGemPrefab);
                DependsOn(authoring.CratePrefab);
                AddComponent(entity, new GameEntityPrefabs
                {
                    PlayerEntityPrefab = GetEntity(authoring.PlayerPrefab, TransformUsageFlags.Dynamic),
                    PassivePrefab = GetEntity(authoring.PassivePrefab, TransformUsageFlags.Dynamic),
                    ExperienceGemPrefab = GetEntity(authoring.ExperienceGemPrefab, TransformUsageFlags.Dynamic),
                    CratePrefab = GetEntity(authoring.CratePrefab, TransformUsageFlags.Dynamic)
                });

                // Although the baker doesn't do anything with weapon or enemy prefabs, calling GetEntity is still required to convert them to ECS entities so they can be available at runtime.
                var weaponPrefabs = Resources.LoadAll("Prefabs/Weapons", typeof(GameObject)).Cast<GameObject>().ToArray();
                foreach (var weaponPrefab in weaponPrefabs)
                {
                    GetEntity(weaponPrefab, TransformUsageFlags.Dynamic);
                }

                var enemyPrefabs = Resources.LoadAll("Prefabs/Enemies", typeof(GameObject)).Cast<GameObject>().ToArray();
                foreach (var enemyPrefab in enemyPrefabs)
                {
                    GetEntity(enemyPrefab, TransformUsageFlags.Dynamic);
                }
                
                // Set up the color lookup for experience point gems by value.
                var newExperiencePointColorLookup = new List<(int, float4)>();
                if (authoring.ExperienceGemColorLookup?.Count > 0)
                { 
                    var orderedExperiencePointList = authoring.ExperienceGemColorLookup.OrderBy(t => t.ExperiencePointValue).ToList();
                    foreach (var experiencePointColor in orderedExperiencePointList)
                    {
                        newExperiencePointColorLookup.Add(new(experiencePointColor.ExperiencePointValue, (Vector4)experiencePointColor.Color));
                    }
                }

                AddComponentObject(entity, new ManagedGameData
                {
                    PlayerWorldUICanvasPrefab = authoring.PlayerWorldUICanvasPrefab,
                    WeaponEntityPrefabLookup = new Dictionary<WeaponType, Entity>(),
                    EnemyEntityPrefabLookup = new Dictionary<EnemyType, Entity>(),
                    ExperienceGemColorLookup = newExperiencePointColorLookup,
                    MoneyBonusItem = authoring.MoneyBonusItem,
                    HealthBonusItem = authoring.HealthBonusItem
                });
                AddComponent<AliensDefeatedCount>(entity);
                AddComponent<CoinsCollected>(entity);
                AddComponent<GameTime>(entity);
                AddComponent(entity, new SelectedCharacterReference
                {
                    Value = authoring.StartingPlayerProperties
                });

                AddComponent(entity, new ExperiencePointTable
                {
                    InitialExperiencePoints = authoring.InitialExperiencePoints,
                    LevelAtExperienceIncrease = new int3
                    {
                        x = authoring.LevelAtExperienceIncrease[0],
                        y = authoring.LevelAtExperienceIncrease[1],
                        z = authoring.LevelAtExperienceIncrease[2],
                    },
                    LevelExperienceIncreases = new int3
                    {
                        x = authoring.LevelExperienceIncrease[0],
                        y = authoring.LevelExperienceIncrease[1],
                        z = authoring.LevelExperienceIncrease[2],
                    }
                });

                AddComponentObject(entity, new PlayerUpgradeData
                {
                    ActiveWeaponEntityLookup = new Dictionary<WeaponUpgradeProperties, Entity>(),
                    ActivePassiveEntityLookup = new Dictionary<PassiveUpgradeProperties, Entity>(),
                    MaxWeaponCount = authoring.MaxWeaponCount,
                    MaxPassiveCount = authoring.MaxPassiveCount,
                });
            }
        }
    }

    /// <summary>
    /// System to initialize things for the start of the game.
    /// </summary>
    /// <remarks>
    /// Updates in the <see cref="DS_InitializationSystemGroup"/> which executes towards the beginning of the frame to ensure the game is initialized before core gameplay systems run.
    /// </remarks>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup))]
    public partial struct GameStartSystem : ISystem 
    {
        /// <summary>
        /// Singleton component associated with this system to store the game index.
        /// </summary>
        public struct Singleton : IComponentData
        {
            /// <summary>
            /// Game index is incremented each time the game is played.
            /// </summary>
            /// <remarks>
            /// This game index is used in the <see cref="InitializeSpawnRandomOnDestroySystem"/> to ensure a unique hash for blob assets are created each time the game is entered. This is important because if blob assets from previous games are used, they will not map to valid prefabs and thus nothing will spawn.
            /// </remarks>
            public int GameIndex;
        }
        
        /// <summary>
        /// Requiring the <see cref="InitializeGameTag"/> means this system should only run the OnUpdate method once as this component will be removed from the game entity immediately. The OnUpdate method is where the game initialization logic exists.
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InitializeGameTag>();
            state.RequireForUpdate<GameEntityTag>();
            state.RequireForUpdate<SelectedCharacterReference>();
            
            state.EntityManager.AddComponent<Singleton>(state.SystemHandle);
        }
        
        public void OnUpdate(ref SystemState state)
        {
            // Remove the InitializeGameTag component from the gameControllerEntity to ensure this game start logic only executes one time.
            var gameControllerEntity = SystemAPI.GetSingletonEntity<GameEntityTag>();
            state.EntityManager.RemoveComponent<InitializeGameTag>(gameControllerEntity);

            // Increment the game index each time the game is played. This is used for blob asset hashes to differentiate between blob assets created in previous runs of the game.
            var singleton = state.EntityManager.GetComponentDataRW<Singleton>(state.SystemHandle);
            singleton.ValueRW.GameIndex += 1;
            
            // Iterate all weapon prefabs and register them to the WeaponEntityPrefabLookup. This must be done at runtime as runtime indices of prefabs will be different at bake time.
            var managedGameData = state.EntityManager.GetComponentObject<ManagedGameData>(gameControllerEntity);
            var weaponEntityPrefabLookup = managedGameData.WeaponEntityPrefabLookup;
            foreach (var (weaponUpgradePropertiesReference, entity) in SystemAPI.Query<WeaponUpgradePropertiesReference>().WithAll<Prefab>().WithEntityAccess().WithOptions(EntityQueryOptions.IncludePrefab))
            {
                var weaponType = weaponUpgradePropertiesReference.Value.Value.WeaponType;
                if (weaponType == WeaponType.None)
                {
                    Debug.LogWarning($"Warning: Weapon: {weaponUpgradePropertiesReference.Value.Value.Name} has WeaponType of 'None' - ensure this value is set in the associated WeaponUpgradeProperties");
                    continue;
                }
                if (!weaponEntityPrefabLookup.TryAdd(weaponType, entity))
                {
                    Debug.LogError($"Error: WeaponType: {weaponType.ToString()} has already been added to the ManagedGameData.WeaponEntityPrefabLookup. This is likely because there are multiple weapon prefabs referencing the same WeaponUpgradeProperties, which is not allowed.");
                }
            }
            
            // Iterate all enemy prefabs and register them in the EnemyEntityPrefabLookup. This must be done at runtime as runtime indices of prefabs will be different at bake time.
            // EnemyTypeInitialization is removed from the prefab as this does not need to be on the instantiated enemies.
            var enemyEntityPrefabLookup = managedGameData.EnemyEntityPrefabLookup;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var (enemyType, entity) in SystemAPI.Query<EnemyTypeInitialization>().WithAll<Prefab>().WithOptions(EntityQueryOptions.IncludePrefab).WithEntityAccess())
            {
                if (enemyType.Value == EnemyType.None)
                {
                    Debug.LogWarning($"Warning: Prefab entity {entity.ToString()} has enemy type set to 'None' set to appropriate enemy type value.");
                }
                else if (!enemyEntityPrefabLookup.TryAdd(enemyType.Value, entity))
                {
                    Debug.LogError($"Error: Enemy Type: {enemyType.Value} has already been added to Enemy Entity Prefab Lookup. Ensure only one prefab of each enemy type is present. Consider adding a new enemy type if needed.");
                }

                ecb.RemoveComponent<EnemyTypeInitialization>(entity);
            }
            ecb.Playback(state.EntityManager);
            
            // Get the selected character from the game authoring (in-editor debug) or the character selected on the title screen.
            var selectedCharacterCount = SystemAPI.QueryBuilder().WithAll<SelectedCharacterReference>().Build().CalculateEntityCount();
            if (selectedCharacterCount > 1)
            {
                state.EntityManager.RemoveComponent<SelectedCharacterReference>(gameControllerEntity);
            }
            var selectedCharacter = SystemAPI.GetSingleton<SelectedCharacterReference>().Value.Value;

            // Set the flag icon to match the space agency of the selected character.
            if (SpaceAgencyFlagController.Instance != null)
            {
                SpaceAgencyFlagController.Instance.SetFlag(selectedCharacter.SpaceAgencySprite);
            }
            
            // Instantiate the player entity.
            var playerEntityPrefab = SystemAPI.GetSingleton<GameEntityPrefabs>().PlayerEntityPrefab;
            var newPlayerEntity = state.EntityManager.Instantiate(playerEntityPrefab);

            // Register the player's material with the ECS graphics system and apply the material to the player prefab.
            var entitiesGraphicsSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EntitiesGraphicsSystem>();
            var playerMaterialID = entitiesGraphicsSystem.RegisterMaterial(selectedCharacter.CharacterMaterial);
            var playerGraphicsEntity = SystemAPI.GetComponent<GraphicsEntity>(newPlayerEntity).Value;
            var playerMaterialMeshInfo = SystemAPI.GetComponentRW<MaterialMeshInfo>(playerGraphicsEntity);
            playerMaterialMeshInfo.ValueRW.MaterialID = playerMaterialID;

            // Initialize the player's stat modifications with any stat modifications set in their CharacterProperties ScriptableObject.
            var baseCharacterStatModifications = state.EntityManager.CreateEntity(typeof(StatModifierEntityTag));
            var baseStatModifiers = state.EntityManager.AddBuffer<StatModifier>(baseCharacterStatModifications);
            foreach (var characterStatModifier in selectedCharacter.StatOverrides)
            {
                baseStatModifiers.Add(new StatModifier
                {
                    Type = characterStatModifier.Type,
                    Value = characterStatModifier.Value
                });
            }
            var playerHitPoints = selectedCharacter.HitPoints;
            state.EntityManager.SetComponentData(newPlayerEntity, new BaseHitPoints { Value = playerHitPoints });
            state.EntityManager.SetComponentData(newPlayerEntity, new CurrentHitPoints { Value = playerHitPoints });
            var activeStatModifiers = SystemAPI.GetBuffer<ActiveStatModifierEntity>(newPlayerEntity);
            activeStatModifiers.Add(new ActiveStatModifierEntity { Value = baseCharacterStatModifications });
            
            // Finalize initialization of the PlayerUpgradeData component for the fields that cannot be baked. This is required as the instance ID of the ScriptableObject stored in the WeightedUpgradeProperties collection will be different during baking and when running the game in a build. Doing the initialization to store the values at runtime ensures the correct instance IDs.
            var upgradeProperties = Resources.LoadAll("ScriptableObjects/UpgradeProperties", typeof(UpgradeProperties)).Cast<UpgradeProperties>().ToArray();
            if (upgradeProperties.Length <= 0) return;
            
            var totalRandomPoints = 0;
            var weightedUpgradeProperties = new WeightedUpgradeProperties[upgradeProperties.Length];

            for (var i = 0; i < upgradeProperties.Length; i++)
            {
                totalRandomPoints += upgradeProperties[i].Rarity;
                weightedUpgradeProperties[i] = new WeightedUpgradeProperties
                {
                    UpgradeProperties = upgradeProperties[i],
                    WeightedRandomValue = totalRandomPoints
                };
            }

            var playerUpgradeData = state.EntityManager.GetComponentObject<PlayerUpgradeData>(gameControllerEntity);
            playerUpgradeData.WeightedUpgradeProperties = weightedUpgradeProperties;
            playerUpgradeData.TotalRandomPoints = totalRandomPoints;

 
            // Hide the loading screen if entering from the main menu.
            if (LoadingScreenUIController.Instance != null)
            {
                LoadingScreenUIController.Instance.HideLoadingScreen();
            }

            // Tell the beam in effect to begin playing.
            if (BeamInEffectController.Instance != null)
            {
                BeamInEffectController.Instance.BeginBeamInEffect();
            }
        }
    }

    /// <summary>
    /// Updates the in-game time during gameplay.
    /// </summary>
    /// <remarks>
    /// System updates in Unity's InitializationSystemGroup and after the <see cref="GlobalTimeUpdateSystem"/> to ensure that SystemAPI.Time.ElapsedTime is accurate for the current frame.
    /// </remarks>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(GlobalTimeUpdateSystem))]
    public partial class UpdateGameTimeSystem : SystemBase
    {
        /// <summary>
        /// Event to update in-game UI with the current time.
        /// </summary>
        public Action<float> OnUpdateGameTime;
        
        protected override void OnCreate()
        {
            RequireForUpdate<GameTime>();
        }
        
        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            var gameTime = SystemAPI.GetSingletonRW<GameTime>();
            gameTime.ValueRW.Value += deltaTime;
            OnUpdateGameTime?.Invoke(gameTime.ValueRO.Value);
        }
    }

    /// <summary>
    /// System to update UI elements on screen.
    /// </summary>
    /// <remarks>
    /// System invokes the event every frame, in this case that is fine as I know there is just a single listener to the event and the alien defeated count it likely to change very frequently so there is no point in adding complexity to manage when to fire these events.
    /// </remarks>
    [UpdateInGroup(typeof(DS_EffectsSystemGroup))]
    public partial class UpdateUISystem : SystemBase 
    {
        /// <summary>
        /// Event invoked to update HUD UI controller with the number of enemies the player has defeated in the current run.
        /// </summary>
        /// <seeaslo cref="HUDUIController"/>
        public Action<int> OnUpdateEnemiesDefeatedCount;
        
        protected override void OnUpdate()
        {
            foreach (var aliensDefeatedCount in SystemAPI.Query<AliensDefeatedCount>())
            {
                OnUpdateEnemiesDefeatedCount?.Invoke(aliensDefeatedCount.Value);
            }
        }
    }
}