using UnityEngine;
using System.Collections.Generic;


public class Terrain : MonoBehaviour
{
	public int seed;
	public int chunkSize;
	public int viewDistance;
	public Dictionary< Position3, Chunk > chunks = new Dictionary< Position3, Chunk >();
	public Chunk chunkPrefab;


	public byte getBlock( Position3 blockPosition )
	{
		Position3 chunkPosition = blockPosition / chunkSize;

		blockPosition %= chunkSize;

		if ( blockPosition.x < 0 )
		{
			chunkPosition.x -= 1;
			blockPosition.x += chunkSize;
		}
		if ( blockPosition.y < 0 )
		{
			chunkPosition.y -= 1;
			blockPosition.y += chunkSize;
		}
		if ( blockPosition.z < 0 )
		{
			chunkPosition.z -= 1;
			blockPosition.z += chunkSize;
		}
		return getBlock( chunkPosition, blockPosition );
	}


	public byte getBlock( Position3 chunk, Position3 position )
	{
//		Debug.Log( "GetBlock( (" + chunk.x + ", " + chunk.y + ", " + chunk.z + "), (" + position.x + ", " + position.y + ", " + position.z + " )" );

		if ( chunks.ContainsKey( chunk ) )
		{
			return chunks[ chunk ].getBlock( position );
		}

		return 255;
	}


	void Start()
	{
		Random.seed = seed;
	}


	void Update()
	{
		if ( chunkSize == 0 )
		{
			Debug.LogWarning( "ChunkSize on terrain " + name + " is zero. Defaulting to 16" );

			chunkSize = 16;
		}

		Position3 playerChunk = new Position3( transform.position / chunkSize ) * chunkSize;

		for ( int x = -viewDistance; x < viewDistance; x += chunkSize )
		{
			for ( int y = -viewDistance; y < viewDistance; y += chunkSize )
			{
				for ( int z = -viewDistance; z < viewDistance; z += chunkSize )
				{
					Position3 position = playerChunk + new Position3( x, y, z );

					if ( (new Vector3( x, y, z ) - transform.position).magnitude > viewDistance ) continue;

					if ( !chunks.ContainsKey( position ) )
					{
						var chunk = (Chunk)Instantiate( chunkPrefab, position.toVector3(), Quaternion.identity );

						chunk.terrain = this;
						chunk.size = chunkSize;
					}
				}
			}
		}
	}
}
