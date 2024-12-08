using System.Collections.Generic;
using MagicLeap.Android;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.OpenXR;
using MagicLeap.OpenXR.Features.PixelSensors;

public class PermissionManager : MonoBehaviour
{
    [SerializeField]
    public DepthImage depthSensorManager;

    [SerializeField]
    private List<string> _requiredPermissions = new List<string> { Permission.Microphone, Permission.Camera, MagicLeap.Android.Permissions.DepthCamera };

    public bool PermissionsGranted => Permissions.CheckPermission(Permission.Microphone)
                                      && Permissions.CheckPermission(Permission.Camera)
                                      && Permissions.CheckPermission(MagicLeap.Android.Permissions.DepthCamera);

    private void OnValidate()
    {
        // Ensure that the required permissions list contains Microphone and Camera permissions
        var required = new List<string> { Permission.Microphone, Permission.Camera, MagicLeap.Android.Permissions.DepthCamera };
        foreach (var permission in required)
        {
            if (!_requiredPermissions.Contains(permission))
            {
                Debug.LogError($"Permission {permission} is required. Adding it to the list.");
                _requiredPermissions.Add(permission);
            }
        }
    }

    // Start is called before the first frame update
    void Awake()
    {
        Debug.Log("PermissionManager Awake");
        Debug.Log($"required premissions are: \n{_requiredPermissions}");
        foreach( var per_string in _requiredPermissions)
        {
            Debug.Log($"permission__: {per_string}");
        }
        RequestPermission();
    }

    public void RequestPermission()
    {
        Debug.Log($"__ {PermissionsGranted}");
        if (!PermissionsGranted)
            Permissions.RequestPermissions(_requiredPermissions.ToArray(), OnPermissionGranted, OnPermissionDenied, OnPermissionDenied);
    }

    // Update is called once per frame
    void OnPermissionGranted(string permission)
    {
        Debug.Log($"{permission} granted. hello");
        if (permission.Equals(MagicLeap.Android.Permissions.DepthCamera))
        {
            Debug.Log("Creating depth sensor");
            // Create the depth sensor after the camera permission is granted
            StartCoroutine(depthSensorManager.CreateSensorAfterPermission());
        }

    }
    void OnPermissionDenied(string permission)
    {
        Debug.LogError($"{permission} denied, example won't function.");
    }
}
