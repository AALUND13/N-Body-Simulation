using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial class CelestialBodySpawnerSystem : SystemBase {
    protected override void OnCreate() {
        RequireForUpdate<NBodyConfig>();
    }

    [BurstCompile]
    protected override void OnUpdate() {
        NBodyConfig config = SystemAPI.GetSingleton<NBodyConfig>();

        // Get all entities with CelestialBodySpawnerComponent
        var spawners = SystemAPI.QueryBuilder().WithAll<CelestialBodySpawnerComponent>().Build();
        var spawnerEntities = spawners.ToEntityArray(Allocator.Temp);

        // Create a command buffer to record commands
        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);

        foreach(var spawnerEntity in spawnerEntities) {
            CelestialBodySpawnerComponent celestialBodySpawner = EntityManager.GetComponentData<CelestialBodySpawnerComponent>(spawnerEntity);

            Entity centralEntity = commandBuffer.Instantiate(celestialBodySpawner.Prefab);
            Random random = new Random(celestialBodySpawner.Seed);

            float centerBodyMass = random.NextFloat(celestialBodySpawner.CenterMassRange.x, celestialBodySpawner.CenterMassRange.y);

            commandBuffer.SetComponent(centralEntity, new LocalTransform {
                Position = celestialBodySpawner.SpawningBounds.center,
                Rotation = quaternion.identity,
                Scale = NBodyUtils.GetRadiusWithMass(centerBodyMass, 1f)
            });
            commandBuffer.SetComponent(centralEntity, new CelestialBodyComponent {
                Mass = centerBodyMass,
                Velocity = float3.zero
            });

            NativeArray<float3> positionEntities = NBodyUtils.GetRandomPositionInSphere(celestialBodySpawner.SpawningBounds, celestialBodySpawner.Seed, celestialBodySpawner.Count);

            for(int i = 0; i < celestialBodySpawner.Count; i++) {
                Entity spawnedEntity = commandBuffer.Instantiate(celestialBodySpawner.Prefab);

                float mass = random.NextFloat(celestialBodySpawner.MassRanage.x, celestialBodySpawner.MassRanage.y);
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

            commandBuffer.SetComponentEnabled<CelestialBodySpawnerComponent>(spawnerEntity, false);
        }

        commandBuffer.Playback(EntityManager);
    }
}
