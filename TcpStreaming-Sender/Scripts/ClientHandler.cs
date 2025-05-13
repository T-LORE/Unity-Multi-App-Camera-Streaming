// ClientHandler.cs
using System;
using System.Net.Sockets;
using UnityEngine;
using System.Threading; // ��� Interlocked

public class ClientHandler
{
    public TcpClient TcpClient { get; private set; }
    private NetworkStream _stream;
    private TextureSenderServer _server; // ������ �� ������� ������

    private readonly byte[] _messageLengthBuffer = new byte[sizeof(int)]; // ��� �������� ����� �����
    private readonly byte[] _frameTimestampBuffer = new byte[sizeof(float)]; // ��� �������� ��������� ����� �����

    private bool _isSending = false;
    private object _sendLock = new object(); // ��� ������������� ������� � _isSending

    public bool IsConnected
    {
        get
        {
            try
            {
                return TcpClient != null && TcpClient.Client != null && TcpClient.Client.Connected;
            }
            catch
            {
                return false;
            }
        }
    }

    public ClientHandler(TcpClient client, TextureSenderServer server)
    {
        TcpClient = client;
        _server = server;
        _stream = TcpClient.GetStream();

        // ��������� ���� ������ �� ������� (���� ����� ���-�� �� ���� ��������, ��������, ACK ��� ����������)
        // ���� ��� ������� ��� ��� ��������, ���� ����������� �������� ����� �� �������.
        // Loom.RunAsync(ReadLoop); 
    }

    // ����� ��� �������� ������ ����� ����� �������
    public void SendFrame(byte[] frameData, float frameTimestamp)
    {
        if (!IsConnected || frameData == null || frameData.Length == 0)
        {
            return;
        }

        // ������������� ������������� �������� ���������� ������ ����� �������,
        // ���� ���������� �������� ��� �� ���������.
        lock (_sendLock)
        {
            if (_isSending)
            {
                // Debug.LogWarning($"ClientHandler {TcpClient.Client.RemoteEndPoint}: ���������� �������� ��� �� ���������. ������� �����.");
                return; // ���������� ����, ���� ��� ���� ��������
            }
            _isSending = true;
        }

        Loom.RunAsync(() =>
        {
            try
            {
                // 1. ���������� ����� ����� (4 ����� int)
                BitConverter.GetBytes(frameData.Length).CopyTo(_messageLengthBuffer, 0);
                _stream.Write(_messageLengthBuffer, 0, _messageLengthBuffer.Length);

                // 2. ���������� ��� ����
                _stream.Write(frameData, 0, frameData.Length);

                // 3. ���������� ��������� ����� ����� (4 ����� float)
                // BitConverter.GetBytes(frameTimestamp).CopyTo(_frameTimestampBuffer, 0);
                // _stream.Write(_frameTimestampBuffer, 0, _frameTimestampBuffer.Length);

                // Debug.Log($"ClientHandler {TcpClient.Client.RemoteEndPoint}: ���� {frameData.Length} ���� ���������.");
            }
            catch (Exception e)
            {
                Debug.LogError($"ClientHandler {TcpClient.Client.RemoteEndPoint}: ������ ��� �������� ������: {e.Message}");
                CloseConnection();
            }
            finally
            {
                lock (_sendLock)
                {
                    _isSending = false;
                }
            }
        });
    }

    // ���� ������ �� ������� (������, ���� ����� ����� �������� �����)
    /*
    private void ReadLoop()
    {
        try
        {
            while (IsConnected)
            {
                // ������ ������ ������ �� �������
                // ��������, ��������� ������������� ��� ����������
                // byte[] buffer = new byte[1024];
                // int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                // if (bytesRead == 0)
                // {
                //    // ������ ����������
                //    break;
                // }
                // ProcessClientData(buffer, bytesRead);
                Thread.Sleep(10); // ��������� ��������, ����� �� ��������� CPU
            }
        }
        catch (Exception e)
        {
            Debug.Log($"ClientHandler {TcpClient?.Client?.RemoteEndPoint}: ������ � ����� ������ ��� ������ ����������: {e.Message}");
        }
        finally
        {
            CloseConnection();
        }
    }
    */

    public void CloseConnection()
    {
        if (TcpClient != null)
        {
            Debug.Log($"ClientHandler: �������� ���������� � {TcpClient.Client?.RemoteEndPoint}");
            TcpClient.Close();
            TcpClient = null;
        }
        _server.RemoveClient(this); // �������� ������� ������� ���� ����������
    }
}