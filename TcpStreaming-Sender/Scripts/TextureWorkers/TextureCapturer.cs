using System;
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
        SetTargetCamera(targetCamera);

        SetStreamSettings(streamSettings);
    }

    private void InitializeTextures()
    {
        if (_streamSettings.ResX <= 0 || _streamSettings.ResY <= 0)
        {
            Debug.LogError($"TextureCapturer: Invalid resolution in StreamSettings: {_streamSettings.ResX}x{_streamSettings.ResY}. Using default 640x480.");
            return;
        }

        int targetWidth = _streamSettings.ResX;
        int targetHeight = _streamSettings.ResY;


        _renderTexture = new RenderTexture(targetWidth, targetHeight, 24, RenderTextureFormat.ARGB32);
        _renderTexture.name = $"CaptureRenderTexture_{targetWidth}x{targetHeight}";
        _renderTexture.Create();

        _capturedTexture2D = new Texture2D(targetWidth, targetHeight, TextureFormat.ARGB32, false);
        _capturedTexture2D.name = $"CapturedTexture2D_{targetWidth}x{targetHeight}";

        _captureRect = new Rect(0, 0, targetWidth, targetHeight);
    }

    private void SetTargetCamera(Camera targetCamera)
    {
        if (targetCamera == null)
        {
            Debug.LogError("TextureCapturer: Attempted to set a null target camera.");
            return;
        }
        _targetCamera = targetCamera;
    }

    private void SetStreamSettings(StreamSettings newStreamSettings)
    {
        if (newStreamSettings == null)
        {
            Debug.LogError("TextureCapturer: Attempted to set null stream settings.");
            return;
        }

        _streamSettings = newStreamSettings;
        InitializeTextures();
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
