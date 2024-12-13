using System.Collections;
using System.Collections.Generic;
using MagicLeap;
using UnityEngine;

public class AnnotationHandler : Singleton<AnnotationHandler>
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnAnnotation(Vector3[] spawnLocations)
    {
        Debug.Log($"-- SpawnLocations: {spawnLocations.Length}");
        foreach (var location in spawnLocations)
        {
            var spawned_obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spawned_obj.transform.position = location;
            spawned_obj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        }
        Debug.Log("-- Done Spawning");
    }
}
