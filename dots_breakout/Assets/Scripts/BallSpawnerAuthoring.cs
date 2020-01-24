using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

struct BallSpawner : IComponentData
{
    public Entity BallPrefab;
    public float2 SpawnPosition;
    public int SpawnCount;
}

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class BallSpawnerAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public GameObject BallPrefab;
    public int NumberOfBalls;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var spawnerEntity = dstManager.CreateEntity(typeof(BallSpawner));
        dstManager.SetComponentData(spawnerEntity, new BallSpawner
        {
            BallPrefab = conversionSystem.GetPrimaryEntity(BallPrefab),
            SpawnPosition = new float2(transform.position.x, transform.position.y),
            SpawnCount = NumberOfBalls
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(BallPrefab);
    }
}