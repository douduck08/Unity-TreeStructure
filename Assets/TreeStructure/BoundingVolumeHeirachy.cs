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

    public static BoundingVolume Union (BoundingVolume a, BoundingVolume b) {
        return new BoundingVolume (Vector3.Min (a.min, b.min), Vector3.Max (a.max, b.max));
    }
}

public class BoundingVolumeHeirachy {

    public class HeirachyNode {
        public BoundingVolume bounding;
        public int parent = -1;
        public int childA = -1;
        public int childB = -1;
        public int dataId;

        public HeirachyNode (BoundingVolume bounding, int dataId) {
            this.bounding = bounding;
            this.dataId = dataId;
        }
    }

    List<Renderer> dataList;
    List<HeirachyNode> heirachyNodes = new List<HeirachyNode> ();

    public IEnumerable<HeirachyNode> nodes {
        get {
            return heirachyNodes;
        }
    }

    public void Build (List<Renderer> objectList) {
        dataList = objectList;
        heirachyNodes.Clear ();

        var queuedNodes = new List<HeirachyNode> ();
        for (int i = 0; i < dataList.Count; i++) {
            var bounds = dataList[i].bounds;
            queuedNodes.Add (new HeirachyNode (new BoundingVolume (bounds.min, bounds.max), i));
        }

        var depth = 0;
        while (queuedNodes.Count > 0 && depth < 10) {
            depth += 1;

            var sortedAxis = Random.Range (0, 3);
            var sortedNodes = queuedNodes.OrderBy (node => node.bounding.center[sortedAxis]).ToList ();
            var idBase = heirachyNodes.Count;
            heirachyNodes.AddRange (sortedNodes);

            if (queuedNodes.Count <= 1) {
                break;
            }

            queuedNodes.Clear ();
            for (int i = 0; i < sortedNodes.Count; i += 2) {
                if (i + 1 == sortedNodes.Count) {
                    // the length is odd, and this is last one.
                    queuedNodes.Add (sortedNodes[i]);
                } else {
                    var nodeA = sortedNodes[i];
                    var nodeB = sortedNodes[i + 1];
                    var parent = new HeirachyNode (BoundingVolume.Union (nodeA.bounding, nodeB.bounding), -1);

                    parent.childA = idBase + i;
                    parent.childB = idBase + i + 1;
                    nodeA.parent = 0; // TODO
                    nodeB.parent = 0;

                    queuedNodes.Add (parent);
                }
            }
        }
    }
}