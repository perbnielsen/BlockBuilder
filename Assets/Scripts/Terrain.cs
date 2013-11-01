using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;


/*
 * 
spawned()

	updateBlocks()

	if ( should be visible ) spawn all neighbours



neighbourBlocksHaveChanged()
	
	if ( all neighbours exist and should be visible ) generate visual mesh



playerMoved()

	regenerate visual mesh 



updateBlocks()

	generateBlocks

	generate collision mesh

	send all existing neighbours neighbourBlocksHaveChanged
	
*/
public class Terrain : MonoBehaviour
{
	public readonly List< Action > runOnePerFrameOnMainThread = new List< Action >();
	public readonly List< Action > runOnMainThread = new List< Action >();
	//
	public bool isQuitting;
	public Player player;
	public int seed;
	public int chunkSize;
	public Chunk chunkPrefab;
	//
	public float _displayChunkDistance;
	float _displayChunkDistanceSqr;
	float _disableChunkDistanceSqr;
	float _destroyChunkDistanceSqr;
	//
	public readonly TaskQueue chunkTasks = new TaskQueue();
	public readonly TaskQueue fileTasks = new TaskQueue();
	//
	readonly Dictionary< Position3, Chunk > chunks = new Dictionary< Position3, Chunk >();
	//
	readonly PrioryTaskQueue<Chunk> chunksNeedingBlocks = new PrioryTaskQueue< Chunk >( chunk =>
	{
		chunk.populateBlocks();

		chunk.terrain.runOnMainThread.Add( chunk.generateNeighbours );
	}, 1 );
	//
	//
	public readonly PrioryTaskQueue<Chunk> chunksNeedingMesh = new PrioryTaskQueue< Chunk >( chunk => chunk.buildMesh(), 1 );
	//
	//
	public float displayChunkDistance {
		get { return _displayChunkDistance; }
		set
		{
			_displayChunkDistance = value;
			_displayChunkDistanceSqr = displayChunkDistance * displayChunkDistance;
			_disableChunkDistanceSqr = (displayChunkDistance + chunkSize * 0) * (displayChunkDistance + chunkSize * 0);
			_destroyChunkDistanceSqr = (displayChunkDistance + chunkSize * 1) * (displayChunkDistance + chunkSize * 1);
		}
	}


	public float displayChunkDistanceSqr { get { return _displayChunkDistanceSqr; } }


	public float disableChunkDistanceSqr { get { return _disableChunkDistanceSqr; } }


	public float destroyChunkDistanceSqr { get { return _destroyChunkDistanceSqr; } }


	public Chunk getChunkAtCoordiate( Vector3 coordinate )
	{
		return getChunk( coordinate / chunkSize );
	}


	Chunk createChunk( Position3 position )
	{
		if ( isQuitting ) return null;

//		Debug.Log( "Creating chunk at " + position );

		var chunk = (Chunk)Instantiate( chunkPrefab, position * chunkSize, Quaternion.identity );

		chunk.transform.parent = transform;
		chunk.terrain = this;
		chunk.size = chunkSize;
		chunk.position = position;
		chunk.center = chunk.transform.position + Vector3.one * chunkSize / 2f;
		chunk.meshFilter = chunk.GetComponent< MeshFilter >();
		chunk.meshCollider = chunk.GetComponent< MeshCollider >();

		chunks.Add( position, chunk );

		chunksNeedingBlocks.enqueueTask( chunk );

		return chunk;
	}


	public Chunk getChunk( Position3 position )
	{
		// Note: Returns the chunk at position (in chunks). If the chunk does not exist it will be created first
		if ( !chunks.ContainsKey( position ) )
		{
			return createChunk( position );
		}

		return chunks[ position ];
	}


	public void deleteChunk( Chunk chunk )
	{
		chunks.Remove( chunk.position );

		Destroy( chunk.gameObject );
	}


	void Start()
	{
		if ( !Directory.Exists( "Chunks" ) ) Directory.CreateDirectory( "Chunks" );

		displayChunkDistance = _displayChunkDistance;
	}


	void Update()
	{
		handleInput();

		chunksNeedingBlocks.reprioritise();
		chunksNeedingMesh.reprioritise();

		runMainThreadTask();
		runOnePerFrameOnMainThreadTask();
	}


	void handleInput()
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


	void runMainThreadTask()
	{
		Action task = null;

		lock ( runOnMainThread )
		{
			while ( runOnMainThread.Count > 0 )
			{
				task = runOnMainThread[ 0 ];
				runOnMainThread.RemoveAt( 0 );

				if ( task != null ) task();
				else Debug.Log( "null on task list???" );
			}
		}
	}


	void runOnePerFrameOnMainThreadTask()
	{
		Action task = null;

		lock ( runOnePerFrameOnMainThread )
		{
			if ( runOnePerFrameOnMainThread.Count > 0 )
			{
				task = runOnePerFrameOnMainThread[ 0 ];
				runOnePerFrameOnMainThread.RemoveAt( 0 );
			}
		}

		if ( task != null ) task();
	}


	void OnApplicationQuit()
	{
		isQuitting = true;
		chunkTasks.stop();
		fileTasks.stop();
	}
}
