using System;
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
        try
        {
            // Логируем, что получили и какие данные
            string dataType = e.IsText ? "Text" : (e.IsBinary ? "Binary" : "Unknown");
            string dataPreview = e.IsText ? e.Data : (e.IsBinary ? $"Binary data, Length: {e.RawData.Length}" : "N/A");
            Debug.Log($"[BroadcastSender] Received message. Type: {dataType}. Preview: '{dataPreview}'. Attempting to broadcast...");

            if (MediaWebsocketServer.Instance == null)
            {
                Debug.LogError("[BroadcastSender] MediaWebsocketServer.Instance is NULL. Cannot broadcast.");
                return;
            }

            var serverInstance = MediaWebsocketServer.Instance.Server;
            if (serverInstance == null)
            {
                Debug.LogError("[BroadcastSender] MediaWebsocketServer.Instance.Server (WebSocketServer) is NULL. Cannot broadcast.");
                return;
            }

            var services = serverInstance.WebSocketServices;
            if (services == null)
            {
                Debug.LogError("[BroadcastSender] WebSocketServices collection is NULL. Cannot broadcast.");
                return;
            }

            WebSocketServiceHost receiveServiceHost;
            if (services.TryGetServiceHost($"/{nameof(BroadcastReceiveBehavior)}", out receiveServiceHost) && receiveServiceHost != null)
            {
                if (receiveServiceHost.Sessions == null)
                {
                    Debug.LogError($"[BroadcastSender] Sessions collection for {nameof(BroadcastReceiveBehavior)} service is NULL. Cannot broadcast.");
                    return;
                }

                Debug.Log($"[BroadcastSender] Found {nameof(BroadcastReceiveBehavior)} service. Number of connected receivers: {receiveServiceHost.Sessions.Count}. Broadcasting now...");

                if (e.IsText)
                {
                    receiveServiceHost.Sessions.Broadcast(e.Data);
                }
                else if (e.IsBinary)
                {
                    // Убедимся, что e.RawData не null, хотя это маловероятно, если e.IsBinary true
                    if (e.RawData != null)
                    {
                        receiveServiceHost.Sessions.Broadcast(e.RawData);
                    }
                    else
                    {
                        Debug.LogWarning("[BroadcastSender] Message is binary, but RawData is null. Not broadcasting.");
                    }
                }
                else
                {
                    Debug.LogWarning("[BroadcastSender] Message type is neither Text nor Binary. Cannot determine how to broadcast.");
                }
                Debug.Log("[BroadcastSender] Broadcast call completed.");
            }
            else
            {
                Debug.LogError($"[BroadcastSender] Service {nameof(BroadcastReceiveBehavior)} NOT FOUND or its host is null. Cannot broadcast.");
                string availablePaths = "Available service paths: ";
                foreach (var path in services.Paths)
                {
                    availablePaths += path + " | ";
                }
                Debug.Log(availablePaths);
            }
        }
        catch (Exception ex)
        {
            // Это самый важный лог для диагностики!
            Debug.LogError($"[BroadcastSender] --- EXCEPTION CAUGHT IN OnMessage ---");
            Debug.LogError($"[BroadcastSender] Exception Type: {ex.GetType().FullName}");
            Debug.LogError($"[BroadcastSender] Exception Message: {ex.Message}");
            Debug.LogError($"[BroadcastSender] Stack Trace: \n{ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Debug.LogError($"[BroadcastSender] --- Inner Exception ---");
                Debug.LogError($"[BroadcastSender] Inner Exception Type: {ex.InnerException.GetType().FullName}");
                Debug.LogError($"[BroadcastSender] Inner Exception Message: {ex.InnerException.Message}");
                Debug.LogError($"[BroadcastSender] Inner Stack Trace: \n{ex.InnerException.StackTrace}");
            }
            Debug.LogError($"[BroadcastSender] --- END OF EXCEPTION DETAILS ---");
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
