using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

struct BrickSpawner : IComponentData
{
    public Entity BrickPrefab;
    public int RowCount;
    public float StartY;
    
}
[DisallowMultipleComponent]
[RequiresEntityConversion]
public class BrickSpawnerAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public GameObject BrickPrefab;
    public int RowCount;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new BrickSpawner
        {
            BrickPrefab = conversionSystem.GetPrimaryEntity(BrickPrefab),
            StartY = transform.position.y,
            RowCount = RowCount
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(BrickPrefab);
    }
}