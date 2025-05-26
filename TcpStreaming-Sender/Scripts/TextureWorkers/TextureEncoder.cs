using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureEncoder
{
    private StreamSettings _streamSettings;

    public TextureEncoder(StreamSettings streamSettings)
    {

        _streamSettings = streamSettings;
    }

    public byte[] EncodeFrame(Texture2D texture, TextureCapturer textureCapturer)
    {
        byte[] dataBytes = texture.EncodeToJPG(80);
        return dataBytes;
    }

}
