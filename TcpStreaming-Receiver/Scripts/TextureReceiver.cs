// TextureReceiver.cs
using System;
using System.Collections;
using System.Net.Sockets;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;

public struct ReceiverSettings
{
    public string ServerIP;
    public int ServerPort;

    public override string ToString()
    {
        return $"ServerIP: {ServerIP}, " +
               $"ServerPort: {ServerPort}";
    }
}

public class TextureReceiver : MonoBehaviour
{
    [Header("Network")]
    public string serverIp = "127.0.0.1";
    public int serverPort = 56666;

    [Header("ReceivedTexture")]
    public Texture2D receivedTexture; // ��������, ������� ����� �����������

    private TcpClient _tcpClient;
    private NetworkStream _stream;
    private Thread _receiveThread;
    public bool _isConnected = false;
    public bool _isTryingToConnect = false;
    public bool _stopReceiveThread = false;

    private byte[] _messageLengthBuffer = new byte[sizeof(int)];
    // private byte[] _frameTimestampBuffer = new byte[sizeof(float)]; // ���� ����� �������� timestamp

    private Queue<byte[]> _receivedFramesQueue = new Queue<byte[]>();
    private object _queueLock = new object();

    public void Connect()
    {
        Loom.Initialize();
        receivedTexture = new Texture2D(2, 2, TextureFormat.RGB24, false); // ��������� ��������
        ConnectToServer();
    }

    public void Disconnect()
    {
        _stopReceiveThread = true; 
        CloseConnection();
    }

    public void SetSettings (ReceiverSettings newSettings)
    {
        serverIp = newSettings.ServerIP;
        serverPort = newSettings.ServerPort;

        if (_isConnected)
        {
            Disconnect();
            ConnectToServer();
        }
    }

    public ReceiverSettings GetSettings()
    {
        ReceiverSettings settings = new ReceiverSettings
        {
            ServerIP = serverIp,
            ServerPort = serverPort
        };

        return settings;
    }

    private void Update()
    {
        // ������������ ���������� ����� � �������� ������
        byte[] frameData = null;
        lock (_queueLock)
        {
            if (_receivedFramesQueue.Count > 0)
            {
                frameData = _receivedFramesQueue.Dequeue();
            }
        }

        if (frameData != null)
        {
            ProcessImageData(frameData);
        }
    }

    private void ConnectToServer()
    {
        if (_isConnected || _isTryingToConnect) return;

        _isTryingToConnect = true;
        _stopReceiveThread = false; // ���������� ���� ��������� ����� ����� �������

        _receiveThread = new Thread(new ThreadStart(ReceiveLoop));
        _receiveThread.IsBackground = true;
        _receiveThread.Start();
    }

