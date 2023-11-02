using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IPriorityTask
{
    float GetPriority();
}

public class PrioryTaskQueue<T> where T : class, IPriorityTask
{
    private bool running = true;
    private readonly Semaphore tasksCount = new(0, int.MaxValue);
    private readonly List<T> tasks = new();
    private readonly Action<T> action;

    public string name;

    public PrioryTaskQueue(string name, Action<T> action, uint threadCount = 1)
    {
        this.name = name;
        this.action = action;

        while (threadCount-- > 0)
        {
            new Thread(BackgroundTask).Start();
        }
    }

    public void Stop()
    {
        running = false;
    }

    public void Prioritise()
    {
        lock (tasks)
        {
            tasks.Sort((a, b) => b.GetPriority().CompareTo(a.GetPriority()));
        }
    }

    private void BackgroundTask()
    {
        T item;
        uint i = 0;
        while (running)
        {
            tasksCount.WaitOne();
            lock (tasks)
            {
                item = tasks[0];
                tasks.RemoveAt(0);
            }
            try
            {
                if (item != null) action(item);
            }
            catch (Exception e)
            {
                Debug.Log(name + " caught an exception: " + e.Message);
            }
            ++i;
        }
    }

    public void EnqueueTask(T newTask)
    {
        lock (tasks)
        {
            if (tasks.Contains(newTask))
            {
                return;
            }

            tasks.Add(newTask);
            tasksCount.Release();
        }
    }

    public void EnqueueTasks(List<T> newTasks)
    {
        lock (tasks)
        {
            var newDistinctTasks = newTasks.Except(tasks).ToList();
            tasks.AddRange(newDistinctTasks);
            tasksCount.Release(newDistinctTasks.Count);
        }
    }

    public void DequeueTask(T task)
    {
        lock (tasks)
        {
            if (tasks.Remove(task))
            {
                tasksCount.WaitOne();
            }
        }
    }

    public int Count { get { return tasks.Count; } }
}
