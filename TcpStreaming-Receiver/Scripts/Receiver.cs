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

    private void Start()
    {
        if (_mediaWebsocketClient == null)
        {
            Debug.LogError("MediaWebsocketClient is not assigned.");
            return;
        }

        if (_displayImage == null)
        {
            Debug.LogError("Display Image is not assigned.");
            return;
        }

        _mediaWebsocketClient.OnMessageAction += FrameRecieved;
        _frameDecoder = new FrameDecoder(new Vector2(640, 480));
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

        _displayImage.texture = frameTexture;
        _displayImage.SetNativeSize(); 

    }
}
