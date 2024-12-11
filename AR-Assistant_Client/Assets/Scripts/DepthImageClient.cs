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
    byte[] LatestDepthArray;

    // Start is called before the first frame update
    void Start()
    {
        UIController.Instance.OnPauseMediaButtonPressed += SaveDepthArrayToFile;
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
}
