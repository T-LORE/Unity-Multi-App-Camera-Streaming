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

    [SerializeField]
    private Texture2D _delayJpgPic;

    private TextureCapturer _textureCapturer;

    private TextureEncoder _textureEncoder;

    private WebsocketTextureSender _websocketTextureSender;

    private FrameDecoder _frameDecoder;

    private bool _sendFrames = false;

    private Coroutine _sendFramesCoroutine;

    private FramesQueue _framesQueue;

    private float _streamStartTime;
    public float StreamStartTime
    {
        get { return _streamStartTime; }
    }

    private void Start()
    {
        _textureCapturer = new TextureCapturer(_targetCamera, streamSettings);
        _textureEncoder = new TextureEncoder(streamSettings);
        _frameDecoder = new FrameDecoder(new Vector2(2,2));
        _framesQueue = new FramesQueue();

    }

    [ContextMenu("update settings")]
    private void TestUpdateSettings()
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
    private void SendFrame(Frame frame)
    {
        _mediaWebsocketClient.SendBytes(frame.Data);

        if (_displayImage == null)
        {
            return;
        }

        //Debug
        Texture2D decodedTextureForDisplay = _frameDecoder.DecodeFrameData(frame.Data);

        if (decodedTextureForDisplay != null)
        {

            if (_displayImage.texture != decodedTextureForDisplay) 
            {
                _displayImage.texture = decodedTextureForDisplay;
            }
        }
        else
        {
            Debug.LogWarning("Debug: decodedTextureForDisplay is null after attempting to decode.");
        }
    }

    private Frame PrepareFrame()
    {
        var texture = _textureCapturer.CaptureFrame();
        var encodedTexture = _textureEncoder.EncodeFrame(texture, _textureCapturer);
        return new Frame(Time.time, encodedTexture);
    }

    [ContextMenu("Start Sending Frames")]
    public void StartSendingFrames()
    {
        if (_sendFrames)
        {
            Debug.LogWarning("Already sending frames.");
            return;
        }

        _streamStartTime = Time.time;
        _sendFrames = true;
        StartServer();
        _sendFramesCoroutine = StartCoroutine(SendFramesCoroutine());
    }

    [ContextMenu("Stop Sending Frames")]
    public void StopSendingFrames()
    {
        if (!_sendFrames)
        {
            Debug.LogWarning("Not currently sending frames.");
            return;
        }

        _sendFrames = false;
        StopServer();
        StopCoroutine(_sendFramesCoroutine);
        _framesQueue.Clear();
    }

    public string GetIP()
    {
        if (!_sendFrames)
        {
            Debug.LogError("Can't get ip: not currently sending frames.");
            return "ERROR";
        }

        return AddressConfigurator.GetLocalIP();
    }

    public string GetPort()
    {
        if (!_sendFrames)
        {
            Debug.LogError("Can't get port: not currently sending frames.");
            return "ERROR";
        }

        return AddressConfigurator.GetLocalPort();
    }

    public int GetRecieversAmount()
    {
        return _mediaWebsocketServer.GetConnectedReceiversCount();

    }

    public float GetAverageBytesPerSecond(float timeRangeSeconds)
    {
        int receiversCount = _mediaWebsocketServer.GetConnectedReceiversCount();
        if (receiversCount == 0)
        {
            return 0f;
        }


        return _mediaWebsocketClient.byteLogger.GetAverage(timeRangeSeconds) * receiversCount;
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
        if (streamSettings.Delay > 0.2)
        {
            Frame delayFrame = new Frame(Time.time, _delayJpgPic.EncodeToJPG(100));
            while (Time.time - _streamStartTime < streamSettings.Delay)
            {
                SendFrame(delayFrame);
                yield return new WaitForSeconds(0.1f);
            }
        }

        Frame nextFrame = null;

        while (_sendFrames)
        {
            _framesQueue.Enqueue(PrepareFrame());

            if (nextFrame == null)
            {
                nextFrame = _framesQueue.Dequeue();
            }

            if (Time.time - nextFrame.Time >= streamSettings.Delay)
            {
                SendFrame(nextFrame);
                nextFrame = null;
            }
            yield return new WaitForSeconds(1.0f / streamSettings.FrameRate);
            //Debug.Log($"Sent frame at {Time.time} seconds. {1 / streamSettings.FrameRate}");
        }
    }
}
