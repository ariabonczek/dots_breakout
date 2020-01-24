using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct RectangleBounds : IComponentData
{
    public float2 HalfWidthHeight;
}

public struct BallVelocity : IComponentData
{
    public float2 Velocity;
}

public struct BallPreviousPosition : IComponentData
{
    public float2 Value;
}

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class BallAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float2 InitialVelocity;
    public float MovementSpeed;
    
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var scale = transform.localScale;
        var bounds = new RectangleBounds
        {
            HalfWidthHeight = new float2(scale.x / 2.0f, scale.y / 2.0f),
        };
        dstManager.AddComponentData(entity, bounds);
        
        dstManager.AddComponentData(entity, new MovementSpeed
        {
            Speed = MovementSpeed
        });

        dstManager.AddComponentData(entity, new BallPreviousPosition
        {
            Value = new float2(transform.position.x, transform.position.y)
        });
        
        dstManager.AddComponent<BallVelocity>(entity);
    }
}
