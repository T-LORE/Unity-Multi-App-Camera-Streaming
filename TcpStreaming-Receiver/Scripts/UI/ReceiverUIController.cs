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
    [SerializeField] private float _reconnectionDelaySeconds = 5f;
    [SerializeField] private float _maxReconnectionTime = 10f;
    [SerializeField] private int _maxConnectionAttempts = 5;

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
    [SerializeField] private Text _connectionLostAlert;
    

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

    private Coroutine _connectingTimer;
    private float _startReconnectionTime;

    private Panel _currentPanel;
    private bool _isDisconnectedButtonClicked;
    private bool _isConneecting;
    

    enum Panel
    {
        None,
        NotConnected,
        StreamEndedOnServer,
        ConnectionLost,
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
        _receiver.OnConnectionLost += OnConnectionLost;

        _receiver.OnDisconnect += (e) =>
        {
            if (e.WasClean)
            {
                if (_isDisconnectedButtonClicked)
                {
                    _isDisconnectedButtonClicked = false;
                    _connectButton.interactable = true;
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
                _connectButton.interactable = true;
                return;
            }

            if (_isConneecting)
            {
                ConnectButtonClicked();
                return;
            }

            _connectButton.interactable = true;
        };

        _receiver.OnOpen += () =>
        {
            _isConneecting = false;
            SwitchUIPanelTo(Panel.Connected);
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

    private void OnConnectionLost()
    {
        if (_isDisconnectedButtonClicked)
            return;
        Debug.Log("Waiting for signal restore...");

        SwitchUIPanelTo(Panel.ReconnectionProgress);

        _startReconnectionTime = Time.time;
        _connectButton.interactable = false;
        StartCoroutine(ReconnectionTimer());
    }

    private IEnumerator ReconnectionTimer()
    {
        while (Time.time - _startReconnectionTime < _maxReconnectionTime)
        {
            if (!_receiver.IsConnectionLost)
            {
                SwitchUIPanelTo(Panel.Connected);
                yield break;
            }
            yield return new WaitForSeconds(1f);
        }
        SwitchUIPanelTo(Panel.ConnectionLost);

        _receiver.DisconnectAsync();
    }

    private IEnumerator ConnectingTimer()
    {
        yield return new WaitForSeconds(5f);
        if (_isConneecting)
            _receiver.DisconnectAsync();
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
            case Panel.ConnectionLost:
                _connectionLostAlert.gameObject.SetActive(true);

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
            case Panel.ConnectionLost:
                _connectionLostAlert.gameObject.SetActive(false);

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

        _isConneecting = true;
        _connectingTimer = StartCoroutine(ConnectingTimer());
        SwitchUIPanelTo(Panel.ConnectionProgress);

        _receiver.Connect(_IP.text.Trim(), ushort.Parse(_port.text.Trim()));
        _IPPortValue.text = $"{ip}:{port}";

    }

    private void DisconnectButtonClicked()
    {
        StopAllCoroutines();

        _isConneecting = false;

        _isDisconnectedButtonClicked = true;
        
        _receiver.DisconnectAsync();
        _connectButton.interactable = false;

        SwitchUIPanelTo(Panel.NotConnected);
    }

    /*
    private IEnumerator ConnectionCoroutine()
    {
        int attempts = 0;

        _isReconnecting = true;

        while (_receiver.Status != MediaWebsocketClient.ClientStatus.Connected)
        {
            attempts++;

            if (attempts > _maxConnectionAttempts)
            {
                Debug.LogWarning("Max connection attempts reached. Stopping reconnection attempts.");
                SwitchUIPanelTo(Panel.NotConnected);
                _isReconnecting = false;
                yield break;
            }

            Debug.Log("Attempting to reconnect...");
            
            

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
    */

    private void OnDisable()
    {
        _receiver.Disconnect();
        StopAllCoroutines();
    }
}
