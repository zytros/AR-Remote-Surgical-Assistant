using System;
using UnityEngine;
using MagicLeap;
using System.Net;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.InputSystem;
using MixedReality.Toolkit.UX;


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

    [SerializeField] private GameObject mainHandUIPanel;
    //[SerializeField] private Button _startMediaButton;
    [SerializeField] private PressableButton _startMediaActionButton;
    [SerializeField] private PressableButton _connectWebRTCActionButton;
    [SerializeField] private PressableButton _disconnectWebRTCActionButton;
    [SerializeField] private PressableButton _muteButton;
    [SerializeField] private TMP_Text _muteButtonText;
    //[SerializeField] private Button _connectWebRTCButton;
    //[SerializeField] private Button _disconnectWebRTCButton;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private TMP_Text _logText;
    [SerializeField] private LazyFollow _lazyFollow;
    [SerializeField] private MixedReality.Toolkit.UX.Slider VolumeSlider;

    private bool _options_menu_open = true;

    //public UnityEngine.UI.Slider VolumeSlider;
    //public GameObject VolSlider;
    public GameObject LeftHandController;

    //The input action that will log it's Vector3 value every frame.
    //[SerializeField]
    //private InputAction positionInputAction =
    //    new InputAction(binding: "<MagicLeapAuxiliaryHandDevice>{LeftHand}/devicePosition", expectedControlType: "Vector3");

    private void Start()
    {
        WebRTCController.Instance.OnWebRTCConnectionStateChange += OnWebRTCConnectionChanged;

        if (_inputField != null && _disconnectWebRTCActionButton != null
            && _connectWebRTCActionButton != null && _startMediaActionButton != null)
        {
            //_startMediaButton.onClick.AddListener(StartMediaButtonPressed);
            _startMediaActionButton.OnClicked.AddListener(StartMediaButtonPressed);
            _connectWebRTCActionButton.OnClicked.AddListener(ConnectWebRTCButtonPressed);
            _disconnectWebRTCActionButton.OnClicked.AddListener(DisconnectWebRTCButtonPressed);

            //prev_ui_config = ShowUIConfig.StartMedia;
            active_ui_config = ShowUIConfig.StartMedia;
            ChangeUI(ShowUIConfig.StartMedia);
            //mainHandUIPanel.SetActive(false);
            _inputField.text = PlayerPrefs.GetString("webrtc-local-ip-config", "");
        }
        else
        {
            Debug.LogError("Check UserInputController parameters, NULLs");
        }

        //positionInputAction.Enable();
    }

    private void Update()
    {
        _lazyFollow.enabled = false;
        //Debug.Log($"Volume: {MediaManager.Instance.ReceiveAudio.volume}");
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
        //_lazyFollow.enabled = false;

        OnWebRTCConnectionChangeButtonPressed?.Invoke(true, _inputField.text);
    }

    private void DisconnectWebRTCButtonPressed()
    {
        Debug.Log("Disconnect webRTC ");
        prev_ui_config = active_ui_config;
        active_ui_config = ShowUIConfig.None;
        ChangeUI(ShowUIConfig.None);
        //_lazyFollow.enabled = true;

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
       //if(_startMediaButton) 
       //    _startMediaButton.onClick.RemoveListener(StartMediaButtonPressed);

        if (_startMediaActionButton)
            _startMediaActionButton.OnClicked.RemoveListener(StartMediaButtonPressed);
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
                ActivationChangeButton(_connectWebRTCActionButton, false);
                ActivationChangeButton(_disconnectWebRTCActionButton, false);
                ActivationChangeButton(_startMediaActionButton, true);
                ActivationChangeButton(_muteButton, false);
                ActivationChangeButton(VolumeSlider, false);
                break;
            case ShowUIConfig.ConnectWebRTC:
                ActivationChangeButton(_inputField, true);
                ActivationChangeButton(_connectWebRTCActionButton, true);
                ActivationChangeButton(_disconnectWebRTCActionButton, false);
                ActivationChangeButton(_startMediaActionButton, false);
                ActivationChangeButton(_muteButton, false);
                ActivationChangeButton(VolumeSlider, false);
                break;
            case ShowUIConfig.DisconnectWebRTC:
                ActivationChangeButton(_inputField, false);
                ActivationChangeButton(_connectWebRTCActionButton, false);
                ActivationChangeButton(_disconnectWebRTCActionButton, true);
                ActivationChangeButton(_startMediaActionButton, false);
                ActivationChangeButton(_muteButton, true);
                ActivationChangeButton(VolumeSlider, true);
                break;
            case ShowUIConfig.None:
                ActivationChangeButton(_inputField, false);
                ActivationChangeButton(_connectWebRTCActionButton, false);
                ActivationChangeButton(_disconnectWebRTCActionButton, false);
                ActivationChangeButton(_startMediaActionButton, false);
                ActivationChangeButton(_muteButton, false);
                ActivationChangeButton(VolumeSlider, false);
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
        _options_menu_open = !_options_menu_open;
        mainHandUIPanel.transform.position = LeftHandController.transform.position + new Vector3(0, 0.2f, 0.3f);
        //_options_menu_open = true;
        mainHandUIPanel.SetActive(_options_menu_open);
        //ChangeUI(prev_ui_config);
        //active_ui_config = prev_ui_config;
        //prev_ui_config = ShowUIConfig.None;
    }

    public void ChangeUIForMenuClose()
    {
        //_options_menu_open = false;
        //mainHandUIPanel.SetActive(_options_menu_open);
        //prev_ui_config = active_ui_config;
        //active_ui_config = ShowUIConfig.None;
        //ChangeUI(ShowUIConfig.None);
    }

    public void ChangeVolume()
    {
        //MediaManager.Instance.ReceiveAudio.volume = VolumeSlider.value;
        MediaManager.Instance.ChangeIncomingAudioVolume(VolumeSlider.Value);
        //MediaManager.Instance.ReceiveAudio.volume = VolSlider.GetComponent<UnityEngine.UI.Slider>().value;
    }

    public void ToggleMuteAudio()
    {
        MediaManager.Instance.ToggleMuteAudio();
        Debug.Log(_muteButtonText.text);
        if (_muteButtonText.text == "Mute Audio")
        {
            _muteButtonText.text = "Unmute Audio";
        }
        else
        {
            _muteButtonText.text = "Mute Audio";
        }

    }
}
