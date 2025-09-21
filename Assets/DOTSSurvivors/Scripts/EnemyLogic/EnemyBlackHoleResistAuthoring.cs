using UnityEngine;
using Unity.Entities;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Enemies tagged with this component are resistant to black holes. Currently only used on ending reaper enemy.
    /// </summary>
    public struct EnemyBlackHoleResistTag : IComponentData {}
    
    /// <summary>
    /// Authoring component to add <see cref="EnemyBlackHoleResistTag"/> to an enemy.
    /// </summary>
    /// <seealso cref="EnemyBlackHoleResistTag"/>
    public class EnemyBlackHoleResistAuthoring : MonoBehaviour
    {
        private class Baker : Baker<EnemyBlackHoleResistAuthoring>
        {
            public override void Bake(EnemyBlackHoleResistAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<EnemyBlackHoleResistTag>(entity);
            }
        }
    }
}