using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum InitialVelocityType {
    Orbital,
    None
}

[ExecuteInEditMode]
public class CelestialBodyManager : MonoBehaviour {
    public static CelestialBodyManager instance;
    public List<CelestialBody> celestialBodies = new List<CelestialBody>();

    [Header("Simulation Settings")]
    public float TimeScale = 1f;
    public float GravitationalConstant = 0.0001f;
    public bool UseUnityPhysics = false;

    [Header("Generation")]
    public bool useRandomGeneration = false;
    public int generationCount = 100;
    public GameObject CelestialBodyPrefab
        ;
    public Vector3 spawnBounds = new Vector3(100, 10, 100);
    public Vector2 massRange = new Vector2(1, 100);

    public bool GenerateCenterBody = true;
    public Vector2 CenterMassRange = new Vector2(10000, 1000000);

    public InitialVelocityType initialVelocityType = InitialVelocityType.Orbital;

    [Header("Compute Shader")]
    public ComputeShader nBodyComputeShader;

    private ComputeBuffer celestialBodyDataBuffer;

    private ComputeBuffer collisionsBuffer;
    private ComputeBuffer collisionCountBuffer;

    private CelestialBodyDate[] celestialBodyData;
    private Collision[] collisions;
    private int[] collisionCount;

    private int bufferCapacity = 10;

    internal Dictionary<CelestialBody, (List<Vector3>, List<Vector3>)> positionsDict = new Dictionary<CelestialBody, (List<Vector3>, List<Vector3>)>();

    private float cachedStep => positionsDict.Count > 0 ? positionsDict.Max(pair => pair.Value.Item1.Count) : 0;

    public Dictionary<CelestialBody, List<Vector3>> SimulateStep(int steps) {
        foreach(var body in celestialBodies) {
            if(!positionsDict.TryGetValue(body, out var lists)) {
                positionsDict[body] = (new List<Vector3> { body.transform.position }, new List<Vector3> { body.Velocity });
            }
        }


        if(celestialBodies.Count == 0)
            return positionsDict.ToDictionary(pair => pair.Key, pair => pair.Value.Item1);

        if(celestialBodyDataBuffer == null || celestialBodyData.Length != celestialBodies.Count) {
            InitializeBuffers(); // Reallocate buffers if count changes
        }

        for(int i = 0; i < Mathf.Max(steps - cachedStep, 0); i++) {
            DispatchComputeShader(true);

            for(int j = 0; j < celestialBodies.Count; j++) {
                CelestialBody body = celestialBodies[j];
                positionsDict[body].Item1.Add(celestialBodyData[j].position);
                positionsDict[body].Item2.Add(celestialBodyData[j].position);
            }
        }

        return positionsDict.ToDictionary(pair => pair.Key, pair => pair.Value.Item1);
    }

    public GameObject CreateBodyFromObject(GameObject gameObject, Vector3 position, Vector3 velocity, float mass) {
        GameObject body = Instantiate(CelestialBodyPrefab, position, Quaternion.identity);
        CelestialBody celestialBody = body.GetComponent<CelestialBody>();
        celestialBody._mass = mass;
        celestialBody.UseUnityPhysics = UseUnityPhysics;

        Rigidbody rb = body.GetComponent<Rigidbody>();

        if(UseUnityPhysics) {
            if(rb == null) {
                rb = body.AddComponent<Rigidbody>();
            }
            rb.mass = mass;
            rb.useGravity = false;
            rb.velocity = velocity;
        } else {
            if(rb != null) {
                DestroyImmediate(rb);
            }
            celestialBody._velocity = velocity;
        }

        celestialBody.transform.position = position;
        celestialBody._position = position;

        return body;
    }

