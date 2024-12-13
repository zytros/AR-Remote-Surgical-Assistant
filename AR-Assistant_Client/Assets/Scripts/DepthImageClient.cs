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
using System.IO;

public class DepthImageClient : Singleton<DepthImageClient>
{
    public struct MetaData
    {
        public Vector3 position;
        public Quaternion rotation;
    };


    byte[] LatestDepthArray;

    Vector4 K_rgb = new Vector4(543.5f, 543.5f, 272f, 240f);
    Vector4 K_depth = new Vector4(800f, 800f, 640f, 360f);

    // Start is called before the first frame update
    void Start()
    {
        UIController.Instance.OnPauseMediaButtonPressed += Get3DPoints;
    }

    // Update is called once per frame
    void Update()
    {
        
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
            array2d[i] = doubleArray[(i * 544)..((i + 1) * 544)];
            //array2d[i, j] = doubleArray[i * 544 + j];
        }

        return array2d;
    }

    public void CombineDepthArrays(byte[] d1, byte[] d2, byte[] d3, byte[] d4)
    {
        if (d1 == null || d2 == null || d3 == null || d4 == null)
        {
            return;
        }
        // Set latest depth array length to combined length of all incoming byte arrays 
        LatestDepthArray = new byte[d1.Length + d2.Length + d3.Length + d4.Length];

        // Combine the four depth arrays together
        System.Buffer.BlockCopy(d1, 0, LatestDepthArray, 0, d1.Length);
        System.Buffer.BlockCopy(d2, 0, LatestDepthArray, d1.Length, d2.Length);
        System.Buffer.BlockCopy(d3, 0, LatestDepthArray, d1.Length + d2.Length, d3.Length);
        System.Buffer.BlockCopy(d4, 0, LatestDepthArray, d1.Length + d2.Length + d3.Length, d4.Length);
    }

    public void SaveDepthArrayToFile()
    {
        double[][] doubleDepthArray = ConvertByteArrayToDoubleArray(LatestDepthArray);

        using (StreamWriter sr = new StreamWriter("C:/Users/" + Environment.UserName + "/Desktop/DepthData.txt"))
        {
            for (int i = 0; i < doubleDepthArray.Length; i++)
            {
                for (int j = 0; j < doubleDepthArray[i].Length; j++)
                {
                    sr.Write(doubleDepthArray[i][j] + " ");
                }
                sr.Write('\n');
            }
        }
    }

    public void Get3DPoints()
    {
        Debug.Log($"-- {LatestDepthArray}");
        double[][] doubleDepthArray = ConvertByteArrayToDoubleArray(LatestDepthArray);

        MetaData md = getMetaData(LatestDepthArray);

        Vector3 p = projectPoint(640, 360, doubleDepthArray, K_depth, K_rgb, md.position, md.rotation);
        Debug.Log($"-- Projected Point: {p.x}, {p.y}, {p.z}");
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

        //int u_depth = (int)math.round(cx_d + fx_d * ((u / fx_rgb) - cx_rgb / fx_rgb));
        int u_depth = (int)math.round(50 + u / 1920 * 441);
        //int v_depth = (int)math.round(cy_d + fy_d * ((v / fy_rgb) - cy_rgb / fy_rgb));
        int v_depth = (int)math.round(65 + v / 1080 * 336);

        Debug.Log($"-- u: {u_depth} & v: {v_depth}");

        Vector4 p = new Vector4((float)u_depth, (float)v_depth, (float)depth[v_depth][u_depth], 1);
        var x = p.x - cx_d / fx_d * p.z;
        var y = p.y - cy_d / fy_d * p.z;
        var z = p.z;
        Vector3 result = new Vector3(x, y, z);
        Debug.Log($"-- Rotation: {rot.x}, {rot.y}, {rot.z}, {rot.w}");
        Debug.Log($"-- Position: {pos.x}, {pos.y}, {pos.z}");
        return rot * result + pos;
    }
}
