using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Sender : MonoBehaviour
{
    [Header("Capture Settings")]
    public StreamSettings streamSettings;

    [Header("Camera Settings")]
    [SerializeField]
    private Camera _targetCamera;
    [SerializeField]
    private RawImage _displayImage;

    [SerializeField]
    private MediaWebsocketServer _mediaWebsocketServer;

    [SerializeField]
    private MediaWebsocketClient _mediaWebsocketClient;

    private TextureCapturer _textureCapturer;

    private TextureEncoder _textureEncoder;

    private WebsocketTextureSender _websocketTextureSender;

    private FrameDecoder _frameDecoder;

    private bool _sendFrames = false;

    private Coroutine _sendFramesCoroutine;

    private void Start()
    {
        _textureCapturer = new TextureCapturer(_targetCamera, streamSettings);
        _textureEncoder = new TextureEncoder(streamSettings);
        _frameDecoder = new FrameDecoder(new Vector2(2,2));

    }

    [ContextMenu("update settings")]
    private void UpdateSettings()
    {
        UpdateSettings(streamSettings);
    }
    
    public void UpdateSettings(StreamSettings newSettings)
    {
        if (newSettings == null)
        {
            Debug.LogError("UpdateSettings: newSettings is null.");
            return;
        }

        if (_textureCapturer == null)
        {
            Debug.LogError("UpdateSettings: TextureCapturer is not initialized."); 
        }


        if (_textureEncoder == null)
        {
            Debug.LogError("UpdateSettings: TextureEncoder is not initialized."); 
        }

        streamSettings = newSettings;
        _textureCapturer = new TextureCapturer(_targetCamera, streamSettings);
        _textureEncoder = new TextureEncoder(streamSettings);
    }


    [ContextMenu("Send Frame")]
    private void SendFrame()
    {
        var texture = _textureCapturer.CaptureFrame();
        var encodedTexture = _textureEncoder.EncodeFrame(texture, _textureCapturer);
        _mediaWebsocketClient.SendBytes(encodedTexture);

        if (_displayImage == null)
        {
            return;
        }
        //Debug
        Texture2D decodedTextureForDisplay = _frameDecoder.DecodeFrameData(encodedTexture);

        if (decodedTextureForDisplay != null)
        {

            if (_displayImage.texture != decodedTextureForDisplay) 
            {
                _displayImage.texture = decodedTextureForDisplay;
            }
             _displayImage.SetNativeSize();
        }
        else
        {
            Debug.LogWarning("Debug: decodedTextureForDisplay is null after attempting to decode.");
        }
    }

    [ContextMenu("Start Sending Frames")]
    private void StartSendingFrames()
    {
        if (_sendFrames)
        {
            Debug.LogWarning("Already sending frames.");
            return;
        }

        _sendFrames = true;
        _sendFramesCoroutine = StartCoroutine(SendFramesCoroutine());
    }

    [ContextMenu("Stop Sending Frames")]
    private void StopSendingFrames()
    {
        if (!_sendFrames)
        {
            Debug.LogWarning("Not currently sending frames.");
            return;
        }

        _sendFrames = false;
        StopCoroutine(_sendFramesCoroutine);
    }

    [ContextMenu("Start Server")]
    private void StartServer()
    {
        string ip = AddressConfigurator.GetLocalIP();
        string port = AddressConfigurator.GetLocalPort();
        _mediaWebsocketServer.StartServer(ip, port);
        _mediaWebsocketClient.ConnectToServer(ip, port, nameof(BroadcastSenderBehavior));
    }

    [ContextMenu("Stop Server")]
    private void StopServer()
    {
        _mediaWebsocketClient.DisconnectFromServer();
        _mediaWebsocketServer.StopServer();
    }

    private IEnumerator SendFramesCoroutine()
    {
        while (_sendFrames)
        {
            SendFrame();
            yield return new WaitForSeconds(1.0f / streamSettings.FrameRate);
            Debug.Log($"Sent frame at {Time.time} seconds. {1 / streamSettings.FrameRate}");
        }
    }
}
