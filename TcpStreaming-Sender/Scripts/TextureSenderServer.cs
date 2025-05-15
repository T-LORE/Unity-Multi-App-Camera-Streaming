// TextureSenderServer.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class TextureSenderServer : MonoBehaviour
{
    [Header("Network")]
    public string listenIp = "192.168.1.50"; // Слушать на всех интерфейсах
    public int port = 56666;

    [Header("Capture Settings")]
    public int quality = 70; // JPG quality
    public float streamFPS = 30;
    // resolutionMulti и адаптивная логика удалены для упрощения, т.к. сервер теперь один
    public TargetCamType targetCamType = TargetCamType.MainCam;
    public ResolutionType outputResolutionType = ResolutionType.p720;
    public Camera specificCam;
    [Tooltip("Используется, если outputResolutionType = Custom")]
    public Vector2 customOutputResolution = new Vector2(1280, 720);

    private Camera _captureSourceCamera; // Камера, с которой реально идет захват
    private Camera _mainMirrorCamInternal; // Внутренняя зеркальная камера для MainCam

    private Vector2 _currentOutputRes;

    private RenderTexture _rt;
    private Texture2D _captureTexture; // Для ReadPixels
    private byte[] _dataBytes; // Закодированные данные кадра

    private TcpListener _tcpListener;
    private Thread _listenThread;
    private List<ClientHandler> _connectedClients = new List<ClientHandler>();
    private object _clientsLock = new object(); // Для синхронизации доступа к _connectedClients

    private bool _isServerRunning = false;

    [Header("debug")]
    public RawImage _testRawImage;

    private void Update()
    {
        if (_testRawImage != null && _captureTexture != null)
        {
            _testRawImage.texture = _captureTexture; // Для отладки
        }
    }

    public void StartConnection()
    {
        StartServer();
        StartCoroutine(CaptureLoop());
    }

    public void StopConnection()
    {
        StopServer();
        StopAllCoroutines();
    }

    void OnDestroy()
    {
        StopServer();
        StopAllCoroutines(); // Останавливаем корутину захвата
        if (_rt != null) { _rt.Release(); Destroy(_rt); }
        if (_captureTexture != null) Destroy(_captureTexture);
        if (_mainMirrorCamInternal != null) Destroy(_mainMirrorCamInternal.gameObject);
    }

    public void StartServer()
    {
        if (_isServerRunning) return;

        try
        {
            _listenThread = new Thread(new ThreadStart(ListenForClients));
            _listenThread.IsBackground = true;
            _listenThread.Start();
            _isServerRunning = true;
            Debug.Log($"TextureSenderServer: Сервер запущен на {listenIp}:{port}");
        }
        catch (Exception e)
        {
            Debug.LogError($"TextureSenderServer: Не удалось запустить сервер: {e.Message}");
        }
    }

    public void StopServer()
    {
        if (!_isServerRunning) return;

        _isServerRunning = false; // Сигнал для потока ListenForClients остановиться

        if (_tcpListener != null)
        {
            _tcpListener.Stop();
            _tcpListener = null;
            Debug.Log("TextureSenderServer: TcpListener остановлен.");
        }

        lock (_clientsLock)
        {
            foreach (var clientHandler in _connectedClients)
            {
                clientHandler.CloseConnection(); // Закрываем соединение без вызова RemoveClient изнутри цикла
            }
            _connectedClients.Clear(); // Очищаем список после закрытия всех
            Debug.Log("TextureSenderServer: Все клиентские соединения закрыты.");
        }

        if (_listenThread != null && _listenThread.IsAlive)
        {
            try
            {
                _listenThread.Join(1000); // Попытка дождаться завершения потока
                if (_listenThread.IsAlive) _listenThread.Abort(); // Принудительное прерывание, если не завершился
            }
            catch (Exception e) { Debug.LogError($"Error stopping listen thread: {e.Message}"); }
            _listenThread = null;
        }
        Debug.Log("TextureSenderServer: Сервер остановлен.");
    }

    private void ListenForClients()
    {
        try
        {
            IPAddress ipAddr = IPAddress.Parse(listenIp);
            _tcpListener = new TcpListener(ipAddr, port);
            _tcpListener.Start();

            while (_isServerRunning)
            {
                if (!_tcpListener.Pending()) // Проверяем, есть ли ожидающие подключения
                {
                    Thread.Sleep(100); // Небольшая пауза, чтобы не грузить CPU
                    continue;
                }

                Debug.Log("TextureSenderServer: Ожидание нового подключения...");
                TcpClient client = _tcpListener.AcceptTcpClient(); // Блокирующий вызов

                if (!_isServerRunning) // Проверка после AcceptTcpClient на случай остановки сервера
                {
                    client.Close();
                    break;
                }

                Debug.Log($"TextureSenderServer: Клиент {client.Client.RemoteEndPoint} подключился.");
                ClientHandler clientHandler = new ClientHandler(client, this);
                lock (_clientsLock)
                {
                    _connectedClients.Add(clientHandler);
                }
            }
        }
        catch (SocketException e)
        {
            if (_isServerRunning) // Логируем ошибку, только если сервер должен был работать
                Debug.LogError($"TextureSenderServer: SocketException в ListenForClients: {e.Message} (Код: {e.SocketErrorCode})");
        }
        catch (ThreadAbortException)
        {
            Debug.Log("TextureSenderServer: Поток ListenForClients прерван.");
        }
        catch (Exception e)
        {
            Debug.LogError($"TextureSenderServer: Непредвиденная ошибка в ListenForClients: {e.Message}");
        }
        finally
        {
            if (_tcpListener != null) _tcpListener.Stop();
            _isServerRunning = false; // Убедимся, что флаг сброшен
            Debug.Log("TextureSenderServer: Поток ListenForClients завершен.");
        }
    }

    public void RemoveClient(ClientHandler clientHandler)
    {
        lock (_clientsLock)
        {
            if (_connectedClients.Contains(clientHandler))
            {
                _connectedClients.Remove(clientHandler);
                Debug.Log($"TextureSenderServer: Клиент {clientHandler.TcpClient?.Client?.RemoteEndPoint} удален из списка.");
            }
        }
    }

    IEnumerator CaptureLoop()
    {
        float nextFrameTime = 0f;
        float interval = 0f;

        // Первоначальная настройка камеры и разрешения
        DetermineCaptureCamera();
        UpdateOutputResolution(); // Устанавливает _currentOutputRes
        EnsureRenderTextureAndCaptureTexture(); // Создает RT и Texture2D под _currentOutputRes

        while (true)
        {
            if (streamFPS <= 0)
            {
                yield return null; // Если FPS 0, просто ждем
                continue;
            }

            interval = 1.0f / streamFPS;
            // Ожидаем до следующего кадра или просто yield return null, если время уже пришло
            // Это позволяет не накапливать задержку, если захват занимает время
            while (Time.realtimeSinceStartup < nextFrameTime)
            {
                yield return null;
            }
            nextFrameTime = Time.realtimeSinceStartup + interval;

            // Захват кадра происходит в конце кадра Unity
            yield return new WaitForEndOfFrame();

            if (_captureSourceCamera == null)
            {
                DetermineCaptureCamera(); // Попытка снова найти камеру, если она пропала
                if (_captureSourceCamera == null)
                {
                    Debug.LogWarning("TextureSenderServer: Камера для захвата не найдена.");
                    yield return null; // Пропускаем итерацию
                    continue;
                }
            }

            // Обновляем разрешение и текстуры, если необходимо (например, если изменились настройки в инспекторе)
            // Эту логику можно вызывать реже, если параметры не меняются динамически часто.
            // UpdateOutputResolution(); 
            // EnsureRenderTextureAndCaptureTexture(); 

            try
            {
                ProcessCameraFrame(); // Рендер в RT и чтение в Texture2D
                EncodeFrameToBytes(); // Кодирование в JPG

                if (_dataBytes != null && _dataBytes.Length > 0)
                {
                    BroadcastFrameToClients(_dataBytes, Time.realtimeSinceStartup);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"TextureSenderServer: Ошибка в цикле захвата: {e.Message}\n{e.StackTrace}");
            }
        }
    }

    void DetermineCaptureCamera()
    {
        switch (targetCamType)
        {
            case TargetCamType.MainCam:
                if (Camera.main != null)
                {
                    if (_mainMirrorCamInternal == null)
                    {
                        GameObject camObj = new GameObject("TextureSender_MainMirrorCam");
                        // camObj.transform.SetParent(Camera.main.transform, false); // Не обязательно делать дочерним
                        _mainMirrorCamInternal = camObj.AddComponent<Camera>();
                        _mainMirrorCamInternal.CopyFrom(Camera.main); // Копируем настройки
                        _mainMirrorCamInternal.enabled = false; // Рендерим вручную
                    }
                    _captureSourceCamera = _mainMirrorCamInternal;
                    // Обновляем параметры зеркальной камеры, если основная изменилась
                    if (_mainMirrorCamInternal.targetTexture != _rt) _mainMirrorCamInternal.targetTexture = _rt; // Назначаем RT если еще не назначена
                    if (Camera.main.clearFlags != _mainMirrorCamInternal.clearFlags) _mainMirrorCamInternal.CopyFrom(Camera.main);

                }
                else Debug.LogError("TextureSenderServer: Camera.main не найдена!");
                break;
            case TargetCamType.SpecificCam:
                _captureSourceCamera = specificCam;
                if (_captureSourceCamera == null) Debug.LogError("TextureSenderServer: SpecificCam не назначена!");
                break;
        }
    }

    void UpdateOutputResolution()
    {
        if (outputResolutionType == ResolutionType.Custom)
            _currentOutputRes = customOutputResolution;
        else
            _currentOutputRes = outputResolutionType.GetResolution(); // resolutionMulti убран

        _currentOutputRes.x = Mathf.Max(16, Mathf.RoundToInt(_currentOutputRes.x)); // Минимальный размер
        _currentOutputRes.y = Mathf.Max(16, Mathf.RoundToInt(_currentOutputRes.y));
    }

    void EnsureRenderTextureAndCaptureTexture()
    {
        // Убедимся, что разрешение актуально
        UpdateOutputResolution();

        int newWidth = (int)_currentOutputRes.x;
        int newHeight = (int)_currentOutputRes.y;

        if (_rt == null || _rt.width != newWidth || _rt.height != newHeight)
        {
            if (_rt != null) { _rt.Release(); Destroy(_rt); }
            _rt = new RenderTexture(newWidth, newHeight, 16, RenderTextureFormat.ARGB32);
            _rt.name = "TextureSender_RT";
            _rt.Create();
            Debug.Log($"TextureSenderServer: RenderTexture обновлена/создана: {newWidth}x{newHeight}");
        }

        if (_captureTexture == null || _captureTexture.width != newWidth || _captureTexture.height != newHeight)
        {
            if (_captureTexture != null) Destroy(_captureTexture);
            _captureTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
            _captureTexture.name = "TextureSender_CaptureTex";
            Debug.Log($"TextureSenderServer: CaptureTexture обновлена/создана: {newWidth}x{newHeight}");
        }

        // Назначаем RT камере, если она определена
        if (_captureSourceCamera != null && _captureSourceCamera.targetTexture != _rt)
        {
            _captureSourceCamera.targetTexture = _rt;
        }
    }


    private void ProcessCameraFrame()
    {
        if (_captureSourceCamera == null || _rt == null || _captureTexture == null)
        {
            Debug.LogWarning("TextureSenderServer: Камера, RenderTexture или CaptureTexture не инициализированы для ProcessCameraFrame.");
            DetermineCaptureCamera(); // Попытка инициализации
            EnsureRenderTextureAndCaptureTexture(); // Попытка инициализации
            if (_captureSourceCamera == null || _rt == null || _captureTexture == null) return; // Если все еще нет, выходим
        }

        // Убедимся, что RT назначена камере
        if (_captureSourceCamera.targetTexture != _rt)
        {
            _captureSourceCamera.targetTexture = _rt;
        }

        _captureSourceCamera.Render(); // Принудительный рендер в _rt

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = _rt;
        _captureTexture.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0, false);
        _captureTexture.Apply();
        RenderTexture.active = previousActive;
    }

    private void EncodeFrameToBytes()
    {
        if (_captureTexture == null)
        {
            _dataBytes = null;
            return;
        }
        _dataBytes = _captureTexture.EncodeToJPG(quality);
    }

    private void BroadcastFrameToClients(byte[] frameData, float frameTimestamp)
    {
        lock (_clientsLock)
        {
            if (_connectedClients.Count == 0) return;

            // Создаем копию списка для итерации, чтобы избежать проблем с модификацией во время итерации
            // (хотя SendFrame асинхронный, сам вызов синхронный и не должен менять список)
            List<ClientHandler> clientsCopy = new List<ClientHandler>(_connectedClients);
            foreach (var clientHandler in clientsCopy)
            {
                if (clientHandler.IsConnected)
                {
                    clientHandler.SendFrame(frameData, frameTimestamp);
                }
                else
                {
                    // Если обнаружили неактивного клиента здесь, можно инициировать его удаление,
                    // но CloseConnection в ClientHandler уже должен это делать.
                }
            }
        }
    }
}