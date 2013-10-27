using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.IO.Compression;


[Serializable]
[RequireComponent( typeof( MeshFilter ) )]
[RequireComponent( typeof( MeshRenderer ) )]
[RequireComponent( typeof( MeshCollider ) )]
public class Chunk : MonoBehaviour
{
	public Terrain terrain;
	public int size;
	public Vector3 center;
	public Position3 position;
	public MeshFilter meshFilter;
	public MeshCollider meshCollider;
	Block.Type[,,] blocks;
	Chunk neighbourRight;
	Chunk neighbourLeft;
	Chunk neighbourUp;
	Chunk neighbourDown;
	Chunk neighbourForward;
	Chunk neighbourBack;
	readonly List<Vector3> vertices = new List<Vector3>();
	readonly List<int> triangles = new List<int>();
	readonly List<Vector2> uvs = new List<Vector2>();


	enum State : byte
	{
		Initialising,
		GeneratingBlocks,
		HasBlocks,
		GeneratingMesh,
		HasMesh,
		Inactive,
		Active
	}


	State state;

	#region Serialization

	[ContextMenu( "Save to disk" )]
	void saveChunkToDisk()
	{
		Terrain.enqueueBackgroundTask( saveChunkToDiskTask );
	}


	void saveChunkToDiskTask()
	{
		BinaryFormatter binaryFmt = new BinaryFormatter();

		using ( FileStream fileStream = new FileStream( "Chunks/" + position + ".chunk", FileMode.OpenOrCreate ) )
		{
			using ( DeflateStream deflateStream = new DeflateStream( fileStream, CompressionMode.Compress ) )
			{
				binaryFmt.Serialize( deflateStream, blocks );
			}
		}
	}


	[ContextMenu( "Load from disk" )]
	void loadFromDisk()
	{
		state = State.GeneratingBlocks;

		enabled = true;

		Terrain.enqueueBackgroundTask( loadChunkFromDiskTask );
	}


	void loadChunkFromDiskTask()
	{
		BinaryFormatter binaryFmt = new BinaryFormatter();

		try
		{
			using ( FileStream fileStream = new FileStream( "Chunks/" + position + ".chunk", FileMode.Open ) )
			{
				using ( DeflateStream deflateStream = new DeflateStream( fileStream, CompressionMode.Decompress ) )
				{
					blocks = (Block.Type[,,])binaryFmt.Deserialize( deflateStream );
				}
			}

			state = State.HasBlocks;
		}
		catch ( FileNotFoundException )
		{
		}
		catch ( DirectoryNotFoundException )
		{
		}
	}

	#endregion

	public Block.Type getBlock( Position3 blockPosition )
	{
		return getBlock( blockPosition.x, blockPosition.y, blockPosition.z );
	}


	public Block.Type getBlock( int x, int y, int z )
	{
		if ( x < 0 ) return neighbourLeft.getBlock( x + size, y, z );
		if ( x >= size ) return neighbourRight.getBlock( x - size, y, z );

		if ( y < 0 ) return neighbourDown.getBlock( x, y + size, z );
		if ( y >= size ) return neighbourUp.getBlock( x, y - size, z );

		if ( z < 0 ) return neighbourBack.getBlock( x, y, z + size );
		if ( z >= size ) return neighbourForward.getBlock( x, y, z - size );

		if ( blocks == null ) return Block.Type.none;

		return blocks[ x, y, z ];
	}


	public void setBlock( Position3 blockPosition, Block.Type blockType )
	{
		if ( state < State.HasBlocks ) return;

		// Set block 
		blocks[ blockPosition.x, blockPosition.y, blockPosition.z ] = blockType;

		// Note: Since we mark recalculation of the mesh as urgent,
		//       it will be added to the head of the background taks queue.
		//       So the order they are executed in, is reverse of the order in which they are added.

		if ( Block.isTransparent( blockType ) )
		{
			// Recalculate mesh
			StartCoroutine( generateMesh( regenerate: true ) );
		}

		// Update any neighbour chunk that might be affected
		if ( blockPosition.x == 0 && neighbourLeft != null ) neighbourLeft.StartCoroutine( neighbourLeft.generateMesh( regenerate: true ) );
		if ( blockPosition.y == 0 && neighbourDown != null ) neighbourDown.StartCoroutine( neighbourDown.generateMesh( regenerate: true ) );
		if ( blockPosition.z == 0 && neighbourBack != null ) neighbourBack.StartCoroutine( neighbourBack.generateMesh( regenerate: true ) );

		if ( blockPosition.x == size - 1 && neighbourRight != null ) neighbourRight.StartCoroutine( neighbourRight.generateMesh( regenerate: true ) );
		if ( blockPosition.y == size - 1 && neighbourUp != null ) neighbourUp.StartCoroutine( neighbourUp.generateMesh( regenerate: true ) );
		if ( blockPosition.z == size - 1 && neighbourForward != null ) neighbourForward.StartCoroutine( neighbourForward.generateMesh( regenerate: true ) );

		if ( !Block.isTransparent( blockType ) )
		{
			// Recalculate mesh
			StartCoroutine( generateMesh( regenerate: true ) );
		}
	}


