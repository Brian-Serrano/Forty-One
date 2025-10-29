using System;
using System.Collections.Concurrent;
using UnityEngine;

public class MainThreadRun
{
    private static MainThreadRun instance;

    public ConcurrentQueue<Action> executionQueue;

    public static MainThreadRun GetInstance()
    {
        instance ??= new MainThreadRun();

        return instance;
    }

    private MainThreadRun()
    {
        executionQueue = new ConcurrentQueue<Action>();
    }

    public void Enqueue(Action action)
    {
        if (executionQueue == null) return;

        executionQueue.Enqueue(action);
    }

    public void Update()
    {
        while (executionQueue.TryDequeue(out var action))
        {
            action?.Invoke();
        }
    }
}
