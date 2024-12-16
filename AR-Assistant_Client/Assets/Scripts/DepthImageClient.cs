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


    public byte[] LatestDepthArray;

    Vector4 K_rgb = new Vector4(543.5f, 543.5f, 272f, 240f);
    Vector4 K_depth = new Vector4(800f, 800f, 640f, 360f);

    // Start is called before the first frame update
    void Start()
    {
        // UIController.Instance.OnPauseMediaButtonPressed += Get3DPoints;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public double[][] ConvertByteArrayToDoubleArray(byte[] byteArray)
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
        Debug.Log($"Double Array Length: {doubleArray.Length}");
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
        // Debug.Log("DepthArray set");

        // Combine the four depth arrays together
        System.Buffer.BlockCopy(d1, 0, LatestDepthArray, 0, d1.Length);
        System.Buffer.BlockCopy(d2, 0, LatestDepthArray, d1.Length, d2.Length);
        System.Buffer.BlockCopy(d3, 0, LatestDepthArray, d1.Length + d2.Length, d3.Length);
        System.Buffer.BlockCopy(d4, 0, LatestDepthArray, d1.Length + d2.Length + d3.Length, d4.Length);
    }

    public void SaveDepthArrayToFile(int x, int y, double[][] doubleDepthArray)
    {
        // double[][] doubleDepthArray = ConvertByteArrayToDoubleArray(depthArray);

        // Debug.Log(x);
        // Debug.Log(y);
        for (int i = -5; i < 5; i++)
        {
            for (int j = -5; j < 5; j++)
            {
                doubleDepthArray[x+i][y+j] = 0;
            }
        }


        Debug.Log("C:/Users/" + Environment.UserName + "/Desktop/DepthData.txt");

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

    public Vector3 Get3DPoints(int x, int y, byte[] depthArray)
    {
        Debug.Log($"-- {depthArray}");
        double[][] doubleDepthArray = ConvertByteArrayToDoubleArray(depthArray);

        MetaData md = getMetaData(depthArray);
        // int x = 640;
        // int y = 360;
        Vector3 p = projectPoint(y, x, doubleDepthArray, K_depth, K_rgb, md.position, md.rotation);
        Debug.Log($"-- Projected Point: {p.x}, {p.y}, {p.z}");
        return p;
    }

    public MetaData getMetaData(byte[] byteArray)
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

    public Vector3 convert_to_depth(int u, int v, double[][] depth)
    {

        //int u_depth = (int)math.round(cx_d + fx_d * ((u / fx_rgb) - cx_rgb / fx_rgb));
        // int u_depth = (int)math.round(50 + Convert.ToDouble(u) / 1440 * 441);
        // int weirdoffset_u = -72; //higher is right //nice for video
        int weirdoffset_u = -60; //higher is right

        int u_depth = (int)math.round(107 + Convert.ToDouble(u) / 1440 * 441) +weirdoffset_u; //higher is higher

        //int v_depth = (int)math.round(cy_d + fy_d * ((v / fy_rgb) - cy_rgb / fy_rgb));
        // int v_depth = (int)math.round(65 + Convert.ToDouble(v) / 1080 * 336);
        // int weirdoffset_v = 30; //higher is up //nice for video
        int weirdoffset_v = 36; //higher is up


        int v_depth = (int)math.round(47 + Convert.ToDouble(v) / 1080 * 336) + weirdoffset_v; //higher -> right (up???)


        Debug.Log($"-- u: {u_depth} & v: {v_depth}");
        // SaveDepthArrayToFile(u_depth, v_depth, depth);
        return new Vector2(u_depth, v_depth);
    }

    public Vector3 transform_2d_point(int u, int v, double[][] depth, MetaData meta)
    {
        // double[][] depth = ConvertByteArrayToDoubleArray(deptharray);
        var vec = convert_to_depth(u, v, depth);
        // double d = depth[(int)vec.y][(int)vec.x];


        int u_depth =(int) vec.x;
        int v_depth =(int) vec.y;

        double d = depth[v_depth][u_depth];

        double d_smooth = 0f;
        int smoothing = 4;
        int cnt = 0;


        for (int i = -smoothing; i <= smoothing; i++)
        {
            for (int j = -smoothing; j <= smoothing; j++)
            {
                d_smooth += depth[v_depth + i][u_depth + j];
                cnt += 1;
            }
        }

        d_smooth /= cnt;
        Debug.Log($"d is {d} and d_smooth is {d_smooth}");
        d = d_smooth * 1.07;
        // d = d_smooth * 0.95;

        double fov_horizontal = 1.22173;
        double fov_vertical = 1.309;
        double new_x = TransformOneDim(fov_horizontal, 544, u_depth, d);
        double new_y = TransformOneDim(fov_vertical, 480, v_depth, d);

        Vector3 ret = new Vector3((float)new_x,(float)new_y, (float) d);

        return meta.rotation * ret + meta.position;
    }
    public double TransformOneDim(double fov, double totcoord, double coord, double depth)
    {
        if (totcoord / 2f == coord)
        {
            return totcoord / 2f;
        }
        bool right = coord > totcoord / 2f;
        bool left = !right;
        double t = -1;
        double x = -1;
        if (right)
        {
            t = (coord - (totcoord / 2f)) / (totcoord / 2f);
            x = depth * math.sin(math.atan(t*math.tan(fov / 2f)));
        }
        else
        {
            t = 1f - (coord / (totcoord / 2f));
            x = -depth * math.sin(math.atan(t * math.tan(fov / 2f)));
        }
        return x;
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
        // int u_depth = (int)math.round(50 + Convert.ToDouble(u) / 1440 * 441);
        int u_depth = (int)math.round(110 + Convert.ToDouble(u) / 1440 * 441);    //higher is higher

        //int v_depth = (int)math.round(cy_d + fy_d * ((v / fy_rgb) - cy_rgb / fy_rgb));
        // int v_depth = (int)math.round(65 + Convert.ToDouble(v) / 1080 * 336);
        int v_depth = (int)math.round(46.5 + Convert.ToDouble(v) / 1080 * 336); //higher -> right


        Debug.Log($"-- u: {u_depth} & v: {v_depth}");
        SaveDepthArrayToFile(u_depth, v_depth, depth);


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
