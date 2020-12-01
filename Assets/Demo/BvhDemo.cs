using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BvhDemo : MonoBehaviour {

    public int sphereNumber = 1000;
    public Vector2 positionRange = new Vector2 (-100f, 100f);
    public Vector2 scaleRange = new Vector2 (0.5f, 3f);

    public bool slowInit = true;
    public bool useBvh = true;

    GameObject sphereRoot;
    List<Renderer> rendererList;
    BoundingVolumeHeirachy boundingVolumeHeirachy;

    int initIndex = 0;
    Camera mainCamera;
    List<BoundingVolumeHeirachy.Node> hitBounds = new List<BoundingVolumeHeirachy.Node> ();

    void Start () {
        Random.InitState (System.DateTime.Now.Millisecond);

        sphereRoot = new GameObject ("Spheres");
        rendererList = new List<Renderer> ();
        for (int i = 0; i < sphereNumber; i++) {
            var sphere = GameObject.CreatePrimitive (PrimitiveType.Sphere);
            sphere.transform.SetParent (sphereRoot.transform);
            sphere.transform.localPosition = new Vector3 (
                Random.Range (positionRange.x, positionRange.y),
                Random.Range (positionRange.x, positionRange.y),
                Random.Range (positionRange.x, positionRange.y)
            );
            var scale = Random.Range (scaleRange.x, scaleRange.y);
            sphere.transform.localScale = new Vector3 (scale, scale, scale);

            rendererList.Add (sphere.GetComponent<Renderer> ());
        }

        boundingVolumeHeirachy = new BoundingVolumeHeirachy ();
        mainCamera = Camera.main;
    }

    void InitBoundingVolumeHeirachy () {
        if (slowInit) {
            var bounds = rendererList[initIndex].bounds;
            boundingVolumeHeirachy.InsertLeaf (initIndex, new BoundingVolume (bounds.min, bounds.max));
            initIndex += 1;
        } else {
            while (initIndex < rendererList.Count) {
                var bounds = rendererList[initIndex].bounds;
                boundingVolumeHeirachy.InsertLeaf (initIndex, new BoundingVolume (bounds.min, bounds.max));
                initIndex += 1;
            }
        }
    }

    void Update () {
        if (initIndex < rendererList.Count) {
            InitBoundingVolumeHeirachy ();
        } else {
            for (int i = 0; i < rendererList.Count; i++) {
                rendererList[i].SetPropertyBlock (null);
            }

            var ray = mainCamera.ScreenPointToRay (Input.mousePosition);
            var from = ray.origin;
            var to = ray.origin + ray.direction * 1000f;
            var direction = to - from;

            if (useBvh) {
                hitBounds.Clear ();
                boundingVolumeHeirachy.RayCast (from, to, RayCastRendererCallback);
                // for (int i = 0; i < hitResults.Count; i++) {
                //     var rendererIndex = hitResults[i].dataId;
                //     RayCastRenderer (from, direction, rendererIndex);
                // }
            } else {
                for (int i = 0; i < rendererList.Count; i++) {
                    RayCastRenderer (from, direction, i);
                }
            }
        }
    }

    void RayCastRendererCallback (Vector3 origin, Vector3 direction, BoundingVolumeHeirachy.Node node) {
        var renderer = rendererList[node.dataId];
        hitBounds.Add (node);
        RayCastRenderer (origin, direction, node.dataId);
    }

    void RayCastRenderer (Vector3 origin, Vector3 direction, int index) {
        var renderer = rendererList[index];
        if (RayCastSphere (origin, direction, renderer.transform.position, renderer.transform.lossyScale.x * 0.5f)) {
            var propertyBlock = new MaterialPropertyBlock ();
            propertyBlock.SetColor ("_Color", Color.red);
            renderer.SetPropertyBlock (propertyBlock);
        }
    }

    bool RayCastSphere (Vector3 origin, Vector3 direction, Vector3 center, float radius) {
        var oc = origin - center;
        var a = Vector3.Dot (direction, direction);
        var b = Vector3.Dot (direction, oc) * 2f;
        var c = Vector3.Dot (oc, oc) - radius * radius;
        var dis = b * b - 4f * a * c;
        if (dis < 0f) {
            return false;
        }

        dis = Mathf.Sqrt (dis);
        var t = (-b + dis) / (2f * a);
        return t >= 0f && t <= 1f; ;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected () {
        if (Application.isPlaying) {
            foreach (var node in boundingVolumeHeirachy.GetNodes ()) {
                var bounding = node.bounding;
                Gizmos.DrawWireCube (bounding.center, bounding.size);
            }

            var ray = mainCamera.ScreenPointToRay (Input.mousePosition);
            Gizmos.color = Color.red;
            Gizmos.DrawRay (ray.origin, ray.origin + ray.direction * 1000f);
            for (int i = 0; i < hitBounds.Count; i++) {
                var bounding = hitBounds[i].bounding;
                Gizmos.DrawWireCube (bounding.center, bounding.size);
            }
        }
    }
#endif
}
