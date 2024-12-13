using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MagicLeap;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.XR.MagicLeap;

/// <summary>
/// Manages media components such as camera and microphone for both device and editor platforms.
/// </summary>
public class MediaManager : Singleton<MediaManager>
{
    [Header("Remote Media")]
    [SerializeField] private RawImage _mainVideoStream;
    [SerializeField] private RawImage _smallVideoStream;
    [SerializeField] private RawImage _imageShareStream;
    [SerializeField]
    public AudioSource  _receiveAudio;

    [Header("Local Media")]

    //[SerializeReference]
    //private MagicLeapCameraManager _magicLeapCameraDeviceManager;
    //[SerializeReference]
    //private WebCameraManager _webCameraDeviceManager;
    [SerializeField]
    private MicrophoneManager _microphoneManager;

    [Header("Permissions")]

    [SerializeField] 
    private PermissionManager _permissionManager;

    [Header("Magic Leap Settings")]

    [SerializeField]
    [Tooltip("Will use the MLCamera APIs instead of the WebCamera Texture component.")]
    private bool _useMLCamera = true;

    //private ICameraDeviceManager _targetCameraDeviceManager;

    public RawImage RemoteVideoRenderer => _smallVideoStream;
    public RawImage PausableVideoRenderer => _mainVideoStream;

    public bool isPaused = false;
    private Texture2D _pausedFrameTexture;

    public Color drawColor = Color.red;
    public int brushSize = 1;
    public float maxDistance = 1f;
    private List<Tuple<int, int>> annotations = new List<Tuple<int, int>>();
    private List<List<Tuple<int, int>>> backAnnotations = new List<List<Tuple<int, int>>>();
    private Texture2D originalTexture;

    private Vector2? lastMousePos = null;
    private Color[] colorBuffer;
    private RenderTexture rt2;
    private RenderTexture sharedTexture;

    private byte[] depthArray;

    private RenderTexture GetCameraTexture()
    {
        rt2 = new RenderTexture(1920, 1080, 0, RenderTextureFormat.BGRA32);
        return rt2;
    }

