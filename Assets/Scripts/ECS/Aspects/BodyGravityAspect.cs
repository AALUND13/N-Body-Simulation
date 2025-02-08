using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public readonly partial struct BodyGravityAspect : IAspect {
    private readonly RefRW<LocalTransform> transform;
    private readonly RefRW<CelestialBodyComponent> body;

    [BurstCompile]
    public void UpdatePosition(float deltaTime, Octree octree) {
        float3 acceleration = octree.CalculateAcceleration(transform.ValueRO.Position);

        body.ValueRW.Velocity += acceleration * deltaTime;
        transform.ValueRW.Position += body.ValueRO.Velocity * deltaTime;
    }
}