	void Start()
	{
		center = transform.position + Vector3.one * size / 2f;
		meshFilter = GetComponent< MeshFilter >();
		meshCollider = GetComponent< MeshCollider >();
	}


	void Update()
	{
		if ( state < State.HasBlocks ) return;

		var distanceToPlayerSqr = ( terrain.player.position - center ).sqrMagnitude;

		if ( distanceToPlayerSqr < terrain.displayChunkDistanceSqr )
		{
			generateNeighbours();

			StartCoroutine( generateMesh() );

			enabled = false;
		}
		
		if ( distanceToPlayerSqr > terrain.disableChunkDistanceSqr )
		{
			disableMesh();

			state = State.Inactive;
		}
		
		if ( distanceToPlayerSqr > terrain.destroyChunkDistanceSqr )
		{
			activateNeighbours();

			terrain.deleteChunk( this );
		}
	}


	public void generateBlocks()
	{
		state = State.GeneratingBlocks;

		blocks = new Block.Type[ size, size, size ];

		for ( int x = 0; x < size; ++x )
		{
			const float scale = 50f;

			var positionX = (float)( position.x * size + x ) / scale;

			for ( int y = 0; y < size; ++y )
			{
				var positionY = (float)( position.y * size + y ) / scale;

				for ( int z = 0; z < size; ++z )
				{
					var positionZ = (float)( position.z * size + z ) / scale;

					blocks[ x, y, z ] = SimplexNoise.Noise.Generate( positionX, positionY, positionZ ) < .75f ? Block.Type.rock : Block.Type.none;

//					blocks[ x, y, z ] = (
//					    ( ( x == 0 ) && ( y == 0 ) && ( z == 0 ) ) ||
//					    ( ( x == 1 ) && ( y == 0 ) && ( z == 0 ) ) ||
//					    ( ( x == 0 ) && ( y == 1 ) && ( z == 0 ) ) ||
//					    ( ( x == 0 ) && ( y == 2 ) && ( z == 0 ) ) ||
//					    ( ( x == 0 ) && ( y == 0 ) && ( z == 1 ) ) ||
//					    ( ( x == 0 ) && ( y == 0 ) && ( z == 2 ) ) ||
//					    ( ( x == 0 ) && ( y == 0 ) && ( z == 3 ) )
//					) ? Block.Type.dirt : Block.Type.none;
				}
			}
		}

		state = State.HasBlocks;
	}


	void disableMesh()
	{
		renderer.enabled = false;
		meshCollider.enabled = false;

	}


	IEnumerator generateMesh( bool regenerate = false )
	{
		if ( state == State.GeneratingMesh )
		{
			Debug.LogWarning( "Already generating mesh! Exiting..." );
			
			yield break;
		}
		
		// If we are becoming active again, no need to regenerate the mesh, just reactivate
		if ( state > State.GeneratingMesh && !regenerate )
		{
			renderer.enabled = true;
			meshCollider.enabled = true;

			yield break;
		}

		state = State.GeneratingMesh;

		Terrain.enqueueBackgroundTask( drawBlocks, regenerate );

		while ( state < State.HasMesh ) yield return null;

		if ( triangles.Count == 0 )
		{
			destroyMesh();
		}
		else
		{
			var mesh = new Mesh();
			mesh.vertices = vertices.ToArray();
			mesh.triangles = triangles.ToArray();
			mesh.uv = uvs.ToArray();

			mesh.RecalculateBounds();
			mesh.RecalculateNormals();

			destroyMesh();

			meshFilter.mesh = mesh;
			meshCollider.sharedMesh = mesh;

			renderer.enabled = true;
			meshCollider.enabled = true;
		}

		vertices.Clear();
		triangles.Clear();
		uvs.Clear();

		state = State.Active;
	}


	void OnDestroy()
	{
		if ( !terrain.isQuitting ) destroyMesh();
	}


	void destroyMesh()
	{
		Destroy( meshFilter.mesh );
		Destroy( meshCollider.sharedMesh );
	}


	void generateNeighbours()
	{
		if ( neighbourRight == null ) neighbourRight = terrain.getChunk( position + Position3.right );
		if ( neighbourLeft == null ) neighbourLeft = terrain.getChunk( position + Position3.left );
		if ( neighbourUp == null ) neighbourUp = terrain.getChunk( position + Position3.up );
		if ( neighbourDown == null ) neighbourDown = terrain.getChunk( position + Position3.down );
		if ( neighbourForward == null ) neighbourForward = terrain.getChunk( position + Position3.forward );
		if ( neighbourBack == null ) neighbourBack = terrain.getChunk( position + Position3.back );

		neighbourRight.neighbourLeft = this;
		neighbourLeft.neighbourRight = this;
		neighbourUp.neighbourDown = this;
		neighbourDown.neighbourUp = this;
		neighbourForward.neighbourBack = this;
		neighbourDown.neighbourUp = this;
	}


