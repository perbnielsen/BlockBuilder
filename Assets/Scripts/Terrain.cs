using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Collections;


public class Terrain : MonoBehaviour
{
	public bool isQuitting;
	public Transform player;
	public int seed;
	public int chunkSize;
	public Chunk chunkPrefab;
	public float _displayChunkDistance;
	float _displayChunkDistanceSqr;
	float _disableChunkDistanceSqr;
	float _destroyChunkDistanceSqr;
	public readonly TaskQueue chunkTasks = new TaskQueue();
	public readonly TaskQueue fileTasks = new TaskQueue();
	readonly Dictionary< Position3, Chunk > chunks = new Dictionary< Position3, Chunk >();


	public float displayChunkDistance
	{
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

		chunks.Add( position, chunk );

		chunkTasks.enqueueTask( chunk.populateBlocks );

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

		if ( chunks.Count == 0 ) getChunkAtCoordiate( player.transform.position );
	}


	void Start()
	{
		if ( !Directory.Exists( "Chunks" ) ) Directory.CreateDirectory( "Chunks" );

		displayChunkDistance = _displayChunkDistance;

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
		chunkTasks.stop();
		fileTasks.stop();
	}
}
