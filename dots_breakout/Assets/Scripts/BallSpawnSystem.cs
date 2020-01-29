using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class BallSpawnSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var random = new Random(0x1a2b3c4d);
        
        Entities.WithStructuralChanges().ForEach((Entity e, ref BallSpawner spawner) =>
        {
            using (var balls = EntityManager.Instantiate(spawner.BallPrefab, spawner.SpawnCount, Allocator.Temp))
            {
                for (int i = 0; i < balls.Length; ++i)
                {
                    EntityManager.SetComponentData(balls[i], new Velocity2D
                    {
                        Velocity = random.NextFloat2Direction()
                    });

                    EntityManager.SetComponentData(balls[i], new Position2D()
                    {
                        Value = new float2(spawner.SpawnPosition)
                    });
                }
            }
            
            EntityManager.DestroyEntity(e);
        }).Run();

        return inputDependencies;
    }
}