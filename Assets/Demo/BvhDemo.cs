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
        boundingVolumeHeirachy.Build (rendererList);
    }

    void Update () {

    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected () {
        foreach (var node in boundingVolumeHeirachy.nodes) {
            Gizmos.DrawWireCube (node.bounding.center, node.bounding.size);
        }
    }
#endif
}
