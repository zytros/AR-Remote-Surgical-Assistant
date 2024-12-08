using System;
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
    // private List<int> annotationsX = new List<int>();
    // private List<int> annotationsY = new List<int>();
    private List<List<Tuple<int, int>>> backAnnotations = new List<List<Tuple<int, int>>>();
    // private List<List<int>> backAnnotationsX = new List<List<int>>();
    // private List<List<int>> backAnnotationsY = new List<List<int>>();
    private Texture2D originalTexture;

    private Vector2? lastMousePos = null;
    private Color[] colorBuffer;
    private RenderTexture rt2;
    private RenderTexture sharedTexture;

    private RenderTexture GetCameraTexture()
    {
        rt2 = new RenderTexture(1920, 1080, 0, RenderTextureFormat.BGRA32);
        // rt2 = new RenderTexture(1920, 1080, 0, RenderTextureFormat.Default);

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
        // backAnnotationsX = new List<List<int>>();
        // backAnnotationsY = new List<List<int>>();
        // annotationsX = new List<int>();
        // annotationsY = new List<int>();
        backAnnotations = new List<List<Tuple<int, int>>>();
        annotations = new List<Tuple<int, int>>();
        if (!isPaused)
        {
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
        // TEMPORARY:

        if (_pausedFrameTexture != null)
        {
            rt2.width = _pausedFrameTexture.width;
            rt2.height = _pausedFrameTexture.height;
            RenderTexture.active = rt2;
            Graphics.Blit(_pausedFrameTexture, rt2);
        }

        //----------------

        isPaused = !isPaused;
        //Debug.Log($"Paused: {PauseRemoteVideo}");
    }

    private void SendAnnotation()
    {

        Texture2D annotatedImage = _pausedFrameTexture;
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
        // Texture2D backTexture = backTextures[idx];
        // List<int> backX = backAnnotationsX[idx];
        // List<int> backY = backAnnotationsY[idx];
        List<Tuple<int, int>> backAnnotation = backAnnotations[idx];
        // backTextures.RemoveAt(idx);
        // backAnnotationsX.RemoveAt(idx);
        // backAnnotationsY.RemoveAt(idx);
        backAnnotations.RemoveAt(idx);
        // _pausedFrameTexture.SetPixels(backTexture.GetPixels());
        // _pausedFrameTexture.Apply();
        // colorBuffer = _pausedFrameTexture.GetPixels();
        colorBuffer = originalTexture.GetPixels();
        // annotationsX = backX;
        // annotationsY = backY;
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
                // backAnnotationsX.RemoveAt(0);
                // backAnnotationsY.RemoveAt(0);
                backAnnotations.RemoveAt(0);
            }
            Texture2D oldTexture = new Texture2D(_pausedFrameTexture.width, _pausedFrameTexture.height);
            oldTexture.SetPixels(_pausedFrameTexture.GetPixels());
            Debug.Log("Adding image to backImages");
            // backAnnotationsX.Add(new List<int>(annotationsX));
            // backAnnotationsY.Add(new List<int>(annotationsY));
            backAnnotations.Add(new List<Tuple<int, int>>(annotations));
            DrawOnTexture(x, y);
        }

        // Update the last position
        lastMousePos = currentMousePos;
        // _pausedFrameTexture.SetPixels(colorBuffer);
        // _pausedFrameTexture.Apply();
        ApplyAnnotation();
    }

    private void ApplyAnnotation()
    {
        for (int point = 0; point < annotations.Count; point++)
        {
            // int x = annotationsX[point];
            // int y = annotationsY[point];
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

        // Apply buffered changes to the texture all at once
        // _pausedFrameTexture.SetPixels(colorBuffer);
        // _pausedFrameTexture.Apply();
    }

    private void DrawOnTexture(int x, int y)
    {
        // Set pixels within the brush size at the given (x, y) position in the buffer
        // for (int i = -brushSize; i <= brushSize; i++)
        // {
        //     for (int j = -brushSize; j <= brushSize; j++)
        //     {
        //         int px = Mathf.Clamp(x + i, 0, _pausedFrameTexture.width - 1);
        //         int py = Mathf.Clamp(y + j, 0, _pausedFrameTexture.height - 1);
        //
        //         int bufferIndex = px + py * _pausedFrameTexture.width;
        //         colorBuffer[bufferIndex] = drawColor;
        //     }
        // }
        int clampedX = Mathf.Clamp(x, 0, _pausedFrameTexture.width - 1);
        int clampedY = Mathf.Clamp(y, 0, _pausedFrameTexture.height - 1);

        // annotationsX.Add(clampedX);
        // annotationsY.Add(clampedY);
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
