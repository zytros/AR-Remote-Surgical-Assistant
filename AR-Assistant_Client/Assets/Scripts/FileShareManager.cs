using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using SFB;
using UnityEngine.Networking;
using TMPro;
using Dummiesman;

public class FileShareManager : MonoBehaviour
{
    public GameObject FileDropdown;
    public RawImage LoadedImageRenderer;
    public ModelViewer ModelViewer;
    public GameObject OBJModel;
    public Camera ModelViewerCamera;

    public GameObject[] ViewerTabs;

    private Dictionary<string, string> FileLookupDictionary = new Dictionary<string, string>();

    public void SwitchViewer(int ViewerTabID)
    {
        foreach (GameObject go in ViewerTabs)
        {
            go.SetActive(false);
        }
        ViewerTabs[ViewerTabID].SetActive(true);
    }

    public void OnClickOpenNewFile()
    {
        // Open file with filter
        var extensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg" ),
            new ExtensionFilter("3D Model Files", "obj"),
            new ExtensionFilter("All Files", "*" ),
        };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);

        if (paths.Length > 0)
        {
            var uri = new System.Uri(paths[0]);
            string url = uri.AbsoluteUri;
            string extension = Path.GetExtension(url);

            if (extension == ".jpg" || extension == ".jpeg" || extension == ".png")
            {
                // Add option to dropdown 
                AddOptionToDropdown(url);
                StartCoroutine(LoadImage(url));
            }
            else if (extension == ".obj")
            {
                // Add option to dropdown 
                AddOptionToDropdown(url);
                StartCoroutine(Load3DObj(url));
            }
            else
            {
                Debug.LogError("Unhandled file type");
            }
            //StartCoroutine(OutputRoutineOpen(uri));
        }
    }

    public void OnDropdownValueChange()
    {
        TMP_Dropdown dropdown = FileDropdown.GetComponent<TMP_Dropdown>();

        string dd_current_option = dropdown.options[dropdown.value].text;

        if (FileLookupDictionary.TryGetValue(dd_current_option, out string url))
        {
            string extension = Path.GetExtension(url);

            if (extension == ".jpg" || extension == ".jpeg" || extension == ".png")
            {
                StartCoroutine(LoadImage(url));
            }
            else if (extension == ".obj")
            {
                StartCoroutine(Load3DObj(url));
            }
        }
        else
        {
            Debug.Log("File not loaded");
        }
    }

    private void AddOptionToDropdown(string url)
    {
        System.Uri uri = new System.Uri(url);
        if (uri.IsFile)
        {
            // Add file to dropdown options
            string filename = Path.GetFileName(uri.LocalPath);
            var dropdown = FileDropdown.GetComponent<TMP_Dropdown>();
            TMP_Dropdown.OptionData dd_optiondata = new TMP_Dropdown.OptionData();
            dd_optiondata.text = filename;
            // If file hasn't already been loaded, add new element to dropdown
            if (dropdown.options.FindIndex(option => option.text == filename) == -1)
            {
                FileLookupDictionary.Add(filename, url);
                // Add value to dropdown
                dropdown.options.Add(dd_optiondata);
                // Set new value to active dropdown element
                dropdown.value = dropdown.options.FindIndex(option => option.text == filename);
            }
        }
    }

    private IEnumerator LoadImage(string url)
    {
        UnityWebRequest imagerequest = UnityWebRequestTexture.GetTexture(url);
        yield return imagerequest.SendWebRequest();
        if (imagerequest.isNetworkError)
        {
            Debug.Log("imagerequest Error: " + imagerequest.error);
        }
        else
        {
            // Set Loaded Image Renderer texture
            var tex = DownloadHandlerTexture.GetContent(imagerequest);
            LoadedImageRenderer.texture = tex;
            SwitchViewer(0);
        }
    }

    private IEnumerator Load3DObj(string url)
    {
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            // Failed opening file
            Debug.Log("WWW Error: " + www.error);
        }
        else
        {
            Debug.Log(www.downloadHandler.text);

            // Load 3D .obj model
            MemoryStream textStream = new MemoryStream(Encoding.UTF8.GetBytes(www.downloadHandler.text));
            if (OBJModel != null)
            {
                Destroy(OBJModel);
            }

            GameObject loadModel = new OBJLoader().Load(textStream);
            OBJModel = loadModel.transform.Find("default").gameObject;

            //OBJModel = new OBJLoader().Load(textStream);
            OBJModel.transform.localScale = new Vector3(1, 1, 1);
            FitOnScreen();
            OBJModel.transform.localPosition = new Vector3(0, 0, 10);

            Material newMat = Resources.Load("DefaultMeshMaterial", typeof(Material)) as Material;
            OBJModel.GetComponent<MeshRenderer>().material = newMat;

            ModelViewer.ModelOBJ = OBJModel;

            SwitchViewer(1);
        }
    }

    private Bounds GetBound(GameObject gameObj)
    {
        Bounds bound = new Bounds(gameObj.transform.position, Vector3.zero);
        var rList = gameObj.GetComponentsInChildren(typeof(MeshRenderer));
        foreach (MeshRenderer r in rList)
        {
            bound.Encapsulate(r.bounds);
        }
        return bound;
    }

    public void FitOnScreen()
    {
        Bounds bound = GetBound(OBJModel);
        Vector3 boundSize = bound.size;
        float diagonal = Mathf.Sqrt((boundSize.x * boundSize.x) + (boundSize.y * boundSize.y) + (boundSize.z * boundSize.z));
        ModelViewerCamera.orthographicSize = diagonal / 2.0f;
        ModelViewerCamera.transform.position = bound.center;
    }

}
