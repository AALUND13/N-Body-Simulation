using Unity.Entities;
using Unity.Mathematics;

public struct CelestialBodyComponent : IComponentData {
    //public float Radius;
    public float Mass;
    public float3 Velocity;
    //public float3 Position;
}