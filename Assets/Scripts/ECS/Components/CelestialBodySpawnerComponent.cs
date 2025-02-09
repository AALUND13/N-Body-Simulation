using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct CelestialBodySpawnerComponent : IComponentData, IEnableableComponent {
    public float2 MassRanage;
    public float2 CenterMassRange;

    public int Count;
    public uint Seed;
    public float MinRadius;
    public Entity Prefab;
}