using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct Octree : IDisposable {
    public NativeList<OctreeNode> Nodes;
    public NativeList<int> Parents;
    public Allocator Allocator;

    public Octree(Allocator allocator) {
        Nodes = new NativeList<OctreeNode>(allocator);
        Parents = new NativeList<int>(allocator);
        Allocator = allocator;
    }

    [BurstCompile]
    public void Clear(Bounds newBounds) {
        Nodes.Clear();
        Parents.Clear();

        Nodes.Add(new OctreeNode(0, newBounds));
    }

    [BurstCompile]
    public void Propagate() {
        // Go through the nodes in reverse order and propagate values up the tree.
        for(int i = Nodes.Length - 1; i >= 0; i--) {
            if(Nodes[i].IsLeaf()) continue;

            int children = Nodes[i].Children;

            OctreeNode node = Nodes[i];
            node.Position = Nodes[children].Position * Nodes[children].Mass
                + Nodes[children + 1].Position * Nodes[children + 1].Mass
                + Nodes[children + 2].Position * Nodes[children + 2].Mass
                + Nodes[children + 3].Position * Nodes[children + 3].Mass
                + Nodes[children + 4].Position * Nodes[children + 4].Mass
                + Nodes[children + 5].Position * Nodes[children + 5].Mass
                + Nodes[children + 6].Position * Nodes[children + 6].Mass
                + Nodes[children + 7].Position * Nodes[children + 7].Mass;

            node.Mass = Nodes[children].Mass
                + Nodes[children + 1].Mass
                + Nodes[children + 2].Mass
                + Nodes[children + 3].Mass
                + Nodes[children + 4].Mass
                + Nodes[children + 5].Mass
                + Nodes[children + 6].Mass
                + Nodes[children + 7].Mass;

            node.Position /= node.Mass;
            Nodes[i] = node;
        }
    }

    [BurstCompile]
    public void Insert(in BodyData body) {
        int nodeIndex = 0;

        while (Nodes[nodeIndex].IsBranch()) {
            int quadrant = Nodes[nodeIndex].FindOctant(body.Position);
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

            int octant1 = Nodes[nodeIndex].FindOctant(existingPos);
            int octant2 = Nodes[nodeIndex].FindOctant(body.Position);

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
        int childrenCount = Nodes.Length;

        OctreeNode node = Nodes[nodeIndex];
        node.Children = childrenCount;
        Nodes[nodeIndex] = node;

        var nexts = new NativeList<int>(8, Allocator.Temp);
        for(int i = 0; i < 7; i++) {
            nexts.Add(childrenCount + i);
        }
        nexts.Add(node.Next);

        node.IntoOctants(out NativeArray<Bounds> octants);
        for(int i = 0; i < 8; i++) {
            Nodes.Add(new OctreeNode(nexts[i], octants[i]));
        }

        return childrenCount;
    }


    [BurstCompile]
    public static void NewContaining(in NativeArray<BodyData> bodyData, out Bounds bounds) {
        if(bodyData.Length == 0)
            bounds = new Bounds(Vector3.zero, Vector3.zero);

        float3 min = new float3(float.MaxValue);
        float3 max = new float3(float.MinValue);

        for(int i = 0; i < bodyData.Length; i++) {
            min = math.min(min, bodyData[i].Position);
            max = math.max(max, bodyData[i].Position);
        }

        float3 center = (min + max) * 0.5f;
        float3 size = max - min;

        bounds = new Bounds(center, size);
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
    public Bounds Bounds;

    // Additional data for a leaf node.
    public float3 Position;  // Position of the body stored in this leaf.


    public float Mass;
    public float3 CenterOfMass;

    public OctreeNode(int next, Bounds bounds) {
        Children = 0;
        Next = next;
        Bounds = bounds;

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

    /// <summary>
    /// Splits this node's bounds into 8 octants.
    /// </summary>
    [BurstCompile]
    public void IntoOctants(out NativeArray<Bounds> octants) {
        octants = new NativeArray<Bounds>(8, Allocator.Temp);

        Vector3 center = Bounds.center;
        Vector3 extents = Bounds.extents * 0.5f;  // New half-size for each octant.

        for(int i = 0; i < 8; i++) {
            Vector3 offset = new Vector3(
                (i & 1) == 0 ? -extents.x : extents.x,
                (i & 2) == 0 ? -extents.y : extents.y,
                (i & 4) == 0 ? -extents.z : extents.z
            );
            octants[i] = new Bounds(center + offset, extents * 2);
        }
    }

    /// <summary>
    /// Determines which octant of this node's bounds contains the given position.
    /// </summary>
    [BurstCompile]
    public int FindOctant(float3 position) {
        int index = 0;
        if(position.x > Bounds.center.x)
            index |= 1;
        if(position.y > Bounds.center.y)
            index |= 2;
        if(position.z > Bounds.center.z)
            index |= 4;
        return index;
    }
}
