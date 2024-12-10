using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Buffers;
using System.Linq;
using System.Text;
using MagicLeap;
using MagicLeap.Android;
using MagicLeap.OpenXR.Features.PixelSensors;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.OpenXR;
using System.Net.Sockets;
using UnityEngine.XR.ARSubsystems;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Debug = UnityEngine.Debug;
using Unity.Mathematics;

public class DepthImage : Singleton<DepthImage>
{
    [SerializeField]
    public WebRTCController webrtccontroller;

    private MagicLeapPixelSensorFeature pixelSensorFeature;
    public PixelSensorId SensorId;
    uint i = 0;
    Vector3 position;
    Quaternion rotation;
    Stopwatch stopwatch = new Stopwatch();

    Matrix<double> K_depth = DenseMatrix.OfArray(new double[,] {
        {543.5,0,272},
        {0,543.5,240},
        {0,0,1}});
    Matrix<double> K_rgb = DenseMatrix.OfArray(new double[,] {
        {800,0,640},
        {0,800,360},
        {0,0,1}});


    void Awake()
    {
        pixelSensorFeature = OpenXRSettings.Instance.GetFeature<MagicLeapPixelSensorFeature>();
        if (pixelSensorFeature == null || !pixelSensorFeature.enabled)
        {
            enabled = false;
            Debug.LogError("PixelSensorFeature is either not enabled or is null. Check Project Settings in the Unity editor to make sure the feature is enabled");
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(CreateSensorAfterPermission());

        
    }

    // Update is called once per frame
    void Update()
    {
        //string abc = "abc";
        //webrtccontroller.AddDataToDataStream(Encoding.UTF8.GetBytes(abc));
        //return;
        // Debug.Log("__ in Update");
        position = Camera.main.transform.position;
        rotation = Camera.main.transform.rotation;
        Debug.Log($"__ position: {position}, rotation: {rotation}");
        uint ConfiguredStream = 0;
        if (!pixelSensorFeature.GetSensorData(SensorId, ConfiguredStream, out var frame, out var metaData, Allocator.Temp))
        {
            Debug.Log("depth__ GetSensorData failed");
            return;
        }
        

        if(frame.IsValid && frame.Planes.Length > 0)
        {
            ProcessFrame(in frame);
        }
    }
    static double[,] ConvertByteArrayToDoubleArray(byte[] byteArray)
    {
        if (byteArray.Length % 4 != 0)
        {
            throw new ArgumentException("Byte array length must be a multiple of 4.");
        }

        int floatCount = byteArray.Length / 4;
        double[] doubleArray = new double[floatCount];

        for (int i = 0; i < floatCount; i++)
        {
            float floatValue = BitConverter.ToSingle(byteArray, i * 4);
            doubleArray[i] = (double)floatValue;
        }
        double[,] array2d = new double[480, 544];
        for (int i = 0; i < 480; i++)
        {
            for (int j = 0; j < 544; j++)
            {
                array2d[i, j] = doubleArray[i * 544 + j];
            }
        }

        return array2d;
    }

    public void ProcessFrame(in PixelSensorFrame frame)
    {
        if (!frame.IsValid || frame.Planes.Length == 0)
        {
            Debug.Log("depth__ Frame is invalid or has no planes");
            return;
        }
        var frameType = frame.FrameType;            // should be Depth32
        ref var firstPlane = ref frame.Planes[0];
        switch (frameType)
        {
            case PixelSensorFrameType.Depth32:
                {
                    // depth image has size 544x480
                    // Debug.Log($"__ height: {firstPlane.Height} width: {firstPlane.Width} stride: {firstPlane.Stride} Pixel stride: {firstPlane.PixelStride} bytes per pixel:  {firstPlane.BytesPerPixel}\nframe type: {frameType}");
                    var byteArray = ArrayPool<byte>.Shared.Rent(firstPlane.ByteData.Length);
                    firstPlane.ByteData.CopyTo(byteArray);

                    int[][] parts = new int[4][]; // Array to hold the 4 parts
                    var partSize = 262144; // byteArray.Length / 4; // Size of each part
                    for (int i = 0; i < 4; i++)
                    {
                        parts[i] = new int[partSize];
                        Array.Copy(byteArray, i * partSize, parts[i], 0, partSize);
                    }

                    Debug.Log($"__ sending message with len: {byteArray.Length}");
                    webrtccontroller.AddDataToDataStream(byteArray[..262144]);
                    Debug.Log("__ sent message");
                    break;
            }
            

            default:
                Debug.Log("depth__ in ProcessFrame default");
                throw new ArgumentOutOfRangeException();
        }
    }

    
    public Vector3 projectPoint(int u, int v, double[,] depth, Matrix<double> K_rgb, Matrix<double> K_depth, Quaternion r, Vector3 t)
    {
        Debug.Log("___ projpoint1");
        double fx = K_depth[0,0];
        double fy = K_depth[1, 1];
        double cx = K_depth[0, 2];
        double cy = K_depth[1, 2];
        Debug.Log("___ projpoint2");
        Vector<double> x_rgb = Vector<double>.Build.DenseOfArray(new double[] {u,v,1.0});
        Debug.Log("___ projpoint3");
        Vector<double> x_depth = K_depth * K_rgb.Inverse() * x_rgb;
        Debug.Log("___ projpoint4");
        x_depth = x_depth / x_depth[2];
        Debug.Log("___ projpoint5");
        u = math.clamp((int)x_depth[0],0,543);
        v = math.clamp((int)x_depth[1],0,479);
        Debug.Log($"___ projpoint6 u: {u} v: {v}");
        double x = (u - cx) * depth[u,v] / fx;
        double y = (v - cy) * depth[u,v] / fy;
        Debug.Log("___ projpoint6.5");
        double z = depth[u,v];
        Debug.Log("___ projpoint7");

        Vector3 p = new Vector3((float)x, (float)y, (float)z);
        Debug.Log("___ projpoint8");
        return r * p + t;
    }

    public IEnumerator CreateSensorAfterPermission()
    {
        var availableSensors = pixelSensorFeature.GetSupportedSensors();
        
        foreach (var sensor in availableSensors)
        {
            Debug.Log($"depth__ Sensor: {sensor}");
            if (sensor.SensorName.Contains("Depth"))
            {
                SensorId = sensor;
            }
        }

        var sensor_created = false;
        var sensor_configured = false;
        var sensor_started = false;

        if (pixelSensorFeature.CreatePixelSensor(SensorId))
        {
            Debug.Log("depth__ Depth sensor created");
            sensor_created = true;
        }
        else
        {
            Debug.LogError("depth__ Unable to create depth sensor");
            sensor_created = false;
        }

        uint configuredStream = 0;
        if (sensor_created)
        {
            var configureResult = pixelSensorFeature.ConfigureSensorWithDefaultCapabilities(SensorId, configuredStream);
            yield return configureResult;
            if (!configureResult.DidOperationSucceed)
            {
                Debug.LogError("depth__ Unable to configure Depth Sensor");
                sensor_configured = false;
            }
            else
            {
                Debug.Log("depth__ Depth sensor configured");
                sensor_configured = true;
            }
        }

        if (sensor_configured)
        {
            var startSensorResult = pixelSensorFeature.StartSensor(SensorId, new[]
            {
                configuredStream
            });

            yield return startSensorResult;
            if (!startSensorResult.DidOperationSucceed)
            {
                Debug.LogError($"depth__ Unable to start Depth Sensor");
                sensor_started = false;
            }
            else
            {
                Debug.Log("depth__ Depth sensor started");
                sensor_started = true;
            }
        }
        
    }

}
