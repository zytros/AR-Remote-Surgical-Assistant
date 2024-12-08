using System;
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

public class DepthImage : Singleton<DepthImage>
{
    private MagicLeapPixelSensorFeature pixelSensorFeature;
    public PixelSensorId SensorId;
    uint i = 0;

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
        uint ConfiguredStream = 0;
        if (!pixelSensorFeature.GetSensorData(SensorId, ConfiguredStream, out var frame, out _, Allocator.Temp))
        {
            Debug.Log("depth__ GetSensorData failed");
            return;
        }
        ProcessFrame(in frame);
    }
    static double[] ConvertByteArrayToDoubleArray(byte[] byteArray)
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

        return doubleArray;
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
                    // Debug.Log($"byte array size__: {byteArray.Length}");
                    double[] doubleData = ConvertByteArrayToDoubleArray(byteArray); //last 1024 bytes are zeros
                    // TODO: send data to server
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
