using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// UI Controller associated with the capability status display that is shown in the game pause menu.
    /// </summary>
    /// <remarks>
    /// When the player pauses the game, this status UI will be shown with an icon of all capabilities (weapons and passive abilities) that the character currently has. Each icon will have a series of boxes to indicate the max amount of levels that capability has, and what is the current level.
    /// </remarks>
    /// <seealso cref="CapabilityStatusUIController"/>
    public class CurrentCapabilitiesUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _capabilityStatusPrefab;
        [SerializeField] private Transform _capabilityStatusContainer;
        [SerializeField] private Transform _passiveStatusContainer;
        
        private List<GameObject> _elementsToCleanup;

        private void Awake()
        {
            _elementsToCleanup = new List<GameObject>();
        }
        
        public void ShowCapabilitiesUI()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var gameEntityQuery = entityManager.CreateEntityQuery(ComponentType.ReadWrite<GameEntityTag>());
            var gameEntity = gameEntityQuery.GetSingletonEntity();
            var upgradeData = entityManager.GetComponentData<PlayerUpgradeData>(gameEntity);
            
            foreach(var activeWeaponEntity in upgradeData.ActiveWeaponEntityLookup.Values)
            {
                var weaponUpgradeProperties = entityManager.GetComponentData<WeaponUpgradePropertiesReference>(activeWeaponEntity).Value.Value;
                var currentWeaponState = entityManager.GetComponentData<WeaponState>(activeWeaponEntity);

                var newCapabilityStatusUI = Instantiate(_capabilityStatusPrefab, _capabilityStatusContainer);
                var newCapabilityStatusUIController = newCapabilityStatusUI.GetComponent<CapabilityStatusUIController>();
                newCapabilityStatusUIController.DisplayUI(weaponUpgradeProperties, currentWeaponState.LevelIndex);

                _elementsToCleanup.Add(newCapabilityStatusUI);
            }
            
            foreach(var activePassiveEntity in upgradeData.ActivePassiveEntityLookup.Values)
            {
                var passiveUpgradeProperties = entityManager.GetComponentData<PassiveUpgradePropertiesReference>(activePassiveEntity).Value.Value;
                var curPassiveLevelIndex = entityManager.GetComponentData<PassiveLevelIndex>(activePassiveEntity).Value;

                var newCapabilityStatusUI = Instantiate(_capabilityStatusPrefab, _passiveStatusContainer);
                var newCapabilityStatusUIController = newCapabilityStatusUI.GetComponent<CapabilityStatusUIController>();
                newCapabilityStatusUIController.DisplayUI(passiveUpgradeProperties, curPassiveLevelIndex);

                _elementsToCleanup.Add(newCapabilityStatusUI);
            }
        }

        public void HideCapabilitiesUI()
        {
            foreach (var elementToCleanup in _elementsToCleanup)
            {
                Destroy(elementToCleanup);
            }
            _elementsToCleanup.Clear();
        }
    }
}