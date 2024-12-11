using System;
using System.Collections;
using System.Collections.Generic;
using System.Buffers;
using System.Linq;
using System.Text;
using System.Diagnostics;
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
//using MathNet.Numerics.LinearAlgebra;
//using MathNet.Numerics.LinearAlgebra.Double;
using Debug = UnityEngine.Debug;
using Unity.Mathematics;
using System.Net.Http;
using System.Threading.Tasks;

public struct MetaData
{
    public Vector3 position;
    public Quaternion rotation;
};

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
    Vector4 K_depth = new Vector4(543.5f, 543.5f, 272f, 240f);
    Vector4 K_rgb = new Vector4(800f, 800f, 640f, 360f);

    /*
    Matrix<double> K_depth = DenseMatrix.OfArray(new double[,] {
        {543.5,0,272},
        {0,543.5,240},
        {0,0,1}});
    Matrix<double> K_rgb = DenseMatrix.OfArray(new double[,] {
        {800,0,640},
        {0,800,360},
        {0,0,1}});
    */

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
        stopwatch.Start();
    }

    // Update is called once per frame
    void Update()
    {
        position = Camera.main.transform.position;
        rotation = Camera.main.transform.rotation;
        uint ConfiguredStream = 0;
        if (!pixelSensorFeature.GetSensorData(SensorId, ConfiguredStream, out var frame, out _, Allocator.Temp))
        {
            Debug.Log("depth__ GetSensorData failed");
            return;
        }
        ProcessFrame(in frame);
    }

    static byte[] convertDoubleToBytes(double value)
    {
        byte[] bytes = BitConverter.GetBytes((float)value);
        Debug.Log($"__ bytes: {bytes.Length}");
        Assert.AreEqual(4, bytes.Length);
        return bytes;
    }

    static MetaData getMetaData(byte[] byteArray)
    {
        MetaData metaData = new MetaData();
        var offset = 1044480;
        metaData.position.x = BitConverter.ToSingle(byteArray, offset + 16);
        metaData.position.y = BitConverter.ToSingle(byteArray, offset + 20);
        metaData.position.z = BitConverter.ToSingle(byteArray, offset + 24);
        metaData.rotation.x = BitConverter.ToSingle(byteArray, offset + 28);
        metaData.rotation.y = BitConverter.ToSingle(byteArray, offset + 32);
        metaData.rotation.z = BitConverter.ToSingle(byteArray, offset + 36);
        metaData.rotation.w = BitConverter.ToSingle(byteArray, offset + 40);
        return metaData;
    }

    public Vector3 projectPoint(int u, int v, double[][] depth, Vector4 K_depth, Vector4 K_rgb, Vector3 pos, Quaternion rot)
    {
        var fx_d = K_depth.x;
        var fy_d = K_depth.y;
        var cx_d = K_depth.z;
        var cy_d = K_depth.w;
        var fx_rgb = K_rgb.x;
        var fy_rgb = K_rgb.y;
        var cx_rgb = K_rgb.z;
        var cy_rgb = K_rgb.w;

        int u_depth = (int) math.round(cx_d + fx_d * ((u / fx_rgb) - cx_rgb / fx_rgb));
        int v_depth = (int) math.round(cy_d + fy_d * ((v / fy_rgb) - cy_rgb / fy_rgb));

        Vector4 p = new Vector4((float)u_depth,(float) v_depth, (float)depth[v_depth][u_depth], 1);
        var x = p.x - cx_d/fx_d * p.z;
        var y = p.y - cy_d / fy_d * p.z;
        var z = p.z;
        Vector3 result = new Vector3(x,y,z);
        return rot * result + pos;
    }

    static double[][] ConvertByteArrayToDoubleArray(byte[] byteArray)
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
        double[][] array2d = new double[480][];
        for (int i = 0; i < 480; i++)
        {
            array2d[i] = doubleArray[(i*544)..((i + 1) * 544)];
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
                    var byteArray = ArrayPool<byte>.Shared.Rent(firstPlane.ByteData.Length);
                    firstPlane.ByteData.CopyTo(byteArray);
                    var offset = 1044480 + 16;
                    
                    byte[] pos_x = convertDoubleToBytes(position.x);
                    byte[] pos_y = convertDoubleToBytes(position.y);
                    byte[] pos_z = convertDoubleToBytes(position.z);
                    byte[] rot_x = convertDoubleToBytes(rotation.x);
                    byte[] rot_y = convertDoubleToBytes(rotation.y);
                    byte[] rot_z = convertDoubleToBytes(rotation.z);
                    byte[] rot_w = convertDoubleToBytes(rotation.w);

                    // write position and rotation to the byte array
                    Array.Copy(pos_x, 0, byteArray, offset, 4);
                    Array.Copy(pos_y, 0, byteArray, offset + 4, 4);
                    Array.Copy(pos_z, 0, byteArray, offset + 8, 4);
                    Array.Copy(rot_x, 0, byteArray, offset + 12, 4);
                    Array.Copy(rot_y, 0, byteArray, offset + 16, 4);
                    Array.Copy(rot_z, 0, byteArray, offset + 20, 4);
                    Array.Copy(rot_w, 0, byteArray, offset + 24, 4);


                    byte[][] parts = new byte[4][]; // Array to hold the 4 parts
                    var partSize = 262144; // byteArray.Length / 4; // Size of each part
                    for (int i = 0; i < 4; i++)
                    {
                        parts[i] = new byte[partSize];
                        Array.Copy(byteArray, i * partSize, parts[i], 0, partSize);
                    }
                    string abc = "abc";
                    Encoding.UTF8.GetBytes(abc);
                    Vector3 point3d = projectPoint(100, 100, ConvertByteArrayToDoubleArray(byteArray), K_depth, K_rgb, position, rotation);
                    Debug.Log($"__ 3D point: {point3d}");
                    Debug.Log($"__ sending message with len: {byteArray.Length}");
                    if(stopwatch.ElapsedMilliseconds > 200)
                    {
                        webrtccontroller.AddDataToDataStream(parts[0]);
                        webrtccontroller.AddDataToDataStream2(parts[1]);
                        webrtccontroller.AddDataToDataStream3(parts[2]);
                        webrtccontroller.AddDataToDataStream4(parts[3]);
                        Debug.Log("__ sent message");
                        stopwatch.Restart();
                    }

                    //Vector3 point3d = projectPoint(100, 100, ConvertByteArrayToDoubleArray(byteArray), K_depth, K_rgb, position, rotation);
                    Debug.Log($"__ 3D point: {point3d}");

                    break;
            }
            

            default:
                Debug.Log("depth__ in ProcessFrame default");
                throw new ArgumentOutOfRangeException();
        }
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
