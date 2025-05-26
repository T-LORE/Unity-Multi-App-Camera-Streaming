using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class BroadcastReceiveBehavior : WebSocketBehavior
{
    protected override void OnOpen()
    {
        Debug.Log($"[Receiver] Client connected. Session ID: {ID} to receive broadcasts.");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        Debug.Log($"[Receiver] Received a direct message (should not happen often in broadcast scenario): {e.Data}");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        Debug.Log($"[Receiver] Client disconnected. Code: {e.Code}, Reason: {e.Reason}");
    }

    protected override void OnError(ErrorEventArgs e)
    {
        Debug.LogError($"[Receiver] Error: {e.Message}");
    }
}
