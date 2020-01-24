using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class MoveBallSystem : JobComponentSystem
{
    [BurstCompile]
    struct MoveBallJob : IJobForEach<Translation, BallPreviousPosition, BallVelocity, MovementSpeed, RectangleBounds>
    {
        public float DeltaTime;
        public ScreenBoundsData ScreenBounds;
        
        public void Execute(
            ref Translation ballTranslation, 
            ref BallPreviousPosition ballPreviousPosition, 
            ref BallVelocity ballVelocity,
            [ReadOnly] ref MovementSpeed ballSpeed,
            [ReadOnly] ref RectangleBounds ballBounds)
        {
            var position = ballTranslation.Value;
            position.xy += ballVelocity.Velocity * ballSpeed.Speed * math.min(0.333f,DeltaTime);
            
            if (position.x - ballBounds.HalfWidthHeight.x < ScreenBounds.XYMin.x || position.x + ballBounds.HalfWidthHeight.x > ScreenBounds.XYMax.x)
                ballVelocity.Velocity.x = -ballVelocity.Velocity.x;
            
            if (position.y - ballBounds.HalfWidthHeight.y < ScreenBounds.XYMin.y || position.y + ballBounds.HalfWidthHeight.y > ScreenBounds.XYMax.y)
                ballVelocity.Velocity.y = -ballVelocity.Velocity.y;
            
            position.xy = math.min(math.max(ScreenBounds.XYMin + ballBounds.HalfWidthHeight, position.xy), ScreenBounds.XYMax - ballBounds.HalfWidthHeight);
            
            ballPreviousPosition.Value = ballTranslation.Value.xy;
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