using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

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
            bounds = new Octant(Vector3.zero, 0);

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

    [BurstCompile]
    public void IntoOctants(out NativeArray<Octant> octants) {
        octants = new NativeArray<Octant>(8, Allocator.Temp);

        float3 center = Center;
        float3 extents = new float3(Size * 0.5f);  // New half-size for each octant.

        for(int i = 0; i < 8; i++) {
            float3 offset = new float3(
                (i & 1) == 0 ? -extents.x : extents.x,
                (i & 2) == 0 ? -extents.y : extents.y,
                (i & 4) == 0 ? -extents.z : extents.z
            );
            octants[i] = new Octant(center + offset, Size * 0.5f);
        }
    }

    /// <summary>
    /// Determines which octant of this node's bounds contains the given position.
    /// </summary>
    [BurstCompile]
    public int FindOctant(float3 position) {
        int index = 0;
        if(position.x > Center.x)
            index |= 1;
        if(position.y > Center.y)
            index |= 2;
        if(position.z > Center.z)
            index |= 4;
        return index;

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

    // Replace the foreach loop in the Propagate method with the following code
    [BurstCompile]
    public void Propagate() {
        // Go through the nodes in reverse order and propagate values up the tree.
        for(int i = Parents.Length - 1; i >= 0; i--) {
            int nodeIndex = Parents[i];
            int childIndex = Nodes[nodeIndex].Children;

            OctreeNode node = Nodes[nodeIndex];
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
            Nodes[nodeIndex] = node;
        }
    }

    [BurstCompile]
    public void Insert(in BodyData body) {
        int nodeIndex = 0;

        while(Nodes[nodeIndex].IsBranch()) {
            int quadrant = Nodes[nodeIndex].Octant.FindOctant(body.Position);
            nodeIndex = Nodes[nodeIndex].Children + quadrant;
        }

        if(Nodes[nodeIndex].IsEmpty()) {
            OctreeNode node = Nodes[nodeIndex];
            node.Position = body.Position;
            node.Mass = body.Mass;
            Nodes[nodeIndex] = node;
            return;
        }

        float3 existingPos = Nodes[nodeIndex].Position;
        float existingMass = Nodes[nodeIndex].Mass;
        if(body.Position.Equals(existingPos)) {
            OctreeNode node = Nodes[nodeIndex];
            node.Mass = existingMass + body.Mass;
            Nodes[nodeIndex] = node;
            return;
        }

        while(true) {
            int children = Subdivide(nodeIndex);

            int octant1 = Nodes[nodeIndex].Octant.FindOctant(existingPos);
            int octant2 = Nodes[nodeIndex].Octant.FindOctant(body.Position);

            if(octant1 == octant2) {
                nodeIndex = children + octant1;
            } else {
                int n1 = children + octant1;
                int n2 = children + octant2;

                OctreeNode node = Nodes[n1];
                node.Position = existingPos;
                node.Mass = existingMass;
                Nodes[n1] = node;

                OctreeNode node2 = Nodes[n2];
                node2.Position = body.Position;
                node2.Mass = body.Mass;
                Nodes[n2] = node2;

                return;
            }
        }
    }

    [BurstCompile]
    public int Subdivide(int nodeIndex) {
        Parents.Add(nodeIndex);
        int childrenStartIndex = Nodes.Length;

        OctreeNode node = Nodes[nodeIndex];
        node.Children = childrenStartIndex;
        Nodes[nodeIndex] = node;

        var nexts = new NativeArray<int>(8, Allocator.Temp);
        for(int i = 0; i < 7; i++) {
            nexts[i] = childrenStartIndex + i + 1;
        }
        nexts[7] = node.Next;

        node.Octant.IntoOctants(out NativeArray<Octant> octants);

        for(int i = 0; i < 8; i++) {
            Nodes.Add(new OctreeNode(nexts[i], octants[i]));
        }

        return childrenStartIndex;
    }

    [BurstCompile]
    public float3 CalculateAcceleration(float3 position) {
        float3 acceleration = new float3(0, 0, 0);

        int node = 0;
        while(true) {
            OctreeNode n = Nodes[node];

            float3 direction = n.Position - position;
            float distanceSquared = math.lengthsq(direction);
            float distance = math.sqrt(distanceSquared);

            if(n.IsLeaf() || ((n.Octant.Size / distance) < thetaSquared)) {
                var denominator = (distanceSquared + epsilonSquared) * distance;
                acceleration += direction * (gravitationalConstant * n.Mass / (denominator + 0.001f));

                if(n.Next == 0) break;
                node = n.Next;
            } else {
                node = n.Children;
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
    // Stores indices to child nodes if subdivided.
    public int Children;
    public int Next;
    public Octant Octant;

    // Additional data for a leaf node.
    public float3 Position;  // Position of the body stored in this leaf.


    public float Mass;
    public float3 CenterOfMass;

    public OctreeNode(int next, Octant bounds) {
        Children = 0;
        Next = next;
        Octant = bounds;

        Position = float3.zero;

        CenterOfMass = float3.zero;
        Mass = 0;
    }

    /// <summary>
    /// Returns true if this node is a leaf (i.e. not subdivided).
    /// </summary>
    [BurstCompile]
    public bool IsLeaf() {
        return Children == 0;
    }

    /// <summary>
    /// Returns true if this node is a branch (i.e. subdivided).
    /// </summary>
    [BurstCompile]
    public bool IsBranch() {
        return Children != 0;
    }

    /// <summary>
    /// Returns true if this node is empty (contains no body).
    /// </summary>
    [BurstCompile]
    public bool IsEmpty() {
        return Mass == 0;
    }
}
