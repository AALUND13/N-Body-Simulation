using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class CelestialBodySpawnerAuthoring : MonoBehaviour {
    public int Count;
    public uint Seed;
    public Bounds SpawningBounds;
    public float MinRadius;
    public float2 MassRanage;
    public float2 CenterMassRange;
    public GameObject Prefab;

    private class Baker : Baker<CelestialBodySpawnerAuthoring> {
        public override void Bake(CelestialBodySpawnerAuthoring authoring) {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CelestialBodySpawnerComponent {
                Count = authoring.Count,
                Seed = authoring.Seed,
                SpawningBounds = authoring.SpawningBounds,
                MinRadius = authoring.MinRadius,
                MassRanage = authoring.MassRanage,
                CenterMassRange = authoring.CenterMassRange,
                Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic)
            });
            SetComponentEnabled<CelestialBodySpawnerComponent>(entity, true);
        }
    }

    public void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(SpawningBounds.center, SpawningBounds.size);
    }
}
