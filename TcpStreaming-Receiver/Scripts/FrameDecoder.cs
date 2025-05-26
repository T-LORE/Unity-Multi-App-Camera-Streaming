using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameDecoder
{
    private Vector2 _frameSize = new Vector2(640, 480);
    private Texture2D _reusableTexture;

    private bool _success = false;
    private byte[] _frameData;
    public FrameDecoder(Vector2 frameSize)
    {
        _frameSize = frameSize;
        _reusableTexture = new Texture2D((int)_frameSize.x, (int)_frameSize.y, TextureFormat.RGB24, false);
        _reusableTexture.name = "Decoder_ReusableTexture";
    }

    public void SetFrameSize(Vector2 frameSize)
    {
        _frameSize = frameSize;
        _reusableTexture.Reinitialize((int)_frameSize.x, (int)_frameSize.y, TextureFormat.RGB24, false);
    }

    public Texture2D DecodeFrameData(byte[] frameData)
    {
        if (frameData == null || frameData.Length == 0)
        {
            Debug.LogWarning("FrameDecoder: Received empty or null frame data.");
            return null;
        }

        bool success = _reusableTexture.LoadImage(frameData, false);

        if (!success)
        {
            Debug.LogError($"FrameDecoder: Failed to load image data. Data length: {frameData.Length}. Texture current state: {_reusableTexture.width}x{_reusableTexture.height} {_reusableTexture.format}");
            return null;
        }

        return _reusableTexture;
    }

    public Texture2D GetLastDecodedTexture()
    {
        return _reusableTexture;
    }
}
