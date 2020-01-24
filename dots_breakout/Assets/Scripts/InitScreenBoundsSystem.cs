using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

struct ScreenBoundsData : IComponentData
{
    public float2 XYMin;
    public float2 XYMax;
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class InitScreenBoundsSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        World.EntityManager.CreateEntity(typeof(ScreenBoundsData));

        var vertExtent = Camera.main.orthographicSize;
        var horzExtent = vertExtent * ((float) Screen.width / Screen.height);
        
        var boundsData = new ScreenBoundsData
        {
            XYMin = new float2(-horzExtent, -vertExtent),
            XYMax = new float2( horzExtent,  vertExtent)
        };
        SetSingleton(boundsData);

        this.Enabled = false;

        return inputDependencies;
    }
}