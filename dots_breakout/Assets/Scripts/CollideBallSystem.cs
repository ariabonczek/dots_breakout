using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[AlwaysUpdateSystem]
[UpdateAfter(typeof(MoveBallSystem))]
public class CollideBallSystem : JobComponentSystem
{
    private EndSimulationEntityCommandBufferSystem m_EndSimECBSystem;

    private EntityQuery m_BrickQuery;
    
    [BurstCompile]
    struct BounceBallsOffPaddleJob : IJobForEach<RectangleBounds, MovementSpeed, Translation, BallVelocity>
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
            var delta =  paddlePosition.xy - ballPosition.xy;
            var combinedHalfBounds = ballBounds.HalfWidthHeight + paddleRect.HalfWidthHeight;

            if (math.all(math.abs(delta) <= combinedHalfBounds))
            {
                velocity = math.normalize(new float2(math.sign(-delta.x), -velocity.y));

                ballPosition.y += combinedHalfBounds.y + delta.y;
                
                ballVelocity.Velocity = velocity;
                ballTranslation.Value = ballPosition;
            }
        }
    }
    
    [BurstCompile]
    struct CollideBallsWithBricksJob : IJobForEachWithEntity<RectangleBounds, MovementSpeed, Translation, BallVelocity>
    {
        public float DeltaTime;

        public EntityCommandBuffer.Concurrent Ecb;

        [ReadOnly] 
        [DeallocateOnJobCompletion]
        public NativeArray<ArchetypeChunk> BrickChunks;
        
        [ReadOnly] public ArchetypeChunkEntityType BrickEntityRO;
        [NativeDisableContainerSafetyRestriction][ReadOnly] public ArchetypeChunkComponentType<Translation> BrickTranslationRO;
        [NativeDisableContainerSafetyRestriction][ReadOnly] public ArchetypeChunkComponentType<RectangleBounds> BrickRectangleBoundsRO;

        public void Execute(
            Entity e,
            int ballIndex,
            [ReadOnly]ref RectangleBounds ballBounds, 
            [ReadOnly]ref MovementSpeed speed,
            [ReadOnly]ref Translation ballTranslation,
            ref BallVelocity ballVelocity)
        {
            var ballPosition = ballTranslation.Value;
            var velocity = ballVelocity.Velocity;

            var invertX = false;
            var invertY = false;

            for (int chunkIndex = 0; chunkIndex < BrickChunks.Length; ++chunkIndex)
            {
                var chunk = BrickChunks[chunkIndex];
                var entityArray = chunk.GetNativeArray(BrickEntityRO);
                var translationArray = chunk.GetNativeArray(BrickTranslationRO);
                var boundsArray = chunk.GetNativeArray(BrickRectangleBoundsRO);

                for (int brickIndex = 0; brickIndex < entityArray.Length; ++brickIndex)
                {
                    var brickPosition = translationArray[brickIndex].Value;
                    var brickRect = boundsArray[brickIndex];

                    var delta = brickPosition.xy - ballPosition.xy;
                    var combinedHalfBounds = ballBounds.HalfWidthHeight + brickRect.HalfWidthHeight;

                    if (math.all(math.abs(delta) <= combinedHalfBounds))
                    {
                        var crossWidth = combinedHalfBounds.x * delta.y;
                        var crossHeight = combinedHalfBounds.y * delta.x;
                        
                        if(crossWidth > crossHeight)
                        {
                            if (crossWidth > -crossHeight)
                                invertY = true;
                            else
                                invertX = true;
                        }
                        else
                        {
                            if (crossWidth > -crossHeight)
                                invertX = true;
                            else
                                invertY = true;
                        }
                        
                        Ecb.DestroyEntity(ballIndex, entityArray[brickIndex]);
                    }
                }
            }

            if (invertY)
                velocity.y = -velocity.y;
            
            if (invertX)
                velocity.x = -velocity.x;

            ballVelocity.Velocity = velocity;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var paddleEntity = GetSingletonEntity<PaddleTag>();
        
        var paddleJob = new BounceBallsOffPaddleJob
        {
            DeltaTime = Time.DeltaTime,
            PaddleEntity = paddleEntity,
            
            PaddleTranslation = GetComponentDataFromEntity<Translation>(true),
            PaddleBounds = GetComponentDataFromEntity<RectangleBounds>(true)
        };
        var paddleJobHandle = paddleJob.Schedule(this, inputDependencies);

        var brickChunks = m_BrickQuery.CreateArchetypeChunkArray(Allocator.TempJob, out var brickChunksHandle);
        var brickJob = new CollideBallsWithBricksJob
        {
            DeltaTime = Time.DeltaTime,
            Ecb = m_EndSimECBSystem.CreateCommandBuffer().ToConcurrent(),
            
            BrickChunks = brickChunks,
            
            BrickEntityRO = GetArchetypeChunkEntityType(),
            BrickTranslationRO = GetArchetypeChunkComponentType<Translation>(true),
            BrickRectangleBoundsRO = GetArchetypeChunkComponentType<RectangleBounds>(true)
        };
        var brickJobHandle = brickJob.Schedule(this, JobHandle.CombineDependencies(brickChunksHandle, paddleJobHandle));
        m_EndSimECBSystem.AddJobHandleForProducer(brickJobHandle);
        
        return brickJobHandle;
    }

    protected override void OnCreate()
    {
        m_EndSimECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        m_BrickQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new []{ ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<RectangleBounds>(), ComponentType.ReadOnly<BrickScore>() }
        });
    }
}