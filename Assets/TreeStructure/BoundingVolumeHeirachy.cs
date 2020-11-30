using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct BoundingVolume {
    public Vector3 min;
    public Vector3 max;

    public Vector3 center {
        get {
            return (min + max) * 0.5f;
        }
    }

    public Vector3 size {
        get {
            return (max - min);
        }
    }

    public BoundingVolume (Vector3 min, Vector3 max) {
        this.min = min;
        this.max = max;
    }

    public float GetPerimeter () {
        var size = max - min;
        return 2.0f * (size.z + size.y + size.z);
    }

    public static BoundingVolume Combine (BoundingVolume a, BoundingVolume b) {
        return new BoundingVolume (Vector3.Min (a.min, b.min), Vector3.Max (a.max, b.max));
    }

    public static BoundingVolume TowPoints (Vector3 p1, Vector3 p2) {
        return new BoundingVolume (Vector3.Min (p1, p2), Vector3.Max (p1, p2));
    }

    public static bool Intersects (BoundingVolume a, BoundingVolume b) {
        return a.min.x <= b.max.x && a.max.x >= b.min.x
          && a.min.y <= b.max.y && a.max.y >= b.min.y
          && a.min.z <= b.max.z && a.max.z >= b.min.z;
    }
}

public class BoundingVolumeHeirachy {

    public struct Node {
        public BoundingVolume bounding;
        public int dataId;
        public int parent;
        public int childA;
        public int childB;

        // 0+: mark the next free node
        // -1: no next free node
        public int nextFree;

        // -1: is free node 
        public int height;

        public bool isLeaf {
            get {
                return childA == -1;
            }
        }

        public bool isActive {
            get {
                return height > -1;
            }
        }
    }

    Node[] nodes = new Node[16];
    int rootNode = -1;
    int nextFreeNode = -1;
    int nodeCount = 0;

    public BoundingVolumeHeirachy () {
        for (int i = 0; i < 15; i++) {
            nodes[i].nextFree = i + 1;
            nodes[i].height = -1;
        }
        nodes[15].nextFree = -1;
        nodes[15].height = -1;
        nextFreeNode = 0;
    }

    public IEnumerable<Node> GetNodes () {
        for (int i = 0; i < nodes.Length; i++) {
            if (nodes[i].isActive) {
                yield return nodes[i];
            }
        }
    }

    public bool Intersects (Vector3 a, Vector3 b, List<Node> results) {
        var hit = false;
        var rayBounding = BoundingVolume.TowPoints (a, b);

        var stack = new Stack<int> ();
        stack.Push (rootNode);
        while (stack.Count > 0) {
            var index = stack.Pop ();
            if (!BoundingVolume.Intersects (rayBounding, nodes[index].bounding)) {
                continue;
            }

            if (nodes[index].isLeaf) {
                results.Add (nodes[index]);
                hit = true;
            } else {
                stack.Push (nodes[index].childA);
                stack.Push (nodes[index].childB);
            }
        }
        return hit;
    }

    int AllocateNode () {
        if (nextFreeNode == -1) {
            // expand the node pool
            var oldNodes = nodes;
            var oldLength = oldNodes.Length;
            nodes = new Node[oldLength * 2];
            oldNodes.CopyTo (nodes, 0);

            // set up free list
            for (int i = oldLength; i < nodes.Length - 1; i++) {
                nodes[i].nextFree = i + 1;
                nodes[i].height = -1;
            }
            nodes[nodes.Length - 1].nextFree = -1;
            nodes[nodes.Length - 1].height = -1;
            nextFreeNode = oldLength;
        }

        // take a free node
        int nodeId = nextFreeNode;
        nextFreeNode = nodes[nodeId].nextFree;

        nodes[nodeId].parent = -1;
        nodes[nodeId].childA = -1;
        nodes[nodeId].childB = -1;
        nodes[nodeId].nextFree = -1;
        nodes[nodeId].height = 0;
        nodeCount += 1;
        return nodeId;
    }

