using UnityEngine;
using System.Collections.Generic;
using System.Threading;


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
			disableChunkDistanceSqr = ( displayChunkDistance + chunkSize * 1 ) * ( displayChunkDistance + chunkSize * 1 );
			destroyChunkDistanceSqr = ( displayChunkDistance + chunkSize * 2 ) * ( displayChunkDistance + chunkSize * 2 );
		}
	}
	//	[HideInInspector]
	public float displayChunkDistanceSqr;
	//	[HideInInspector]
	public float disableChunkDistanceSqr;
	public float destroyChunkDistanceSqr;
	public Chunk chunkPrefab;
	readonly Dictionary< Position3, Chunk > chunks = new Dictionary< Position3, Chunk >();


	public Chunk getChunkAtCoordiate( Vector3 coordinate )
	{
		return getChunk( ( coordinate - Vector3.one * chunkSize * 0.5f ) / chunkSize);
	}


	/**
	 * @return the chunk at position (in chunks). If the chunk does not exist it will be created first
	 */
	public Chunk getChunk( Position3 position )
	{
		if ( !chunks.ContainsKey( position ) )
		{
			// Debug.Log( "Did not find chunk at " + position + " Creating it on frame " + Time.frameCount );
			var chunk = (Chunk)Instantiate( chunkPrefab, position * chunkSize, Quaternion.identity );

			chunk.transform.parent = transform;
			chunk.terrain = this;
			chunk.size = chunkSize;
			chunk.position = position;
			Chunk.enqueueBackgroundTask( chunk.generateBlocks );
//			chunk.generateBlocks();
			
			chunks.Add( position, chunk );
		}

		return chunks[ position ];
	}


	public void deleteChunk( Chunk chunk )
	{
//		Debug.Log( "Deleting chunk at " + chunk.position );

		chunks.Remove( chunk.position );

		Destroy( chunk.gameObject );

		if ( chunks.Count == 0 ) getChunkAtCoordiate( player.transform.position );
	}


	void Start()
	{
		( new Thread( Chunk.backgroundTask ) ).Start();

		displayChunkDistance = 64;

		getChunkAtCoordiate( player.transform.position );
	}


	void Update()
	{
		if ( Input.GetKey( KeyCode.F1 ) )
		{
			displayChunkDistance = Mathf.Max( chunkSize, displayChunkDistance - 8 );

			Debug.Log( "displayChunkDistance: " + displayChunkDistance );
		}

		if ( Input.GetKey( KeyCode.F2 ) )
		{
			displayChunkDistance += 8;

			Debug.Log( "displayChunkDistance: " + displayChunkDistance );
		}
	}
}
