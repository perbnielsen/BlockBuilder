using UnityEngine;
using System.Collections.Generic;
using System;


public class Terrain : MonoBehaviour
{
	public Transform player;
	public int seed;
	public int chunkSize;
	float _displayChunkDistance;


	public float displayChunkDistance
	{
		get { return _displayChunkDistance; }
		set
		{
			_displayChunkDistance = value;
			displayChunkDistanceSqr = displayChunkDistance * displayChunkDistance;
			destroyChunkDistanceSqr = (displayChunkDistance + chunkSize) * (displayChunkDistance + chunkSize);
		}
	}


	[NonSerialized]
	public float displayChunkDistanceSqr;
	[NonSerialized]
	public float destroyChunkDistanceSqr;
	public Chunk chunkPrefab;
	readonly Dictionary< Position3, Chunk > chunks = new Dictionary< Position3, Chunk >();


	/**
	 * @return the chunk at position. If the chunk does not exist it will be created first
	 */
	public Chunk getChunk( Position3 position )
	{
		if ( !chunks.ContainsKey( position ) )
		{
//			Debug.Log( "Did not find chunk at " + position + ". Creating it on frame " + Time.frameCount );
			
			var chunk = (Chunk)Instantiate( chunkPrefab, position * chunkSize, Quaternion.identity );

			chunk.transform.parent = transform;
			chunk.terrain = this;
			chunk.size = chunkSize;
			chunk.position = position;
			chunk.generateBlocks();
			
			chunks.Add( position, chunk );
		}

		return chunks[ position ];
	}


	public void deleteChunk( Chunk chunk )
	{
//		Debug.Log( "Deleting chunk at " + chunk.position );

		chunks.Remove( chunk.position );

		Destroy( chunk.gameObject );
	}


	void Start()
	{
		displayChunkDistance = 128;

	}


	void Update()
	{
		if ( Input.GetKey( KeyCode.F1 ) ) displayChunkDistance -= 8;
		if ( Input.GetKey( KeyCode.F2 ) ) displayChunkDistance += 8;

		getChunk( player.transform.position / chunkSize );
	}
}
