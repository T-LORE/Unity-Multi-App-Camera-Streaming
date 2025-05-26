using UnityEngine;
using System;
using System.Collections.Concurrent; 

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();
    private static MainThreadDispatcher _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_instance == null)
        {
            _instance = new GameObject("MainThreadDispatcher").AddComponent<MainThreadDispatcher>();
            DontDestroyOnLoad(_instance.gameObject);
        }
    }

    public static void Enqueue(Action action)
    {
        if (action == null)
        {
            Debug.LogWarning("MainThreadDispatcher: Tried to enqueue a null action.");
            return;
        }
        _executionQueue.Enqueue(action);
    }

    private void Update()
    {
        while (_executionQueue.TryDequeue(out Action action))
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error executing action on main thread: {e.Message}\n{e.StackTrace}");
            }
        }
    }

    void OnDestroy()
    {
        while (_executionQueue.TryDequeue(out _)) { }
        if (_instance == this)
        {
            _instance = null;
        }
    }
}