    public void GenerateRandomBodies() {
        GameObject parent = new GameObject("Celestial Bodies");
        GameObject centerBody = null;
        if(GenerateCenterBody) {
            centerBody = CreateBodyFromObject(gameObject, Vector3.zero, Vector3.zero, UnityEngine.Random.Range(CenterMassRange.x, CenterMassRange.y));
            centerBody.transform.SetParent(parent.transform);
        }

        float radiusX = spawnBounds.x;
        float radiusY = spawnBounds.y;
        float radiusZ = spawnBounds.z;

        for(int i = 0; i < generationCount; i++) {
            float theta = Random.Range(0f, Mathf.PI * 2f);
            float phi = Mathf.Acos(Random.Range(-1f, 1f));
            float r = Mathf.Pow(Random.value, 1f / 3f);

            Vector3 position = new Vector3(
                Mathf.Sin(phi) * Mathf.Cos(theta) * radiusX * r,
                Mathf.Sin(phi) * Mathf.Sin(theta) * radiusY * r,
                Mathf.Cos(phi) * radiusZ * r
            );

            float mass = Random.Range(massRange.x, massRange.y);

            Vector3 velocity = Vector3.zero;
            switch(initialVelocityType) {
                case InitialVelocityType.Orbital:
                    if(GenerateCenterBody) {
                        velocity = GetOrbitalVelocity(position, centerBody.transform.position, centerBody.GetComponent<CelestialBody>()._mass);
                    } else {
                        float avgMass = (massRange.x + massRange.y) / 2 * generationCount / 2;
                        velocity = GetOrbitalVelocity(position, Vector3.zero, avgMass);
                    }
                    break;
                case InitialVelocityType.None:
                    velocity = Vector3.zero;
                    break;
            }
            CreateBodyFromObject(gameObject, position, velocity, mass).transform.SetParent(parent.transform);
        }
    }


    public Vector3 GetOrbitalVelocity(Vector3 position, Vector3 centerPostion, float mass) {
        Vector3 direction = position - centerPostion;
        float distance = direction.magnitude;

        if(distance == 0)
            return Vector3.zero;

        float speed = Mathf.Sqrt(GravitationalConstant * mass / distance);

        Vector3 orbitalVelocity = Vector3.Cross(direction.normalized, Vector3.up).normalized * speed;

        if(orbitalVelocity == Vector3.zero)
            orbitalVelocity = Vector3.Cross(direction.normalized, Vector3.right).normalized * speed;

        return orbitalVelocity;
    }


    private void InitializeBuffers() {
        if(celestialBodies.Count <= bufferCapacity && celestialBodyDataBuffer != null) return;

        bufferCapacity = Mathf.Max(celestialBodies.Count, bufferCapacity * 2);

        ReleaseBuffers();

        celestialBodyData = new CelestialBodyDate[bufferCapacity];

        for(int i = 0; i < celestialBodies.Count; i++) {
            if(celestialBodies[i] == null) {
                celestialBodies.RemoveAt(i);
                i--;
                continue;
            }
            if(positionsDict.TryGetValue(celestialBodies[i], out var lists)) {
                celestialBodyData[i].position = lists.Item1.Last();
                celestialBodyData[i].velocity = lists.Item2.Last();
            } else {
                celestialBodyData[i].position = celestialBodies[i].ObjTransform.position;
                celestialBodyData[i].velocity = celestialBodies[i].Velocity;
            }
            celestialBodyData[i].mass = celestialBodies[i]._mass;
            celestialBodyData[i].radius = celestialBodies[i]._radius;
        }

        AllocateBuffers();
    }

    private void AllocateBuffers() {
        celestialBodyDataBuffer = new ComputeBuffer(bufferCapacity, CelestialBodyDate.GetBufferLength());

        collisionsBuffer = new ComputeBuffer(bufferCapacity, sizeof(int) * 2 + sizeof(float));
        collisionCountBuffer = new ComputeBuffer(1, sizeof(int));

        collisions = new Collision[bufferCapacity];
        collisionCount = new int[1];


        nBodyComputeShader.SetBuffer(0, "Collisions", collisionsBuffer);
        nBodyComputeShader.SetBuffer(0, "CollisionCount", collisionCountBuffer);

        nBodyComputeShader.SetFloat("deltaTime", Time.fixedDeltaTime);
        nBodyComputeShader.SetFloat("gravitationalConstant", GravitationalConstant);

        nBodyComputeShader.SetBuffer(0, "Bodies", celestialBodyDataBuffer);
        celestialBodyDataBuffer.SetData(celestialBodyData);
    }

