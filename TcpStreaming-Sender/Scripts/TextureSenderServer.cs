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
    public string listenIp = "192.168.1.50"; // ������� �� ���� �����������
    public int port = 56666;

    [Header("Capture Settings")]
    public int quality = 70; // JPG quality
    public float streamFPS = 30;
    // resolutionMulti � ���������� ������ ������� ��� ���������, �.�. ������ ������ ����
    public TargetCamType targetCamType = TargetCamType.MainCam;
    public ResolutionType outputResolutionType = ResolutionType.p720;
    public Camera specificCam;
    [Tooltip("������������, ���� outputResolutionType = Custom")]
    public Vector2 customOutputResolution = new Vector2(1280, 720);

    private Camera _captureSourceCamera; // ������, � ������� ������� ���� ������
    private Camera _mainMirrorCamInternal; // ���������� ���������� ������ ��� MainCam

    private Vector2 _currentOutputRes;

    private RenderTexture _rt;
    private Texture2D _captureTexture; // ��� ReadPixels
    private byte[] _dataBytes; // �������������� ������ �����

    private TcpListener _tcpListener;
    private Thread _listenThread;
    private List<ClientHandler> _connectedClients = new List<ClientHandler>();
    private object _clientsLock = new object(); // ��� ������������� ������� � _connectedClients

    private bool _isServerRunning = false;

    [Header("debug")]
    public RawImage _testRawImage;

    private void Update()
    {
        if (_testRawImage != null && _captureTexture != null)
        {
            _testRawImage.texture = _captureTexture; // ��� �������
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
        StopAllCoroutines(); // ������������� �������� �������
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
            Debug.Log($"TextureSenderServer: ������ ������� �� {listenIp}:{port}");
        }
        catch (Exception e)
        {
            Debug.LogError($"TextureSenderServer: �� ������� ��������� ������: {e.Message}");
        }
    }

    public void StopServer()
    {
        if (!_isServerRunning) return;

        _isServerRunning = false; // ������ ��� ������ ListenForClients ������������

        if (_tcpListener != null)
        {
            _tcpListener.Stop();
            _tcpListener = null;
            Debug.Log("TextureSenderServer: TcpListener ����������.");
        }

        lock (_clientsLock)
        {
            foreach (var clientHandler in _connectedClients)
            {
                clientHandler.CloseConnection(); // ��������� ���������� ��� ������ RemoveClient ������� �����
            }
            _connectedClients.Clear(); // ������� ������ ����� �������� ����
            Debug.Log("TextureSenderServer: ��� ���������� ���������� �������.");
        }

        if (_listenThread != null && _listenThread.IsAlive)
        {
            try
            {
                _listenThread.Join(1000); // ������� ��������� ���������� ������
                if (_listenThread.IsAlive) _listenThread.Abort(); // �������������� ����������, ���� �� ����������
            }
            catch (Exception e) { Debug.LogError($"Error stopping listen thread: {e.Message}"); }
            _listenThread = null;
        }
        Debug.Log("TextureSenderServer: ������ ����������.");
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
                if (!_tcpListener.Pending()) // ���������, ���� �� ��������� �����������
                {
                    Thread.Sleep(100); // ��������� �����, ����� �� ������� CPU
                    continue;
                }

                Debug.Log("TextureSenderServer: �������� ������ �����������...");
                TcpClient client = _tcpListener.AcceptTcpClient(); // ����������� �����

                if (!_isServerRunning) // �������� ����� AcceptTcpClient �� ������ ��������� �������
                {
                    client.Close();
                    break;
                }

                Debug.Log($"TextureSenderServer: ������ {client.Client.RemoteEndPoint} �����������.");
                ClientHandler clientHandler = new ClientHandler(client, this);
                lock (_clientsLock)
                {
                    _connectedClients.Add(clientHandler);
                }
            }
        }
        catch (SocketException e)
        {
            if (_isServerRunning) // �������� ������, ������ ���� ������ ������ ��� ��������
                Debug.LogError($"TextureSenderServer: SocketException � ListenForClients: {e.Message} (���: {e.SocketErrorCode})");
        }
        catch (ThreadAbortException)
        {
            Debug.Log("TextureSenderServer: ����� ListenForClients �������.");
        }
        catch (Exception e)
        {
            Debug.LogError($"TextureSenderServer: �������������� ������ � ListenForClients: {e.Message}");
        }
        finally
        {
            if (_tcpListener != null) _tcpListener.Stop();
            _isServerRunning = false; // ��������, ��� ���� �������
            Debug.Log("TextureSenderServer: ����� ListenForClients ��������.");
        }
    }

    public void RemoveClient(ClientHandler clientHandler)
    {
        lock (_clientsLock)
        {
            if (_connectedClients.Contains(clientHandler))
            {
                _connectedClients.Remove(clientHandler);
                Debug.Log($"TextureSenderServer: ������ {clientHandler.TcpClient?.Client?.RemoteEndPoint} ������ �� ������.");
            }
        }
    }

    IEnumerator CaptureLoop()
    {
        float nextFrameTime = 0f;
        float interval = 0f;

        // �������������� ��������� ������ � ����������
        DetermineCaptureCamera();
        UpdateOutputResolution(); // ������������� _currentOutputRes
        EnsureRenderTextureAndCaptureTexture(); // ������� RT � Texture2D ��� _currentOutputRes

        while (true)
        {
            if (streamFPS <= 0)
            {
                yield return null; // ���� FPS 0, ������ ����
                continue;
            }

            interval = 1.0f / streamFPS;
            // ������� �� ���������� ����� ��� ������ yield return null, ���� ����� ��� ������
            // ��� ��������� �� ����������� ��������, ���� ������ �������� �����
            while (Time.realtimeSinceStartup < nextFrameTime)
            {
                yield return null;
            }
            nextFrameTime = Time.realtimeSinceStartup + interval;

            // ������ ����� ���������� � ����� ����� Unity
            yield return new WaitForEndOfFrame();

            if (_captureSourceCamera == null)
            {
                DetermineCaptureCamera(); // ������� ����� ����� ������, ���� ��� �������
                if (_captureSourceCamera == null)
                {
                    Debug.LogWarning("TextureSenderServer: ������ ��� ������� �� �������.");
                    yield return null; // ���������� ��������
                    continue;
                }
            }

            // ��������� ���������� � ��������, ���� ���������� (��������, ���� ���������� ��������� � ����������)
            // ��� ������ ����� �������� ����, ���� ��������� �� �������� ����������� �����.
            // UpdateOutputResolution(); 
            // EnsureRenderTextureAndCaptureTexture(); 

            try
            {
                ProcessCameraFrame(); // ������ � RT � ������ � Texture2D
                EncodeFrameToBytes(); // ����������� � JPG

                if (_dataBytes != null && _dataBytes.Length > 0)
                {
                    BroadcastFrameToClients(_dataBytes, Time.realtimeSinceStartup);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"TextureSenderServer: ������ � ����� �������: {e.Message}\n{e.StackTrace}");
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
                        // camObj.transform.SetParent(Camera.main.transform, false); // �� ����������� ������ ��������
                        _mainMirrorCamInternal = camObj.AddComponent<Camera>();
                        _mainMirrorCamInternal.CopyFrom(Camera.main); // �������� ���������
                        _mainMirrorCamInternal.enabled = false; // �������� �������
                    }
                    _captureSourceCamera = _mainMirrorCamInternal;
                    // ��������� ��������� ���������� ������, ���� �������� ����������
                    if (_mainMirrorCamInternal.targetTexture != _rt) _mainMirrorCamInternal.targetTexture = _rt; // ��������� RT ���� ��� �� ���������
                    if (Camera.main.clearFlags != _mainMirrorCamInternal.clearFlags) _mainMirrorCamInternal.CopyFrom(Camera.main);

                }
                else Debug.LogError("TextureSenderServer: Camera.main �� �������!");
                break;
            case TargetCamType.SpecificCam:
                _captureSourceCamera = specificCam;
                if (_captureSourceCamera == null) Debug.LogError("TextureSenderServer: SpecificCam �� ���������!");
                break;
        }
    }

    void UpdateOutputResolution()
    {
        if (outputResolutionType == ResolutionType.Custom)
            _currentOutputRes = customOutputResolution;
        else
            _currentOutputRes = outputResolutionType.GetResolution(); // resolutionMulti �����

        _currentOutputRes.x = Mathf.Max(16, Mathf.RoundToInt(_currentOutputRes.x)); // ����������� ������
        _currentOutputRes.y = Mathf.Max(16, Mathf.RoundToInt(_currentOutputRes.y));
    }

    void EnsureRenderTextureAndCaptureTexture()
    {
        // ��������, ��� ���������� ���������
        UpdateOutputResolution();

        int newWidth = (int)_currentOutputRes.x;
        int newHeight = (int)_currentOutputRes.y;

        if (_rt == null || _rt.width != newWidth || _rt.height != newHeight)
        {
            if (_rt != null) { _rt.Release(); Destroy(_rt); }
            _rt = new RenderTexture(newWidth, newHeight, 16, RenderTextureFormat.ARGB32);
            _rt.name = "TextureSender_RT";
            _rt.Create();
            Debug.Log($"TextureSenderServer: RenderTexture ���������/�������: {newWidth}x{newHeight}");
        }

        if (_captureTexture == null || _captureTexture.width != newWidth || _captureTexture.height != newHeight)
        {
            if (_captureTexture != null) Destroy(_captureTexture);
            _captureTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
            _captureTexture.name = "TextureSender_CaptureTex";
            Debug.Log($"TextureSenderServer: CaptureTexture ���������/�������: {newWidth}x{newHeight}");
        }

        // ��������� RT ������, ���� ��� ����������
        if (_captureSourceCamera != null && _captureSourceCamera.targetTexture != _rt)
        {
            _captureSourceCamera.targetTexture = _rt;
        }
    }


    private void ProcessCameraFrame()
    {
        if (_captureSourceCamera == null || _rt == null || _captureTexture == null)
        {
            Debug.LogWarning("TextureSenderServer: ������, RenderTexture ��� CaptureTexture �� ���������������� ��� ProcessCameraFrame.");
            DetermineCaptureCamera(); // ������� �������������
            EnsureRenderTextureAndCaptureTexture(); // ������� �������������
            if (_captureSourceCamera == null || _rt == null || _captureTexture == null) return; // ���� ��� ��� ���, �������
        }

        // ��������, ��� RT ��������� ������
        if (_captureSourceCamera.targetTexture != _rt)
        {
            _captureSourceCamera.targetTexture = _rt;
        }

        _captureSourceCamera.Render(); // �������������� ������ � _rt

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

            // ������� ����� ������ ��� ��������, ����� �������� ������� � ������������ �� ����� ��������
            // (���� SendFrame �����������, ��� ����� ���������� � �� ������ ������ ������)
            List<ClientHandler> clientsCopy = new List<ClientHandler>(_connectedClients);
            foreach (var clientHandler in clientsCopy)
            {
                if (clientHandler.IsConnected)
                {
                    clientHandler.SendFrame(frameData, frameTimestamp);
                }
                else
                {
                    // ���� ���������� ����������� ������� �����, ����� ������������ ��� ��������,
                    // �� CloseConnection � ClientHandler ��� ������ ��� ������.
                }
            }
        }
    }
}