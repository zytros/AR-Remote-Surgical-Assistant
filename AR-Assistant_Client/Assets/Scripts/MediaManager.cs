using System;
using System.Collections;
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
    }

    private void StartMedia()
    {
        //_targetCameraDeviceManager.StartMedia();
        _microphoneManager.SetupAudio();
    }

    private void TogglePause()
    {
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
