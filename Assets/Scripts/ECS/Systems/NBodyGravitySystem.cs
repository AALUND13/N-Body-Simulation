using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct NBodyGravitySystem : ISystem {
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<NBodyConfig>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        // Build a query for the needed components using QueryBuilder.
        var query = SystemAPI.QueryBuilder().WithAll<CelestialBodyComponent, LocalTransform>().Build();

        // Get an array of matching entities so we know how many there are.
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

        // Allocate a NativeArray to hold our body data.
        NativeArray<BodyData> bodyData = new NativeArray<BodyData>(entities.Length, Allocator.TempJob);

        // Populate the bodyData array by iterating over the query.
        int index = 0;
        foreach(var (celestialBody, localTransform) in SystemAPI.Query<RefRO<CelestialBodyComponent>, RefRO<LocalTransform>>()) {
            bodyData[index] = new BodyData {
                Position = localTransform.ValueRO.Position,
                Mass = celestialBody.ValueRO.Mass
            };
            index++;
        }

        // Schedule the gravity job with the populated NativeArray.
        new BodyGravityJob {
            DeltaTime = SystemAPI.Time.DeltaTime,
            Config = SystemAPI.GetSingleton<NBodyConfig>(),
            Bodies = bodyData
        }.ScheduleParallel();

        // Dispose of the temporary NativeArray after the job completes.
        state.Dependency = bodyData.Dispose(state.Dependency);
    }

    [BurstCompile]
    private partial struct BodyGravityJob : IJobEntity {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public NBodyConfig Config;
        [ReadOnly] public NativeArray<BodyData> Bodies;

        [BurstCompile]
        public void Execute(ref LocalTransform transform, ref CelestialBodyComponent body) {
            float3 force = float3.zero;
            for(int i = 0; i < Bodies.Length; i++) {
                BodyData otherBody = Bodies[i];
                // Skip self by comparing positions.
                if(transform.Position.Equals(otherBody.Position))
                    continue;

                float3 direction = otherBody.Position - transform.Position;
                float distanceSquared = math.lengthsq(direction) + 0.01f; // Avoid division by zero.
                float3 unitMagnitude = math.normalize(direction);
                float forceMagnitude = Config.G * body.Mass * otherBody.Mass / distanceSquared;
                force += forceMagnitude * unitMagnitude;
            }

            body.Velocity += force / body.Mass * DeltaTime;
            transform.Position += body.Velocity * DeltaTime;
        }
    }

}

public struct BodyData {
    public float3 Position;
    public float Mass;
}
