using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using WebSocketSharp;

public class MediaWebsocketClient : MonoBehaviour
{
    public UnityAction OnOpenAction;
    public UnityAction<MessageEventArgs> OnMessageAction;
    public UnityAction<ErrorEventArgs> OnErrorAction;
    public UnityAction<CloseEventArgs> OnCloseAction;

    private WebSocket _ws;
    private string _ip;
    private string _port;
    private string _service;

    public bool ConnectToServer(string ip, string port, string service)
    {
        if (_ws != null && (_ws.IsAlive))
        {
            Debug.LogWarning($"Already connected to ws://{_ip}:{_port}/{_service}");
            return false;
        }

        _ip = ip;
        _port = port;
        _service = service;

        string url = $"ws://{_ip}:{_port}/{_service}";
        Debug.Log($"Attempting to connect to WebSocket server at {url}");
        _ws = new WebSocket(url);

        _ws.OnOpen += OnOpenConnection;
        _ws.OnMessage += OnRecieveMessage;
        _ws.OnError += OnRecieveError;
        _ws.OnClose += OnCloseConenction;

        return TryConnect();
    }

    public bool ReconnectToServer()
    {
        if (string.IsNullOrEmpty(_ip) || string.IsNullOrEmpty(_port) || string.IsNullOrEmpty(_service))
        {
            Debug.LogError("Cannot reconnect: IP, port, or service is not set.");
            return false;
        }

        if (_ws != null && _ws.IsAlive)
        {
            Debug.LogWarning($"Already connected to ws://{_ip}:{_port}/{_service}");
            return true;
        }

        Debug.Log($"Reconnecting to WebSocket server at ws://{_ip}:{_port}/{_service}");
        _ws = new WebSocket($"ws://{_ip}:{_port}/{_service}");

        _ws.OnOpen += OnOpenConnection;
        _ws.OnMessage += OnRecieveMessage;
        _ws.OnError += OnRecieveError;
        _ws.OnClose += OnCloseConenction;

        return TryConnect();
    }

    private bool TryConnect()
    {
        try
        {
            _ws.Connect();

            return true;
        }
        catch (WebSocketException ex)
        {
            Debug.LogError($"WebSocket connection failed: {ex.Message}");
            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Unexpected error during WebSocket connection: {ex.Message}");
            return false;
        }
    }

    public bool DisconnectFromServer()
    {
        if (_ws != null && _ws.IsAlive)
        {
            _ws.Close();
            Debug.Log("Disconnected from WebSocket server.");
            return true;
        }
        else
        {
            Debug.LogWarning("WebSocket is not connected.");
            return false;
        }
    }

    public void SendBytes(byte[] data)
    {
        if (_ws == null || !_ws.IsAlive)
        {
            Debug.LogWarning("Can't send bytes - WebSocket is not connected.");
            return;
        }

        _ws.Send(data);
        Debug.Log($"Sent {data.Length} bytes.");
    }
   
    public void SendString(string message)
    {
        if (_ws != null && _ws.IsAlive)
        {
            Debug.LogWarning("Can't send string - WebSocket is not connected.");
        }

        _ws.Send(message);
        Debug.Log($"Sent message: {message}");
    }

    private void OnOpenConnection(object sender, System.EventArgs e)
    {
        Debug.Log("WebSocket connection opened successfully.");
        OnOpenAction?.Invoke();
    }

    private void OnRecieveMessage(object sender, MessageEventArgs e)
    {
        if (e.IsBinary)
        {
            Debug.LogWarning("Received binary data, which is not supported in this client.");

        }
        else
        {
            Debug.Log($"Received message: {e.Data}");
        }
        OnMessageAction?.Invoke(e);
    }

    private void OnRecieveError(object sender, ErrorEventArgs e)
    {
        Debug.LogError($"WebSocket Error: {e.Message}");
        if (e.Exception != null)
        {
            Debug.LogError($"WebSocket Exception: {e.Exception}");
        }
        OnErrorAction?.Invoke(e);
    }

    private void OnCloseConenction(object sender, CloseEventArgs e)
    {
        Debug.Log($"WebSocket connection closed. Code: {e.Code}, Reason: {e.Reason}");
        _ws = null;
        OnCloseAction?.Invoke(e);
    }

    [ContextMenu("Test connect receiver")]
    public void TestConnectReceiver()
    {
        string ip = AddressConfigurator.GetLocalIP();
        string port = AddressConfigurator.GetLocalPort();
        ConnectToServer(ip, port, nameof(BroadcastReceiveBehavior));
    }

    [ContextMenu("Test connect sender")]
    public void TestConnectSender()
    {
        string ip = AddressConfigurator.GetLocalIP();
        string port = AddressConfigurator.GetLocalPort();
        ConnectToServer(ip, port, nameof(BroadcastSenderBehavior));
    }

    string sendMessage = "Hello from MediaWesocketClient!";
    [ContextMenu("Test Send Message")]
    public void TestSendMessage()
    {
        SendString(sendMessage);
    }

    [ContextMenu("Test Disconnect")]
    public void TestDisconnect()
    {
        DisconnectFromServer();
    }
}
