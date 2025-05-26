using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class ReceiverUIController : MonoBehaviour
{
    [Header("Receiver")]
    [SerializeField] private Receiver _receiver;

    [Header("Canvas")]
    [SerializeField] private GameObject _canvasGameObject;

    [Header("Connection status")]
    [SerializeField] private GameObject _statusGameObject;
    [SerializeField] private Text _streamOff;
    [SerializeField] private Text _streamConnectionPregress;
    [SerializeField] private Text _streamOn;

    [Header("Settings")]
    [SerializeField] private GameObject _settingsGameObject;
    [SerializeField] private InputField _IP;
    [SerializeField] private InputField _port;

    [Header("Stream Status")]
    [SerializeField] private GameObject _streamVideoGameObject;
    [SerializeField] private Text _IPPortValue;
    [SerializeField] private RawImage _video;

    [Header("Control buttons")]
    [SerializeField] private GameObject _controlButtonsGameObject;
    [SerializeField] private Button _connectButton;
    [SerializeField] private Button _disconnectButton;

    private Panel _currentPanel;
    enum Panel
    {
        None,
        NotConnected,
        ConnectionProgress,
        Connected
    }

    struct ValidationError
    {
        public string Message;
    }


    private void Start()
    {
        if (_receiver == null)
        {
            Debug.LogWarning("Receiver is not set");
        }

        _currentPanel = Panel.None;
        SwitchUIPanelTo(Panel.NotConnected);
        _connectButton.onClick.AddListener(ConnectButtonClicked);
        _disconnectButton.onClick.AddListener(DisconnectButtonClicked);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            _canvasGameObject.SetActive(!_canvasGameObject.activeSelf);
        }

        if (_currentPanel == Panel.ConnectionProgress)
        {
            if (_receiver.Status == MediaWebsocketClient.ClientStatus.Connected)
            {
                SwitchUIPanelTo(Panel.Connected);
            }
        }

        if (_receiver != null && _currentPanel == Panel.Connected)
        {
            _video.texture = _receiver.ReceivedTexture;
        }
    }

    private void SwitchUIPanelTo(Panel newPanel)
    {
        ClosePanel(_currentPanel);
        OpenPanel(newPanel);
        _currentPanel = newPanel;
    }

    private void OpenPanel(Panel panel)
    {
        switch (panel)
        {
            case Panel.NotConnected:
                _statusGameObject.SetActive(true);
                _streamOff.gameObject.SetActive(true);


                _settingsGameObject.SetActive(true);

                _controlButtonsGameObject.gameObject.SetActive(true);
                _connectButton.gameObject.SetActive(true);
                break;
            case Panel.Connected:
                _statusGameObject.SetActive(true);
                _streamOn.gameObject.SetActive(true);

                _streamVideoGameObject.SetActive(true);

                _controlButtonsGameObject.gameObject.SetActive(true);
                _disconnectButton.gameObject.SetActive(true);
                break;
            case Panel.ConnectionProgress:
                _statusGameObject.SetActive(true);
                _streamConnectionPregress.gameObject.SetActive(true);

                _streamVideoGameObject.SetActive(true);

                _controlButtonsGameObject.gameObject.SetActive(true);
                _disconnectButton.gameObject.SetActive(true);
                break;
            default:
                break;
        }
    }

    private void ClosePanel(Panel panel)
    {
        switch (panel)
        {
            case Panel.NotConnected:
                _statusGameObject.SetActive(false);
                _streamOff.gameObject.SetActive(false);

                _settingsGameObject.SetActive(false);

                _controlButtonsGameObject.gameObject.SetActive(false);
                _connectButton.gameObject.SetActive(false);
                break;
            case Panel.Connected:
                _statusGameObject.SetActive(false);
                _streamOn.gameObject.SetActive(false);

                _streamVideoGameObject.SetActive(false);

                _controlButtonsGameObject.gameObject.SetActive(false);
                _disconnectButton.gameObject.SetActive(false);
                break;
            case Panel.ConnectionProgress:
                _statusGameObject.SetActive(false);
                _streamConnectionPregress.gameObject.SetActive(false);

                _streamVideoGameObject.SetActive(false);

                _controlButtonsGameObject.gameObject.SetActive(false);
                _disconnectButton.gameObject.SetActive(false);
                break;
            case Panel.None:
                _statusGameObject.SetActive(false);
                _streamOff.gameObject.SetActive(false);
                _streamOn.gameObject.SetActive(false);

                _settingsGameObject.SetActive(false);

                _streamVideoGameObject.SetActive(false);

                _controlButtonsGameObject.gameObject.SetActive(false);
                _connectButton.gameObject.SetActive(false);
                _disconnectButton.gameObject.SetActive(false);
                break;
        }
    }

    private List<ValidationError> ValidateAddress()
    {
        string ip = _IP.text.Trim();
        string port = _port.text.Trim();

        var errors = new List<ValidationError>();

        if (!Regex.IsMatch(ip, @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$"))
        {
            errors.Add(new ValidationError { Message = "Invalid IP adress" });
        }
        
        if (!Regex.IsMatch(port, @"^\d{1,5}$") || !ushort.TryParse(port, out ushort portValue) || portValue == 0)
        {
            errors.Add(new ValidationError { Message = "Invalid port" });
        }

        return errors;
    }

    private void ConnectButtonClicked()
    {
        if (ValidateAddress().Count > 0)
        {
            foreach (var error in ValidateAddress())
            {
                Debug.LogError(error.Message);
            }
            return;
        }

        string ip = _IP.text.Trim();
        int port = ushort.Parse(_port.text.Trim());
        _receiver.Connect(ip, port);
        _IPPortValue.text = $"{_receiver.GetConnectedIP()}:{_receiver.GetConnectedPort()}";

        SwitchUIPanelTo(Panel.ConnectionProgress);
    }

    private void DisconnectButtonClicked()
    {
        _receiver.Disconnect();

        SwitchUIPanelTo(Panel.NotConnected);
    }
}
