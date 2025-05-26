using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp.Server;

public class MediaWebsocketServer : MonoBehaviour
{
    public static MediaWebsocketServer Instance { get; private set; }

    private WebSocketServer _webSocketServer;
    public WebSocketServer Server => _webSocketServer;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;          
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartServer(string ip, string port)
    {
        if (_webSocketServer != null && _webSocketServer.IsListening)
        {
            Debug.LogWarning("Server is already running.");
            return;
        }

        _webSocketServer = new WebSocketServer($"ws://{ip}:{port}");

        _webSocketServer.AddWebSocketService<BroadcastSenderBehavior>($"/{nameof(BroadcastSenderBehavior)}");
        _webSocketServer.AddWebSocketService<BroadcastReceiveBehavior>($"/{nameof(BroadcastReceiveBehavior)}");

        _webSocketServer.Start();

        if (_webSocketServer.IsListening)
        {
            Debug.Log($"WebSocket server started at ws://{ip}:{port} " +
                $"- Senders connect to: /{nameof(BroadcastSenderBehavior)} " +
                $" - Receivers connect to: /{nameof(BroadcastReceiveBehavior)}");
        }
        else
        {
            Debug.LogError("WebSocket server FAILED to start.");
        }
    }

    public void StopServer()
    {
        if (_webSocketServer != null && _webSocketServer.IsListening)
        {
            _webSocketServer.Stop();
            Debug.Log("WebSocket server stopped.");
            _webSocketServer = null;
        }
        else
        {
            Debug.LogWarning("WebSocket server is not running or already stopped.");
        }
    }

    void OnDestroy()
    {
        StopServer();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    [ContextMenu("Test Start Server")]
    public void TestStartServer()
    {
        string ip = AddressConfigurator.GetLocalIP();
        string port = AddressConfigurator.GetLocalPort();
        StartServer(ip, port);
    }

    [ContextMenu("Test Stop Server")]
    public void TestStopServer()
    {
        StopServer();
    }
}

