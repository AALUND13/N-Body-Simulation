using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

public class OctreeTestRunner : MonoBehaviour {
    // Custom comparer for float3 values using an epsilon.
    private bool Float3Equal(float3 a, float3 b, float epsilon = 1e-5f) {
        return (math.abs(a.x - b.x) < epsilon) &&
               (math.abs(a.y - b.y) < epsilon) &&
               (math.abs(a.z - b.z) < epsilon);
    }

    void Start() {
        try {
            TestNewContaining();
            TestInsertSingleBody();
            TestInsertTwoBodiesAndPropagate();
            TestFindQuadrant();
            //TestAccCalculation();

            Debug.Log("All Octree tests passed!");
        } catch(Exception ex) {
            Debug.LogError("Octree test failed: " + ex.Message);
        }
    }

    // Test that NewContaining returns a Bounds that encloses all bodies.
    void TestNewContaining() {
        // Create an array of bodies.
        var bodies = new NativeArray<BodyData>(3, Allocator.Temp);
        bodies[0] = new BodyData { Position = new float3(0, 0, 0), Mass = 1f };
        bodies[1] = new BodyData { Position = new float3(10, 10, 10), Mass = 2f };
        bodies[2] = new BodyData { Position = new float3(-10, -10, -10), Mass = 3f };

        // Use your Octree.NewContaining method.
        Octree.NewContaining(bodies, out Bounds bounds);

        // Expected center and size.
        Vector3 expectedCenter = new Vector3(0, 0, 0);
        Vector3 expectedSize = new Vector3(20, 20, 20);

        if(bounds.center != expectedCenter)
            throw new Exception($"TestNewContaining: Expected center {expectedCenter}, got {bounds.center}");
        if(bounds.size != expectedSize)
            throw new Exception($"TestNewContaining: Expected size {expectedSize}, got {bounds.size}");

        bodies.Dispose();
    }

    // Test inserting a single body into an otherwise empty octree.
    void TestInsertSingleBody() {
        Octree tree = new Octree(Allocator.Temp);

        // Create a root node with a bounding box.
        Bounds bounds = new Bounds(Vector3.zero, new Vector3(100, 100, 100));
        tree.Clear(bounds);

        BodyData body = new BodyData { Position = new float3(10, 10, 10), Mass = 5f };
        tree.Insert(body);

        OctreeNode root = tree.Nodes[0];
        if(Mathf.Abs(root.Mass - 5f) > 1e-5f)
            throw new Exception("TestInsertSingleBody: Root node mass should be 5");
        if(!Float3Equal(root.Position, body.Position))
            throw new Exception("TestInsertSingleBody: Root node position should match inserted body");

        tree.Dispose();
    }

    // Test inserting two bodies and propagating the aggregated mass and center-of-mass.
    void TestInsertTwoBodiesAndPropagate() {
        Octree tree = new Octree(Allocator.Temp);
        Bounds bounds = new Bounds(Vector3.zero, new Vector3(100, 100, 100));
        tree.Clear(bounds);

        BodyData body1 = new BodyData { Position = new float3(10, 10, 10), Mass = 5f };
        BodyData body2 = new BodyData { Position = new float3(-10, -10, -10), Mass = 3f };

        tree.Insert(body1);
        tree.Insert(body2);

        tree.Propagate();

        OctreeNode root = tree.Nodes[0];
        float expectedMass = 5f + 3f;
        if(Mathf.Abs(root.Mass - expectedMass) > 1e-5f)
            throw new Exception($"TestInsertTwoBodiesAndPropagate: Expected aggregated mass {expectedMass}, got {root.Mass}");

        float3 expectedCOM = (body1.Position * body1.Mass + body2.Position * body2.Mass) / expectedMass;
        if(!Float3Equal(root.Position, expectedCOM))
            throw new Exception($"TestInsertTwoBodiesAndPropagate: Expected COM {expectedCOM}, got {root.Position}");
        tree.Dispose();
    }

    // Test the FindQuadrant function of an OctreeNode.
    void TestFindQuadrant() {
        Bounds bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(10, 10, 10));
        OctreeNode node = new OctreeNode(0, bounds);

        // In a standard octree, for a node centered at (0,0,0) and with extents 5,
        // a point (1,1,1) should fall in the quadrant with index 7 (binary 111).
        int quadrant = node.FindOctant(new float3(1, 1, 1));
        if(quadrant != 7)
            throw new Exception($"TestFindQuadrant: Expected quadrant 7 for (1,1,1), got {quadrant}");

        // Similarly, (-1,-1,-1) should be in quadrant 0.
        quadrant = node.FindOctant(new float3(-1, -1, -1));
        if(quadrant != 0)
            throw new Exception($"TestFindQuadrant: Expected quadrant 0 for (-1,-1,-1), got {quadrant}");
    }

    //// Test the Acc method (acceleration computation) of the octree.
    //void TestAccCalculation() {
    //    Octree tree = new Octree(Allocator.Temp);
    //    Bounds bounds = new Bounds(Vector3.zero, new Vector3(100, 100, 100));
    //    tree.Nodes.Add(new OctreeNode(bounds, Allocator.Temp, false));

    //    // Insert a body at position (20,0,0) with mass 10.
    //    BodyData body = new BodyData { Position = new float3(20, 0, 0), Mass = 10f };
    //    tree.Insert(body);
    //    tree.Propagate();

    //    Vector2 acc = tree.Acc(new Vector2(0, 0));

    //    // With a single body at (20,0,0), acceleration should be in the positive x direction.
    //    if(acc.x <= 0f)
    //        throw new Exception("TestAccCalculation: Acceleration.x should be positive");
    //    if(Mathf.Abs(acc.y) > 1e-5f)
    //        throw new Exception("TestAccCalculation: Acceleration.y should be near zero");

    //    tree.Dispose();
    //}
}
