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

    public bool StartServer(string ip, string port)
    {
        if (_webSocketServer != null && _webSocketServer.IsListening)
        {
            Debug.LogWarning("Server is already running.");
            return false;
        }
        try
        {
            Debug.Log($"Trying to create server on: ws://{ip}:{port}");
            _webSocketServer = new WebSocketServer($"ws://{ip}:{port}");
        } catch (System.Exception ex)
        {
            Debug.LogError($"Error when creating server: {ex.Message}");
            _webSocketServer = null;
            return false;
        }

        _webSocketServer.AddWebSocketService<BroadcastSenderBehavior>($"/{nameof(BroadcastSenderBehavior)}");
        _webSocketServer.AddWebSocketService<BroadcastReceiveBehavior>($"/{nameof(BroadcastReceiveBehavior)}");

        try
        {
            Debug.Log($"Trying to start server on: ws://{ip}:{port}");

            _webSocketServer.Start();
        } catch (System.Exception ex)
        {
            Debug.LogError($"Error when starting server: {ex.Message}");
            _webSocketServer = null;
            return false;
        }


        if (_webSocketServer.IsListening)
        {
            Debug.Log($"WebSocket server started at ws://{ip}:{port} " +
                $"- Senders connect to: /{nameof(BroadcastSenderBehavior)} " +
                $" - Receivers connect to: /{nameof(BroadcastReceiveBehavior)}");
        }
        else
        {
            Debug.LogError("WebSocket server FAILED to start.");
            _webSocketServer = null;
            return false;
        }

        return true;
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

