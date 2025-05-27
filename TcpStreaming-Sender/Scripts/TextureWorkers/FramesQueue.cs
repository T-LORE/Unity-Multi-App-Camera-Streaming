using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;


public class Frame
{    
    public float Time { get; private set; }
    public byte[] Data { get; private set; }

    public Frame(float time, byte[] data)
    {
        Time = time;
        Data = data;
    }
}

public class FramesQueue
{
    private Queue<Frame> _framesQueue = new Queue<Frame>();

    public FramesQueue()
    {

    }

    public void Enqueue(Frame frame)
    {
        _framesQueue.Enqueue(frame);
    }

    public Frame Dequeue()
    {
        if (_framesQueue.Count == 0)
        {
            return null;
        }
        return _framesQueue.Dequeue();
    }

    public void Clear()
    {
        _framesQueue.Clear();
    }

    public int Count => _framesQueue.Count;
    
}
