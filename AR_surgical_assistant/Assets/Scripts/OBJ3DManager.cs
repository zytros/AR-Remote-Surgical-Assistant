using System.Collections;
using System.Collections.Generic;
using Dummiesman;
using System.IO;
using System.Text;
using MagicLeap;
using UnityEngine;
using MixedReality.Toolkit.SpatialManipulation;

public class OBJ3DManager : MonoBehaviour
{
    private string _currentOBJString = "";
    public Mesh manipMesh;
    public GameObject OBJModel;
    public GameObject refModel;
    public Material newMat;

    public void load3DModel(string objString)
    {
        _currentOBJString = objString;

        // Load 3D .obj model
        MemoryStream textStream = new MemoryStream(Encoding.UTF8.GetBytes(_currentOBJString));
        if (OBJModel != null)
        {
            Destroy(OBJModel);
        }

        GameObject loadModel = new OBJLoader().Load(textStream);

        
        
        OBJModel = loadModel.transform.Find("default").gameObject;

        //OBJModel = new OBJLoader().Load(textStream);
        loadModel.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        loadModel.transform.localPosition = new Vector3(0, 0, 2);
        

        //Material newMat = Resources.Load("DefaultMeshMaterial", typeof(Material)) as Material;
        //OBJModel.GetComponent<MeshRenderer>().material = newMat;

        //var meshfilter = loadModel.AddComponent<MeshFilter>();
        //meshfilter.mesh = OBJModel.GetComponent<MeshFilter>().mesh;
        OBJModel.GetComponent<MeshRenderer>().enabled = false;
        //loadModel.AddComponent<MeshRenderer>();
        //loadModel.AddComponent<MeshCollider>();
        //var obj_manip = loadModel.AddComponent<ObjectManipulator>();
        //loadModel.GetComponent<MeshRenderer>().material = newMat;

        //var mf = refModel.AddComponent<MeshFilter>();
        Mesh mesh = OBJModel.GetComponent<MeshFilter>().mesh;
        refModel.GetComponent<MeshFilter>().mesh = mesh;
        refModel.GetComponent<MeshRenderer>().material = newMat;
        refModel.GetComponent<MeshCollider>().sharedMesh = mesh;
    }
}
