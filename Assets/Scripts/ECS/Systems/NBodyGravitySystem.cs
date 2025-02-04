using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

//[BurstCompile]
public partial struct NBodyGravitySystem : ISystem {
    //[BurstCompile]
    public void OnUpdate(ref SystemState state) {
        NBodyConfig config = SystemAPI.GetSingleton<NBodyConfig>();

        foreach((RefRW<LocalTransform> transform, RefRW<CelestialBodyComponent> body) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<CelestialBodyComponent>>()) {
            float3 force = float3.zero;
            foreach((RefRO<LocalTransform> otherTransform, RefRO<CelestialBodyComponent> otherBody) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<CelestialBodyComponent>>()) {
                if(transform.ValueRO.Position.Equals(otherTransform.ValueRO.Position)) continue;

                float3 direction = otherTransform.ValueRO.Position - transform.ValueRO.Position;
                float distanceSquared = math.lengthsq(direction) + 0.01f; // Avoid division by zero

                float3 unitMagnitude = math.normalize(direction);
                float forceMagnitude = config.G * body.ValueRO.Mass * otherBody.ValueRO.Mass / distanceSquared;

                force += forceMagnitude * unitMagnitude;
            }

            body.ValueRW.Velocity += force / body.ValueRO.Mass * SystemAPI.Time.DeltaTime;
            transform.ValueRW.Position += body.ValueRO.Velocity * SystemAPI.Time.DeltaTime;
        }
    }
}