using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct CelestialBodySpawnerComponent : IComponentData, IEnableableComponent {
    public int Count;
    public uint Seed;
    public Bounds SpawningBounds;
    public float2 MassRanage;
    public float2 CenterMassRange;
    public Entity Prefab;
}