    private readonly List<(CelestialBody, CelestialBody, float)> collisionData = new List<(CelestialBody, CelestialBody, float)>();

    private List<(CelestialBody, CelestialBody, float)> DispatchComputeShader(bool simulate = false) {
        if(celestialBodies.Count == 0) return new List<(CelestialBody, CelestialBody, float)>();


        // Update buffer data
        bool hasDataChanged = false;
        for(int i = 0; i < celestialBodies.Count; i++) {
            if(celestialBodies[i] == null) {
                celestialBodies.RemoveAt(i);
                i--;
                continue;
            }

            Vector3 newPosition = positionsDict.ContainsKey(celestialBodies[i]) ? positionsDict[celestialBodies[i]].Item1.Last() : celestialBodies[i].ObjTransform.position;
            Vector3 newVelocity = positionsDict.ContainsKey(celestialBodies[i]) ? positionsDict[celestialBodies[i]].Item2.Last() : celestialBodies[i].Velocity;

            if(celestialBodyData[i].position != newPosition
                || celestialBodyData[i].velocity != newVelocity
                || celestialBodyData[i].mass != celestialBodies[i]._mass
                || celestialBodyData[i].radius != celestialBodies[i]._radius
            ) {
                celestialBodyData[i].position = newPosition;
                celestialBodyData[i].velocity = newVelocity;
                celestialBodyData[i].mass = celestialBodies[i]._mass;
                celestialBodyData[i].radius = celestialBodies[i]._radius;
                hasDataChanged = true;
            }

        }

        if(hasDataChanged) {
            celestialBodyDataBuffer.SetData(celestialBodyData);
        }

        if(collisionCount[0] > 0) {
            collisionCount[0] = 0;
            nBodyComputeShader.SetBuffer(0, "CollisionCount", collisionCountBuffer);
            collisionCountBuffer.SetData(collisionCount);
            collisionData.Clear();
        }

        nBodyComputeShader.SetInt("numBodies", celestialBodies.Count);
        nBodyComputeShader.SetFloat("timeScale", Application.isPlaying ? TimeScale : 25f);
        nBodyComputeShader.SetFloat("deltaTime", Time.fixedDeltaTime);
        //nBodyComputeShader.SetFloat("gravitationalConstant", GravitationalConstant);

        nBodyComputeShader.Dispatch(0, Mathf.CeilToInt(celestialBodies.Count / 64.0f), 1, 1);

        // Retrieve updated collision data
        collisionCountBuffer.GetData(collisionCount);
        int numCollisions = collisionCount[0];

        if(!UseUnityPhysics && numCollisions > 0 && !simulate) {
            collisionsBuffer.GetData(collisions);
            // Store collided bodies to prevent multiple collisions
            List<CelestialBody> collidedBodies = new List<CelestialBody>();
            for(int i = 0; i < numCollisions; i++) {
                if(collidedBodies.Contains(celestialBodies[collisions[i].bodyA]) || collidedBodies.Contains(celestialBodies[collisions[i].bodyB])) continue;

                Collision collision = collisions[i];

                CelestialBody bodyA = celestialBodies[collision.bodyA];
                CelestialBody bodyB = celestialBodies[collision.bodyB];

                collidedBodies.Add(bodyA);
                collidedBodies.Add(bodyB);

                collisionData.Add((bodyA, bodyB, collision.depth));
            }
        }

        celestialBodyDataBuffer.GetData(celestialBodyData);

        return collisionData;
    }

