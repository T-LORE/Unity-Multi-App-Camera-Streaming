// ClientHandler.cs
using System;
using System.Net.Sockets;
using UnityEngine;
using System.Threading; // Для Interlocked

public class ClientHandler
{
    public TcpClient TcpClient { get; private set; }
    private NetworkStream _stream;
    private TextureSenderServer _server; // Ссылка на главный сервер

    private readonly byte[] _messageLengthBuffer = new byte[sizeof(int)]; // Для отправки длины кадра
    private readonly byte[] _frameTimestampBuffer = new byte[sizeof(float)]; // Для отправки временной метки кадра

    private bool _isSending = false;
    private object _sendLock = new object(); // Для синхронизации доступа к _isSending

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

        // Запускаем цикл чтения от клиента (если нужно что-то от него получать, например, ACK или статистику)
        // Пока что оставим это для будущего, если понадобится обратная связь от клиента.
        // Loom.RunAsync(ReadLoop); 
    }

    // Метод для отправки данных кадра этому клиенту
    public void SendFrame(byte[] frameData, float frameTimestamp)
    {
        if (!IsConnected || frameData == null || frameData.Length == 0)
        {
            return;
        }

        // Предотвращаем одновременную отправку нескольких кадров этому клиенту,
        // если предыдущая отправка еще не завершена.
        lock (_sendLock)
        {
            if (_isSending)
            {
                // Debug.LogWarning($"ClientHandler {TcpClient.Client.RemoteEndPoint}: Предыдущая отправка еще не завершена. Пропуск кадра.");
                return; // Пропускаем кадр, если уже идет отправка
            }
            _isSending = true;
        }

        Loom.RunAsync(() =>
        {
            try
            {
                // 1. Отправляем длину кадра (4 байта int)
                BitConverter.GetBytes(frameData.Length).CopyTo(_messageLengthBuffer, 0);
                _stream.Write(_messageLengthBuffer, 0, _messageLengthBuffer.Length);

                // 2. Отправляем сам кадр
                _stream.Write(frameData, 0, frameData.Length);

                // 3. Отправляем временную метку кадра (4 байта float)
                // BitConverter.GetBytes(frameTimestamp).CopyTo(_frameTimestampBuffer, 0);
                // _stream.Write(_frameTimestampBuffer, 0, _frameTimestampBuffer.Length);

                // Debug.Log($"ClientHandler {TcpClient.Client.RemoteEndPoint}: Кадр {frameData.Length} байт отправлен.");
            }
            catch (Exception e)
            {
                Debug.LogError($"ClientHandler {TcpClient.Client.RemoteEndPoint}: Ошибка при отправке данных: {e.Message}");
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

    // Цикл чтения от клиента (пример, если нужна будет обратная связь)
    /*
    private void ReadLoop()
    {
        try
        {
            while (IsConnected)
            {
                // Логика чтения данных от клиента
                // Например, получение подтверждений или статистики
                // byte[] buffer = new byte[1024];
                // int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                // if (bytesRead == 0)
                // {
                //    // Клиент отключился
                //    break;
                // }
                // ProcessClientData(buffer, bytesRead);
                Thread.Sleep(10); // Небольшая задержка, чтобы не загружать CPU
            }
        }
        catch (Exception e)
        {
            Debug.Log($"ClientHandler {TcpClient?.Client?.RemoteEndPoint}: Ошибка в цикле чтения или клиент отключился: {e.Message}");
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
            Debug.Log($"ClientHandler: Закрытие соединения с {TcpClient.Client?.RemoteEndPoint}");
            TcpClient.Close();
            TcpClient = null;
        }
        _server.RemoveClient(this); // Сообщаем серверу удалить этот обработчик
    }
}