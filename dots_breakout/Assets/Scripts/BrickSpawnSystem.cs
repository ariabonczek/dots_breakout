using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

[UpdateAfter(typeof(InitScreenBoundsSystem))]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public class BrickSpawnSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        Entities.WithStructuralChanges().ForEach((Entity e, ref BrickSpawner spawner) =>
        {
            var screenBounds = GetSingleton<ScreenBoundsData>();
            var screenWidth = screenBounds.XYMax.x - screenBounds.XYMin.x;
        
            var brickBounds = EntityManager.GetComponentData<RectangleBounds>(spawner.BrickPrefab);
            var brickWidth = brickBounds.HalfWidthHeight.x * 2.0f;
            var brickHeight = brickBounds.HalfWidthHeight.y * 2.0f;
            var bricksPerRow = (int) math.floor(screenWidth / brickWidth);
            var startX = screenBounds.XYMin.x + (screenWidth - brickWidth * bricksPerRow) / 2.0f + brickBounds.HalfWidthHeight.x;
            var currentX = startX;
            var currentY = spawner.StartY;
        
            var brickCount = spawner.RowCount * bricksPerRow;
            using (var bricks = EntityManager.Instantiate(spawner.BrickPrefab, brickCount, Allocator.Temp))
            {
                for (int y = 0; y < spawner.RowCount; ++y)
                {
                    for (int x = 0; x < bricksPerRow; ++x)
                    {
                        var brickIndex = y * bricksPerRow + x;
                        EntityManager.SetComponentData(bricks[brickIndex], new Position2D{Value = new float2(currentX, currentY)});
                        currentX += brickWidth;
                    }
                    currentY += brickHeight;
                    currentX = startX;
                }
            }
            
            EntityManager.DestroyEntity(e);
        }).Run();
        return inputDependencies;
    }
}