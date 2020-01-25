using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

struct BrickScore : IComponentData
{
    public int Value;
}

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class BrickAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var scale = transform.localScale;
        var bounds = new RectangleBounds
        {
            HalfWidthHeight = new float2(scale.x / 2.0f, scale.y / 2.0f),
        };
        dstManager.AddComponentData(entity, bounds);

        dstManager.AddComponent<BrickScore>(entity);
    }
}
