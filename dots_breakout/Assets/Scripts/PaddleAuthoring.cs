using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct PaddleTag : IComponentData { }

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class PaddleAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float MovementSpeed;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var scale = transform.localScale;
        var bounds = new RectangleBounds
        {
            HalfWidthHeight = new float2(scale.x / 2.0f, scale.y / 2.0f),
        };
        dstManager.AddComponentData(entity, bounds);
        dstManager.AddComponent(entity, typeof(PaddleTag));
        
        dstManager.AddComponentData(entity, new MovementSpeed
        {
            Speed = MovementSpeed
        });
    }
}
