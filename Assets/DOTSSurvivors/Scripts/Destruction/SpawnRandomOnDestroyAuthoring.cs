using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Hash128 = Unity.Entities.Hash128;

namespace TMG.DOTSSurvivors
{
    /// <summary>
    /// Data component to store the hash code of the <see cref="RandomItemDropProperties"/> ScriptableObject associated with this entity.
    /// </summary>
    /// <remarks>
    /// This is used in the <see cref="InitializeSpawnRandomOnDestroySystem"/> to determine if a <see cref="RandomItemToSpawnBlob"/> blob asset has already been created for this ScriptableObject. If it has, that blob asset will be assigned, if it hasn't been created, it will create one and associate with this hash value.
    /// </remarks>
    /// <seealso cref="RandomItemDropProperties"/>
    /// <seealso cref="RandomItemToSpawnBlob"/>
    /// <seealso cref="DestroyEntitySystem"/>
    public struct SpawnRandomPropertiesHashCode : IComponentData
    {
        public uint Value;
    }

    /// <summary>
    /// Dynamic Buffer component which is required because there is no way to "covert" an GameObject into an Entity at runtime. As such the entity conversion will take place in the <see cref="SpawnRandomOnDestroyAuthoring.Baker"/> and those prefabs will be stored in this Dynamic Buffer. At runtime, a blob asset will be generated storing these entity prefabs.
    /// </summary>
    /// <remarks>
    /// Due to Unity's entity remapping from the "bake-time" entity to the runtime entity, the entity index will not be the same. Thus the prefabs need to be created and available at runtime before a reference to them can be stored in a blob asset.
    /// </remarks>
    public struct SpawnRandomPrefabHelper : IBufferElementData
    {
        public int Rarity;
        public Entity Prefab;
    }

    /// <summary>
    /// Struct to store information related to items that can be spawned when this entity is destroyed. Used as the base element of the <see cref="RandomItemToSpawnBlob"/> blob array.
    /// </summary>
    /// <seealso cref="RandomItemToSpawnBlob"/>
    public struct RandomItemToSpawn
    {
        public int RarityKey;
        public Entity Entity;
    }

    /// <summary>
    /// Component containing the blob array of <see cref="RandomItemToSpawn"/> elements. Used in the <see cref="DestroyEntitySystem"/> to determine which entity prefab to instantiate when the entity with this component is destroyed.
    /// </summary>
    /// <remarks>
    /// Due to the nature of entity remapping from prefabs created during baking to prefabs available at runtime, this blob array must be populated with the required prefabs at runtime. To facilitate with this, prefabs that can be spawned are first baked into entities in the <see cref="SpawnRandomOnDestroyAuthoring.Baker"/> into a DynamicBuffer of <see cref="SpawnRandomPrefabHelper"/>. The <see cref="InitializeSpawnRandomOnDestroySystem"/> will populate this blob array with the appropriate data.
    /// This could be implemented as a cleanup component to avoid having the logic in <see cref="DestroyEntitySystem"/>, however I chose to have the logic held there so that we can also have an <see cref="InstantDestroyEntitySystem"/> which does not spawn additional entities. With this as a cleanup component, we'd need to specifically remove the component in <see cref="InstantDestroyEntitySystem"/>.
    /// </remarks>
    /// <seealso cref="RandomItemToSpawn"/>
    /// <seealso cref="RandomItemDropProperties"/>
    /// <seealso cref="DestroyEntitySystem"/>
    public struct RandomItemToSpawnBlob : IComponentData
    {
        public BlobAssetReference<BlobArray<RandomItemToSpawn>> Value;
    }
    
    /// <summary>
    /// Authoring component to associate a <see cref="RandomItemDropProperties"/> ScriptableObject with an entity. This authoring component will also convert GameObject prefabs set in the <see cref="RandomItemDropProperties"/> and store them in a temporary <see cref="SpawnRandomPrefabHelper"/> DynamicBuffer.
    /// </summary>
    /// <remarks>
    /// Due to the nature of entity remapping from prefabs created during baking to prefabs available at runtime, a <see cref="RandomItemToSpawnBlob"/> blob array must be populated with the required prefabs at runtime. To facilitate with this, prefabs that can be spawned are first baked into entities in the <see cref="SpawnRandomOnDestroyAuthoring.Baker"/> into a DynamicBuffer of <see cref="SpawnRandomPrefabHelper"/>. The <see cref="InitializeSpawnRandomOnDestroySystem"/> will populate this blob array with the appropriate data. It is important that we include DependsOn(authoring.RandomItemDropProperties) so baking is re-ran when values on the ScriptableObject are changed.
    /// </remarks>
    /// <seealso cref="RandomItemToSpawn"/>
    /// <seealso cref="RandomItemDropProperties"/>
    /// <seealso cref="DestroyEntitySystem"/>
    /// <seealso cref="InitializeSpawnRandomOnDestroySystem"/>
    /// <seealso cref="SpawnRandomPrefabHelper"/>
    [RequireComponent(typeof(DamageableEntityAuthoring))]
    public class SpawnRandomOnDestroyAuthoring : MonoBehaviour
    {
        /// <summary>
        /// ScriptableObject defining the items and rarity this entity can drop upon destruction.
        /// </summary>
        /// <remarks>
        /// It is important that we include DependsOn(authoring.RandomItemDropProperties) so baking is re-ran when values on the ScriptableObject are changed.
        /// </remarks>
        public RandomItemDropProperties RandomItemDropProperties;

