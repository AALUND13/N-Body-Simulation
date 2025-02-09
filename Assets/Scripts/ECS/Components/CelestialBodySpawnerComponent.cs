using Unity.Entities;
using Unity.Mathematics;

public struct CelestialBodySpawnerComponent : IComponentData, IEnableableComponent {
    public float2 MassRanage;
    public float2 CenterMassRange;

    public int Count;
    public uint Seed;
    public float MinRadius;
    public Entity Prefab;
}