    private void HandleCollision(CelestialBody bodyA, CelestialBody bodyB, float penetrationDepth) {
        //Debug.Log($"Collision detected: {bodyA.name} <-> {bodyB.name} (Depth: {penetrationDepth})");
        // Trigger collision event on the heavier body
        if(bodyA.Mass >= bodyB.Mass) {
            bodyA.OnCollision(bodyB, penetrationDepth);
            bodyA.OnPropertyChange();
        } else {
            bodyB.OnCollision(bodyA, penetrationDepth);
            bodyB.OnPropertyChange();
        }
    }

    private void OnEnable() {
        instance = this;
    }
    private void Start() {
        if(!Application.isPlaying) return;

        if(useRandomGeneration) {
            GenerateRandomBodies();
        }

        bufferCapacity = celestialBodies.Count;
        InitializeBuffers();
    }
    private void OnDestroy() {
        ReleaseBuffers();
    }

    private void ReleaseBuffers() {
        if(collisionsBuffer != null) collisionsBuffer.Release();
        if(collisionCountBuffer != null) collisionCountBuffer.Release();
        if(celestialBodyDataBuffer != null) celestialBodyDataBuffer.Release();
    }

    private void SetPositions() {
        for(int i = 0; i < celestialBodies.Count; i++) {
            CelestialBody body = celestialBodies[i];

            if(UseUnityPhysics) {
                if(body.ObjRigidbody != null) {
                    body.ObjRigidbody.AddForce(celestialBodyData[i].force);
                }
                body._velocity = body.ObjRigidbody.velocity;
            } else {
                if(body.ObjTransform.hasChanged || celestialBodyData[i].position != body._position) {
                    body.ObjTransform.position = celestialBodyData[i].position;
                    body._position = celestialBodyData[i].position;
                    body._velocity = celestialBodyData[i].velocity;
                }
            }
        }
    }

    private void OnValidate() {
        foreach(CelestialBody celestialBody in celestialBodies) {
            if(!positionsDict.ContainsKey(celestialBody)) return;
            positionsDict[celestialBody].Item1.Clear();
            positionsDict[celestialBody].Item2.Clear();
        }
    }

    private void FixedUpdate() {
        if(!Application.isPlaying) return;
        Time.timeScale = TimeScale;


        if(UseUnityPhysics) {
            DispatchComputeShader();
            SetPositions();
            return;
        }

        if(positionsDict.Count > 0 && positionsDict[celestialBodies[0]].Item1.Count > 0) {
            foreach(var body in celestialBodies) {
                var bodyPositions = positionsDict[body].Item1;
                var bodyVelocities = positionsDict[body].Item2;

                if(bodyPositions.Count > 0) {
                    body.ObjTransform.position = bodyPositions[0];
                    body._position = bodyPositions[0];
                    body._velocity = bodyVelocities[0];

                    bodyPositions.RemoveAt(0);
                    bodyVelocities.RemoveAt(0);
                }
            }
            return;
        }

        InitializeBuffers();
        var collisions = DispatchComputeShader();
        SetPositions();

        foreach(var collision in collisions) {
            HandleCollision(collision.Item1, collision.Item2, collision.Item3);
        }
    }



    void OnDrawGizmos() {
        if(useRandomGeneration) {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(spawnBounds.x * 2, spawnBounds.y * 2, spawnBounds.z * 2));
        }
    }

    struct Collision {
        public int bodyA;
        public int bodyB;
        public float depth;
    }

    struct CelestialBodyDate {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 force;
        public float radius;
        public float mass;

        public CelestialBodyDate Create(CelestialBody body) {
            return new CelestialBodyDate {
                position = body.ObjTransform.position,
                velocity = body.Velocity,
                mass = body.Mass,
                radius = body.Radius,
                force = Vector3.zero
            };
        }

        public void Apply(CelestialBody body) {
            body.ObjTransform.position = position;
            body._position = position;
            body._velocity = velocity;
            body._radius = radius;
            body._mass = mass;
            if(body.ObjRigidbody != null) {
                body.ObjRigidbody.AddForce(force);
            }
        }

        public static int GetBufferLength() {
            return (sizeof(float) * 3 * 3) + (sizeof(float) * 2);
        }
    }
}
