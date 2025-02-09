using System;
using System.Linq;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.UIElements;

[BurstCompile]
public struct Octant {
    public float3 Center;
    public float Size;

    public Octant(float3 center, float size) {
        Center = center;
        Size = size;
    }

    [BurstCompile]
    public static void NewContaining(in NativeArray<BodyData> bodyData, out Octant bounds) {
        if(bodyData.Length == 0)
            bounds = new Octant(float3.zero, 0);

        float3 min = new float3(float.MaxValue);
        float3 max = new float3(float.MinValue);

        for(int i = 0; i < bodyData.Length; i++) {
            min = math.min(min, bodyData[i].Position);
            max = math.max(max, bodyData[i].Position);
        }

        float3 center = (min + max) * 0.5f;
        float size = math.cmax(max - min);

        bounds = new Octant(center, size);
    }


    /// <summary>
    /// Determines which octant of this node's bounds contains the given position.
    /// </summary>
    [BurstCompile]
    public int FindOctant(float3 position) {
        return (math.select(0, 1, position.x > Center.x) |
                math.select(0, 2, position.y > Center.y) |
                math.select(0, 4, position.z > Center.z));
    }
}

[BurstCompile]
public struct Octree : IDisposable {
    public NativeList<OctreeNode> Nodes;
    public NativeList<int> Parents;
    public bool IsCreated => Nodes.IsCreated && Parents.IsCreated;

    private float thetaSquared;
    private float epsilonSquared;
    private float gravitationalConstant;

    public Octree(float theta, float epsilon, float gravitationalConstant, Allocator allocator) {
        thetaSquared = theta * theta;
        epsilonSquared = epsilon * epsilon;

        Nodes = new NativeList<OctreeNode>(allocator);
        Parents = new NativeList<int>(allocator);
        this.gravitationalConstant = gravitationalConstant;
    }

    [BurstCompile]
    public void Clear(Octant newOctant) {
        Nodes.Clear();
        Parents.Clear();

        Nodes.Add(new OctreeNode(0, newOctant));
    }

    [BurstCompile]
    public void Propagate() {
        // Go through the nodes in reverse order and propagate values up the tree.
        for(int i = Parents.Length - 1; i >= 0; i--) {
            int nodeIndex = Parents[i];
            int childIndex = Nodes[nodeIndex].Children;

            ref OctreeNode node = ref Nodes.ElementAt(nodeIndex);
            node.Position = Nodes[childIndex].Position * Nodes[childIndex].Mass
                + Nodes[childIndex + 1].Position * Nodes[childIndex + 1].Mass
                + Nodes[childIndex + 2].Position * Nodes[childIndex + 2].Mass
                + Nodes[childIndex + 3].Position * Nodes[childIndex + 3].Mass
                + Nodes[childIndex + 4].Position * Nodes[childIndex + 4].Mass
                + Nodes[childIndex + 5].Position * Nodes[childIndex + 5].Mass
                + Nodes[childIndex + 6].Position * Nodes[childIndex + 6].Mass
                + Nodes[childIndex + 7].Position * Nodes[childIndex + 7].Mass;

            node.Mass = Nodes[childIndex].Mass
                + Nodes[childIndex + 1].Mass
                + Nodes[childIndex + 2].Mass
                + Nodes[childIndex + 3].Mass
                + Nodes[childIndex + 4].Mass
                + Nodes[childIndex + 5].Mass
                + Nodes[childIndex + 6].Mass
                + Nodes[childIndex + 7].Mass;

            node.Position /= node.Mass;
        }
    }

