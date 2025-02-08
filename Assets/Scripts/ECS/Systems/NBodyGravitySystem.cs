using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct NBodyGravitySystem : ISystem {
    private Octree octree;

    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<NBodyConfig>();
    }

    public void OnDestroy(ref SystemState state) {
        if(octree.IsCreated)
            octree.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        NBodyConfig config = SystemAPI.GetSingleton<NBodyConfig>();

        if(!octree.IsCreated) {
            octree = new Octree(config.Theta, config.Epsilon, config.G, Allocator.Persistent);

            // Debugging gizmos, Commeted out for burst compilation
            //var localOctree = octree;
            //GizmoHandler.Instance.OnDrawGizmosAction += () => {
            //    localOctree.DrawGizmos();
            //};
        }

        NativeList<BodyData> bodies = new NativeList<BodyData>(Allocator.Temp);

        float3 min = new float3(float.MaxValue);
        float3 max = new float3(float.MinValue);

        foreach(var (celestialBody, localTransform, enity) in SystemAPI.Query<RefRO<CelestialBodyComponent>, RefRO<LocalTransform>>().WithEntityAccess()) {
            min = math.min(min, localTransform.ValueRO.Position);
            max = math.max(max, localTransform.ValueRO.Position);

            bodies.Add(new BodyData {
                Position = localTransform.ValueRO.Position,
                Mass = celestialBody.ValueRO.Mass,
            });
        }

        Octant bounds = new Octant((min + max) * 0.5f, math.cmax(max - min));

        octree.Clear(bounds);

        foreach(var body in bodies) {
            octree.Insert(body.Position, body.Mass);
        }

        octree.Propagate();

        new BodyGravityJob {
            DeltaTime = SystemAPI.Time.DeltaTime,
            Config = config,
            Octree = octree,
        }.ScheduleParallel();
    }

    [BurstCompile]
    private partial struct BodyGravityJob : IJobEntity {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public NBodyConfig Config;
        [ReadOnly] public Octree Octree;

        [BurstCompile]
        public void Execute(ref LocalTransform transform, ref CelestialBodyComponent body) {
            float3 acceleration = Octree.CalculateAcceleration(transform.Position);

            body.Velocity += acceleration * DeltaTime;
            transform.Position += body.Velocity * DeltaTime;
        }
    }
}