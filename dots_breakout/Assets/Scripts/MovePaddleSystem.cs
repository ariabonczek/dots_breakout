using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class MovePaddleSystem : JobComponentSystem
{
    [BurstCompile]
    [RequireComponentTag(typeof(PaddleTag))]
    struct MovePaddleJob : IJobForEach<Position2D, RectangleBounds, MovementSpeed>
    {
        public ScreenBoundsData ScreenBounds;
        public float MoveDirection;
        public float DeltaTime;
        
        public void Execute(
            ref Position2D translation,
            [ReadOnly] ref RectangleBounds bounds,
            [ReadOnly] ref MovementSpeed speed)
        {
            var position = translation.Value;
            position.x += MoveDirection * speed.Speed * DeltaTime;
            position = math.min(math.max(ScreenBounds.XYMin + bounds.HalfWidthHeight, position), ScreenBounds.XYMax - bounds.HalfWidthHeight);
            translation.Value = position;
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new MovePaddleJob
        {
            ScreenBounds = GetSingleton<ScreenBoundsData>(),
            MoveDirection = Input.GetAxis("Horizontal"),
            DeltaTime = Time.DeltaTime
        };

        return job.Schedule(this, inputDependencies);
    }
}