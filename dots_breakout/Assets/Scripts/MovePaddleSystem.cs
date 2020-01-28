using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

public class MovePaddleSystem : JobComponentSystem
{
    [BurstCompile]
    [RequireComponentTag(typeof(PaddleTag))]
    struct MovePaddleJob : IJobForEach<Position2D, MovementSpeed>
    {
        public float MoveDirection;
        public float DeltaTime;
        
        public void Execute(
            ref Position2D translation,
            [ReadOnly] ref MovementSpeed speed)
        {
            var position = translation.Value;
            position.x += MoveDirection * speed.Speed * DeltaTime;
            translation.Value = position;
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new MovePaddleJob
        {
            MoveDirection = Input.GetAxis("Horizontal"),
            DeltaTime = Time.DeltaTime
        };

        return job.Schedule(this, inputDependencies);
    }
}