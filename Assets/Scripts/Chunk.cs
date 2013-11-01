using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;


[RequireComponent( typeof( MeshFilter ) )]
[RequireComponent( typeof( MeshRenderer ) )]
[RequireComponent( typeof( MeshCollider ) )]
public class Chunk : MonoBehaviour, IPriorityTask
{
	public Terrain terrain;
	public int size;
	public Vector3 center;
	public Position3 position;
	public MeshFilter meshFilter;
	public MeshCollider meshCollider;
	//
	byte[] blocks;
	//
	Chunk neighbourRight;
	Chunk neighbourLeft;
	Chunk neighbourUp;
	Chunk neighbourDown;
	Chunk neighbourForward;
	Chunk neighbourBack;


	public float getPriority()
	{
		Vector3 fromPlayerToChunk = center - terrain.player.transform.position;
		return (Vector3.Dot( fromPlayerToChunk.normalized, terrain.player.transform.forward.normalized ) + 1f) / fromPlayerToChunk.sqrMagnitude;
	}
	//
	static int lastFrameChunkWasAdded;
	//	enum State : byte
	//	{
	//		Initialising,
	//		GeneratingBlocks,
	//		HasBlocks,
	//		GeneratingMesh,
	//		HasMesh,
	//		Active
	//	}
	//
	//
	//	State state;


	#region Serialization

	[ContextMenu( "Save to disk" )]
	void saveChunkToDisk()
	{
		terrain.fileTasks.enqueueTask( saveChunkToDiskTask );
	}


	void saveChunkToDiskTask()
	{
		using ( var file = File.Create( "Chunks/" + position + ".chunk" ) )
		{
			using ( var binaryWriter = new BinaryWriter( file ) )
			{
				binaryWriter.Write( GZip.compress( blocks ) );
			} 
		}
	}


	[ContextMenu( "Load from disk" )]
	// Note: Only for use from Unity!
	void loadFromDisk()
	{
//		state = State.GeneratingBlocks;

		enabled = true;

		terrain.fileTasks.enqueueTask( loadChunkFromDiskTask );
	}