    [BurstCompile]
    public void Insert(float3 position, float mass) {
        int nodeIndex = 0;

        // Loop until we have inserted the new body.
        while(true) {
            ref OctreeNode node = ref Nodes.ElementAt(nodeIndex);

            // If we are at a leaf node, handle the insertion.
            if(node.IsLeaf()) {
                if(node.IsEmpty()) {
                    node.Position = position;
                    node.Mass = mass;
                    return;
                }

                if(position.Equals(node.Position)) {
                    node.Mass += mass;
                    return;
                }

                int childrenStart = Subdivide(nodeIndex);

                // Determine in which child (octant) the existing body and the new body belong.
                int octantExisting = node.Octant.FindOctant(node.Position);
                int octantNew = node.Octant.FindOctant(position);

                // If both bodies belong in the same child, we need to descend and insert into that child.
                if(octantExisting == octantNew) {
                    ref OctreeNode child = ref Nodes.ElementAt(childrenStart + octantExisting);
                    child.Position = node.Position;
                    child.Mass = node.Mass;

                    nodeIndex = childrenStart + octantNew;
                } else {
                    // Otherwise, assign each body to its respective child.
                    ref OctreeNode child1 = ref Nodes.ElementAt(childrenStart + octantExisting);
                    child1.Position = node.Position;
                    child1.Mass = node.Mass;

                    ref OctreeNode child2 = ref Nodes.ElementAt(childrenStart + octantNew);
                    child2.Position = position;
                    child2.Mass = mass;
                    return;
                }
            } else {
                int quadrant = node.Octant.FindOctant(position);
                nodeIndex = node.Children + quadrant;
            }
        }
    }


    [BurstCompile]
    public int Subdivide(int nodeIndex) {
        Parents.Add(nodeIndex);
        int childrenStartIndex = Nodes.Length;

        Nodes.ElementAt(nodeIndex).Children = childrenStartIndex;

        float3 center = Nodes[nodeIndex].Octant.Center;
        float3 extents = Nodes[nodeIndex].Octant.Size * 0.5f;

        int next = childrenStartIndex + 1;

        for(int i = 0; i < 8; i++) {
            float3 offset = new float3(
                (i & 1) == 0 ? -extents.x : extents.x,
                (i & 2) == 0 ? -extents.y : extents.y,
                (i & 4) == 0 ? -extents.z : extents.z
            );
            Octant octant = new Octant(center + offset, extents.x);

            if(i == 7) {
                Nodes.Add(new OctreeNode(Nodes[nodeIndex].Next, octant));
            } else {
                Nodes.Add(new OctreeNode(next + i, octant));
            }
        }

        return childrenStartIndex;
    }

    [BurstCompile]
    public float3 CalculateAcceleration(float3 position) {
        float3 acceleration = float3.zero;

        int nodeIndex = 0;
        while(true) {
            OctreeNode node = Nodes[nodeIndex];

            float3 direction = node.Position - position;
            float distanceSquared = math.lengthsq(direction);

            if(node.IsLeaf() || (node.Octant.Size * node.Octant.Size < distanceSquared * thetaSquared)) {
                float denominator = math.max((distanceSquared + epsilonSquared) * math.sqrt(distanceSquared), 0.01f);
                acceleration += direction * (gravitationalConstant * node.Mass / denominator);

                if(node.Next == 0) break;
                nodeIndex = node.Next;
            } else {
                nodeIndex = node.Children;
            }
        }

        return acceleration;
    }

    [BurstCompile]
    public void Dispose() {
        Nodes.Dispose();
        Parents.Dispose();
    }
}

[BurstCompile]
public struct OctreeNode {
    public int Children;
    public int Next;
    public Octant Octant;

    public float3 Position;
    public float3 CenterOfMass;

    public float Mass;

    public OctreeNode(int next, Octant bounds) {
        Children = 0;
        Next = next;
        Octant = bounds;

        Position = float3.zero;

        CenterOfMass = float3.zero;
        Mass = 0;
    }

    [BurstCompile]
    public bool IsLeaf() {
        return Children == 0;
    }

    [BurstCompile]
    public bool IsBranch() {
        return Children != 0;
    }

    [BurstCompile]
    public bool IsEmpty() {
        return Mass == 0;
    }
}