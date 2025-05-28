using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ReceiverUIController : MonoBehaviour
{
    [Header("Receiver")]
    [SerializeField] private Receiver _receiver;
    [SerializeField] private float _reconnectionDelaySeconds = 3f;

    [Header("Canvas")]
    [SerializeField] private GameObject _canvasGameObject;

    [Header("Connection status")]
    [SerializeField] private GameObject _statusGameObject;
    [SerializeField] private Text _streamOff;
    [SerializeField] private Text _streamConnectionPregress;
    [SerializeField] private Text _streamOn;

    [Header("Reconnection status")]
    [SerializeField] private Text _streamReconnectionProgress;
    [SerializeField] private Text _reconnectionErrorAlert;

    [Header("Settings")]
    [SerializeField] private GameObject _settingsGameObject;
    [SerializeField] private InputField _IP;
    [SerializeField] private InputField _port;
    [SerializeField] private Text _streamEndedAlert;

    [Header("Stream Status")]
    [SerializeField] private GameObject _streamVideoGameObject;
    [SerializeField] private Text _IPPortValue;
    [SerializeField] private RawImage _video;
    [SerializeField] Texture2D _loadingPic;
    [SerializeField] Texture2D _connectionLostPic;

    [Header("Control buttons")]
    [SerializeField] private GameObject _controlButtonsGameObject;
    [SerializeField] private Button _connectButton;
    [SerializeField] private Button _disconnectButton;

    private Coroutine _reconnectionCoroutine;

    private Panel _currentPanel;
    private bool _isDisconnectedButtonClicked;
    private bool _isReconnecting = false;
    

    enum Panel
    {
        None,
        NotConnected,
        StreamEndedOnServer,
        ConnectionProgress,
        ReconnectionProgress,
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

        _receiver.OnDisconnect += (e) =>
        {
            if (_isReconnecting)
            {
                return;
            }

            if (e.WasClean)
            {
                if (_isDisconnectedButtonClicked)
                {
                    _isDisconnectedButtonClicked = false;
                }
                else
                {
                    SwitchUIPanelTo(Panel.StreamEndedOnServer);
                }
                return;
            }

            if (_isDisconnectedButtonClicked)
            {
                _isDisconnectedButtonClicked = false;
                return;
            }

            Debug.LogError("Connection error occurred. Attempting to reconnect...");

            SwitchUIPanelTo(Panel.ReconnectionProgress);
            _reconnectionCoroutine = StartCoroutine(ConnectionCoroutine());
            
        };


    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            _canvasGameObject.SetActive(!_canvasGameObject.activeSelf);
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
            case Panel.StreamEndedOnServer:
                _streamEndedAlert.gameObject.SetActive(true);

                _statusGameObject.SetActive(true);
                _streamOff.gameObject.SetActive(true);


                _settingsGameObject.SetActive(true);

                _controlButtonsGameObject.gameObject.SetActive(true);
                _connectButton.gameObject.SetActive(true);
                break;
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
                _video.texture = _loadingPic;

                _statusGameObject.SetActive(true);
                _streamConnectionPregress.gameObject.SetActive(true);

                _streamVideoGameObject.SetActive(true);

                _controlButtonsGameObject.gameObject.SetActive(true);
                _disconnectButton.gameObject.SetActive(true);
                break;
            case Panel.ReconnectionProgress:
                _video.texture = _connectionLostPic;
                _statusGameObject.SetActive(true);
                _streamConnectionPregress.gameObject.SetActive(true);

                _streamVideoGameObject.SetActive(true);

                _controlButtonsGameObject.gameObject.SetActive(true);
                _disconnectButton.gameObject.SetActive(true);

                _reconnectionErrorAlert.gameObject.SetActive(true);
                break;
            default:
                break;
        }
    }

    private void ClosePanel(Panel panel)
    {
        switch (panel)
        {
            case Panel.StreamEndedOnServer:
                _streamEndedAlert.gameObject.SetActive(false);

                _statusGameObject.SetActive(false);
                _streamOff.gameObject.SetActive(false);

                _settingsGameObject.SetActive(false);

                _controlButtonsGameObject.gameObject.SetActive(false);
                _connectButton.gameObject.SetActive(false);
                break;
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
            case Panel.ReconnectionProgress:
                _statusGameObject.SetActive(false);
                _streamConnectionPregress.gameObject.SetActive(false);
                _reconnectionErrorAlert.gameObject.SetActive(false);
                _streamVideoGameObject.SetActive(false);
                _controlButtonsGameObject.gameObject.SetActive(false);
                _disconnectButton.gameObject.SetActive(false);
                _reconnectionErrorAlert.gameObject.SetActive(false);
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

        SwitchUIPanelTo(Panel.ConnectionProgress);

        _reconnectionCoroutine = StartCoroutine(ConnectionCoroutine());
        _IPPortValue.text = $"{_receiver.GetConnectedIP()}:{_receiver.GetConnectedPort()}";

    }

    private void DisconnectButtonClicked()
    {
        StopCoroutine(_reconnectionCoroutine);
        _isReconnecting = false;

        _isDisconnectedButtonClicked = true;
        _receiver.Disconnect();

        SwitchUIPanelTo(Panel.NotConnected);
    }

    private IEnumerator ConnectionCoroutine()
    {
        _isReconnecting = true;

        while (_receiver.Status != MediaWebsocketClient.ClientStatus.Connected)
        {
            Debug.Log("Attempting to reconnect...");
            
            _receiver.Connect(_IP.text.Trim(), ushort.Parse(_port.text.Trim()));

            yield return new WaitForSeconds(_reconnectionDelaySeconds);
        }

        _isReconnecting = false;

        if (_receiver.Status == MediaWebsocketClient.ClientStatus.Connected)
        {
            SwitchUIPanelTo(Panel.Connected);
            
        }
        else
        {
            Debug.LogError("Reconnection failed");
            SwitchUIPanelTo(Panel.NotConnected);
        }

    }
}
