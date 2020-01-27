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
    [RequireComponentTag(typeof(BrickScore))]
    struct HashBrickPositionsJob : IJobForEachWithEntity<Translation, RectangleBounds>
    {
        public NativeMultiHashMap<int, Entity>.ParallelWriter BrickHashMap;
        public float GridCellSize;
        public float ScreenWidthInCells;

        private void HashAndInsert(float2 brickCornerPosition, Entity e)
        {
            var hash = (int) ((math.floor(brickCornerPosition.x / GridCellSize)) +
                              (math.floor(brickCornerPosition.y / GridCellSize)) * ScreenWidthInCells);
            BrickHashMap.Add(hash, e);
        }
        
        public void Execute(
            Entity e,
            int index,
            [ReadOnly]ref Translation brickPosition, 
            [ReadOnly]ref RectangleBounds brickBounds)
        {
            var brickCenter = brickPosition.Value.xy;
            HashAndInsert(brickCenter + brickBounds.HalfWidthHeight, e);
            HashAndInsert(brickCenter - brickBounds.HalfWidthHeight, e);
            
            HashAndInsert(brickCenter + brickBounds.HalfWidthHeight * new float2(-1.0f,  1.0f), e);
            HashAndInsert(brickCenter + brickBounds.HalfWidthHeight * new float2( 1.0f, -1.0f), e);
        }
    }
    
    [BurstCompile]
    struct CollideBallsWithBricksJob_Accelerated : IJobForEachWithEntity<RectangleBounds, MovementSpeed, Translation, BallVelocity>
    {
        public EntityCommandBuffer.Concurrent Ecb;

        [ReadOnly] 
        public NativeMultiHashMap<int, Entity> BrickHashMap;
        public float GridCellSize;
        public float ScreenWidthInCells;
        
        [ReadOnly] public ComponentDataFromEntity<Translation> BrickTranslationRO;
        [ReadOnly] public ComponentDataFromEntity<RectangleBounds> BrickRectangleBoundsRO;

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

            var hashBallPosition = (int) ((math.floor(ballPosition.x / GridCellSize)) +
                                          (math.floor(ballPosition.y / GridCellSize)) * ScreenWidthInCells);
            var bricksInCell = BrickHashMap.GetValuesForKey(hashBallPosition);
            while (bricksInCell.MoveNext())
            {
                var brickEntity = bricksInCell.Current;
                // todo: remove bricks from hashmap when they are destroyed
                if(!BrickTranslationRO.Exists(brickEntity))
                    continue;
                
                var brickPosition = BrickTranslationRO[brickEntity].Value;
                var brickRect = BrickRectangleBoundsRO[brickEntity];

                var delta = brickPosition.xy - ballPosition.xy;
                var combinedHalfBounds = ballBounds.HalfWidthHeight + brickRect.HalfWidthHeight;

                if (math.all(math.abs(delta) <= combinedHalfBounds))
                {
                    var crossWidth = combinedHalfBounds.x * delta.y;
                    var crossHeight = combinedHalfBounds.y * delta.x;
                        
                    if(crossWidth > crossHeight)
                        if (crossWidth > -crossHeight)
                            invertY = true;
                        else
                            invertX = true;
                    else
                        if (crossWidth > -crossHeight)
                            invertX = true;
                        else
                            invertY = true;
                        
                    Ecb.DestroyEntity(ballIndex, brickEntity);
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
            PaddleEntity = paddleEntity,
            
            PaddleTranslation = GetComponentDataFromEntity<Translation>(true),
            PaddleBounds = GetComponentDataFromEntity<RectangleBounds>(true)
        };
        var paddleHandle = paddleJob.Schedule(this, inputDependencies);
        var collideBrickHandle = paddleHandle;
        
        var brickCount = m_BrickQuery.CalculateEntityCount();
        if (brickCount > 0)
        {
            var screenBounds = GetSingleton<ScreenBoundsData>();
            var gridCellSize = 2.5f;
            var screenWidthHeight = math.abs(screenBounds.XYMax - screenBounds.XYMin);
            var screenWidthInCells = (int) math.ceil(screenWidthHeight.x / gridCellSize);
       
            var brickHashMap = new NativeMultiHashMap<int, Entity>(brickCount * 4, Allocator.TempJob);
            var hashBricksJob = new HashBrickPositionsJob
            {
                BrickHashMap = brickHashMap.AsParallelWriter(),
                GridCellSize = gridCellSize,
                ScreenWidthInCells = screenWidthInCells
            };
            var hashBricksHandle = hashBricksJob.Schedule(this, paddleHandle);
            
            var brickJob = new CollideBallsWithBricksJob_Accelerated
            {
                Ecb = m_EndSimECBSystem.CreateCommandBuffer().ToConcurrent(),
                
                BrickHashMap = brickHashMap,
                GridCellSize = gridCellSize,
                ScreenWidthInCells = screenWidthInCells,

                BrickTranslationRO = GetComponentDataFromEntity<Translation>(true),
                BrickRectangleBoundsRO = GetComponentDataFromEntity<RectangleBounds>(true)
            };
            
            collideBrickHandle = brickJob.Schedule(this, hashBricksHandle);
            m_EndSimECBSystem.AddJobHandleForProducer(collideBrickHandle);
            collideBrickHandle = brickHashMap.Dispose(collideBrickHandle);
        }

        return collideBrickHandle;
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