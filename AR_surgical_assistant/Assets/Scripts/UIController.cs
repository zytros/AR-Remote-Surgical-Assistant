using System;
using UnityEngine;
using MagicLeap;
using System.Net;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;


/// <summary>
/// UIController manages the user interface for starting media and handling WebRTC connections.
/// </summary>
public class UIController : Singleton<UIController>
{
    /// <summary>
    /// Enum indicating the available buttons in the UI menu.
    /// </summary>
    private enum ShowUIConfig
    {
        StartMedia,
        ConnectWebRTC,
        DisconnectWebRTC,
        None
    }

    private ShowUIConfig active_ui_config;
    private ShowUIConfig prev_ui_config;

    /// <summary>
    /// Event triggered when the "Start Media" button is pressed.
    /// </summary>
    public Action OnStartMediaButtonPressed;

    /// <summary>
    /// Event indicating a WebRTC connection or disconnection. 
    /// The bool value represents the connection status, and the string value is the signaling server IP.
    /// </summary>
    public Action<bool, string> OnWebRTCConnectionChangeButtonPressed;

    [SerializeField] private Button _startMediaButton;
    [SerializeField] private Button _connectWebRTCButton;
    [SerializeField] private Button _disconnectWebRTCButton;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private TMP_Text _logText;
    [SerializeField] private LazyFollow _lazyFollow;

    private void Start()
    {
        WebRTCController.Instance.OnWebRTCConnectionStateChange += OnWebRTCConnectionChanged;

        if (_inputField != null && _disconnectWebRTCButton != null
            && _connectWebRTCButton != null && _startMediaButton != null)
        {
            _startMediaButton.onClick.AddListener(StartMediaButtonPressed);
            _connectWebRTCButton.onClick.AddListener(ConnectWebRTCButtonPressed);
            _disconnectWebRTCButton.onClick.AddListener(DisconnectWebRTCButtonPressed);

            active_ui_config = ShowUIConfig.None;
            prev_ui_config = ShowUIConfig.StartMedia;
            ChangeUI(ShowUIConfig.None);
            _inputField.text = PlayerPrefs.GetString("webrtc-local-ip-config", "");
        }
        else
        {
            Debug.LogError("Check UserInputController parameters, NULLs");
        }
    }

    private void OnWebRTCConnectionChanged(WebRTCController.WebRTCConnectionState connectionState)
    {
        // If Connected we disable ConnectWebRTC button and enable DisconnectWebRTC
        if (connectionState == WebRTCController.WebRTCConnectionState.Connected)
        {
            prev_ui_config = active_ui_config;
            active_ui_config = ShowUIConfig.DisconnectWebRTC;
            ChangeUI(ShowUIConfig.DisconnectWebRTC);
        }
        else if (connectionState == WebRTCController.WebRTCConnectionState.Connecting)
        {
            // If disconnected we disable DisconnectWebRTC button and enable ConnectWebRTC
            prev_ui_config = active_ui_config;
            active_ui_config = ShowUIConfig.None;
            ChangeUI(ShowUIConfig.None);
        }
        else
        {
            // If disconnected we disable DisconnectWebRTC button and enable ConnectWebRTC
            prev_ui_config = active_ui_config;
            active_ui_config = ShowUIConfig.ConnectWebRTC;
            ChangeUI(ShowUIConfig.ConnectWebRTC);
        }
    }

    private void ConnectWebRTCButtonPressed()
    {
        // Check if text written on the input field is valid IP format
        if (!CheckIPValid(_inputField.text))
        {
            Debug.LogError("Not Valid Format IP Address");
            return;
        }
        // Disable button
        PlayerPrefs.SetString("webrtc-local-ip-config", _inputField.text);

        prev_ui_config = active_ui_config;
        active_ui_config = ShowUIConfig.None;
        ChangeUI(ShowUIConfig.None);
        _lazyFollow.enabled = false;

        OnWebRTCConnectionChangeButtonPressed?.Invoke(true, _inputField.text);
    }

    private void DisconnectWebRTCButtonPressed()
    {
        Debug.Log("Disconnect webRTC ");
        prev_ui_config = active_ui_config;
        active_ui_config = ShowUIConfig.None;
        ChangeUI(ShowUIConfig.None);
        _lazyFollow.enabled = true;

        OnWebRTCConnectionChangeButtonPressed?.Invoke(false, "");
    }

