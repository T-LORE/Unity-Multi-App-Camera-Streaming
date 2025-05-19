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
    public Texture2D receivedTexture; // Текстура, которая будет обновляться

    private TcpClient _tcpClient;
    private NetworkStream _stream;
    private Thread _receiveThread;
    public bool _isConnected = false;
    public bool _isTryingToConnect = false;
    public bool _stopReceiveThread = false;

    private byte[] _messageLengthBuffer = new byte[sizeof(int)];
    // private byte[] _frameTimestampBuffer = new byte[sizeof(float)]; // Если будем получать timestamp

    private Queue<byte[]> _receivedFramesQueue = new Queue<byte[]>();
    private object _queueLock = new object();

    public void Connect()
    {
        Loom.Initialize();
        receivedTexture = new Texture2D(2, 2, TextureFormat.RGB24, false); // Начальная заглушка
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
        // Обрабатываем полученные кадры в основном потоке
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
        _stopReceiveThread = false; // Сбрасываем флаг остановки перед новым потоком

        _receiveThread = new Thread(new ThreadStart(ReceiveLoop));
        _receiveThread.IsBackground = true;
        _receiveThread.Start();
    }

    private void ReceiveLoop()
    {
        try
        {
            _tcpClient = new TcpClient();
            Debug.Log($"TextureReceiver: Попытка подключения к {serverIp}:{serverPort}...");
            _tcpClient.Connect(serverIp, serverPort); // Блокирующий вызов
            _stream = _tcpClient.GetStream();
            _isConnected = true;
            _isTryingToConnect = false;
            Debug.Log($"TextureReceiver: Успешно подключен к серверу.");

            Loom.QueueOnMainThread(() => { /* Действия при успешном подключении, если нужны */ });

            while (_isConnected && !_stopReceiveThread)
            {
                // 1. Читаем длину кадра
                int bytesRead = 0;
                do
                {
                    int read = _stream.Read(_messageLengthBuffer, bytesRead, _messageLengthBuffer.Length - bytesRead);
                    if (read == 0) throw new Exception("Соединение разорвано при чтении длины кадра.");
                    bytesRead += read;
                } while (bytesRead < _messageLengthBuffer.Length);
                int frameSize = BitConverter.ToInt32(_messageLengthBuffer, 0);

                if (frameSize <= 0 || frameSize > 10 * 1024 * 1024) // Проверка на адекватный размер (макс 10MB)
                {
                    Debug.LogError($"TextureReceiver: Получен некорректный размер кадра: {frameSize}. Разрыв соединения.");
                    throw new Exception("Некорректный размер кадра.");
                }

                // 2. Читаем сам кадр
                byte[] frameData = new byte[frameSize];
                bytesRead = 0;
                do
                {
                    int read = _stream.Read(frameData, bytesRead, frameSize - bytesRead);
                    if (read == 0) throw new Exception("Соединение разорвано при чтении данных кадра.");
                    bytesRead += read;
                } while (bytesRead < frameSize);

                // 3. Читаем временную метку (если сервер ее отправляет)
                // bytesRead = 0;
                // do
                // {
                //    int read = _stream.Read(_frameTimestampBuffer, bytesRead, _frameTimestampBuffer.Length - bytesRead);
                //    if (read == 0) throw new Exception("Соединение разорвано при чтении timestamp.");
                //    bytesRead += read;
                // } while (bytesRead < _frameTimestampBuffer.Length);
                // float frameTimestamp = BitConverter.ToSingle(_frameTimestampBuffer, 0);

                // Добавляем кадр в очередь для обработки в основном потоке
                lock (_queueLock)
                {
                    // Ограничиваем размер очереди, чтобы не было большой задержки при лагах
                    if (_receivedFramesQueue.Count < 5) // Например, не больше 5 кадров в очереди
                    {
                        _receivedFramesQueue.Enqueue(frameData);
                    }
                    else
                    {
                        // Debug.LogWarning("TextureReceiver: Очередь кадров переполнена, кадр пропущен.");
                    }
                }
            }
        }
        catch (ThreadAbortException)
        {
            Debug.Log("TextureReceiver: Поток ReceiveLoop прерван.");
        }
        catch (Exception e)
        {
            Debug.LogError($"TextureReceiver: Ошибка в ReceiveLoop или соединение потеряно: {e.Message}");
        }
        finally
        {
            CloseConnection();
            _isTryingToConnect = false; // Позволяем повторную попытку подключения
            // Можно добавить логику автоматического переподключения здесь через некоторое время
            if (!_stopReceiveThread) // Если поток не был остановлен намеренно
            {
                Loom.QueueOnMainThread(() => StartCoroutine(ReconnectAfterDelay(5.0f)));
            }
        }
    }

    IEnumerator ReconnectAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!_isConnected && !_isTryingToConnect && enabled) // Проверяем, что компонент все еще активен
        {
            Debug.Log("TextureReceiver: Попытка переподключения...");
            ConnectToServer();
        }
    }

    private void ProcessImageData(byte[] byteData)
    {
        if (byteData == null || byteData.Length == 0) return;

        // LoadImage автоматически изменяет размер текстуры, если это необходимо
        if (receivedTexture == null)
        {
            // LoadImage требует, чтобы текстура была не null, но может быть 0x0
            receivedTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        }

        bool success = receivedTexture.LoadImage(byteData); // Загружаем JPG данные
        if (!success)
        {
            Debug.LogError("TextureReceiver: Не удалось загрузить изображение из байтов.");
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
        Debug.Log("TextureReceiver: Соединение закрыто.");
    }

    void OnDestroy()
    {
        _stopReceiveThread = true; // Сигнал потоку остановиться
        CloseConnection();
        if (_receiveThread != null && _receiveThread.IsAlive)
        {
            try
            {
                _receiveThread.Join(500); // Даем потоку немного времени на завершение
                if (_receiveThread.IsAlive) _receiveThread.Abort(); // Принудительно, если не завершился
            }
            catch (Exception e) { Debug.LogError($"Error stopping receive thread: {e.Message}"); }
        }
        if (receivedTexture != null) Destroy(receivedTexture);
    }
}