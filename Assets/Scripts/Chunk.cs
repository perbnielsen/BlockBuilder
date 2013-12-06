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
	Chunk _neighbourRight;
	Chunk _neighbourLeft;
	Chunk _neighbourUp;
	Chunk _neighbourDown;
	Chunk _neighbourForward;
	Chunk _neighbourBack;


	Chunk neighbourRight {
		get { return _neighbourRight; }
		set
		{
			_neighbourRight = value;
			updateHasAllNeighbours();
		}
	}


	Chunk neighbourLeft {
		get { return _neighbourLeft; }
		set
		{
			_neighbourLeft = value;
			updateHasAllNeighbours();
		}
	}


	Chunk neighbourUp {
		get { return _neighbourUp; }
		set
		{
			_neighbourUp = value;
			updateHasAllNeighbours();
		}
	}


	Chunk neighbourDown {
		get { return _neighbourDown; }
		set
		{
			_neighbourDown = value;
			updateHasAllNeighbours();
		}
	}


	Chunk neighbourForward {
		get { return _neighbourForward; }
		set
		{
			_neighbourForward = value;
			updateHasAllNeighbours();
		}
	}


	Chunk neighbourBack {
		get { return _neighbourBack; }
		set
		{
			_neighbourBack = value;
			updateHasAllNeighbours();
		}
	}


	[Flags]
	enum State : byte
	{
		Initialised = 0x0,
		HasBlocks = 0x1,
		GeneratingAllNeighbours = 0x2,
		HasMesh = 0x4,
		HasCollisionMesh = 0x8,
		IsActive = 0x10
	}


	State state;


	public float getPriority()
	{
		Vector3 fromPlayerToChunk = center - terrain.player.transform.position;

		return (Vector3.Dot( fromPlayerToChunk.normalized, terrain.player.transform.forward.normalized ) + 1.25f) / fromPlayerToChunk.sqrMagnitude;
	}


	bool hasAllNeighbours;


	void updateHasAllNeighbours()
	{
		hasAllNeighbours = 
			(neighbourRight != null
		&& neighbourLeft != null
		&& neighbourUp != null
		&& neighbourDown != null
		&& neighbourForward != null
		&& neighbourBack != null);
	}


	public bool hasBlocks { get { return (state & State.HasBlocks) == State.HasBlocks; } }


	public bool hasMesh { get { return (state & State.HasMesh) == State.HasMesh; } }


	public bool hasCollisionMesh { get { return (state & State.HasCollisionMesh) == State.HasCollisionMesh; } }


	public bool isActive { get { return (state & State.IsActive) == State.IsActive; } }


	[ContextMenu( "Show state" )]
	void showState()
	{
		Debug.Log( state );
	}


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
				using ( var binaryReader = new BinaryReader( fs ) )
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
		buildMeshState = 11;

		if ( !hasAllNeighbours ) return Block.Type.none;
		
		if ( x < 0 ) return neighbourLeft.getBlock( x + size, y, z );
		if ( x >= size ) return neighbourRight.getBlock( x - size, y, z );

		if ( y < 0 ) return neighbourDown.getBlock( x, y + size, z );
		if ( y >= size ) return neighbourUp.getBlock( x, y - size, z );

		if ( z < 0 ) return neighbourBack.getBlock( x, y, z + size );
		if ( z >= size ) return neighbourForward.getBlock( x, y, z - size );


		buildMeshState = 12;

		if ( blocks == null ) return Block.Type.none;

		return (Block.Type)blocks[ x + size * (y + size * z) ];
	}


	public void setBlock( Position3 blockPosition, Block.Type blockType )
	{
		if ( blocks == null ) return;

		// Set block 
		blocks[ blockPosition.x + size * (blockPosition.y + size * blockPosition.z) ] = (byte)blockType;

		terrain.chunksNeedingMesh.enqueueTask( this );
		terrain.chunksNeedingCollisionMesh.enqueueTask( this );

		// Update any neighbour chunk that might be affected
		if ( blockPosition.x == 0 && neighbourLeft != null ) neighbourLeft.neighbourBlocksHaveChanged();
		if ( blockPosition.y == 0 && neighbourDown != null ) neighbourDown.neighbourBlocksHaveChanged();
		if ( blockPosition.z == 0 && neighbourBack != null ) neighbourBack.neighbourBlocksHaveChanged();

		if ( blockPosition.x == size - 1 && neighbourRight != null ) neighbourRight.neighbourBlocksHaveChanged();
		if ( blockPosition.y == size - 1 && neighbourUp != null ) neighbourUp.neighbourBlocksHaveChanged();
		if ( blockPosition.z == size - 1 && neighbourForward != null ) neighbourForward.neighbourBlocksHaveChanged();

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

		terrain.runOnMainThread.Add( () =>
		{
			terrain.inactiveChunks.Add( this );
			informNeighboursOfBlockGeneration();
		} );

		state |= State.HasBlocks;
	}


	public void generateBlocks()
	{
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
	}


	[ContextMenu( "Check if still inactive" )]
	public bool checkIfStillInactive()
	{
		var distanceToPlayerSqr = (terrain.player.transform.position - center).sqrMagnitude;

		if ( distanceToPlayerSqr > terrain.destroyChunkDistanceSqr )
		{
//			Debug.Log( "Chunk at " + position + " is being destroyed" );
			terrain.deleteChunk( this );

			return false;
		}
		else
		if ( distanceToPlayerSqr < terrain.displayChunkDistanceSqr )
		{
//			Debug.Log( "Chunk at " + position + " is turning active" );

			state |= State.IsActive;

			terrain.activeChunks.Add( this );

			if ( !hasAllNeighbours )
			{
				generateNeighbours();
			}
			else
			{
				if ( hasMesh )
				{
					terrain.runOnMainThread.Add( () => renderer.enabled = true );
				}
				else
				{
					terrain.chunksNeedingMesh.enqueueTask( this );
				}

				if ( hasCollisionMesh )
				{
					terrain.runOnMainThread.Add( () => meshCollider.enabled = true );
				}
				else
				{
					terrain.chunksNeedingCollisionMesh.enqueueTask( this );
				}
			}

			return false;
		}

		return true;
	}


	[ContextMenu( "Check if still active" )]
	public bool checkIfStillActive()
	{
		var distanceToPlayerSqr = (terrain.player.transform.position - center).sqrMagnitude;

		if ( distanceToPlayerSqr > terrain.disableChunkDistanceSqr )
		{
			state &= ~State.IsActive;

			terrain.runOnMainThread.Add( () =>
			{
				renderer.enabled = false;
				meshCollider.enabled = false;
			} );

			terrain.chunksNeedingBlocks.dequeueTask( this );
			terrain.chunksNeedingCollisionMesh.dequeueTask( this );
			terrain.chunksNeedingMesh.dequeueTask( this );

			terrain.inactiveChunks.Add( this );

			return false;
		}

		return true;
	}


	public void setMesh( Vector3[] vertices, int[] triangles, Vector2[] uvs )
	{
//		destroyMesh();
		state |= State.HasMesh;

		if ( triangles.Length == 0 ) return;

		if ( meshFilter.mesh == null ) meshFilter.mesh = new Mesh();

		Mesh mesh = meshFilter.mesh;
		mesh.Clear();

		mesh.vertices = vertices;
		mesh.uv = uvs;
		mesh.triangles = triangles;

//		mesh.Optimize();
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		renderer.enabled = true;
	}


	public void setCollisionMesh( Vector3[] vertices, int[] triangles )
	{
//		destroyCollisionMesh();
		state |= State.HasCollisionMesh;

		if ( meshCollider == null ) return;
		
		meshCollider.enabled = false;

		if ( triangles.Length == 0 ) return;

		if ( meshCollider.sharedMesh == null ) meshCollider.sharedMesh = new Mesh();

		Mesh mesh = meshCollider.sharedMesh;
		mesh.Clear();

		mesh.vertices = vertices;
		mesh.triangles = triangles;

		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		meshCollider.enabled = true;
	}


	void OnDestroy()
	{
//		Debug.Log( "Destroying chunk at " + position );

		terrain.chunksNeedingBlocks.dequeueTask( this );
		terrain.chunksNeedingMesh.dequeueTask( this );
		terrain.chunksNeedingCollisionMesh.dequeueTask( this );

		if ( neighbourRight != null ) neighbourRight.neighbourLeft = null;
		if ( neighbourLeft != null ) neighbourLeft.neighbourRight = null;
		if ( neighbourUp != null ) neighbourUp.neighbourDown = null; 
		if ( neighbourDown != null ) neighbourDown.neighbourUp = null; 
		if ( neighbourForward != null ) neighbourForward.neighbourBack = null; 
		if ( neighbourBack != null ) neighbourBack.neighbourForward = null; 

		if ( !terrain.isQuitting )
		{
			destroyMesh();
			destroyCollisionMesh();
		}
	}


	void destroyMesh()
	{
		state &= ~State.HasMesh;

		renderer.enabled = false;

		Destroy( meshFilter.mesh );
	}


	void destroyCollisionMesh()
	{
		state &= ~State.HasCollisionMesh;

		meshCollider.enabled = false;

		Destroy( meshCollider.sharedMesh );
	}


	public void neighbourBlocksHaveChanged()
	{
		if ( hasAllNeighbours && isActive )
		{
			terrain.chunksNeedingMesh.enqueueTask( this );
			terrain.chunksNeedingCollisionMesh.enqueueTask( this );
		}
	}


	public void informNeighboursOfBlockGeneration()
	{
//		Debug.Log( "Generating neighbours for " + position );

		if ( neighbourRight = terrain.getChunk( position + Position3.right, createIfNonexistent: false ) )
		{
			neighbourRight.neighbourLeft = this;
			neighbourRight.neighbourBlocksHaveChanged();
		}

		if ( neighbourLeft = terrain.getChunk( position + Position3.left, createIfNonexistent: false ) )
		{
			neighbourLeft.neighbourRight = this;
			neighbourLeft.neighbourBlocksHaveChanged();
		}

		if ( neighbourUp = terrain.getChunk( position + Position3.up, createIfNonexistent: false ) )
		{
			neighbourUp.neighbourDown = this;
			neighbourUp.neighbourBlocksHaveChanged();
		}

		if ( neighbourDown = terrain.getChunk( position + Position3.down, createIfNonexistent: false ) )
		{
			neighbourDown.neighbourUp = this;
			neighbourDown.neighbourBlocksHaveChanged();
		}

		if ( neighbourForward = terrain.getChunk( position + Position3.forward, createIfNonexistent: false ) )
		{
			neighbourForward.neighbourBack = this;
			neighbourForward.neighbourBlocksHaveChanged();
		}

		if ( neighbourBack = terrain.getChunk( position + Position3.back, createIfNonexistent: false ) )
		{
			neighbourBack.neighbourForward = this;
			neighbourBack.neighbourBlocksHaveChanged();
		}
	}


	[ContextMenu( "Generate neighbours" )]
	public void generateNeighbours()
	{
		state |= State.GeneratingAllNeighbours;

		if ( neighbourRight == null ) terrain.getChunk( position + Position3.right ).neighbourLeft = this;
		if ( neighbourLeft == null ) terrain.getChunk( position + Position3.left ).neighbourRight = this;
		if ( neighbourUp == null ) terrain.getChunk( position + Position3.up ).neighbourDown = this;
		if ( neighbourDown == null ) terrain.getChunk( position + Position3.down ).neighbourUp = this;
		if ( neighbourForward == null ) terrain.getChunk( position + Position3.forward ).neighbourBack = this;
		if ( neighbourBack == null ) terrain.getChunk( position + Position3.back ).neighbourForward = this;
	}


	public static int buildMeshState = 0;


	[ContextMenu( "Print buildMeshState" )]
	void printBuildMeshState()
	{
		Debug.Log( buildMeshState );
	}


	[ContextMenu( "Build mesh" )]
	public void buildMesh()
	{
		buildMeshState = 1;
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		List<Vector2> uvs = new List<Vector2>();

		buildMeshState = 2;

		var positionRelativeToPlayer = terrain.player.chunk.position - position;
		buildMeshState = 3;
		generateMesh( positionRelativeToPlayer, ref vertices, ref triangles, ref uvs );

		buildMeshState = 4;

		var vertexArray = vertices.ToArray();
		var triangleArray = triangles.ToArray();
		var uvArray = uvs.ToArray();

		buildMeshState = 5;

//		if ( triangles.Count > 0 ) 
		terrain.runOnMainThread.Add( () => setMesh( vertexArray, triangleArray, uvArray ) );

		buildMeshState = 6;
	}


	[ContextMenu( "Build collision mesh" )]
	public void buildCollisionMesh()
	{
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		List<Vector2> uvs = new List<Vector2>();

		generateMesh( Position3.zero, ref vertices, ref triangles, ref uvs );

		var vertexArray = vertices.ToArray();
		var triangleArray = triangles.ToArray();

//		if ( triangles.Count > 0 ) 
		terrain.runOnePerFrameOnMainThread.Add( () => setCollisionMesh( vertexArray, triangleArray ) );
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
						for ( int i = 0; i < width; i++ ) faces[ x + i, y + height, z ] = false;
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
		vertices.Add( origin + up );
		vertices.Add( origin + (up + right) );
		vertices.Add( origin + right );

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


	public List< Chunk > getAllNeighboursInX()
	{
		var neighbours = new List< Chunk >();

		for ( var currentChunkRight = this; currentChunkRight.neighbourRight != null; currentChunkRight = currentChunkRight.neighbourRight )
		{
			neighbours.Add( currentChunkRight );
		}

		for ( var currentChunkLeft = this; currentChunkLeft.neighbourLeft != null; currentChunkLeft = currentChunkLeft.neighbourLeft )
		{
			neighbours.Add( currentChunkLeft );
		}

		return neighbours;
	}


	public List< Chunk > getAllNeighboursInY()
	{
		var neighbours = new List< Chunk >();

		for ( var currentChunkUp = this; currentChunkUp.neighbourUp != null; currentChunkUp = currentChunkUp.neighbourUp )
		{
			neighbours.Add( currentChunkUp );
		}

		for ( var currentChunkDown = this; currentChunkDown.neighbourDown != null; currentChunkDown = currentChunkDown.neighbourDown )
		{
			neighbours.Add( currentChunkDown );
		}

		return neighbours;
	}


	public List< Chunk > getAllNeighboursInZ()
	{
		var neighbours = new List< Chunk >();

		for ( var currentChunkForward = this; currentChunkForward.neighbourForward != null; currentChunkForward = currentChunkForward.neighbourForward )
		{
			neighbours.Add( currentChunkForward );
		}

		for ( var currentChunkBack = this; currentChunkBack.neighbourBack != null; currentChunkBack = currentChunkBack.neighbourBack )
		{
			neighbours.Add( currentChunkBack );
		}

		return neighbours;
	}


	public List< Chunk > getAllNeighboursInXYPlane()
	{
		var neighboursInXY = new List< Chunk >();

		var neighboursInX = getAllNeighboursInX();

		foreach ( var neighbour in neighboursInX )
		{
			neighboursInXY.AddRange( neighbour.getAllNeighboursInY() );
		}

		neighboursInXY.AddRange( neighboursInX );

		return neighboursInXY;
	}


	public List< Chunk > getAllNeighboursInXZPlane()
	{
		var neighboursInXZ = new List< Chunk >();

		var neighboursInX = getAllNeighboursInX();

		foreach ( var neighbour in neighboursInX )
		{
			neighboursInXZ.AddRange( neighbour.getAllNeighboursInZ() );
		}

		neighboursInXZ.AddRange( neighboursInX );

		return neighboursInXZ;
	}


	public List< Chunk > getAllNeighboursInYZPlane()
	{
		var neighboursInYZ = new List< Chunk >();

		var neighboursInY = getAllNeighboursInY();

		foreach ( var neighbour in neighboursInY )
		{
			neighboursInYZ.AddRange( neighbour.getAllNeighboursInZ() );
		}

		neighboursInYZ.AddRange( neighboursInY );

		return neighboursInYZ;
	}
}
