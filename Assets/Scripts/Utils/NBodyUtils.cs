using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public static class NBodyUtils {
    [BurstCompile]
    public static float GetRadiusWithMass(float mass, float density) {
        return math.pow(mass / (density * 4f / 3f * math.PI), 1f / 3f);
    }

    public static float3 GetOrbitalVelocity(float3 bodyAPos, float3 bodyBPos, float bodyBMass, float gravitationalConstant) {
        float3 direction = bodyBPos - bodyAPos;
        float distance = math.length(direction);
        if(distance == 0) return float3.zero;

        float speed = math.sqrt(gravitationalConstant * bodyBMass / distance);
        float3 orbitalVelocity = math.normalize(math.cross(math.normalize(direction), new float3(0, 1, 0))) * speed;

        if(math.all(orbitalVelocity == float3.zero))
            orbitalVelocity = math.normalize(math.cross(math.normalize(direction), new float3(1, 0, 0))) * speed;

        return orbitalVelocity;
    }

    public static float3 GetRandomPositionInSphere(Bounds bounds, uint seed) {
        float radiusX = bounds.size.x / 2f;
        float radiusY = bounds.size.y / 2f;
        float radiusZ = bounds.size.z / 2f;

        Unity.Mathematics.Random random = new Unity.Mathematics.Random(seed);

        float theta = random.NextFloat(0, 2 * math.PI);
        float phi = math.acos(random.NextFloat(-1, 1));
        float r = math.pow(random.NextFloat(), 1f / 3f);

        return new float3(
            math.sin(phi) * math.cos(theta) * radiusX * r,
            math.sin(phi) * math.sin(theta) * radiusY * r,
            math.cos(phi) * radiusZ * r
        );
    }

    public static NativeArray<float3> GetRandomPositionInSphere(Bounds bounds, uint seed, int count) {
        float radiusX = bounds.size.x / 2f;
        float radiusY = bounds.size.y / 2f;
        float radiusZ = bounds.size.z / 2f;
        Unity.Mathematics.Random random = new Unity.Mathematics.Random(seed);
        NativeArray<float3> positions = new NativeArray<float3>(count, Allocator.Temp);
        for(int i = 0; i < count; i++) {
            float theta = random.NextFloat(0, 2 * math.PI);
            float phi = math.acos(random.NextFloat(-1, 1));
            float r = math.pow(random.NextFloat(), 1f / 3f);
            positions[i] = new float3(
                math.sin(phi) * math.cos(theta) * radiusX * r,
                math.sin(phi) * math.sin(theta) * radiusY * r,
                math.cos(phi) * radiusZ * r
            );
        }
        return positions;
    }
}