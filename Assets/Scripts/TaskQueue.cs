using System.Threading;
using System.Collections.Generic;
using System;

public class TaskQueue
{
    private bool running = true;
    private readonly Semaphore tasksCount = new Semaphore(0, int.MaxValue);
    private readonly List<Action> tasks = new List<Action>();

    public TaskQueue()
    {
        (new Thread(backgroundTask)).Start();
    }

    public void stop()
    {
        running = false;
    }

    private void backgroundTask()
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

    public void enqueueTask(Action task, bool urgent = false)
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
