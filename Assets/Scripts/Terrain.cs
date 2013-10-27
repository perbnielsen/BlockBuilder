using UnityEngine;
using System.Collections.Generic;
using System.Threading;


public class Terrain : MonoBehaviour
{
	public bool isQuitting;
	public Transform player;
	public int seed;
	public int chunkSize;
	public Chunk chunkPrefab;
	float _displayChunkDistance;
	float _displayChunkDistanceSqr;
	float _disableChunkDistanceSqr;
	float _destroyChunkDistanceSqr;


	public float displayChunkDistance
	{
		get { return _displayChunkDistance; }
		set
		{
			_displayChunkDistance = value;
			_displayChunkDistanceSqr = displayChunkDistance * displayChunkDistance;
			_disableChunkDistanceSqr = ( displayChunkDistance + chunkSize * 1 ) * ( displayChunkDistance + chunkSize * 1 );
			_destroyChunkDistanceSqr = ( displayChunkDistance + chunkSize * 2 ) * ( displayChunkDistance + chunkSize * 2 );
		}
	}


	public float displayChunkDistanceSqr { get { return _displayChunkDistanceSqr; } }


	public float disableChunkDistanceSqr { get { return _disableChunkDistanceSqr; } }


	public float destroyChunkDistanceSqr { get { return _destroyChunkDistanceSqr; } }


	readonly Dictionary< Position3, Chunk > chunks = new Dictionary< Position3, Chunk >();


	public Chunk getChunkAtCoordiate( Vector3 coordinate )
	{
		return getChunk( coordinate / chunkSize );
	}

	#region Background task system

	public delegate void Task();


	public static Semaphore backgroundTasksCount = new Semaphore( 0, int.MaxValue );
	public static readonly List< Task > backgroundTasks = new List< Task >();


	public static void backgroundTask()
	{
		while ( true )
		{
			backgroundTasksCount.WaitOne();

			Task task;

			lock ( backgroundTasks )
			{
				task = backgroundTasks[ 0 ];
				backgroundTasks.RemoveAt( 0 );
			}

			task();
		}
	}


	public static void enqueueBackgroundTask( Task task, bool urgent = false )
	{
		lock ( backgroundTasks )
		{
			if ( urgent )
			{
				backgroundTasks.Insert( 0, task );
			}
			else
			{
				backgroundTasks.Add( task );
			}
		}

		backgroundTasksCount.Release();
	}

	#endregion

	void createChunk( Position3 position )
	{
		if ( isQuitting ) return;

		var chunk = (Chunk)Instantiate( chunkPrefab, position * chunkSize, Quaternion.identity );
		chunk.transform.parent = transform;
		chunk.terrain = this;
		chunk.size = chunkSize;
		chunk.position = position;

		chunks.Add( position, chunk );

		enqueueBackgroundTask( chunk.generateBlocks );
	}


	public Chunk getChunk( Position3 position )
	{
		// Note: Returns the chunk at position (in chunks). If the chunk does not exist it will be created first
		if ( !chunks.ContainsKey( position ) )
		{
			createChunk( position );
		}

		return chunks[ position ];
	}


	public void deleteChunk( Chunk chunk )
	{
		chunks.Remove( chunk.position );

		Destroy( chunk.gameObject );

		if ( chunks.Count == 0 ) getChunkAtCoordiate( player.transform.position );
	}


	void Start()
	{
		( new Thread( backgroundTask ) ).Start();

		displayChunkDistance = 64;

		getChunkAtCoordiate( player.transform.position );
	}


	void Update()
	{
		if ( Input.GetKeyDown( KeyCode.F1 ) )
		{
			displayChunkDistance = Mathf.Max( chunkSize, displayChunkDistance - 8 );

			Debug.Log( "displayChunkDistance: " + displayChunkDistance );
		}

		if ( Input.GetKeyDown( KeyCode.F2 ) )
		{
			displayChunkDistance += 8;

			Debug.Log( "displayChunkDistance: " + displayChunkDistance );
		}
	}


	void OnApplicationQuit()
	{
		isQuitting = true;
	}
}