    void FreeNode (int nodeId) {
        nodes[nodeId].nextFree = nextFreeNode;
        nodes[nodeId].height = -1;
        nextFreeNode = nodeId;
        nodeCount -= 1;
    }

    public void InsertLeaf (int objectIndex, BoundingVolume bounding) {
        var leaf = AllocateNode ();
        nodes[leaf].bounding = bounding;
        nodes[leaf].dataId = objectIndex;

        if (rootNode == -1) {
            rootNode = leaf;
            return;
        }

        // Stage 1: find the best sibling for the new leaf
        var sibling = PickBestSibling (leaf);

        // Stage 2: create a new parent
        var oldParent = nodes[sibling].parent;
        var newParent = AllocateNode ();
        nodes[newParent].parent = oldParent;
        nodes[newParent].bounding = BoundingVolume.Combine (nodes[leaf].bounding, nodes[sibling].bounding);
        nodes[newParent].height = nodes[sibling].height + 1;

        if (oldParent != -1) {
            // The sibling was not the root.
            if (nodes[oldParent].childA == sibling) {
                nodes[oldParent].childA = newParent;
            } else {
                nodes[oldParent].childB = newParent;
            }

            nodes[newParent].childA = sibling;
            nodes[newParent].childB = leaf;
            nodes[sibling].parent = newParent;
            nodes[leaf].parent = newParent;
        } else {
            // The sibling was the root.
            nodes[newParent].childA = sibling;
            nodes[newParent].childB = leaf;
            nodes[sibling].parent = newParent;
            nodes[leaf].parent = newParent;
            rootNode = newParent;
        }

        // Stage 3: walk back up the tree refitting AABBs
        var index = nodes[leaf].parent;
        while (index != -1) {
            index = Balance (index);

            var childA = nodes[index].childA;
            var childB = nodes[index].childB;
            nodes[index].height = 1 + Mathf.Max (nodes[childA].height, nodes[childB].height);
            nodes[index].bounding = BoundingVolume.Combine (nodes[childA].bounding, nodes[childB].bounding);
            index = nodes[index].parent;
        }
    }

    int PickBestSibling (int leaf) {
        var sibling = rootNode;
        var leafBounding = nodes[leaf].bounding;
        while (nodes[sibling].isLeaf == false) {
            var childA = nodes[sibling].childA;
            var childB = nodes[sibling].childB;
            float area = nodes[sibling].bounding.GetPerimeter ();

            var combinedBounding = BoundingVolume.Combine (nodes[sibling].bounding, leafBounding);
            float combinedArea = combinedBounding.GetPerimeter ();

            // Cost of creating a new parent for this node and the new leaf
            float cost = 2.0f * combinedArea;

            // Minimum cost of pushing the leaf further down the tree
            float inheritanceCost = 2.0f * (combinedArea - area);

            // Cost of descending into childA
            float costA;
            if (nodes[childA].isLeaf) {
                costA = BoundingVolume.Combine (leafBounding, nodes[childA].bounding).GetPerimeter () + inheritanceCost;
            } else {
                float oldArea = nodes[childA].bounding.GetPerimeter ();
                float newArea = BoundingVolume.Combine (leafBounding, nodes[childA].bounding).GetPerimeter ();
                costA = (newArea - oldArea) + inheritanceCost;
            }

            // Cost of descending into child2
            float costB;
            if (nodes[childB].isLeaf) {
                costB = BoundingVolume.Combine (leafBounding, nodes[childB].bounding).GetPerimeter () + inheritanceCost;
            } else {
                float oldArea = nodes[childB].bounding.GetPerimeter ();
                float newArea = BoundingVolume.Combine (leafBounding, nodes[childB].bounding).GetPerimeter ();
                costB = (newArea - oldArea) + inheritanceCost;
            }

            // Descend according to the minimum cost.
            if (cost < costA && cost < costB) {
                break;
            }

            // Descend
            if (costA < costB) {
                sibling = childA;
            } else {
                sibling = childB;
            }
        }
        return sibling;
    }

    int Balance (int index) {
        // TODO
        return index;
    }
}