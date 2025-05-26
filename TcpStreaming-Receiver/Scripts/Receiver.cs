using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class Receiver : MonoBehaviour
{
    [SerializeField]
    private MediaWebsocketClient _mediaWebsocketClient;

    [SerializeField]
    private RawImage _displayImage;

    private FrameDecoder _frameDecoder;

    private Texture2D _receivedTexture;

    public Texture2D ReceivedTexture
    {
        get { return _receivedTexture; }
    }

    public MediaWebsocketClient.ClientStatus Status
    {
        get { return _mediaWebsocketClient.Status; }
    }

    private void Start()
    {
        if (_mediaWebsocketClient == null)
        {
            Debug.LogError("MediaWebsocketClient is not assigned.");
            return;
        }

        if (_displayImage == null)
        {
            Debug.LogWarning("Display Image is not assigned.");
            return;
        }

        _mediaWebsocketClient.OnMessageAction += FrameRecieved;
        _frameDecoder = new FrameDecoder(new Vector2(_displayImage.uvRect.width, _displayImage.uvRect.height));
    }

    public void Connect(string ip, int port)
    {
        _mediaWebsocketClient.ConnectToServer(ip, port.ToString(), nameof(BroadcastReceiveBehavior));
    }

    public string GetConnectedIP()
    {
        if (_mediaWebsocketClient.Status == MediaWebsocketClient.ClientStatus.Disconnected)
        {
            Debug.LogError("Cannot get connected IP: Client is disconnected.");
            return string.Empty;
        }
        return _mediaWebsocketClient.IP;
    }
    public string GetConnectedPort()
    {
        if (_mediaWebsocketClient.Status == MediaWebsocketClient.ClientStatus.Disconnected)
        {
            Debug.LogError("Cannot get connected port: Client is disconnected.");
            return string.Empty;
        }
        return _mediaWebsocketClient.Port;
    }

    public void Disconnect()
    {
        _mediaWebsocketClient.DisconnectFromServer();
    }

    private void FrameRecieved(MessageEventArgs args)
    {
        byte[] imageData = args.RawData;    

        Texture2D frameTexture = _frameDecoder.DecodeFrameData(imageData);
        
        if (frameTexture == null)
        {
            Debug.LogError("Failed to decode frame data.");
            return;
        }

        _receivedTexture = frameTexture;

        if (_displayImage != null)
        {
            _displayImage.texture = frameTexture;
        }
    }
}
