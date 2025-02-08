using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class CelestialBodyAuthoring : MonoBehaviour {
    public float Mass;
    public float3 Velocity;

    private class Baker : Baker<CelestialBodyAuthoring> {
        public override void Bake(CelestialBodyAuthoring authoring) {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CelestialBodyComponent {
                Mass = authoring.Mass,
                Velocity = authoring.Velocity,
            });
        }
    }
}
