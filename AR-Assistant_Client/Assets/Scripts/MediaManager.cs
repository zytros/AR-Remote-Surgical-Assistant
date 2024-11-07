using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MagicLeap;
using UnityEngine.UI;

/// <summary>
/// Manages media components such as camera and microphone for both device and editor platforms.
/// </summary>
public class MediaManager : Singleton<MediaManager>
{
    [Header("Remote Media")]
    [SerializeField] private RawImage _mainVideoStream;
    [SerializeField] private RawImage _smallVideoStream;
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
    private List<Texture2D> backTextures = new List<Texture2D>();

    private Vector2? lastMousePos = null;
    private Color[] colorBuffer;

    //public RenderTexture CameraTexture => _targetCameraDeviceManager.CameraTexture;

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
        UIController.Instance.OnBackMediaButtonPRessed += GoBack;
    }

    private void StartMedia()
    {
        //_targetCameraDeviceManager.StartMedia();
        _microphoneManager.SetupAudio();
    }

    private void TogglePause()
    {
        backTextures = new List<Texture2D>();
        if (!isPaused)
        {
            _smallVideoStream.texture = _mainVideoStream.texture;
            Texture texture = _mainVideoStream.texture;

            if (texture is Texture2D sourceTexture2D)
            {
                Debug.Log(texture.isReadable);
                _pausedFrameTexture = new Texture2D(sourceTexture2D.width, sourceTexture2D.height, TextureFormat.RGBA32, false);
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

// Clean up
                RenderTexture.active = currentRT;
                RenderTexture.ReleaseTemporary(renderTexture);
                _mainVideoStream.texture = _pausedFrameTexture;
                colorBuffer = _pausedFrameTexture.GetPixels();
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
        }

        isPaused = !isPaused;
        //Debug.Log($"Paused: {PauseRemoteVideo}");
    }

    void GoBack()
    {
        if (backTextures.Count == 0 || !isPaused)
        {
            return;
        }
        int idx = backTextures.Count - 1;
        Texture2D backTexture = backTextures[idx];
        backTextures.RemoveAt(idx);
        _pausedFrameTexture.SetPixels(backTexture.GetPixels());
        _pausedFrameTexture.Apply();
        colorBuffer = _pausedFrameTexture.GetPixels();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TogglePause();
        }
        else if (Input.GetKey("escape"))
        {
            Application.Quit();
        }
        if (isPaused)
        {
            if (Input.GetMouseButton(0) && RectTransformUtility.RectangleContainsScreenPoint(_mainVideoStream.rectTransform, Input.mousePosition, Camera.main))
            {
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_mainVideoStream.rectTransform, Input.mousePosition, Camera.main, out localPoint);

                Rect rect = _mainVideoStream.rectTransform.rect;
                float normalizedX = (localPoint.x - rect.x) / rect.width;
                float normalizedY = (localPoint.y - rect.y) / rect.height;
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
                    if (backTextures.Count > 5)
                    {
                        backTextures.RemoveAt(0);
                    }
                    Texture2D old_Texture = new Texture2D(_pausedFrameTexture.width, _pausedFrameTexture.height);
                    old_Texture.SetPixels(_pausedFrameTexture.GetPixels());
                    Debug.Log("Adding image to backImages");
                    backTextures.Add(old_Texture);
                    DrawOnTexture(x, y);
                }

                // Update the last position
                lastMousePos = currentMousePos;
                _pausedFrameTexture.SetPixels(colorBuffer);
                _pausedFrameTexture.Apply();
            }
            else
            {
                lastMousePos = null;
            }
        }
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
        _pausedFrameTexture.SetPixels(colorBuffer);
        _pausedFrameTexture.Apply();
    }

    private void DrawOnTexture(int x, int y)
    {
        // Set pixels within the brush size at the given (x, y) position in the buffer
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
