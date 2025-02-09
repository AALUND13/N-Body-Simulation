using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial class CelestialBodySpawnerSystem : SystemBase {
    protected override void OnCreate() {
        RequireForUpdate<NBodyConfig>();
    }

    [BurstCompile]
    protected override void OnUpdate() {
        NBodyConfig config = SystemAPI.GetSingleton<NBodyConfig>();

        // Create a command buffer to record commands
        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);

        foreach(var (spawnerComponent, localTransform, localToWorld, entity) in SystemAPI.Query<RefRO<CelestialBodySpawnerComponent>, RefRO<LocalTransform>, RefRO<LocalToWorld>>().WithEntityAccess()) {
            Entity centralEntity = commandBuffer.Instantiate(spawnerComponent.ValueRO.Prefab);
            Unity.Mathematics.Random random = new Unity.Mathematics.Random(spawnerComponent.ValueRO.Seed);

            float centerBodyMass = random.NextFloat(spawnerComponent.ValueRO.CenterMassRange.x, spawnerComponent.ValueRO.CenterMassRange.y);

            commandBuffer.SetComponent(centralEntity, new LocalTransform {
                Position = localTransform.ValueRO.Position,
                Rotation = quaternion.identity,
                Scale = NBodyUtils.GetRadiusWithMass(centerBodyMass, 1f)
            });
            commandBuffer.SetComponent(centralEntity, new CelestialBodyComponent {
                Mass = centerBodyMass,
                Velocity = float3.zero
            });

            float4x4 matrix = localToWorld.ValueRO.Value;
            float3 scale;
            scale.x = math.length(matrix.c0.xyz);
            scale.y = math.length(matrix.c1.xyz);
            scale.z = math.length(matrix.c2.xyz);
            Bounds bounds = new Bounds(localTransform.ValueRO.Position, scale);

            NativeArray<float3> positionEntities = NBodyUtils.GetRandomPositionInSphere(bounds, spawnerComponent.ValueRO.MinRadius, spawnerComponent.ValueRO.Seed, spawnerComponent.ValueRO.Count);

            for(int i = 0; i < spawnerComponent.ValueRO.Count; i++) {
                Entity spawnedEntity = commandBuffer.Instantiate(spawnerComponent.ValueRO.Prefab);

                float mass = random.NextFloat(spawnerComponent.ValueRO.MassRanage.x, spawnerComponent.ValueRO.MassRanage.y);
                commandBuffer.SetComponent(spawnedEntity, new LocalTransform {
                    Position = positionEntities[i],
                    Rotation = quaternion.identity,
                    Scale = NBodyUtils.GetRadiusWithMass(mass, 1f)
                });
                commandBuffer.SetComponent(spawnedEntity, new CelestialBodyComponent {
                    Mass = mass,
                    Velocity = NBodyUtils.GetOrbitalVelocity(positionEntities[i], float3.zero, centerBodyMass, config.G)
                });
            }

            commandBuffer.SetComponentEnabled<CelestialBodySpawnerComponent>(entity, false);
        }

        commandBuffer.Playback(EntityManager);
    }
}
