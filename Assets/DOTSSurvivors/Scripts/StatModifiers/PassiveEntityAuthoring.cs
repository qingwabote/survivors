using Unity.Entities;
using UnityEngine;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Enableable component to signify this passive ability should be upgraded to the next level in the <see cref="UpgradePassiveSystem"/>.
    /// </summary>
    public struct UpgradePassiveFlag : IComponentData, IEnableableComponent {}

    /// <summary>
    /// Current level index of the passive ability. Level 1 (lowest level) is index 0.
    /// </summary>
    public struct PassiveLevelIndex : IComponentData
    {
        public int Value;
    }

    /// <summary>
    /// Component to reference the associated <see cref="PassiveUpgradeProperties"/> via UnityObjectRef.
    /// </summary>
    public struct PassiveUpgradePropertiesReference : IComponentData
    {
        /// <summary>
        /// UnityObjectRef to the <see cref="PassiveUpgradeProperties"/> for this passive ability.
        /// </summary>
        public UnityObjectRef<PassiveUpgradeProperties> Value;

        /// <summary>
        /// Custom indexer to retrieve the <see cref="PassiveLevelInfo"/> for a given level. Contains information about stats to modify.
        /// </summary>
        /// <param name="i">Index of the level to get <see cref="PassiveLevelInfo"/> of. Level 1 = index 0</param>
        public PassiveLevelInfo this[int i] => Value.Value.UpgradeProperties[i];
    }
    
    /// <summary>
    /// Authoring script to add required components to passive entity.
    /// </summary>
    /// <remarks>
    /// <see cref="PassiveLevelIndex"/> is initialized to a value of -1 as passive will be upgraded after instantiation as <see cref="UpgradePassiveFlag"/> will be enabled by default.
    /// </remarks>
    /// <seealso cref="UpgradePassiveSystem"/>
    public class PassiveEntityAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PassiveEntityAuthoring>
        {
            public override void Bake(PassiveEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddBuffer<StatModifier>(entity);
                AddComponent(entity, new PassiveLevelIndex { Value = -1 });
                AddComponent<PassiveUpgradePropertiesReference>(entity);
                AddComponent<UpgradePassiveFlag>(entity);
                AddComponent<StatModifierEntityTag>(entity);
            }
        }
    }

    /// <summary>
    /// System to upgrade passive to the next level.
    /// </summary>
    /// <remarks>
    /// Stat modifier upgrades do not stack. The <see cref="StatModifier"/> dynamic buffer is cleared on each upgrade so when authoring the stat modifications in <see cref="PassiveUpgradeProperties"/> only the stat modifications for the current level will be applied.
    /// </remarks>
    public partial struct UpgradePassiveSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (statModifiers, passiveLevelIndex, upgradePropertiesReference, shouldUpgrade) in SystemAPI.Query<DynamicBuffer<StatModifier>, RefRW<PassiveLevelIndex>, PassiveUpgradePropertiesReference, EnabledRefRW<UpgradePassiveFlag>>())
            {
                statModifiers.Clear();
                passiveLevelIndex.ValueRW.Value += 1;

                foreach (var curStatModifier in upgradePropertiesReference[passiveLevelIndex.ValueRO.Value].StatModifiers)
                {
                    statModifiers.Add(curStatModifier);
                }

                shouldUpgrade.ValueRW = false;
            }
        }
    }
}