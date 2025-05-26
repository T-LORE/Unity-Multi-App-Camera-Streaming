using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AddressConfigurator
{

    private static string localIP = "192.168.1.126";

    private static string localPort = "1672";


   public static string GetLocalIP()
   {
        /*
                 var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList) {
            if (ip.AddressFamily == AddressFamily.InterNetwork) {
                serverIpv4Address = ip.ToString();
                break;
            }
        }
        */

        //TODO: dynamically get the local IP address
        string ip = localIP;

        return ip;
   }

    public static string GetLocalPort()
    {
        string port = localPort;

        return port;
    }
}
