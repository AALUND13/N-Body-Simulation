using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class CelestialBodyAuthoring : MonoBehaviour {
    //public float Radius;
    public float Mass;
    public float3 Velocity;
    //public float3 Position;

    private class Baker : Baker<CelestialBodyAuthoring> {
        public override void Bake(CelestialBodyAuthoring authoring) {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CelestialBodyComponent {
                //Radius = authoring.Radius,
                Mass = authoring.Mass,
                Velocity = authoring.Velocity,
                //Position = authoring.Position
            });
        }
    }
}