    private bool CheckIPValid(string textIPAddress)
    {
        if (string.IsNullOrEmpty(_inputField.text))
        {
            Debug.LogError("Not Valid Format IP Address");
            return false;
        }

        string[] splitValues = textIPAddress.Split('.');
        if (splitValues.Length != 4)
        {
            return false;
        }

        if (IPAddress.TryParse(textIPAddress, out IPAddress address))
        {
            switch (address.AddressFamily)
            {
                case System.Net.Sockets.AddressFamily.InterNetwork:
                    // Magic Leap supports IPv6
                    Debug.Log("Format IPv4");
                    return true;
                case System.Net.Sockets.AddressFamily.InterNetworkV6:
                    // Magic Leap supports IPv6
                    Debug.Log("Format IPv6");
                    return true;
                default:
                    // Magic Leap does not support this IP
                    Debug.LogError($"IP Format Not Supported : {address.AddressFamily}");
                    return false;
            }
        }
        return false;
    }

    private void StartMediaButtonPressed()
    {
        prev_ui_config = active_ui_config;
        active_ui_config = ShowUIConfig.ConnectWebRTC;
        ChangeUI(ShowUIConfig.ConnectWebRTC);
        OnStartMediaButtonPressed?.Invoke();
    }

    private void OnDestroy()
    {
       if(_startMediaButton) 
           _startMediaButton.onClick.RemoveListener(StartMediaButtonPressed);
    }

    private void ActivationChangeButton(Component componentToChange, bool active)
    {
        componentToChange.gameObject.SetActive(active);
    }

    

    private void ChangeUI(ShowUIConfig desiredUIConfig)
    {
        switch (desiredUIConfig)
        {
            case ShowUIConfig.StartMedia:
                ActivationChangeButton(_inputField, false);
                ActivationChangeButton(_connectWebRTCButton, false);
                ActivationChangeButton(_disconnectWebRTCButton, false);
                ActivationChangeButton(_startMediaButton, true);
                break;
            case ShowUIConfig.ConnectWebRTC:
                ActivationChangeButton(_inputField, true);
                ActivationChangeButton(_connectWebRTCButton, true);
                ActivationChangeButton(_disconnectWebRTCButton, false);
                ActivationChangeButton(_startMediaButton, false);
                break;
            case ShowUIConfig.DisconnectWebRTC:
                ActivationChangeButton(_inputField, false);
                ActivationChangeButton(_connectWebRTCButton, false);
                ActivationChangeButton(_disconnectWebRTCButton, true);
                ActivationChangeButton(_startMediaButton, false);
                break;
            case ShowUIConfig.None:
                ActivationChangeButton(_inputField, false);
                ActivationChangeButton(_connectWebRTCButton, false);
                ActivationChangeButton(_disconnectWebRTCButton, false);
                ActivationChangeButton(_startMediaButton, false);
                break;
            default:
                break;
        }
    }

    public void LogMessageInPanel(string msg)
    {
        if (_logText == null)
        {
            Debug.LogError("log Panel is null");
            return;
        }

        Debug.Log(" LogInPanel - " + msg);
        _logText.text = msg;
    }

    public void ChangeUIForMediaInput()
    {
        prev_ui_config = active_ui_config;
        active_ui_config = ShowUIConfig.StartMedia;
        ChangeUI(ShowUIConfig.StartMedia);
    }

    public void ChangeUIForWebRTCConnection()
    {
        prev_ui_config = active_ui_config;
        active_ui_config = ShowUIConfig.ConnectWebRTC;
        ChangeUI(ShowUIConfig.ConnectWebRTC);
    }

    public void ChangeUIForMenuOpen()
    {
        ChangeUI(prev_ui_config);
        active_ui_config = prev_ui_config;
        prev_ui_config = ShowUIConfig.None;
    }

    public void ChangeUIForMenuClose()
    {
        prev_ui_config = active_ui_config;
        active_ui_config = ShowUIConfig.None;
        ChangeUI(ShowUIConfig.None);
    }
}
