using Unity.Entities;
using Unity.Mathematics;

public struct CelestialBodyComponent : IComponentData {
    public float Mass;
    public float3 Velocity;
}