// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2024) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%
using MagicLeap.Android;
using MagicLeap.Examples;
using System;
using System.Net.Sockets;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.OpenXR;
using MagicLeap.OpenXR.Features.Meshing;
using Random = UnityEngine.Random;
using Utils = MagicLeap.Examples.Utils;
using Unity.Collections;
using System.Collections.Generic;

public class MeshingExample : MonoBehaviour
{
    [Serializable]
    private class MeshQuerySetting
    {
        [SerializeField] public MeshingQuerySettings meshQuerySettings;
        [SerializeField] public float meshDensity;
        [SerializeField] public Vector3 meshBoundsOrigin;
        [SerializeField] public Vector3 meshBoundsRotation;
        [SerializeField] public Vector3 meshBoundsScale;
        [SerializeField] public MeshingMode renderMode;
        [SerializeField] public MeshFilter meshPrefab;
    }

    private class NetworkClient
    {
        string ipAddress = "10.5.34.225";
        int port = 8030;

        public void SendMessageToServer(string msg)
        {
            try
            {
                using (TcpClient client = new TcpClient(ipAddress, port))
                {
                    NetworkStream stream = client.GetStream();

                    if (stream.CanWrite)
                    {
                        byte[] messageBytes = Encoding.UTF8.GetBytes(msg);

                        // First, send the length of the message (as a 4-byte integer)
                        int messageLength = messageBytes.Length;
                        byte[] lengthBytes = BitConverter.GetBytes(messageLength);

                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(lengthBytes); // Ensure it's sent as big-endian
                        }

                        // Send the length of the message
                        stream.Write(lengthBytes, 0, lengthBytes.Length);

                        // Send the actual message
                        stream.Write(messageBytes, 0, messageBytes.Length);
                        stream.Flush();

                        Debug.Log($"Sent message of length: {messageLength}");
                    }

                    // Optionally, read the response
                    if (stream.CanRead)
                    {
                        byte[] responseBytes = new byte[1024]; // Buffer for reading the response
                        int bytesRead = stream.Read(responseBytes, 0, responseBytes.Length);
                        string response = Encoding.UTF8.GetString(responseBytes, 0, bytesRead);
                        Debug.Log($"Received response: {response}");
                    }

                    // Explicitly close the stream and client
                    stream.Close();
                    client.Close();
                }
            }
            catch (SocketException e)
            {
                Debug.Log($"SocketException: {e.Message}");
            }
            catch (Exception e)
            {
                Debug.Log($"Exception: {e.Message}");
            }
        }
    }



    [SerializeField] private ARMeshManager meshManager;
    [SerializeField] private ARPointCloudManager pointCloudManager;
    [SerializeField] private MeshingProjectile projectilePrefab;
    private Camera mainCamera;

    private const float ProjectileLifetime = 5f;
    private const float ProjectileForce = 300f;
    private const float MinScale = 0.1f;
    private const float MaxScale = 0.3f;
    private MagicLeapMeshingFeature meshingFeature;
    private int currentIndex;

    [SerializeField] private MeshQuerySetting[] allSettings;

    [SerializeField] private Text updateText;

    private StringBuilder statusText = new();
    private MeshDetectorFlags[] allFlags;
    private ObjectPool<MeshingProjectile> projectilePool;
    private MeshingMode previousRenderMode;
    private MeshTexturedWireframeAdapter wireframeAdapter;

    private MagicLeapController Controller => MagicLeapController.Instance;

    private NetworkClient networkClient = new NetworkClient();

    private void Awake()
    {
        allFlags = (MeshDetectorFlags[])Enum.GetValues(typeof(MeshDetectorFlags));
    }

    IEnumerator Start()
    {
        mainCamera = Camera.main;
        meshManager.enabled = false;
        pointCloudManager.enabled = false;
        yield return new WaitUntil(Utils.AreSubsystemsLoaded<XRMeshSubsystem>);
        meshingFeature = OpenXRSettings.Instance.GetFeature<MagicLeapMeshingFeature>();
        wireframeAdapter = GetComponent<MeshTexturedWireframeAdapter>();
        if (!meshingFeature.enabled)
        {
            Debug.LogError($"{nameof(MagicLeapMeshingFeature)} was not enabled. Disabling script");
            enabled = false;
        }

        projectilePool = new ObjectPool<MeshingProjectile>(() => Instantiate(projectilePrefab), (meshProjectile) =>
        {
            meshProjectile.gameObject.SetActive(true);
        }, (meshProjectile) => meshProjectile.gameObject.SetActive(false), defaultCapacity: 20);
        Controller.TriggerPressed += FireProjectile;
        Controller.BumperPressed += CycleSettings;
        Permissions.RequestPermission(Permissions.SpatialMapping, OnPermissionGranted, OnPermissionDenied);
    }

    private void Update()
    {
        ref var meshSettings = ref allSettings[currentIndex];
        ref var activeSettings = ref meshSettings.meshQuerySettings;
        //Show the status text
        statusText.Clear();
        statusText.AppendLine("Current Settings:");
        statusText.AppendLine($"Bounding Box Origin: {meshSettings.meshBoundsOrigin}");
        statusText.AppendLine($"Bounding Box Scale: {meshSettings.meshBoundsScale}");
        statusText.AppendLine($"Bounding Box Rotation: {meshSettings.meshBoundsRotation}");
        statusText.AppendLine($"Fill Hole Length: {activeSettings.fillHoleLength}");
        statusText.AppendLine($"Disconnected Areas Length: {activeSettings.appliedDisconnectedComponentArea}");
        statusText.AppendLine($"Using Ion Allocator: {activeSettings.useIonAllocator}");
        statusText.AppendLine($"Mesh Density: {meshSettings.meshDensity}");
        statusText.Append($"Render Mode: {meshSettings.renderMode}");
        statusText.AppendLine(" Flags:");
        foreach (var flag in allFlags)
        {
            statusText.AppendLine($"{flag} : {activeSettings.meshDetectorFlags.HasFlag(flag)}");
        }

        statusText.AppendLine($"Mesh Density: {meshManager.density}");
        updateText.text = statusText.ToString();
    }

    private void OnDestroy()
    {
        Controller.TriggerPressed -= FireProjectile;
        Controller.BumperPressed -= CycleSettings;
    }

    private void CycleSettings(InputAction.CallbackContext obj)
    {
        currentIndex = (currentIndex + 1) % allSettings.Length;
        UpdateSettings();
    }

    private void FireProjectile(InputAction.CallbackContext obj)
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        var projectile = projectilePool.Get();
        projectile.Initialize(projectilePool, ProjectileLifetime);
        projectile.transform.position = mainCamera.transform.position;
        projectile.transform.localScale = Vector3.one * Random.Range(MinScale, MaxScale);
        projectile.rb.AddForce(mainCamera.transform.forward * ProjectileForce);
        ExtractPointCloud();
    }

    void UpdateSettings()
    {
        ref var meshSettings = ref allSettings[currentIndex];
        var currentRenderMode = meshSettings.renderMode;
        meshManager.transform.localScale = meshSettings.meshBoundsScale;
        meshManager.transform.rotation = Quaternion.Euler(meshSettings.meshBoundsRotation);
        meshManager.transform.localPosition = meshSettings.meshBoundsOrigin;
        if (currentRenderMode == MeshingMode.Triangles)
        {
            meshManager.density = meshSettings.meshDensity;
            meshManager.meshPrefab = meshSettings.meshPrefab;
            if (wireframeAdapter != null)
            {
                wireframeAdapter.ComputeConfidences = meshSettings.meshQuerySettings.meshDetectorFlags.HasFlag(MeshDetectorFlags.ComputeConfidence);
                wireframeAdapter.ComputeNormals = meshSettings.meshQuerySettings.meshDetectorFlags.HasFlag(MeshDetectorFlags.ComputeNormals);
                wireframeAdapter.enabled = currentIndex == 0;
            }
        }
        else
        {
            meshingFeature.MeshDensity = meshSettings.meshDensity;
            meshingFeature.MeshBoundsOrigin = meshSettings.meshBoundsOrigin;
            meshingFeature.MeshBoundsRotation = Quaternion.Euler(meshSettings.meshBoundsRotation);
            meshingFeature.MeshBoundsScale = meshSettings.meshBoundsScale;
        }
        meshingFeature.UpdateMeshQuerySettings(in meshSettings.meshQuerySettings);
        meshingFeature.InvalidateMeshes();
        if (previousRenderMode == currentRenderMode)
        {
            return;
        }
        meshManager.DestroyAllMeshes();
        meshManager.enabled = false;
        pointCloudManager.SetTrackablesActive(false);
        pointCloudManager.enabled = false;
        meshingFeature.MeshRenderMode = currentRenderMode;
        var isPointCloud = currentRenderMode == MeshingMode.PointCloud;
        switch (isPointCloud)
        {
            case true:
                meshManager.enabled = false;
                pointCloudManager.enabled = true;
                pointCloudManager.SetTrackablesActive(true);
                break;
            case false:
                pointCloudManager.SetTrackablesActive(false);
                pointCloudManager.enabled = false;
                meshManager.enabled = true;
                break;
        }
        previousRenderMode = currentRenderMode;
    }

    private void OnPermissionGranted(string permission)
    {
        meshManager.enabled = true;
        previousRenderMode = MeshingMode.Triangles;
        UpdateSettings();
    }

    private void OnPermissionDenied(string permission)
    {
        Debug.LogError($"Failed to create Meshing Subsystem due to missing or denied {permission} permission. Please add to manifest. Disabling script.");
        enabled = false;
    }

    
    private void ExtractPointCloud()
    {
        var sb = new StringBuilder();
        int numPoints = 0;
        var trackableCollection = pointCloudManager.trackables;
        foreach (var pointCloud in trackableCollection)
        {
            // Collect the points in the point cloud
            if (!pointCloud.positions.HasValue)
                continue;

            var points = pointCloud.positions.Value;
            var xform = pointCloud.transform;
            

            foreach(var point in points)
            {
                sb.AppendLine($"point {point.x} {point.y} {point.z}");
            }
            var transform = pointCloud.gameObject.transform;
            var local2WorldMat = transform.localToWorldMatrix;
            sb.AppendLine($"transform_mat {local2WorldMat.m00} {local2WorldMat.m01} {local2WorldMat.m02} {local2WorldMat.m03}\n" +
                                          $"{local2WorldMat.m10} {local2WorldMat.m11} {local2WorldMat.m12} {local2WorldMat.m13}\n" +
                                          $"{local2WorldMat.m20} {local2WorldMat.m21} {local2WorldMat.m22} {local2WorldMat.m23}\n" +
                                          $"{local2WorldMat.m30} {local2WorldMat.m31} {local2WorldMat.m32} {local2WorldMat.m33}"
                                          );
            numPoints += points.Length;
            
        }
        
        // Send the data to the server
        String msg = sb.ToString();
        networkClient.SendMessageToServer($"sending {msg.Length} bytes");
        networkClient.SendMessageToServer(sb.ToString());
        Debug.Log("Debug: Extracting " + numPoints + " total points");
    }
    private void ExtractMeshes()
    {

        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        

        int meshNr = 0;
        var sb = new StringBuilder();
        Debug.Log($"Debug: Extracting {meshManager.meshes.Count} meshes");
        foreach (var meshFilter in meshManager.meshes)
        {
            Mesh mesh = meshFilter.sharedMesh; // Access the actual mesh data
            stopwatch.Start();
            SaveMeshAsObj(mesh, "ExtractedMesh_" + meshNr, sb);
            stopwatch.Stop();
            Debug.Log($"Debug: Function execution time: {stopwatch.ElapsedMilliseconds} milliseconds");
            stopwatch.Reset();
            meshNr++;
        }
        networkClient.SendMessageToServer(sb.ToString());

        
    }

    private void SaveMeshAsObj(Mesh mesh, string fileName, StringBuilder sb)
    {

        sb.AppendLine("o " + fileName);

        // Write vertices
        Debug.Log("Vertices: " + mesh.vertexCount);
        foreach (Vector3 vertex in mesh.vertices)
        {
            sb.AppendLine($"v {vertex.x} {vertex.y} {vertex.z}");
        }

        // Write normals
        foreach (Vector3 normal in mesh.normals)
        {
            sb.AppendLine($"vn {normal.x} {normal.y} {normal.z}");
        }

        // Write UVs (if available)
        foreach (Vector2 uv in mesh.uv)
        {
            sb.AppendLine($"vt {uv.x} {uv.y}");
        }

        // Write faces
        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            sb.AppendLine($"f {mesh.triangles[i] + 1}/{mesh.triangles[i] + 1}/{mesh.triangles[i] + 1} " +
                          $"{mesh.triangles[i + 1] + 1}/{mesh.triangles[i + 1] + 1}/{mesh.triangles[i + 1] + 1} " +
                          $"{mesh.triangles[i + 2] + 1}/{mesh.triangles[i + 2] + 1}/{mesh.triangles[i + 2] + 1}");
        }
       
        
    }


}
