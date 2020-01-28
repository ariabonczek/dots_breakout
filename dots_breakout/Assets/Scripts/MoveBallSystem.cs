using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class MoveBallSystem : JobComponentSystem
{
    [BurstCompile]
    struct MoveBallJob : IJobForEach<Position2D, BallVelocity, MovementSpeed, RectangleBounds>
    {
        public float DeltaTime;
        public ScreenBoundsData ScreenBounds;
        
        public void Execute(
            ref Position2D ballTranslation, 
            ref BallVelocity ballVelocity,
            [ReadOnly] ref MovementSpeed ballSpeed,
            [ReadOnly] ref RectangleBounds ballBounds)
        {
            var position = ballTranslation.Value;
            position.xy += ballVelocity.Velocity * ballSpeed.Speed * math.min(0.0333f,DeltaTime);
            
            if (position.x - ballBounds.HalfWidthHeight.x < ScreenBounds.XYMin.x || position.x + ballBounds.HalfWidthHeight.x > ScreenBounds.XYMax.x)
                ballVelocity.Velocity.x = -ballVelocity.Velocity.x;
            
            if (position.y - ballBounds.HalfWidthHeight.y < ScreenBounds.XYMin.y || position.y + ballBounds.HalfWidthHeight.y > ScreenBounds.XYMax.y)
                ballVelocity.Velocity.y = -ballVelocity.Velocity.y;
            
            position.xy = math.min(math.max(ScreenBounds.XYMin + ballBounds.HalfWidthHeight, position.xy), ScreenBounds.XYMax - ballBounds.HalfWidthHeight);
            
            ballTranslation.Value = position;
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new MoveBallJob
        {
            DeltaTime = Time.DeltaTime,
            ScreenBounds = GetSingleton<ScreenBoundsData>()
        };
        
        return job.Schedule(this, inputDependencies);
    }
}