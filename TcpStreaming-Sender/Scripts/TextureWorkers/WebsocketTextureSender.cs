using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WebsocketTextureSender
{
    private MediaWebsocketServer _websocketServer;
    private MediaWebsocketClient _websocketClient;

    public WebsocketTextureSender(MediaWebsocketServer websocketServer, MediaWebsocketClient websocketClient)
    {
        _websocketServer = websocketServer;
        _websocketClient = websocketClient;
    }

    public bool SendEncodedFrame(byte[] encodedFrame)
    {     
        _websocketClient.SendBytes(encodedFrame);
        return true;
    }


}
