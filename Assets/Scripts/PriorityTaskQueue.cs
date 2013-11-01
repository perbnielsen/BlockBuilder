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
	readonly List< T > items = new List< T >();
	public Action< T > action;


	public PrioryTaskQueue( Action< T > action, uint threadCount = 1 )
	{
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
		lock ( items )
		{
			items.Sort( ( a, b ) => b.getPriority().CompareTo( a.getPriority() ) );
		}
	}


	void backgroundTask()
	{
		T item;

		while ( running )
		{
			tasksCount.WaitOne();

			lock ( items )
			{
				item = items[ 0 ];
				items.RemoveAt( 0 );
			}

			action( item );
		}
	}


	public void enqueueTask( T task )
	{
		lock ( items )
		{
			items.Add( task );
		}

		tasksCount.Release();
	}
}
