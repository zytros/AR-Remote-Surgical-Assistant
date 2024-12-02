using System;
using System.Collections;
using System.Collections.Generic;
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
using UnityEngine.XR.OpenXR;

public class DepthImage : Singleton<DepthImage>
{
    private MagicLeapPixelSensorFeature pixelSensorFeature;
    public PixelSensorId SensorId;

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
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator CreateSensorAfterPermission()
    {
        var availableSensors = pixelSensorFeature.GetSupportedSensors();
        
        foreach (var sensor in availableSensors)
        {
            Debug.Log($"depth--- Sensor: {sensor}");
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
            Debug.Log("depth--- Depth sensor created");
            sensor_created = true;
        }
        else
        {
            Debug.LogError("depth--- Unable to create depth sensor");
            sensor_created = false;
        }

        uint configuredStream = 0;
        if (sensor_created)
        {
            var configureResult = pixelSensorFeature.ConfigureSensorWithDefaultCapabilities(SensorId, configuredStream);
            yield return configureResult;
            if (!configureResult.DidOperationSucceed)
            {
                Debug.LogError($"depth--- Unable to configure Depth Sensor");
                sensor_configured = false;
            }
            else
            {
                Debug.Log("depth--- Depth sensor configured");
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
                Debug.LogError($"depth--- Unable to start Depth Sensor");
                sensor_started = false;
            }
            else
            {
                Debug.Log("depth--- Depth sensor started");
                sensor_started = true;
            }
        }
        
    }
}
