using System;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using System.Linq;
using System.Collections.Generic;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Stat modifier controller singleton. Objects with this component will be marked as Don't Destroy on Load as all scenes will need access to <see cref="StatModifierProperties"/> ScriptableObjects loaded from this controller.
    /// </summary>
    public class StatModifierController : MonoBehaviour
    {
        /// <summary>
        /// Public static singleton access.
        /// </summary>
        public static StatModifierController Instance;
        
        /// <summary>
        /// Array of <see cref="StatModifierProperties"/> loaded from the Resources/ScriptableObjects/StatModifierProperties folder.
        /// </summary>
        public static StatModifierProperties[] StatModifierProperties { get; private set; }

        /// <summary>
        /// Dictionary lookup to find the <see cref="StatModifierProperties"/> for a given <see cref="StatModifierType"/>
        /// </summary>
        private Dictionary<StatModifierType, StatModifierProperties> _statModifierPropertiesLookup;
        
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            LoadStatModifications();
        }

        /// <summary>
        /// Method called in Awake() to load <see cref="StatModifierProperties"/> from resources folder, populate into array and dictionary.
        /// </summary>
        /// <remarks>
        /// Error will be logged in the console if multiple ScriptableObjects with a single <see cref="StatModifierType"/> are detected.
        /// </remarks>
        private void LoadStatModifications()
        {
            StatModifierProperties = Resources.LoadAll("ScriptableObjects/StatModifierProperties", typeof(StatModifierProperties)).Cast<StatModifierProperties>().ToArray();
            var characterDefaultModificationValues = new CharacterDefaultModificationValues();

            _statModifierPropertiesLookup = new Dictionary<StatModifierType, StatModifierProperties>();
            foreach (var statModifierProperties in StatModifierProperties)
            {
                if (!_statModifierPropertiesLookup.TryAdd(statModifierProperties.ModifierType, statModifierProperties))
                {
                    Debug.LogError($"Error: multiple stat modifiers of type: {statModifierProperties.ModifierType} detected");
                    continue;
                }

                var defaultValue = statModifierProperties.CalculationType.GetDefaultModificationValue();
                characterDefaultModificationValues.SetDefaultValue(statModifierProperties.ModifierType, defaultValue);
            }

            var characterDefaultStatsEntity = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntity(typeof(CharacterDefaultModificationValues));
            World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(characterDefaultStatsEntity, characterDefaultModificationValues);
        }

        /// <summary>
        /// Gets the minimum modification value as defined by the associated <see cref="StatModifierProperties"/>.
        /// </summary>
        /// <param name="statModifierType">Stat modification type to get minimum modification value of</param>
        /// <returns>Minimum modification value for desired stat modification type</returns>
        public float GetMinimumModificationValue(StatModifierType statModifierType)
        {
            if (!_statModifierPropertiesLookup.TryGetValue(statModifierType, out var statModifierProperties))
            {
                Debug.LogError($"Error unable to find stat modifier type: {statModifierType}");
                return 0f;
            }

            return statModifierProperties.MinimumModificationValue;
        }

        /// <summary>
        /// Gets the maximum modification value as defined by the associated <see cref="StatModifierProperties"/>.
        /// </summary>
        /// <param name="statModifierType">Stat modification type to get maximum modification value of</param>
        /// <returns>Maximum modification value for desired stat modification type</returns>
        public float GetMaximumModificationValue(StatModifierType statModifierType)
        {
            if (!_statModifierPropertiesLookup.TryGetValue(statModifierType, out var statModifierProperties))
            {
                Debug.LogError($"Error unable to find stat modifier type: {statModifierType}");
                return 0f;
            }

            return statModifierProperties.MaximumModificationValue;
        }
    }

    /// <summary>
    /// Helper class to add extension methods to <see cref="StatModifierType"/> and <see cref="StatModifierCalculationType"/> enums.
    /// </summary>
    public static class StatModifierHelper
    {
        /// <summary>
        /// Extension method to get the default modification value of a stat modification type. Uses the <see cref="StatModifierCalculationType"/> as each of these types will have the same default value.
        /// </summary>
        /// <remarks>
        /// Default value for percentage types is 1f as when it is multiplied by the base value, the base vale will remain unchanged. For other types where stat modifications are added to the base value, the value is initialized to a default value of 0f.
        /// </remarks>
        /// <param name="calculationType">Value type of the stat modifier.</param>
        /// <returns>Default modification value.</returns>
        public static float GetDefaultModificationValue(this StatModifierCalculationType calculationType)
        {
            return calculationType switch
            {
                StatModifierCalculationType.AbsoluteInteger => 0f,
                StatModifierCalculationType.AbsoluteDecimal => 0f,
                StatModifierCalculationType.IncreasingInteger => 0f,
                StatModifierCalculationType.Percentage => 1f,
                _ => 0f
            };
        }

        /// <summary>
        /// Extension method to clamp modification value within minimum and maximum values defined in the <see cref="StatModifierProperties"/> ScriptableObject.
        /// </summary>
        /// <param name="statModifierType">Stat modifier type, used to lookup the minimum and maximum values for the type.</param>
        /// <param name="value">Value to clamp. Variable is a reference type so variable passed in when calling this method will be updated to be inside the minimum and maximum values.</param>
        public static void ClampModificationValue(this StatModifierType statModifierType, ref float value)
        {
            
            var minimumValue = StatModifierController.Instance.GetMinimumModificationValue(statModifierType);
            var maximumValue = StatModifierController.Instance.GetMaximumModificationValue(statModifierType);
            value = math.clamp(value, minimumValue, maximumValue);
            
        }
    }
    
    /// <summary>
    /// Component to hold the default modification values for each <see cref="StatModifierType"/>.
    /// </summary>
    /// <remarks>
    /// This is used as a baseline component to initialize <see cref="CharacterStatModificationState"/> before calculating stat modifications.
    /// </remarks>
    public struct CharacterDefaultModificationValues : IComponentData
    {
        /// <summary>
        /// Move speed modification. This value is multiplied by the character's <see cref="CharacterBaseMoveSpeed"/> in the <see cref="CharacterMoveSystem"/> to calculate the current effective move speed.
        /// </summary>
        public float MoveSpeed;
        /// <summary>
        /// Damage dealing modification. This value is multiplied by the base damage value for an attack in various attack systems to calculate the current effective damage to be assigned to that attack entity.
        /// </summary>
        public float DamageDealt;
        /// <summary>
        /// Additional hit points to increase the character's maximum hit points.
        /// </summary>
        public int AdditionalHitPoints;
        /// <summary>
        /// Reduce incoming damage points by this integer value.
        /// </summary>
        public int DamageReceived;
        /// <summary>
        /// Health regeneration modification. This value represents how many hit points per second are restored in the <see cref="CharacterHealthRegenerationSystem"/>.
        /// </summary>
        public float HealthRegeneration;
        /// <summary>
        /// Attack cooldown modification. This value is multiplied by the base cooldown for an attack in various attack systems to calculate the current effective cooldown for that attack.
        /// </summary>
        public float AttackCooldown;
        /// <summary>
        /// Attack projectile speed modification. This value is multiplied by the base projectile speed for an attack in various attack systems to calculate the current effective projectile speed of attack entities.
        /// </summary>
        public float AttackProjectileSpeed;
        /// <summary>
        /// Attack duration modification. This value is multiplied by the base attack duration for an attack in various attack systems to calculate the current effective duration for how long the attack entity exists in the world.
        /// </summary>
        public float AttackDuration;
        /// <summary>
        /// Number of additional attack projectiles to spawn for each applicable attack.
        /// </summary>
        public int AdditionalAttackProjectiles;
        /// <summary>
        /// Attack area modification. This value is multiplied by the base attack area for an attack in various attack systems to calculate the current scale of the attack.
        /// </summary>
        public float AttackArea;
        /// <summary>
        /// Item attraction radius modifier. This value is multiplied by the player's <see cref="PlayerAttractionAreaData.Radius"/> in the <see cref="DetectItemAttractionSystem"/> to calculate the effective attraction area for the player.
        /// </summary>
        public float ItemAttractionRadius;
        /// <summary>
        /// Experience point gain modifier. This value is multiplied by the value of experience points collected by the player in the <see cref="HandlePlayerExperienceThisFrameSystem"/> to calculate the effective experience point gain for the collected gem.
        /// </summary>
        public float ExperiencePointGain;

        /// <summary>
        /// Helper method to easily initialize values for a given <see cref="StatModifierType"/>.
        /// </summary>
        /// <param name="type">Type of stat modification to initialize.</param>
        /// <param name="value">Value of the stat modifier type.</param>
        /// <exception cref="ArgumentOutOfRangeException">Throws if stat modifier type is invalid.</exception>
        public void SetDefaultValue(StatModifierType type, float value)
        {
            type.ClampModificationValue(ref value);
            switch (type)
            {
                case StatModifierType.DamageDealt:
                    DamageDealt = value;
                    break;
                case StatModifierType.DamageReceived:
                    DamageReceived = (int)value;
                    break;
                case StatModifierType.AdditionalHitPoints:
                    AdditionalHitPoints = (int)value;
                    break;
                case StatModifierType.HealthRegeneration:
                    HealthRegeneration = value;
                    break;
                case StatModifierType.AttackCooldown:
                    AttackCooldown = value;
                    break;
                case StatModifierType.AttackArea:
                    AttackArea = value;
                    break;
                case StatModifierType.AttackProjectileSpeed:
                    AttackProjectileSpeed = value;
                    break;
                case StatModifierType.MoveSpeed:
                    MoveSpeed = value;
                    break;
                case StatModifierType.AttackDuration:
                    AttackDuration = value;
                    break;
                case StatModifierType.AdditionalAttackProjectiles:
                    AdditionalAttackProjectiles = (int)value;
                    break;
                case StatModifierType.ItemAttractionRadius:
                    ItemAttractionRadius = value;
                    break;
                case StatModifierType.ExperiencePointGain:
                    ExperiencePointGain = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Helper method to easily get the default value of a given <see cref="StatModifierType"/>.
        /// </summary>
        /// <param name="type">Stat modifier type to get the default value of.</param>
        /// <returns>Default value of the desired stat modifier type.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Throws if invalid stat modifier type is passed into this function.</exception>
        public float GetDefaultValue(StatModifierType type)
        {
            return type switch
            {
                StatModifierType.DamageDealt => DamageDealt,
                StatModifierType.DamageReceived => DamageReceived,
                StatModifierType.AdditionalHitPoints => AdditionalHitPoints,
                StatModifierType.HealthRegeneration => HealthRegeneration,
                StatModifierType.AttackCooldown => AttackCooldown,
                StatModifierType.AttackArea => AttackArea,
                StatModifierType.AttackProjectileSpeed => AttackProjectileSpeed,
                StatModifierType.MoveSpeed => MoveSpeed,
                StatModifierType.AttackDuration => AttackDuration,
                StatModifierType.AdditionalAttackProjectiles => AdditionalAttackProjectiles,
                StatModifierType.ItemAttractionRadius => ItemAttractionRadius,
                StatModifierType.ExperiencePointGain => ExperiencePointGain,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
    
    /// <summary>
    /// Enum containing all stat modifier types. Used as a way to author stat modifications in the editor and used as a key in several places in code to access information for a given stat modifier type via the associated <see cref="StatModifierProperties"/>
    /// </summary>
    [Serializable]
    public enum StatModifierType : byte
    {
        /// <summary>
        /// None - unused, invalid value.
        /// </summary>
        None = 0,
        /// <summary>
        /// Damage dealing modification. This value is multiplied by the base damage value for an attack in various attack systems to calculate the current effective damage to be assigned to that attack entity.
        /// </summary>
        DamageDealt = 1,
        /// <summary>
        /// Reduce incoming damage points by this integer value.
        /// </summary>
        DamageReceived = 2,
        /// <summary>
        /// Additional hit points to increase the character's maximum hit points.
        /// </summary>
        AdditionalHitPoints = 3,
        /// <summary>
        /// Health regeneration modification. This value represents how many hit points per second are restored in the <see cref="CharacterHealthRegenerationSystem"/>.
        /// </summary>
        HealthRegeneration = 4,
        /// <summary>
        /// Attack cooldown modification. This value is multiplied by the base cooldown for an attack in various attack systems to calculate the current effective cooldown for that attack.
        /// </summary>
        AttackCooldown = 5,
        /// <summary>
        /// Item attraction radius modifier. This value is multiplied by the player's <see cref="PlayerAttractionAreaData.Radius"/> in the <see cref="DetectItemAttractionSystem"/> to calculate the effective attraction area for the player.
        /// </summary>
        AttackArea = 6,
        /// <summary>
        /// Attack duration modification. This value is multiplied by the base attack duration for an attack in various attack systems to calculate the current effective duration for how long the attack entity exists in the world.
        /// </summary>
        AttackProjectileSpeed = 7,
        /// <summary>
        /// Move speed modification. This value is multiplied by the character's <see cref="CharacterBaseMoveSpeed"/> in the <see cref="CharacterMoveSystem"/> to calculate the current effective move speed.
        /// </summary>
        MoveSpeed = 8,
        /// <summary>
        /// Attack duration modification. This value is multiplied by the base attack duration for an attack in various attack systems to calculate the current effective duration for how long the attack entity exists in the world.
        /// </summary>
        AttackDuration = 9,
        /// <summary>
        /// Number of additional attack projectiles to spawn for each applicable attack.
        /// </summary>
        AdditionalAttackProjectiles = 10,
        /// <summary>
        /// Item attraction radius modifier. This value is multiplied by the player's <see cref="PlayerAttractionAreaData.Radius"/> in the <see cref="DetectItemAttractionSystem"/> to calculate the effective attraction area for the player.
        /// </summary>
        ItemAttractionRadius = 11,
        /// <summary>
        /// Experience point gain modifier. This value is multiplied by the value of experience points collected by the player in the <see cref="HandlePlayerExperienceThisFrameSystem"/> to calculate the effective experience point gain for the collected gem.
        /// </summary>
        ExperiencePointGain = 12
    }

    /// <summary>
    /// Defines the calculation type of the stat modifier.
    /// </summary>
    public enum StatModifierCalculationType : byte
    {
        /// <summary>
        /// None - invalid stat modifier calculation type.
        /// </summary>
        None = 0,
        /// <summary>
        /// Stat modification type is an absolute integer. This means this value will be added to the base value for stat modifications and the full value is displayed as an integer in the UI, not just the differential value.
        /// </summary>
        AbsoluteInteger = 1,
        /// <summary>
        /// Stat modification type is an absolute decimal. This means this value will be added to the base value for the stat modifications and the full value is displayed as a decimal in the UI, not just the differential value.
        /// </summary>
        AbsoluteDecimal = 2,
        /// <summary>
        /// Stat modification type is an increasing integer. This means this value will be added to the base value for the stat modification and just the differential value from the default value (0) will be displayed in the UI.
        /// </summary>
        IncreasingInteger = 3,
        /// <summary>
        /// Stat modification type is a percentage. This means this value will be multiplied by the base stat modification value for the type and just the differential value from the default value (1) will be displayed in the UI.
        /// </summary>
        Percentage = 4,
    }
    
    /// <summary>
    /// Tag component to identify a stat modifier entity.
    /// </summary>
    public struct StatModifierEntityTag : IComponentData {}
    
    /// <summary>
    /// Helper struct to author stat modifications in the editor.
    /// </summary>
    [Serializable]
    public struct StatModifierInfo
    {
        /// <summary>
        /// Type of stat modification.
        /// </summary>
        public StatModifierType Type;
        /// <summary>
        /// Value of the stat modification.
        /// </summary>
        public float Value;
    }
    
    /// <summary>
    /// Dynamic buffer to hold stat modifier types and values.
    /// </summary>
    /// <remarks>
    /// This dynamic buffer is attached to the active stat entity for a character, references are stored in <see cref="ActiveStatModifierEntity"/>.
    /// Internal buffer capacity is set to worst case of 12, matching the number of unique stat modifier types. Memory space is not critical for these entities so we can store up to 12 elements inside the chunk.
    /// </remarks>
    /// <seealso cref="RecalculateStatsSystem"/>
    [Serializable]
    [InternalBufferCapacity(12)]
    public struct StatModifier : IBufferElementData
    {
        public StatModifierType Type;
        public float Value;
    }
}