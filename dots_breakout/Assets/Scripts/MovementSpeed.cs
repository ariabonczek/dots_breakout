using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct MovementSpeed : IComponentData
{
    public float Speed;
}