        private class Baker : Baker<SpawnRandomOnDestroyAuthoring>
        {
            public override void Bake(SpawnRandomOnDestroyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                DependsOn(authoring.RandomItemDropProperties);
                AddComponent(entity, new SpawnRandomPropertiesHashCode { Value = (uint)authoring.RandomItemDropProperties.GetHashCode() });
                var itemDropInitializationBuffer = AddBuffer<SpawnRandomPrefabHelper>(entity);
                foreach (var itemDrop in authoring.RandomItemDropProperties.ItemDrops)
                {
                    itemDropInitializationBuffer.Add(new SpawnRandomPrefabHelper
                    {
                        Rarity = itemDrop.Rarity,
                        Prefab = GetEntity(itemDrop.Prefab, TransformUsageFlags.Dynamic)
                    });
                }
            }
        }
    }

    /// <summary>
    /// This system is responsible for creating a blob asset containing a blob array of <see cref="RandomItemToSpawn"/> to be stored in a <see cref="RandomItemToSpawnBlob"/> component.
    /// </summary>
    /// <remarks>
    /// Due to the nature of entity remapping from prefabs created during baking to prefabs available at runtime, a <see cref="RandomItemToSpawnBlob"/> blob array must be populated with the required prefabs at runtime. To facilitate with this, prefabs that can be spawned are first baked into entities in the <see cref="SpawnRandomOnDestroyAuthoring.Baker"/> into a DynamicBuffer of <see cref="SpawnRandomPrefabHelper"/>. The <see cref="InitializeSpawnRandomOnDestroySystem"/> will populate this blob array with the appropriate data.
    /// This system uses the <see cref="GameStartSystem.Singleton.GameIndex"/> to ensure a unique hash for blob assets are created each time the game is entered. This is important because if blob assets from previous games are used, they will not map to valid prefabs and thus nothing will spawn.
    /// </remarks>
    /// <seealso cref="RandomItemToSpawn"/>
    /// <seealso cref="RandomItemDropProperties"/>
    /// <seealso cref="DestroyEntitySystem"/>
    /// <seealso cref="SpawnRandomOnDestroyAuthoring "/>
    /// <seealso cref="SpawnRandomPrefabHelper"/>
    [UpdateInGroup(typeof(DS_InitializationSystemGroup))]
    public partial struct InitializeSpawnRandomOnDestroySystem : ISystem
    {
        /// <summary>
        /// Blob Asset Store is required to perform Blob Asset Operations. In this case it is used to determine if a blob asset has already been created for the <see cref="RandomItemDropProperties"/> ScriptableObject. If not, a new one will be created and added to the Blob Asset Store so that subsequent entities with the same RandomItemDropProperties will reference the same Blob Asset without creating a new one.
        /// </summary>
        private BlobAssetStore _blobAssetStore;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStartSystem.Singleton>();
            _blobAssetStore = new BlobAssetStore(1);
        }
        
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            var gameIndex = (uint)SystemAPI.GetSingleton<GameStartSystem.Singleton>().GameIndex;
            foreach (var (randomItemBuffer, hashCode, entity) in SystemAPI.Query<DynamicBuffer<SpawnRandomPrefabHelper>, SpawnRandomPropertiesHashCode>().WithNone<RandomItemToSpawnBlob>().WithEntityAccess())
            {
                var hash = new Hash128(hashCode.Value, gameIndex, 0u, 0u);
                
                if (!_blobAssetStore.TryGet<BlobArray<RandomItemToSpawn>>(hash, out var blobAssetReference))
                {
                    var builder = new BlobBuilder(state.WorldUpdateAllocator);
                    ref var randomBlob = ref builder.ConstructRoot<BlobArray<RandomItemToSpawn>>();

                    var arrayBuilder = builder.Allocate(ref randomBlob, randomItemBuffer.Length);

                    var rarity = 0;
                    for (var i = 0; i < randomItemBuffer.Length; i++)
                    {
                        rarity += randomItemBuffer[i].Rarity;

                        arrayBuilder[i] = new RandomItemToSpawn
                        {
                            Entity = randomItemBuffer[i].Prefab,
                            RarityKey = rarity
                        };
                    }

                    blobAssetReference = builder.CreateBlobAssetReference<BlobArray<RandomItemToSpawn>>(Allocator.Persistent);
                    _blobAssetStore.TryAdd(hash, ref blobAssetReference);
                }
                
                ecb.AddComponent(entity, new RandomItemToSpawnBlob { Value = blobAssetReference });
            }

            ecb.Playback(state.EntityManager);
        }
    }
}