    private RenderTexture GetDefaultTexture()
    {
        sharedTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.BGRA32);
        return sharedTexture;
    }

    public RenderTexture CameraTexture => GetCameraTexture();

    public RenderTexture FileShareImageTexture => GetDefaultTexture();

    public AudioSource SourceAudio => _microphoneManager.SourceAudio;

    public AudioSource ReceiveAudio => _receiveAudio;

    public RawImage GetActiveVideoRenderer()
    {
        return isPaused ? _smallVideoStream : _mainVideoStream;
    }

    private IEnumerator Start()
    {

        //if (_useMLCamera && Application.platform == RuntimePlatform.Android
        //    && SystemInfo.deviceModel == "Magic Leap Magic Leap 2")
        //{
        //    _targetCameraDeviceManager = _magicLeapCameraDeviceManager;
        //}
        //else
        //{
        //    _targetCameraDeviceManager = _webCameraDeviceManager;
        //}
        //_targetCameraDeviceManager = _webCameraDeviceManager;
        _permissionManager.RequestPermission();
        yield return new WaitUntil(() => _permissionManager.PermissionsGranted);
        UIController.Instance.OnStartMediaButtonPressed += StartMedia;
        UIController.Instance.OnPauseMediaButtonPressed += TogglePause;
        UIController.Instance.OnBackMediaButtonPressed += GoBack;
        UIController.Instance.OnShareImageButtonPressed += SetSharedImageTexture;
    }

    private void StartMedia()
    {
        //_targetCameraDeviceManager.StartMedia();
        _microphoneManager.SetupAudio();
    }

    private void TogglePause()
    {
        backAnnotations = new List<List<Tuple<int, int>>>();
        // annotations = new List<Tuple<int, int>>();
        if (!isPaused)
        {
            depthArray = DepthImageClient.Instance.LatestDepthArray;
            _smallVideoStream.texture = _mainVideoStream.texture;
            Texture texture = _mainVideoStream.texture;

            if (texture is Texture2D sourceTexture2D)
            {
                Debug.Log(texture.isReadable);
                _pausedFrameTexture = new Texture2D(sourceTexture2D.width, sourceTexture2D.height, TextureFormat.RGBA32, false);
                originalTexture = new Texture2D(sourceTexture2D.width, sourceTexture2D.height, TextureFormat.RGBA32, false);

                RenderTexture currentRT = RenderTexture.active;
                RenderTexture renderTexture = RenderTexture.GetTemporary(
                    sourceTexture2D.width,
                    sourceTexture2D.height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.sRGB // Use sRGB to match Unity's default gamma workflow
                );

// Blit the source texture to the RenderTexture
                Graphics.Blit(sourceTexture2D, renderTexture);
                RenderTexture.active = renderTexture;

// Copy the RenderTexture to the paused frame texture
                _pausedFrameTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                _pausedFrameTexture.Apply();

                originalTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                originalTexture.Apply();
// Clean up
                RenderTexture.active = currentRT;
                RenderTexture.ReleaseTemporary(renderTexture);
                _mainVideoStream.texture = _pausedFrameTexture;
                colorBuffer = originalTexture.GetPixels();
            }
            else
            {
                Debug.LogWarning(texture);
            }
        }
        else
        {
            _mainVideoStream.texture = _smallVideoStream.texture;
            _smallVideoStream.texture = _pausedFrameTexture;
            SendAnnotation();
        }

        annotations = new List<Tuple<int, int>>();

        // TEMPORARY:

        if (_pausedFrameTexture != null)
        {
            rt2.width = _pausedFrameTexture.width;
            rt2.height = _pausedFrameTexture.height;
            RenderTexture.active = rt2;
            Graphics.Blit(_pausedFrameTexture, rt2);
        }

        //----------------
        Save_Texture2D(_pausedFrameTexture, "C:/Users/" + Environment.UserName + "/Desktop/PausedImage.png");

        isPaused = !isPaused;
        //Debug.Log($"Paused: {PauseRemoteVideo}");
    }

    private void Save_Texture2D(in Texture2D tex, string path)
    {
        byte[] bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
    }

    private void SendAnnotation()
    {
        Debug.Log(annotations.Count);
        List<Vector3> points3D = new List<Vector3>();
        for (int i = 0; i < annotations.Count; i++)
        {
            Debug.Log("Loop");
            Vector3 point3D = DepthImageClient.Instance.transform_2d_point(annotations[i].Item1, annotations[i].Item2, depthArray);
            Debug.Log($"-- x: {point3D.x}, y: {point3D.y}, z: {point3D.z}");
            // points3D.Append(point3D);
            points3D.Add(point3D);
            // Vector3 point3D = DepthImageClient.Instance.Get3DPoints(annotations[i].Item1, annotations[i].Item2, depthArray);

        }

        // Do reproduction stuff
        // String annotationString = string.Join(", ", points3D.Select(t => $"({t.x}, {t.y}, {t.z})"));
        byte[] annotationString = new byte[12];
        Array.Copy(convertDoubleToBytes(points3D[0].x), 0, annotationString, 0,4);
        Array.Copy(convertDoubleToBytes(points3D[0].y), 0, annotationString, 4,4);
        Array.Copy(convertDoubleToBytes(points3D[0].z), 0, annotationString, 8,4);


        // annotationString = "ANN#" + annotationString;

        WebRTCController.Instance.AddAnnotationToDataStream(annotationString);
    }

    static byte[] convertDoubleToBytes(double value)
    {
        byte[] bytes = BitConverter.GetBytes((float)value);
        Debug.Log($"__ bytes: {bytes.Length}");
        // Assert.AreEqual(4, bytes.Length);
        return bytes;
    }

    public void SetSharedImageTexture()
    {
        if (_imageShareStream.texture != null)
        {
            if (sharedTexture == null)
            {
                GetDefaultTexture();
            }
            sharedTexture.width = _imageShareStream.texture.width;
            sharedTexture.height = _imageShareStream.texture.height;
            Graphics.Blit(_imageShareStream.texture, sharedTexture);

            //rt2.width = _imageShareStream.texture.width;
            //rt2.height = _imageShareStream.texture.height;
            //RenderTexture.active = rt2;
            //Graphics.Blit(_imageShareStream.texture, rt2);
        }
    }

    void GoBack()
    {
        if (backAnnotations.Count == 0 || !isPaused)
        {
            return;
        }
        int idx = backAnnotations.Count - 1;
        List<Tuple<int, int>> backAnnotation = backAnnotations[idx];
        backAnnotations.RemoveAt(idx);
        colorBuffer = originalTexture.GetPixels();
        annotations = backAnnotation;
        ApplyAnnotation();
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.isPressed)
        {
            TogglePause();
        }
        else if (Keyboard.current.escapeKey.isPressed)
        {
            Application.Quit();
        }
        if (isPaused)
        {
            if ((Mouse.current != null && Mouse.current.leftButton.isPressed) ||
                (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed))
            {
                if ((Mouse.current != null && Mouse.current.leftButton.isPressed) &&
                    RectTransformUtility.RectangleContainsScreenPoint(_mainVideoStream.rectTransform,
                        Mouse.current.position.ReadValue(), Camera.main))
                {
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(_mainVideoStream.rectTransform,
                        Mouse.current.position.ReadValue(), Camera.main, out localPoint);

                    HandlePosInput(localPoint);

                }
                else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                {
                    Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                    if (RectTransformUtility.RectangleContainsScreenPoint(
                            _mainVideoStream.rectTransform,
                            touchPosition,
                            Camera.main))
                    {
                        Vector2 localPoint;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            _mainVideoStream.rectTransform,
                            touchPosition,
                            Camera.main,
                            out localPoint);
                        HandlePosInput(localPoint);
                    }
                }
                else
                {
                    lastMousePos = null;
                }
            }
            else
            {
                lastMousePos = null;
            }
        }
    }

    private void HandlePosInput(Vector2 location)
    {
        Rect rect = _mainVideoStream.rectTransform.rect;
        float normalizedX = (location.x - rect.x) / rect.width;
        float normalizedY = (location.y - rect.y) / rect.height;
        int x = Mathf.Clamp((int)(normalizedX * _pausedFrameTexture.width), 0, _pausedFrameTexture.width - 1);
        int y = Mathf.Clamp((int)(normalizedY * _pausedFrameTexture.height), 0, _pausedFrameTexture.height - 1);

        Vector2 currentMousePos = new Vector2(x, y);


        if (lastMousePos.HasValue)
        {
            // Interpolate between last and current positions to fill in gaps
            DrawLine(lastMousePos.Value, currentMousePos);
        }
        else
        {
            if (backAnnotations.Count > 5)
            {
                backAnnotations.RemoveAt(0);
            }
            Texture2D oldTexture = new Texture2D(_pausedFrameTexture.width, _pausedFrameTexture.height);
            oldTexture.SetPixels(_pausedFrameTexture.GetPixels());
            Debug.Log("Adding image to backImages");
            backAnnotations.Add(new List<Tuple<int, int>>(annotations));
            DrawOnTexture(x, y);
        }

        // Update the last position
        lastMousePos = currentMousePos;
        ApplyAnnotation();
    }

    private void ApplyAnnotation()
    {
        for (int point = 0; point < annotations.Count; point++)
        {
            int x = annotations[point].Item1;
            int y = annotations[point].Item2;

            for (int i = -brushSize; i <= brushSize; i++)
            {
                for (int j = -brushSize; j <= brushSize; j++)
                {
                    int px = Mathf.Clamp(x + i, 0, _pausedFrameTexture.width - 1);
                    int py = Mathf.Clamp(y + j, 0, _pausedFrameTexture.height - 1);

                    int bufferIndex = px + py * _pausedFrameTexture.width;
                    colorBuffer[bufferIndex] = drawColor;
                }
            }
        }

        _pausedFrameTexture.SetPixels(originalTexture.GetPixels());
        _pausedFrameTexture.Apply();
        _pausedFrameTexture.SetPixels(colorBuffer);
        _pausedFrameTexture.Apply();

    }

    private void DrawLine(Vector2 start, Vector2 end)
    {
        float distance = Vector2.Distance(start, end);
        Vector2 direction = (end - start).normalized;

        // Place points along the line based on maxDistance
        for (float i = 0; i < distance; i += maxDistance)
        {
            Vector2 point = start + direction * i;
            DrawOnTexture((int)point.x, (int)point.y);
        }
        // Ensure the endpoint is drawn
        DrawOnTexture((int)end.x, (int)end.y);
    }

    private void DrawOnTexture(int x, int y)
    {
        int clampedX = Mathf.Clamp(x, 0, _pausedFrameTexture.width - 1);
        int clampedY = Mathf.Clamp(y, 0, _pausedFrameTexture.height - 1);
        Debug.Log($"width: {_pausedFrameTexture.width}, height: {_pausedFrameTexture.height}");
        annotations.Add(new Tuple<int, int>(clampedX, clampedY));

    }

    private void OnDisable()
    {
        UIController.Instance.OnStartMediaButtonPressed -= StartMedia;
        StopMedia();
    }

    /// <summary>
    /// Stop microphone and camera on Magic Leap 2
    /// TODO: Implement UI to actually call this function
    /// </summary>
    private void StopMedia()
    {
        //_targetCameraDeviceManager.StopMedia();
        _microphoneManager.StopMicrophone();

    }

    /// <summary>
    /// Returns true if local microphone and camera are ready
    /// </summary>
    public bool IsMediaReady()
    {
        //Debug.Log($" Camera Device Ready = {_targetCameraDeviceManager.IsConfiguredAndReady} && Microphone Ready = {_microphoneManager.IsConfiguredAndReady} ");
        Debug.Log($"Microphone Ready = {_microphoneManager.IsConfiguredAndReady} ");
        //return _targetCameraDeviceManager != null 
        //       && _targetCameraDeviceManager.IsConfiguredAndReady 
        //       && _microphoneManager.IsConfiguredAndReady;
        return _microphoneManager.IsConfiguredAndReady;
    }

    public bool IsMediaPaused()
    {
        Debug.Log($"Paused: {isPaused}");
        return isPaused;
    }
}
