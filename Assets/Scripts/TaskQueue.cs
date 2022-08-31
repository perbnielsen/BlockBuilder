using System.Threading;
using System.Collections.Generic;
using System;

public class TaskQueue
{
    private bool running = true;
    private readonly Semaphore tasksCount = new(0, int.MaxValue);
    private readonly List<Action> tasks = new();

    public TaskQueue()
    {
        new Thread(BackgroundTask).Start();
    }

    public void Stop()
    {
        running = false;
    }

    private void BackgroundTask()
    {
        Action task;
        while (running)
        {
            tasksCount.WaitOne();
            lock (tasks)
            {
                task = tasks[0];
                tasks.RemoveAt(0);
            }
            task();
        }
    }

    public void EnqueueTask(Action task, bool urgent = false)
    {
        lock (tasks)
        {
            if (urgent)
            {
                tasks.Insert(0, task);
            }
            else
            {
                tasks.Add(task);
            }
        }
        tasksCount.Release();
    }
}
