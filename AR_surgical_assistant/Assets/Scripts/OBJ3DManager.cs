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

    public GameObject OBJModel;
    
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
        OBJModel.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        OBJModel.transform.localPosition = new Vector3(0, 0, 5);

        Material newMat = Resources.Load("DefaultMeshMaterial", typeof(Material)) as Material;
        OBJModel.GetComponent<MeshRenderer>().material = newMat;

        OBJModel.AddComponent<ObjectManipulator>();
    }
}
