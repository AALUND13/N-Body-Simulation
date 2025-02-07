using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
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

        var query = SystemAPI.QueryBuilder().WithAll<CelestialBodyComponent, LocalTransform>().Build();
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        NativeArray<BodyData> bodyData = new NativeArray<BodyData>(entities.Length, Allocator.Temp);

        int index = 0;
        foreach(var (celestialBody, localTransform) in SystemAPI.Query<RefRO<CelestialBodyComponent>, RefRO<LocalTransform>>()) {
            bodyData[index] = new BodyData {
                Position = localTransform.ValueRO.Position,
                Mass = celestialBody.ValueRO.Mass,
            };
            index++;
        }

        Octant.NewContaining(bodyData, out Octant bounds);
        octree.Clear(bounds);

        for(int i = 0; i < bodyData.Length; i++) {
            octree.Insert(bodyData[i]);
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