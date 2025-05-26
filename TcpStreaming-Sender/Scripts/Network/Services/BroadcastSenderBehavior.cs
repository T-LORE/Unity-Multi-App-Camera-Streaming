using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class BroadcastSenderBehavior : WebSocketBehavior
{
    protected override void OnOpen()
    {
        Debug.Log($"[BroadcastSender] Client connected. Session ID: {ID}");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        if (MediaWebsocketServer.Instance != null && MediaWebsocketServer.Instance.Server != null)
        {
            MediaWebsocketServer.Instance.Server.WebSocketServices[$"/{nameof(BroadcastReceiveBehavior)}"].Sessions.Broadcast(e.Data);
        }
        else
        {
            Debug.LogError("[BroadcastSender] MediaWebsocketServer.Instance or its server is null. Cannot broadcast.");
        }
    }

    protected override void OnClose(CloseEventArgs e)
    {
        Debug.Log($"[BroadcastSender] Client disconnected. Code: {e.Code}, Reason: {e.Reason}");
    }

    protected override void OnError(ErrorEventArgs e)
    {
        Debug.LogError($"[BroadcastSender] Error: {e.Message}");
    }
}
