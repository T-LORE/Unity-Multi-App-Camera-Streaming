using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ByteLogger
{
    struct BytePacket
    {
        public float logTime;
        public int bytes;
    }

    private List<BytePacket> _bytePackets = new List<BytePacket>();

    public void AddBytePacket(int bytes)
    {       
        float logTime = Time.time;
        BytePacket packet = new BytePacket
        {
            logTime = logTime,
            bytes = bytes
        };
        _bytePackets.Add(packet);
    }

    public void Clear()
    {
        _bytePackets.Clear();
    }

    public float GetAverage(float timeRangeSeconds)
    {
        if (_bytePackets.Count == 0)
            return 0f;

        float totalBytes = 0f;
        float currentTime = Time.time;

        foreach (var packet in _bytePackets)
        {
            if (currentTime - packet.logTime <= timeRangeSeconds)
            {
                totalBytes += packet.bytes;
            }
        }

        return totalBytes / timeRangeSeconds;
    }
}
