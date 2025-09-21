using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// UI controller associated with the current stats panel that is displayed in the game pause menu.
    /// </summary>
    /// <remarks>
    /// Current stats panel displays the current stat value for all 12 modifyable stats in the game. These stats account for modifications the character spawns with and ones modified via passive abilities. Depending on the stat type it will display as an absolute number, absolute difference, or percentage difference.
    /// </remarks>
    /// <seealso cref="PlayerStatStatusUIController"/>
    public class CurrentStatsUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _statModifierPrefab;
        
        private List<GameObject> _elementsToCleanup;
        
        private void Awake()
        {
            _elementsToCleanup = new List<GameObject>();
        }
        
        public void ShowStatsUI()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var defaultValueQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<CharacterDefaultModificationValues>());
            var defaultValues = defaultValueQuery.GetSingleton<CharacterDefaultModificationValues>();
            
            var playerQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<CharacterStatModificationState>());
            var currentValues = playerQuery.GetSingleton<CharacterStatModificationState>();
            var playerEntity = playerQuery.GetSingletonEntity();
            var playerBaseHitPoints = entityManager.GetComponentData<BaseHitPoints>(playerEntity).Value;
            
            var statModifierPropertiesArray = StatModifierController.StatModifierProperties;
            foreach (var statModifierProperties in statModifierPropertiesArray)
            {
                var defaultValue = defaultValues.GetDefaultValue(statModifierProperties.ModifierType);
                var currentValue = currentValues.GetCurrentValue(statModifierProperties.ModifierType);
                if (statModifierProperties.ModifierType == StatModifierType.AdditionalHitPoints)
                {
                    defaultValue = playerBaseHitPoints;
                    currentValue += playerBaseHitPoints;
                }
                var newStatModifierUI = Instantiate(_statModifierPrefab, transform);
                var newStatModifierUIController = newStatModifierUI.GetComponent<PlayerStatStatusUIController>();
                newStatModifierUIController.ShowUI(statModifierProperties, defaultValue, currentValue);

                _elementsToCleanup.Add(newStatModifierUI);
            }
        }

        public void ShowStatsUIForCharacter(CharacterProperties curCharacterProperties)
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var defaultValueQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<CharacterDefaultModificationValues>());
            var defaultValues = defaultValueQuery.GetSingleton<CharacterDefaultModificationValues>();
            
            var statModifierPropertiesArray = StatModifierController.StatModifierProperties;
            foreach (var statModifierProperties in statModifierPropertiesArray)
            {
                var defaultValue = defaultValues.GetDefaultValue(statModifierProperties.ModifierType);
                var currentValue = curCharacterProperties.GetCurrentValue(statModifierProperties.ModifierType, defaultValue);

                var newStatModifierUI = Instantiate(_statModifierPrefab, transform);
                var newStatModifierUIController = newStatModifierUI.GetComponent<PlayerStatStatusUIController>();
                newStatModifierUIController.ShowUI(statModifierProperties, defaultValue, currentValue);

                _elementsToCleanup.Add(newStatModifierUI);
            }
        }

        public void HideStatsUI()
        {
            foreach (var elementToCleanup in _elementsToCleanup)
            {
                Destroy(elementToCleanup);
            }
            _elementsToCleanup.Clear();
        }
    }
}