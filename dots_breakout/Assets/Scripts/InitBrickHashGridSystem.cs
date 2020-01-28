using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

struct BrickHashGrid : ISystemStateComponentData
{
    public UnsafeMultiHashMap<int, Entity> Grid;
    public float GridCellSize;
    public float ScreenWidthInCells;
}

// Used for querying for the grid during destruction
struct BrickHashGridTag : IComponentData
{}

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(InitScreenBoundsSystem))]
[UpdateAfter(typeof(BrickSpawnSystem))]
public class InitBrickHashGridSystem : JobComponentSystem
{
    private EntityQuery m_BrickQuery;

    [BurstCompile]
    [RequireComponentTag(typeof(BrickScore))]
    struct HashBrickPositionsJob : IJobForEachWithEntity<Position2D, RectangleBounds>
    {
        public UnsafeMultiHashMap<int, Entity> Grid;
        public float GridCellSize;
        public float ScreenWidthInCells;
        
        private void HashAndInsert(float2 brickCornerPosition, Entity e)
        {
            var hash = (int) ((math.floor(brickCornerPosition.x / GridCellSize)) +
                              (math.floor(brickCornerPosition.y / GridCellSize)) * ScreenWidthInCells);
            Grid.Add(hash, e);
        }
        
        public void Execute(
            Entity e,
            int index,
            [ReadOnly]ref Position2D brickPosition, 
            [ReadOnly]ref RectangleBounds brickBounds)
        {
            var brickCenter = brickPosition.Value.xy;
            HashAndInsert(brickCenter + brickBounds.HalfWidthHeight, e);
            HashAndInsert(brickCenter - brickBounds.HalfWidthHeight, e);
            
            HashAndInsert(brickCenter + brickBounds.HalfWidthHeight * new float2(-1.0f,  1.0f), e);
            HashAndInsert(brickCenter + brickBounds.HalfWidthHeight * new float2( 1.0f, -1.0f), e);
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var brickCount = m_BrickQuery.CalculateEntityCount();
        if (brickCount == 0)
            return inputDependencies;
        
        World.EntityManager.CreateEntity(typeof(BrickHashGrid), typeof(BrickHashGridTag));

        var screenBounds = GetSingleton<ScreenBoundsData>();
        var gridCellSize = 2.5f;
        var screenWidthHeight = math.abs(screenBounds.XYMax - screenBounds.XYMin);
        var screenWidthInCells = (int) math.ceil(screenWidthHeight.x / gridCellSize);
        var hashGrid = new UnsafeMultiHashMap<int, Entity>(brickCount * 4, Allocator.Persistent);
        
        new HashBrickPositionsJob
        {
            Grid = hashGrid,
            GridCellSize = gridCellSize,
            ScreenWidthInCells = screenWidthInCells
        }.Run(this, inputDependencies);
        
        var brickHashGrid = new BrickHashGrid
        {
            Grid = hashGrid,
            GridCellSize = gridCellSize,
            ScreenWidthInCells = screenWidthInCells
        };
        SetSingleton(brickHashGrid);
        this.Enabled = false;

        return inputDependencies;
    }
    
    protected override void OnCreate()
    {
        m_BrickQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new []{ ComponentType.ReadOnly<Position2D>(), ComponentType.ReadOnly<RectangleBounds>(), ComponentType.ReadOnly<BrickScore>() }
        });
    }
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class DestroyBrickHashGridSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        Entities
            .WithNone<BrickHashGridTag>()
            .ForEach((ref BrickHashGrid grid) =>
            {
                grid.Grid.Dispose();
            }).Run();

        return inputDependencies;
    }
}