	void loadChunkFromDiskTask()
	{
//		state = State.GeneratingBlocks;

		try
		{
			using ( var fs = File.OpenRead( "Chunks/" + position + ".chunk" ) )
			{
				using ( var binaryReader = new BinaryReader(fs) )
				{
					blocks = GZip.decompress( binaryReader.ReadBytes( size * size * size ), size * size * size );
				} 
			}

//			state = State.HasBlocks;
		}
		catch ( FileNotFoundException )
		{
			Debug.LogWarning( "Chunk at " + position + " tried to load data from disk but the file was not found" );
		}
		catch ( DirectoryNotFoundException )
		{
			Debug.LogWarning( "Chunk at " + position + " tried to load data from disk but the directory was not found" );
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

		return (Block.Type)blocks[ x + size * (y + size * z) ];
	}


	public void setBlock( Position3 blockPosition, Block.Type blockType )
	{
//		if ( state < State.HasBlocks ) return;

		// Set block 
		blocks[ blockPosition.x + size * (blockPosition.y + size * blockPosition.z) ] = (byte)blockType;

		// Note: Since we mark recalculation of the mesh as urgent,
		//       it will be added to the head of the background taks queue.
		//       So the order they are executed in, is reverse of the order in which they are added.

//		if ( !Block.isTransparent( blockType ) )
//		{
//			// Recalculate mesh
//			StartCoroutine( generateMesh( regenerate: true ) );
//		}
//
//		// Update any neighbour chunk that might be affected
//		if ( blockPosition.x == 0 && neighbourLeft != null ) neighbourLeft.StartCoroutine( neighbourLeft.generateMesh( regenerate: true ) );
//		if ( blockPosition.y == 0 && neighbourDown != null ) neighbourDown.StartCoroutine( neighbourDown.generateMesh( regenerate: true ) );
//		if ( blockPosition.z == 0 && neighbourBack != null ) neighbourBack.StartCoroutine( neighbourBack.generateMesh( regenerate: true ) );
//
//		if ( blockPosition.x == size - 1 && neighbourRight != null ) neighbourRight.StartCoroutine( neighbourRight.generateMesh( regenerate: true ) );
//		if ( blockPosition.y == size - 1 && neighbourUp != null ) neighbourUp.StartCoroutine( neighbourUp.generateMesh( regenerate: true ) );
//		if ( blockPosition.z == size - 1 && neighbourForward != null ) neighbourForward.StartCoroutine( neighbourForward.generateMesh( regenerate: true ) );
//
//		if ( Block.isTransparent( blockType ) )
//		{
//			// Recalculate mesh
//			StartCoroutine( generateMesh( regenerate: true ) );
//		}

		saveChunkToDisk();
	}


	public void populateBlocks()
	{
		if ( File.Exists( "Chunks/" + position + ".chunk" ) )
		{
			loadChunkFromDiskTask();
		}
		else
		{
			generateBlocks();
		}
	}


	public void generateBlocks()
	{
//		state = State.GeneratingBlocks;

		const float scale = 50f;
		
		blocks = new byte[ size * size * size ];

		int i = 0;

		for ( int z = 0; z < size; ++z )
		{
			var positionZ = (float)(position.z * size + z) / scale;
			
			for ( int y = 0; y < size; ++y )
			{
				var positionY = (float)(position.y * size + y) / scale;
				
				for ( int x = 0; x < size; ++x )
				{
					var positionX = (float)(position.x * size + x) / scale;

					blocks[ i++ ] = positionY < Noise.Generate( positionX, positionY, positionZ ) ? (byte)Block.Type.rock : (byte)Block.Type.none;
				}
			}
		}

//		state = State.HasBlocks;
	}


	void disableMesh()
	{
		renderer.enabled = false;
		meshCollider.enabled = false;
	}


	public void setMesh( Vector3[] vertices, int[] triangles, Vector2[] uvs )
	{
		Destroy( meshFilter.mesh );

		if ( triangles.Length == 0 ) return;

		var mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;

		mesh.RecalculateBounds();
		mesh.RecalculateNormals();

		meshFilter.mesh = mesh;
//		meshCollider.sharedMesh = mesh;

		renderer.enabled = true;
//		meshCollider.enabled = true;
	}


	public void setCollisionMesh( Vector3[] vertices, int[] triangles )
	{
		Destroy( meshCollider.sharedMesh );

		if ( triangles.Length == 0 ) return;

		var mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.triangles = triangles;

		mesh.RecalculateBounds();  // Todo: Needed for collision mesh?
		mesh.RecalculateNormals(); // Todo: Needed for collision mesh?

		meshCollider.sharedMesh = mesh;

		meshCollider.enabled = true;
	}


	void OnDestroy()
	{
		Debug.Log( "Destroying chunk at " + position );

		if ( !terrain.isQuitting ) destroyMesh();
	}


	void destroyMesh()
	{
		Destroy( meshFilter.mesh );
		Destroy( meshCollider.sharedMesh );
	}


	bool hasAllNeighbours()
	{
		return (null != neighbourRight)
		&& (null != neighbourLeft)
		&& (null != neighbourUp)
		&& (null != neighbourDown)
		&& (null != neighbourForward)
		&& (null != neighbourBack);
	}


	public void neighbourAdded()
	{
//		Debug.Log( position + " was poked" );
//		if ( meshFilter.mesh == null )
		{
			if ( hasAllNeighbours() )
			{
				terrain.chunksNeedingMesh.enqueueTask( this );
				terrain.chunksNeedingCollisionMesh.enqueueTask( this );
			}
		}
	}


	public void generateNeighbours()
	{
		var distanceToPlayerSqr = (terrain.player.transform.position - center).sqrMagnitude;

		if ( distanceToPlayerSqr > terrain.displayChunkDistanceSqr ) return;

//		Debug.Log( "Generating neighbours for " + position );

		if ( null == neighbourRight )
		{
			terrain.getChunk( position + Position3.right ).neighbourLeft = this;
		}
		else
		{
			neighbourRight.neighbourLeft = this;
			neighbourRight.neighbourAdded();
		}

		if ( null == neighbourLeft )
		{
			terrain.getChunk( position + Position3.left ).neighbourRight = this;
		}
		else
		{
			neighbourLeft.neighbourRight = this;
			neighbourLeft.neighbourAdded();
		}

		if ( null == neighbourUp )
		{
			terrain.getChunk( position + Position3.up ).neighbourDown = this;
		}
		else
		{
			neighbourUp.neighbourDown = this;
			neighbourUp.neighbourAdded();
		}

		if ( null == neighbourDown )
		{
			terrain.getChunk( position + Position3.down ).neighbourUp = this;
		}
		else
		{
			neighbourDown.neighbourUp = this;
			neighbourDown.neighbourAdded();
		}

		if ( null == neighbourForward )
		{
			terrain.getChunk( position + Position3.forward ).neighbourBack = this;
		}
		else
		{
			neighbourForward.neighbourBack = this;
			neighbourForward.neighbourAdded();
		}

		if ( null == neighbourBack )
		{
			terrain.getChunk( position + Position3.back ).neighbourForward = this;
		}
		else
		{
			neighbourBack.neighbourForward = this;
			neighbourBack.neighbourAdded();
		}
	}


	public void generateMesh( Position3 positionRelativeToPlayer, ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector2> uvs )
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
					if ( blocks[ x + size * (y + size * z) ] == 0 ) continue;

					if ( positionRelativeToPlayer.x >= 0f && Block.isTransparent( getBlock( x + 1, y, z ) ) ) right[ z, y, size - 1 - x ] = true;
					if ( positionRelativeToPlayer.x <= 0f && Block.isTransparent( getBlock( x - 1, y, z ) ) ) left[ size - 1 - z, y, x ] = true;
					if ( positionRelativeToPlayer.y >= 0f && Block.isTransparent( getBlock( x, y + 1, z ) ) ) top[ x, z, size - 1 - y ] = true;
					if ( positionRelativeToPlayer.y <= 0f && Block.isTransparent( getBlock( x, y - 1, z ) ) ) bottom[ x, size - 1 - z, y ] = true;
					if ( positionRelativeToPlayer.z >= 0f && Block.isTransparent( getBlock( x, y, z + 1 ) ) ) front[ size - 1 - x, y, size - 1 - z ] = true;
					if ( positionRelativeToPlayer.z <= 0f && Block.isTransparent( getBlock( x, y, z - 1 ) ) ) back[ x, y, z ] = true;
				}
			}
		}

		if ( positionRelativeToPlayer.x >= 0f ) drawFaces( ref right, Vector3.right * size, Vector3.forward, Vector3.up, Vector3.left, ref vertices, ref triangles, ref uvs );
		if ( positionRelativeToPlayer.x <= 0f ) drawFaces( ref left, Vector3.forward * size, Vector3.back, Vector3.up, Vector3.right, ref vertices, ref triangles, ref uvs );
		if ( positionRelativeToPlayer.y >= 0f ) drawFaces( ref top, Vector3.up * size, Vector3.right, Vector3.forward, Vector3.down, ref vertices, ref triangles, ref uvs );
		if ( positionRelativeToPlayer.y <= 0f ) drawFaces( ref bottom, Vector3.forward * size, Vector3.right, Vector3.back, Vector3.up, ref vertices, ref triangles, ref uvs );
		if ( positionRelativeToPlayer.z >= 0f ) drawFaces( ref front, Vector3.forward * size + Vector3.right * size, Vector3.left, Vector3.up, Vector3.back, ref vertices, ref triangles, ref uvs );
		if ( positionRelativeToPlayer.z <= 0f ) drawFaces( ref back, Vector3.zero, Vector3.right, Vector3.up, Vector3.forward, ref vertices, ref triangles, ref uvs );
	}


	void drawFaces( ref bool[,,] faces, Vector3 offset, Vector3 right, Vector3 up, Vector3 forward, ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector2> uvs )
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

					drawFace( offset + x * right + y * up + z * forward, right * width, up * height, ref vertices, ref triangles, ref uvs );

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


	void drawFace( Vector3 origin, Vector3 right, Vector3 up, ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector2> uvs )
	{
		int index = vertices.Count;

		vertices.Add( origin );
		vertices.Add( origin + up * 1.0001f );
		vertices.Add( origin + (up + right) * 1.0001f );
		vertices.Add( origin + right * 1.0001f );

		if ( uvs != null )
		{
			var uvUp = Vector2.up * up.magnitude;
			var uvRight = Vector2.right * right.magnitude;

			uvs.Add( Vector2.zero );
			uvs.Add( uvUp );
			uvs.Add( uvUp + uvRight );
			uvs.Add( uvRight );
		}

		triangles.Add( index );
		triangles.Add( index + 1 );
		triangles.Add( index + 2 );

		triangles.Add( index + 2 );
		triangles.Add( index + 3 );
		triangles.Add( index );
	}
}
