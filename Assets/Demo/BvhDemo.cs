using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BvhDemo : MonoBehaviour {

    public int sphereNumber = 1000;
    public Vector2 positionRange = new Vector2 (-100f, 100f);
    public Vector2 scaleRange = new Vector2 (0.5f, 3f);

    GameObject sphereRoot;
    List<Renderer> rendererList;
    BoundingVolumeHeirachy boundingVolumeHeirachy;

    int initIndex = 0;
    Camera mainCamera;
    List<BoundingVolumeHeirachy.Node> hitResults = new List<BoundingVolumeHeirachy.Node> ();

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

    void Update () {
        if (initIndex < rendererList.Count) {
            var bounds = rendererList[initIndex].bounds;
            boundingVolumeHeirachy.InsertLeaf (initIndex, new BoundingVolume (bounds.min, bounds.max));
            initIndex += 1;
        } else {
            for (int i = 0; i < rendererList.Count; i++) {
                rendererList[i].SetPropertyBlock (null);
            }

            var ray = mainCamera.ScreenPointToRay (Input.mousePosition);
            hitResults.Clear ();
            boundingVolumeHeirachy.Intersects (ray.origin, ray.origin + ray.direction * 1000f, hitResults);
            for (int i = 0; i < hitResults.Count; i++) {
                var rendererIndex = hitResults[i].dataId;
                var renderer = rendererList[rendererIndex];
                var propertyBlock = new MaterialPropertyBlock ();
                propertyBlock.SetColor ("_Color", Color.red);
                renderer.SetPropertyBlock (propertyBlock);
            }
        }
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
            for (int i = 0; i < hitResults.Count; i++) {
                var bounding = hitResults[i].bounding;
                Gizmos.DrawWireCube (bounding.center, bounding.size);
            }
        }
    }
#endif
}
