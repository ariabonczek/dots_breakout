using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

struct BrickScore : IComponentData
{
    public int Value;
}

struct Position2D : IComponentData
{
    public float2 Value;
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

        dstManager.AddComponent<Position2D>(entity);
        dstManager.RemoveComponent<Translation>(entity);
        dstManager.RemoveComponent<Rotation>(entity);
    }
}