    private void ReceiveLoop()
    {
        try
        {
            _tcpClient = new TcpClient();
            Debug.Log($"TextureReceiver: ������� ����������� � {serverIp}:{serverPort}...");
            _tcpClient.Connect(serverIp, serverPort); // ����������� �����
            _stream = _tcpClient.GetStream();
            _isConnected = true;
            _isTryingToConnect = false;
            Debug.Log($"TextureReceiver: ������� ��������� � �������.");

            Loom.QueueOnMainThread(() => { /* �������� ��� �������� �����������, ���� ����� */ });

            while (_isConnected && !_stopReceiveThread)
            {
                // 1. ������ ����� �����
                int bytesRead = 0;
                do
                {
                    int read = _stream.Read(_messageLengthBuffer, bytesRead, _messageLengthBuffer.Length - bytesRead);
                    if (read == 0) throw new Exception("���������� ��������� ��� ������ ����� �����.");
                    bytesRead += read;
                } while (bytesRead < _messageLengthBuffer.Length);
                int frameSize = BitConverter.ToInt32(_messageLengthBuffer, 0);

                if (frameSize <= 0 || frameSize > 10 * 1024 * 1024) // �������� �� ���������� ������ (���� 10MB)
                {
                    Debug.LogError($"TextureReceiver: ������� ������������ ������ �����: {frameSize}. ������ ����������.");
                    throw new Exception("������������ ������ �����.");
                }

                // 2. ������ ��� ����
                byte[] frameData = new byte[frameSize];
                bytesRead = 0;
                do
                {
                    int read = _stream.Read(frameData, bytesRead, frameSize - bytesRead);
                    if (read == 0) throw new Exception("���������� ��������� ��� ������ ������ �����.");
                    bytesRead += read;
                } while (bytesRead < frameSize);

                // 3. ������ ��������� ����� (���� ������ �� ����������)
                // bytesRead = 0;
                // do
                // {
                //    int read = _stream.Read(_frameTimestampBuffer, bytesRead, _frameTimestampBuffer.Length - bytesRead);
                //    if (read == 0) throw new Exception("���������� ��������� ��� ������ timestamp.");
                //    bytesRead += read;
                // } while (bytesRead < _frameTimestampBuffer.Length);
                // float frameTimestamp = BitConverter.ToSingle(_frameTimestampBuffer, 0);

                // ��������� ���� � ������� ��� ��������� � �������� ������
                lock (_queueLock)
                {
                    // ������������ ������ �������, ����� �� ���� ������� �������� ��� �����
                    if (_receivedFramesQueue.Count < 5) // ��������, �� ������ 5 ������ � �������
                    {
                        _receivedFramesQueue.Enqueue(frameData);
                    }
                    else
                    {
                        // Debug.LogWarning("TextureReceiver: ������� ������ �����������, ���� ��������.");
                    }
                }
            }
        }
        catch (ThreadAbortException)
        {
            Debug.Log("TextureReceiver: ����� ReceiveLoop �������.");
        }
        catch (Exception e)
        {
            Debug.LogError($"TextureReceiver: ������ � ReceiveLoop ��� ���������� ��������: {e.Message}");
        }
        finally
        {
            CloseConnection();
            _isTryingToConnect = false; // ��������� ��������� ������� �����������
            // ����� �������� ������ ��������������� ��������������� ����� ����� ��������� �����
            if (!_stopReceiveThread) // ���� ����� �� ��� ���������� ���������
            {
                Loom.QueueOnMainThread(() => StartCoroutine(ReconnectAfterDelay(5.0f)));
            }
        }
    }

    IEnumerator ReconnectAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!_isConnected && !_isTryingToConnect && enabled) // ���������, ��� ��������� ��� ��� �������
        {
            Debug.Log("TextureReceiver: ������� ���������������...");
            ConnectToServer();
        }
    }

    private void ProcessImageData(byte[] byteData)
    {
        if (byteData == null || byteData.Length == 0) return;

        // LoadImage ������������� �������� ������ ��������, ���� ��� ����������
        if (receivedTexture == null)
        {
            // LoadImage �������, ����� �������� ���� �� null, �� ����� ���� 0x0
            receivedTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        }

        bool success = receivedTexture.LoadImage(byteData); // ��������� JPG ������
        if (!success)
        {
            Debug.LogError("TextureReceiver: �� ������� ��������� ����������� �� ������.");
        }
    }

    private void CloseConnection()
    {
        _isConnected = false;
        if (_stream != null)
        {
            _stream.Close();
            _stream = null;
        }
        if (_tcpClient != null)
        {
            _tcpClient.Close();
            _tcpClient = null;
        }
        Debug.Log("TextureReceiver: ���������� �������.");
    }

    void OnDestroy()
    {
        _stopReceiveThread = true; // ������ ������ ������������
        CloseConnection();
        if (_receiveThread != null && _receiveThread.IsAlive)
        {
            try
            {
                _receiveThread.Join(500); // ���� ������ ������� ������� �� ����������
                if (_receiveThread.IsAlive) _receiveThread.Abort(); // �������������, ���� �� ����������
            }
            catch (Exception e) { Debug.LogError($"Error stopping receive thread: {e.Message}"); }
        }
        if (receivedTexture != null) Destroy(receivedTexture);
    }
}