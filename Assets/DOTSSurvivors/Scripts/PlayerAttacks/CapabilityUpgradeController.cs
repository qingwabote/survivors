using System;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Managed controller to handle upgrading player capabilities from the GameObject side. This script is required to serve as a bridge between GameObject and ECS sides of the game. UI elements will call methods in this script to configure things on both GameObject and ECS sides related to upgrading capabilities (weapons and passive items).
    /// </summary>
    public class CapabilityUpgradeController : MonoBehaviour
    {
        /// <summary>
        /// Public singleton access so UI elements can call methods on this controller.
        /// </summary>
        public static CapabilityUpgradeController Instance;

        /// <summary>
        /// Event invoked when the player gets a new weapon. UI events listen to this to display the new weapon icon in the HUD.
        /// </summary>
        public Action<WeaponUpgradeProperties> OnAddNewWeapon;
        /// <summary>
        /// Event invoked when the player gets a new passive item. UI events listen to this to display the new passive item icon in the HUD.
        /// </summary>
        public Action<PassiveUpgradeProperties> OnAddNewPassive;
        
        /// <summary>
        /// Entity manager for the default world.
        /// </summary>
        private EntityManager _entityManager;
        /// <summary>
        /// Entity archetype for bonus items. Bonus items are items that are displayed in the upgrade UI when all item slots are full and maxed out. This is used to instantiate the bonus item if required.
        /// </summary>
        /// <seealso cref="BonusItemProperties"/>
        private EntityArchetype _bonusItemArchetype;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Warning, multiple instances of EntityUpgradeControllerManaged are present. Destroying new one.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _bonusItemArchetype = _entityManager.CreateArchetype(ComponentType.ReadWrite<ItemTag>(), ComponentType.ReadWrite<DestroyEntityFlag>(), ComponentType.ReadWrite<EntityInteraction>(), ComponentType.ReadWrite<DestroySelfOnInteractionTag>());
        }

        /// <summary>
        /// "Entry point" for upgrading a capability. Depending on the type of capability specific methods will be called by this method.
        /// </summary>
        /// <param name="upgradeProperties">Upgrade properties ScriptableObject of the capability to be upgraded. Will either be of type <see cref="WeaponUpgradeProperties"/> or <see cref="PassiveUpgradeProperties"/>.</param>
        public void UpgradeCapability(UpgradeProperties upgradeProperties)
        {
            if (upgradeProperties is WeaponUpgradeProperties weaponUpgradeProperties)
            {
                UpgradeWeapon(weaponUpgradeProperties);
            }
            else if (upgradeProperties is PassiveUpgradeProperties passiveUpgradeProperties)
            {
                UpgradePassive(passiveUpgradeProperties);
            }
        }
        
        /// <summary>
        /// Method to upgrade a weapon. On The ECS side, it will spawn a new weapon entity if needed or enable the <see cref="UpgradeWeaponFlag"/>. On the managed Unity side, it will invoke events for things to show up properly in the UI.
        /// </summary>
        /// <param name="upgradeProperties">Upgrade properties for the weapon to be upgraded</param>
        private void UpgradeWeapon(WeaponUpgradeProperties upgradeProperties)
        {
            var gameEntityQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<GameEntityTag>());
            var gameEntity = gameEntityQuery.GetSingletonEntity();
            var upgradeData = _entityManager.GetComponentObject<PlayerUpgradeData>(gameEntity);

            if (upgradeData.ActiveWeaponEntityLookup.TryGetValue(upgradeProperties, out var weaponEntity))
            {
                _entityManager.SetComponentEnabled<UpgradeWeaponFlag>(weaponEntity, true);
            }
            else
            {
                var gameControllerQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<GameEntityTag>());
                var gameControllerEntity = gameControllerQuery.GetSingletonEntity();
                var weaponEntityPrefabLookup = _entityManager.GetComponentObject<ManagedGameData>(gameControllerEntity).WeaponEntityPrefabLookup;
                var weaponPrefab = weaponEntityPrefabLookup[upgradeProperties.WeaponType];
                var newWeaponEntity = _entityManager.Instantiate(weaponPrefab);

                var playerQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<PlayerTag>());
                var playerEntity = playerQuery.GetSingletonEntity();

                _entityManager.AddComponentData(newWeaponEntity, new Parent { Value = playerEntity });
                _entityManager.SetComponentData(newWeaponEntity, new WeaponState
                {
                    AttackCount = 0,
                    LevelIndex = 0,
                    CooldownTimer = 0.15f,
                    NextAttackTimer = 0f
                });
                upgradeData.ActiveWeaponEntityLookup.Add(upgradeProperties, newWeaponEntity);

                OnAddNewWeapon?.Invoke(upgradeProperties);
            }
            
            LevelUpUIController.Instance.HideLevelUpUI();
        }

        /// <summary>
        /// Method to upgrade a passive item. On The ECS side, it will spawn a new passive item entity if needed or enable the <see cref="UpgradePassiveFlag"/>. On the managed Unity side, it will invoke events for things to show up properly in the UI.
        /// </summary>
        /// <param name="upgradeProperties">Upgrade properties for the passive item to be upgraded.</param>
        private void UpgradePassive(PassiveUpgradeProperties upgradeProperties)
        {
            var gameEntityQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<GameEntityTag>(), ComponentType.ReadWrite<GameEntityPrefabs>());
            var gameEntity = gameEntityQuery.GetSingletonEntity();
            var upgradeData = _entityManager.GetComponentObject<PlayerUpgradeData>(gameEntity);
            
            var playerQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<PlayerTag>());
            var playerEntity = playerQuery.GetSingletonEntity();

            if (upgradeData.ActivePassiveEntityLookup.TryGetValue(upgradeProperties, out var passiveEntity))
            {
                _entityManager.SetComponentEnabled<UpgradePassiveFlag>(passiveEntity, true);
            }
            else
            {
                var passivePrefab = gameEntityQuery.GetSingleton<GameEntityPrefabs>().PassivePrefab;
                var newPassive = _entityManager.Instantiate(passivePrefab);
                _entityManager.SetComponentData(newPassive, new PassiveUpgradePropertiesReference { Value = upgradeProperties });
                
                var statModifierBuffer = _entityManager.GetBuffer<ActiveStatModifierEntity>(playerEntity);
                statModifierBuffer.Add(new ActiveStatModifierEntity { Value = newPassive });
                upgradeData.ActivePassiveEntityLookup.Add(upgradeProperties, newPassive);
                
                OnAddNewPassive?.Invoke(upgradeProperties);
            }

            _entityManager.SetComponentEnabled<RecalculateStatsFlag>(playerEntity, true);
            LevelUpUIController.Instance.HideLevelUpUI();
        }

        /// <summary>
        /// Method for granting the player a bonus item. A bonus item is shown when a player levels up or obtains a crate when all capability slots are full and at max level.
        /// </summary>
        /// <param name="itemType">Type of bonus item - health or money.</param>
        /// <param name="itemValue">Value of health or money to grant to the player.</param>
        /// <seealso cref="BonusItemProperties"/>
        public void CollectBonusItem(BonusItemType itemType, int itemValue)
        {
            var playerQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<PlayerTag>());
            var playerEntity = playerQuery.GetSingletonEntity();

            var newBonusItemEntity = _entityManager.CreateEntity(_bonusItemArchetype);
            _entityManager.SetComponentEnabled<DestroyEntityFlag>(newBonusItemEntity, false);
            var healthInteractionBuffer = _entityManager.GetBuffer<EntityInteraction>(newBonusItemEntity);
            healthInteractionBuffer.Add(new EntityInteraction { IsHandled = false, TargetEntity = playerEntity });
            
            switch (itemType)
            {
                case BonusItemType.Health:
                    _entityManager.AddComponentData(newBonusItemEntity, new HealOnInteraction { Value = itemValue });
                    break;

                case BonusItemType.Money:
                    _entityManager.AddComponentData(newBonusItemEntity, new GrantMoneyOnInteraction { Value = itemValue });
                    break;
            }
        }
    }
}