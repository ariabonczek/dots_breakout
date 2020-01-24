using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

struct CollisionEvent
{
    public Entity BallEntity;
    public Entity CollidedEntity;
}

[UpdateAfter(typeof(MoveBallSystem))]
public class CollideBallSystem : JobComponentSystem
{
    [BurstCompile]
    struct BounceBallOffPaddleJob : IJobForEach<RectangleBounds, MovementSpeed, Translation, BallVelocity>
    {
        public float DeltaTime;
        public Entity PaddleEntity;
        
        [ReadOnly]
        [NativeDisableContainerSafetyRestriction] 
        public ComponentDataFromEntity<Translation> PaddleTranslation;
        
        [ReadOnly] 
        public ComponentDataFromEntity<RectangleBounds> PaddleBounds;
        
        public void Execute(
            [ReadOnly]ref RectangleBounds ballBounds, 
            [ReadOnly]ref MovementSpeed speed,
            ref Translation ballTranslation,
            ref BallVelocity ballVelocity)
        {
            var ballPosition = ballTranslation.Value;
            var paddlePosition = PaddleTranslation[PaddleEntity].Value;
            var paddleRect = PaddleBounds[PaddleEntity];

            var velocity = ballVelocity.Velocity;
            
            // if a collision is detected...
            if (ballPosition.x - ballBounds.HalfWidthHeight.x < paddlePosition.x + paddleRect.HalfWidthHeight.x &&
                ballPosition.x + ballBounds.HalfWidthHeight.x > paddlePosition.x - paddleRect.HalfWidthHeight.x &&
                ballPosition.y - ballBounds.HalfWidthHeight.y < paddlePosition.y + paddleRect.HalfWidthHeight.y &&
                ballPosition.y + ballBounds.HalfWidthHeight.y > paddlePosition.y - paddleRect.HalfWidthHeight.y)
            {
                ballTranslation.Value.xy -= velocity * speed.Speed * DeltaTime;
                ballVelocity.Velocity = -velocity;
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var paddleEntity = GetSingletonEntity<PaddleTag>();
        
        var paddleJob = new BounceBallOffPaddleJob
        {
            DeltaTime = Time.DeltaTime,
            PaddleEntity = paddleEntity,
            
            PaddleTranslation = GetComponentDataFromEntity<Translation>(true),
            PaddleBounds = GetComponentDataFromEntity<RectangleBounds>(true)
        };
        var paddleJobHandle = paddleJob.Schedule(this, inputDependencies);
        
        return paddleJob.Schedule(this, paddleJobHandle);
    }
}