using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureCapturer
{
    private Camera _targetCamera;
    private StreamSettings _streamSettings;

    private RenderTexture _renderTexture;
    private Texture2D _capturedTexture2D;
    private Rect _captureRect;

    public TextureCapturer(Camera targetCamera, StreamSettings streamSettings)
    {
        _targetCamera = targetCamera;
        _streamSettings = streamSettings;
        if (_targetCamera == null)
        {
            Debug.LogError("Target camera is not assigned.");
        }
        if (_streamSettings == null)
        {
            Debug.LogError("Stream settings are not assigned.");
        }

        _renderTexture = new RenderTexture(_targetCamera.pixelWidth, _targetCamera.pixelHeight, 24, RenderTextureFormat.ARGB32);
        _capturedTexture2D = new Texture2D(_targetCamera.pixelWidth, _targetCamera.pixelHeight, TextureFormat.RGBA32, false);
        _captureRect = new Rect(0, 0, _targetCamera.pixelWidth, _targetCamera.pixelHeight);
    }

    public void SetTargetCamera(Camera targetCamera)
    {
        _targetCamera = targetCamera;
    }

    public void SetStreamSettings(StreamSettings streamSettings)
    {
        _streamSettings = streamSettings;
    }

    public Texture2D CaptureFrame()
    {
        RenderTexture previousActive = RenderTexture.active; 
        RenderTexture previousCameraTarget = _targetCamera.targetTexture;

        _targetCamera.targetTexture = _renderTexture;
        _targetCamera.Render();

        RenderTexture.active = _renderTexture;


        if (_capturedTexture2D.width != _renderTexture.width || _capturedTexture2D.height != _renderTexture.height)
        {
            Debug.LogWarning($"Resizing _capturedTexture2D from {_capturedTexture2D.width}x{_capturedTexture2D.height} to {_renderTexture.width}x{_renderTexture.height}");
            _capturedTexture2D.Reinitialize(_renderTexture.width, _renderTexture.height);
        }

        _capturedTexture2D.ReadPixels(_captureRect, 0, 0); 
        _capturedTexture2D.Apply();

        RenderTexture.active = previousActive;
        _targetCamera.targetTexture = previousCameraTarget;

        return _capturedTexture2D;

    }
}
