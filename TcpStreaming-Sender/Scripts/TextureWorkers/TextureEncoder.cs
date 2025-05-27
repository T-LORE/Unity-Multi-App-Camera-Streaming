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
        byte[] dataBytes;

        switch (_streamSettings.Codec)
        {
            case StreamSettings.CodecEnum.MJPG:
                dataBytes = texture.EncodeToJPG(80);
                break;
            case StreamSettings.CodecEnum.MGP:
                dataBytes = texture.EncodeToPNG();
                break;
            default:
                Debug.LogError("Invalid Codec selected. Defaulting to MJPG.");
                dataBytes = texture.EncodeToJPG(80);
                break;
        }

        return dataBytes;
    }

}
