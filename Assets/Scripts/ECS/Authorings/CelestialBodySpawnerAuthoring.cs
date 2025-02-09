using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class CelestialBodySpawnerAuthoring : MonoBehaviour {
    [Header("Mass Range")]
    public float2 MassRanage;
    public float2 CenterMassRange;

    [Header("Other Settings")]
    public int Count;
    public uint Seed;
    public float MinRadius;
    public GameObject Prefab;

    private class Baker : Baker<CelestialBodySpawnerAuthoring> {
        public override void Bake(CelestialBodySpawnerAuthoring authoring) {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CelestialBodySpawnerComponent {
                MassRanage = authoring.MassRanage,
                CenterMassRange = authoring.CenterMassRange,

                Count = authoring.Count,
                Seed = authoring.Seed,
                MinRadius = authoring.MinRadius,
                Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic)
            });
            SetComponentEnabled<CelestialBodySpawnerComponent>(entity, true);
        }
    }

    public void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
