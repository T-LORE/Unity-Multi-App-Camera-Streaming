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

    private string _ip;
    private string _port;

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
    public bool StartSendingFramesAutoIP()
    {
        if (_sendFrames)
        {
            Debug.LogWarning("Already sending frames.");
            return true;
        }

        _streamStartTime = Time.time;
        _sendFrames = true;
        bool res = StartServerAutoIP();
        if (!res)
        {
            Debug.LogWarning("Failed to start server with auto IP.");
            return false;
        }
        _sendFramesCoroutine = StartCoroutine(SendFramesCoroutine());
        return true;
    }

    public bool StartSendingFrames(string ip, string port)
    {
        if (_sendFrames)
        {
            Debug.LogWarning("Already sending frames.");
            return false;
        }

        _streamStartTime = Time.time;
        _sendFrames = true;
        bool res = StartServer(ip, port);
        if (!res)
        {
            return false;
        }
        _sendFramesCoroutine = StartCoroutine(SendFramesCoroutine());
        return true;
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

        return _ip;
    }

    public string GetPort()
    {
        if (!_sendFrames)
        {
            Debug.LogError("Can't get port: not currently sending frames.");
            return "ERROR";
        }

        return _port;
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
    private bool StartServerAutoIP()
    {
        _ip = AddressConfigurator.GetLocalIP();
        _port = AddressConfigurator.GetLocalPort();
        bool res = false;
        for (int i = 0; i < 10; i++)
        {
            _port = (i == 0) ? _port : (int.Parse(_port) + i).ToString();
            if (StartServer(_ip, _port))
            {
                Debug.Log($"Server started on {_ip}:{_port}");
                res = true;
                break;
            }
            else
            {
                Debug.LogWarning($"Failed to start server on {_ip}:{_port}, retrying...");
                System.Threading.Thread.Sleep(1000); // Wait for a second before retrying
            }
        }
        if (!res)
        {
            Debug.LogWarning("Failed to start server after multiple attempts.");
            return false;
        }
        Debug.Log("Auto IP server started successfully.");
        _mediaWebsocketClient.ConnectToServer(_ip, _port, nameof(BroadcastSenderBehavior));
        return true;
    }

    private bool StartServer(string ip, string port)
    {
        bool res = _mediaWebsocketServer.StartServer(ip, port);
        if (!res)
            return res;
        _ip = ip;
        _port = port;
        _mediaWebsocketClient.ConnectToServer(ip, port, nameof(BroadcastSenderBehavior));
        return res;
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
            Debug.Log("Delay started, sending delay frame.");
            Frame delayFrame = new Frame(Time.time, _delayJpgPic.EncodeToJPG(20));
            while (Time.time - _streamStartTime < streamSettings.Delay)
            {
                SendFrame(delayFrame);
                _framesQueue.Enqueue(PrepareFrame());
                yield return new WaitForSeconds(1.0f / streamSettings.FrameRate);
            }
        }

        Debug.Log($"Starting to send frames at {Time.time} seconds.");
        Frame nextFrame = _framesQueue.Dequeue();
        while (_sendFrames)
        {
            _framesQueue.Enqueue(PrepareFrame());

            if (nextFrame == null)
            {
                nextFrame = _framesQueue.Dequeue();
            }


            SendFrame(nextFrame);
            nextFrame = null;

            yield return new WaitForSeconds(1.0f / streamSettings.FrameRate);
            //Debug.Log($"Sent frame at {Time.time} seconds. {1 / streamSettings.FrameRate}");
        }
    }
}
