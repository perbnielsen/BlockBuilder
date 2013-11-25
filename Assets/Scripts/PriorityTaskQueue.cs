using System;
using System.Threading;
using System.Collections.Generic;


public interface IPriorityTask
{
	float getPriority();
}


public class PrioryTaskQueue< T > where T : IPriorityTask
{
	bool running = true;
	readonly Semaphore tasksCount = new Semaphore( 0, int.MaxValue );
	readonly List< T > tasks = new List< T >();
	public Action< T > action;
	public string name;


	public PrioryTaskQueue( string name, Action< T > action, uint threadCount = 1 )
	{
		this.name = name;
		this.action = action;

		while ( threadCount-- > 0 )
		{
			(new Thread( backgroundTask )).Start();
		}
	}


	public void stop()
	{
		running = false;
	}


	public void reprioritise()
	{
		lock ( tasks )
		{
			tasks.Sort( ( a, b ) => b.getPriority().CompareTo( a.getPriority() ) );

//			if ( tasks.Count > 0 ) UnityEngine.Debug.Log( name + " length on frame " + UnityEngine.Time.frameCount + " was " + tasks.Count );
		}
	}


	void backgroundTask()
	{
		T item;

		uint i = 0;

		while ( running )
		{
//			UnityEngine.Debug.Log( name + ": Waiting..." + i );
			tasksCount.WaitOne();

			lock ( tasks )
			{
				item = tasks[ 0 ];
				tasks.RemoveAt( 0 );
			}
//			UnityEngine.Debug.Log( name + ": Running..." + i + " " + (item as Chunk).position );
			action( item );
			++i;
		}
	}


	public void enqueueTask( T newTask )
	{
		lock ( tasks )
		{
			tasks.Add( newTask );
		}

		tasksCount.Release();
	}


	public void enqueueTasks( List< T > newTasks )
	{
		lock ( tasks )
		{
			tasks.AddRange( newTasks );
		}

		tasksCount.Release( newTasks.Count );
	}


	public void dequeueTask( T task )
	{
		lock ( tasks )
		{
			while ( tasks.Remove( task ) ) tasksCount.WaitOne();
		}
	}
}
