using UnityEngine;
using System.Collections.Generic;
using System;


public class Terrain : MonoBehaviour
{
	public Transform player;
	public int seed;
	public int chunkSize;
	//	public float preloadChunksDistance;
	public float displayChunkDistance;
	public float destroyChunkDistance;
	[NonSerializedAttribute]
	public float displayChunkDistanceSqr;
	[NonSerializedAttribute]
	public float destroyChunkDistanceSqr;
	public Dictionary< Position3, Chunk > chunks = new Dictionary< Position3, Chunk >();
	public Chunk chunkPrefab;


	public Chunk getChunk( Position3 position )
	{
		if ( !chunks.ContainsKey( position ) )
		{
			Debug.Log( "Did not find chunk at " + position + ". Creating it on frame " + Time.frameCount );
			
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


	void Start()
	{
//		UnityEnigne.Random.seed = seed;

		displayChunkDistanceSqr = displayChunkDistance * displayChunkDistance;
		destroyChunkDistanceSqr = destroyChunkDistance * destroyChunkDistance;

	}


	void Update()
	{
		getChunk( player.transform.position / chunkSize );
	}
}
