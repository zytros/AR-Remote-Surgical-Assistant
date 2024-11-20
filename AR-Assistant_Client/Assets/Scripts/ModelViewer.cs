using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ModelViewer : MonoBehaviour, IDragHandler
{
    public GameObject ModelOBJ;

    public void OnDrag(PointerEventData eventData)
    {
        if (ModelOBJ != null)
        {
            ModelOBJ.transform.eulerAngles += new Vector3(-eventData.delta.y, -eventData.delta.x);
        }
    }

    public void ResetRotation()
    {
        if (ModelOBJ != null)
        {
            ModelOBJ.transform.localRotation = Quaternion.identity;
        }
    }
}
