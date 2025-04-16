using System.Collections;
using UnityEngine;

public class TextureReceiver : MonoBehaviour
{
    [Header("Network")] 
    public int port = 56666;
    public string ipAddress = "127.0.0.1";
    
    [Header("ReceivedTexture")]
    public Texture2D receivedTexture;
    
    private BytesTcpServer _bytesTcpServer;

    [ContextMenu("Connect")]
    public void Connect()
    {
        if (_bytesTcpServer == null)
            _bytesTcpServer = gameObject.AddComponent<BytesTcpServer>();
        _bytesTcpServer.BeginServer(ipAddress, port, ProcessImageData);
    }

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        Connect();
    }

    private void ProcessImageData(byte[] byteData)
    {
        StartCoroutine(CoProcessImageData(byteData));
    }

    IEnumerator CoProcessImageData(byte[] byteData) {
        if (byteData == null || byteData.Length == 0)
            yield break;
        
        if (receivedTexture == null)
            receivedTexture = new Texture2D(0, 0);

        receivedTexture.LoadImage(byteData);
        yield return null;
    }
}
