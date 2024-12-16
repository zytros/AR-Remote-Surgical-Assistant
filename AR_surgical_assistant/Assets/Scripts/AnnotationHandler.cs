using System.Collections;
using System.Collections.Generic;
using MagicLeap;
using MagicLeap.OpenXR.Features.Reprojection;
using UnityEngine;
using UnityEngine.XR.OpenXR;

public class AnnotationHandler : Singleton<AnnotationHandler>
{
    public GameObject AnnotationPrefab;

    public List<GameObject> spawned_annotations = new List<GameObject>();

    //public MagicLeapReprojectionFeature.ReprojectionMode reprojectionMode = MagicLeapReprojectionFeature.ReprojectionMode.Depth;
    ////public Transform targetObject;  // Assign in the Inspector
    //private MagicLeapReprojectionFeature reprojectionFeature;
    //private List<Vector3> previousPositions = new List<Vector3>();


    // Start is called before the first frame update
    void Start()
    {
        //reprojectionFeature = OpenXRSettings.Instance.GetFeature<MagicLeapReprojectionFeature>();

        //if (reprojectionFeature == null || !reprojectionFeature.enabled)
        //{
        //    Debug.LogError("MagicLeapReprojectionFeature is not enabled!");
        //    enabled = false;
        //    return;
        //}

        //// Enable reprojeciton at start
        //reprojectionFeature.EnableReprojection = true;

        ////previousPosition = targetObject.position;
        //ApplyReprojectionMode();
    }

    private void ApplyReprojectionMode()
    {
        //reprojectionFeature.SetReprojectionMode(reprojectionMode);
        //Debug.Log($"Reprojection mode set to: {reprojectionMode}");
    }

    // Update is called once per frame
    void Update()
    {
        //// If you have additional info about where the user is looking, you can improve reprojeciton quality
        //// by setting the PlaneInfo regardless of the selected Reprojection Mode.
        //// When using PlanarManual, setting these values is required.
        //if (spawned_annotations.Count > 0)
        //{
        //    for (int i = 0; i < spawned_annotations.Count; i++)
        //    {
        //        Vector3 position = spawned_annotations[i].transform.position;
        //        Vector3 normal = GetNormal(spawned_annotations[i].transform);
        //        Vector3 velocity = (position - previousPositions[i]) / Time.deltaTime;

        //        reprojectionFeature.SetReprojectionPlaneInfo(position, normal, velocity);
        //        previousPositions[i] = position;
        //    }
        //}
    }

    private Vector3 GetNormal(Transform target)
    {
        // If the targetObject is a planar object (like a textured quad), its forward direction
        // can be used as the normal. For 3D shapes, however, the normal should point towards the user/camera.
        // The normal here is calculated to always face the main camera/user. 
        // In this example, we assume that objects will have the tag Planar if they are 2D.
        if (target.CompareTag("Planar"))
        {
            return target.forward;  // Use forward direction for planar objects
        }
        else
        {
            return (Camera.main.transform.position - target.position).normalized;  // Normal points to the user
        }
    }

    public void SpawnAnnotation(Vector3[] spawnLocations)
    {
        Debug.Log($"-- SpawnLocations: {spawnLocations.Length}");
        foreach (var obj in spawned_annotations)
        {
            Destroy(obj);
        }
        spawned_annotations.Clear();
        //previousPositions.Clear();

        foreach (var location in spawnLocations)
        {
            var spawned_obj = Instantiate(AnnotationPrefab);
            //var spawned_obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spawned_obj.transform.position = location;
            spawned_obj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            spawned_annotations.Add(spawned_obj);
            //previousPositions.Add(location);
        }
        Debug.Log("-- Done Spawning");
    }
}
