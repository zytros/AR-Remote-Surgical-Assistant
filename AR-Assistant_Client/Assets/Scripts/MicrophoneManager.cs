using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Android;
using FMODUnity;

/// <summary>
/// Manages the microphone for capturing audio in Unity.
/// This includes initializing the microphone, and configuring the AudioSource represent the captured audio.
/// </summary>
public class MicrophoneManager : MonoBehaviour
{
    [SerializeField] private AudioSource _sourceAudio;

    private string _microphoneName;
    private bool _isMicrophoneConfiguredAndReady = false;

    // Magic Leap 2 Microphone defaults
    private const int MIC_LENGTH = 1; // Seconds
    private const int MIC_SAMPLING_FREQ = 44100;
    private const int MICROPHONE_INDEX = 0;

    public AudioSource SourceAudio => _sourceAudio;
    public bool IsConfiguredAndReady => _isMicrophoneConfiguredAndReady;


    /// <summary>
    /// Stop microphone recording and AudioSource
    /// </summary>
    public void StopMicrophone()
    {
        if (_isMicrophoneConfiguredAndReady)
        {
            Microphone.End(_microphoneName);
            _sourceAudio.Stop();
            _sourceAudio.clip = null;
        }
        _isMicrophoneConfiguredAndReady = false;
    }

    /// <summary>
    /// Initializes the microphone if permissions are granted.
    /// </summary>
    public void SetupAudio()
    {
        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            StartCoroutine(InitializeMicrophone());
        }
        else
        {
            Debug.LogError("Microphone permission has not been granted.");
        }
    }

    /// <summary>
    /// Initializes microphone recording and setup AudioSource
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitializeMicrophone()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Debug.LogFormat("Authorization for using the Microphone is denied");
            yield break;
        }
        Permission.RequestUserPermission(Permission.Microphone);

        // Debug.Log($"Platform: {Application.platform}, API Level: {Application.unityVersion}");
        //
        // Debug.Log("Initializing FMOD logging.");
        // FMOD.Debug.Initialize(FMOD.DEBUG_FLAGS.LOG | FMOD.DEBUG_FLAGS.TYPE_MEMORY, FMOD.DEBUG_MODE.FILE);
        
        FMODUnity.RuntimeManager.CoreSystem.setOutput(FMOD.OUTPUTTYPE.AUTODETECT);
        
        Debug.Log("PERMISSION");
        Debug.Log(Permission.HasUserAuthorizedPermission(Permission.Microphone));

        string[] devices = Microphone.devices;
        
        Debug.Log("Available Microphones:");
        if (devices.Length == 0)
        {
            Debug.LogError("No microphone devices found.");
            yield break;
        }

        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log($"Microphone {i}: {devices[i]}");
        }
        
        _microphoneName = Microphone.devices.Length > MICROPHONE_INDEX ? Microphone.devices[MICROPHONE_INDEX] : null;

        if (string.IsNullOrEmpty(_microphoneName))
        {
            Debug.LogError("No microphone devices found.");
            yield break;
        }

        Debug.Log($"Available Microphone device: {_microphoneName}");

        Microphone.GetDeviceCaps(_microphoneName, out int minFreq, out int maxFreq);
        var micClip = Microphone.Start(_microphoneName, true, MIC_LENGTH, MIC_SAMPLING_FREQ);


        // Wait until the microphone starts recording
        yield return new WaitUntil(() => Microphone.GetPosition(_microphoneName) > 0);

        _sourceAudio.clip = micClip;
        _sourceAudio.loop = true;
        _sourceAudio.Play();

        _isMicrophoneConfiguredAndReady = true;
        Debug.Log($"Microphone {_microphoneName} Ready");
    }

    private void OnDestroy()
    {
        UIController.Instance.OnStartMediaButtonPressed -= SetupAudio;
    }

    /// <summary>
    /// Returns true if local microphone and camera are ready
    /// </summary>
    public bool IsMediaReady()
    {
        return _isMicrophoneConfiguredAndReady;
    }
}