	void activateNeighbours()
	{
		if ( neighbourRight != null ) neighbourRight.enabled = true;
		if ( neighbourLeft != null ) neighbourLeft.enabled = true;
		if ( neighbourUp != null ) neighbourUp.enabled = true;
		if ( neighbourDown != null ) neighbourDown.enabled = true;
		if ( neighbourForward != null ) neighbourForward.enabled = true;
		if ( neighbourBack != null ) neighbourBack.enabled = true;
	}


	void drawBlocks()
	{
		var right = new bool[size, size, size];
		var left = new bool[size, size, size];
		var top = new bool[size, size, size];
		var bottom = new bool[size, size, size];
		var front = new bool[size, size, size];
		var back = new bool[size, size, size];

		for ( int x = 0; x < size; ++x )
		{
			for ( int y = 0; y < size; ++y )
			{
				for ( int z = 0; z < size; ++z )
				{
					if ( blocks[ x, y, z ] == 0 ) continue;

					if ( Block.isTransparent( getBlock( x + 1, y, z ) ) ) right[ z, y, size - 1 - x ] = true;
					if ( Block.isTransparent( getBlock( x - 1, y, z ) ) ) left[ size - 1 - z, y, x ] = true;
					if ( Block.isTransparent( getBlock( x, y + 1, z ) ) ) top[ x, z, size - 1 - y ] = true;
					if ( Block.isTransparent( getBlock( x, y - 1, z ) ) ) bottom[ x, size - 1 - z, y ] = true;
					if ( Block.isTransparent( getBlock( x, y, z + 1 ) ) ) front[ size - 1 - x, y, size - 1 - z ] = true;
					if ( Block.isTransparent( getBlock( x, y, z - 1 ) ) ) back[ x, y, z ] = true;
				}
			}
		}

		drawFaces( ref right, Vector3.right * size, Vector3.forward, Vector3.up, Vector3.left );
		drawFaces( ref left, Vector3.forward * size, Vector3.back, Vector3.up, Vector3.right );
		drawFaces( ref top, Vector3.up * size, Vector3.right, Vector3.forward, Vector3.down );
		drawFaces( ref bottom, Vector3.forward * size, Vector3.right, Vector3.back, Vector3.up );
		drawFaces( ref front, Vector3.forward * size + Vector3.right * size, Vector3.left, Vector3.up, Vector3.back );
		drawFaces( ref back, Vector3.zero, Vector3.right, Vector3.up, Vector3.forward );

		state = State.HasMesh;
	}


	void drawFaces( ref bool[,,] faces, Vector3 offset, Vector3 right, Vector3 up, Vector3 forward )
	{
		int width;
		int height;

		for ( int z = 0; z < size; ++z )
		{
			for ( int y = 0; y < size; ++y )
			{
				for ( int x = 0; x < size; )
				{
					if ( !faces[ x, y, z ] )
					{
						++x;
						continue;
					}

					width = 1;
					height = 1;

					// Expand face in x
					while ( x + width < size && faces[ x + width, y, z ] )
					{
						//faces[ x, y, z + width ] = false;
						++width;
					}

					// Expand face in y
					while ( faceCanBeExtended( ref faces, x, y + height, z, width ) )
					{
						for ( int  i = 0; i < width; i++ ) faces[ x + i, y + height, z ] = false;
						++height;
					}

					drawFace( offset + x * right + y * up + z * forward, right * width, up * height );

					x += width;
				}
			}
		}
	}


	bool faceCanBeExtended( ref bool[,,] faces, int x, int y, int z, int width )
	{
		if ( y >= size ) return false;

		for ( int i = x; i < x + width; ++i ) if ( !faces[ i, y, z ] ) return false;

		return true;
	}


	void drawFace( Vector3 origin, Vector3 right, Vector3 up )
	{
		int index = vertices.Count;

		vertices.Add( origin );
		vertices.Add( origin + up );
		vertices.Add( origin + up + right );
		vertices.Add( origin + right );

		var uvUp = Vector2.up * up.magnitude;
		var uvRight = Vector2.right * right.magnitude;

		uvs.Add( Vector2.zero );
		uvs.Add( uvUp );
		uvs.Add( uvUp + uvRight );
		uvs.Add( uvRight );

		triangles.Add( index );
		triangles.Add( index + 1 );
		triangles.Add( index + 2 );

		triangles.Add( index + 2 );
		triangles.Add( index + 3 );
		triangles.Add( index );
	}
}
