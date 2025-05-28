using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public static class AddressConfigurator
{

    private static string localIP = "127.0.0.1";

    private static string localPort = "5555";


   public static string GetLocalIP()
   {
        string ip = localIP;

        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ips in host.AddressList) {
            if (ips.AddressFamily == AddressFamily.InterNetwork) {
                ip = ips.ToString();
                break;
            }
        }

        return ip;
   }

    public static string GetLocalPort()
    {
        string port = localPort;

        return port;
    }
}
