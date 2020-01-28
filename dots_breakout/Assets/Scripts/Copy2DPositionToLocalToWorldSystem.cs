using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(CollideBallSystem))]
public class Copy2DPositionToLocalToWorldSystem : JobComponentSystem
{
    [BurstCompile]
    struct Copy2DPositionToLocalToWorldSystemJob : IJobForEach<Position2D, RectangleBounds, LocalToWorld>
    {
        public void Execute(
            [ReadOnly]ref Position2D position, 
            [ReadOnly]ref RectangleBounds bounds, 
            ref LocalToWorld localToWorld)
        {
            var scale = float4x4.Scale(new float3(bounds.HalfWidthHeight * 2.0f, 1.0f));
            var translate = float4x4.Translate(new float3(position.Value, 0.0f));
            localToWorld.Value = math.mul(translate, scale);
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new Copy2DPositionToLocalToWorldSystemJob();
        return job.Schedule(this, inputDependencies);
    }
}