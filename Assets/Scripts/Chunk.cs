using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Threading;
using System.Linq;
using System.Xml;
using System.ComponentModel;
using System.Configuration;


[RequireComponent( typeof( MeshFilter ) )]
[RequireComponent( typeof( MeshRenderer ) )]
[RequireComponent( typeof( MeshCollider ) )]
public class Chunk : MonoBehaviour
{
	public Terrain terrain;
	public int size;
	public Vector3 center;
	public Position3 position;
	MeshFilter meshFilter;
	MeshCollider meshCollider;
	Block.Type[,,] blocks;
	bool hasMesh;
	bool buildingMesh;
	Chunk neighbourRight;
	Chunk neighbourLeft;
	Chunk neighbourUp;
	Chunk neighbourDown;
	Chunk neighbourForward;
	Chunk neighbourBack;
	readonly List<Vector3> vertices = new List<Vector3>();
	readonly List<int> triangles = new List<int>();
	readonly List<Vector2> uvs = new List<Vector2>();


	public delegate void Task();


	public static Semaphore backgroundTasksCount = new Semaphore( 0, int.MaxValue );
	public static readonly Queue< Task > backgroundTasks = new Queue<Task>();


	public static void backgroundTask()
	{
		while ( true )
		{
			backgroundTasksCount.WaitOne();

			Task task;

			lock ( backgroundTasks )
			{
				task = backgroundTasks.Dequeue();
			}

			task();
		}
	}


	public static void enqueueBackgroundTask( Task task )
	{
		lock ( backgroundTasks )
		{
			backgroundTasks.Enqueue( task );
		}

		backgroundTasksCount.Release();
	}


	public Block.Type getBlock( Position3 blockPosition )
	{
		return getBlock( blockPosition.x, blockPosition.y, blockPosition.z );
	}


	public Block.Type getBlock( int x, int y, int z )
	{
		if ( x < 0 )
		{
			return neighbourLeft.getBlock( x + size, y, z );
		}
		if ( x >= size )
		{
			return neighbourRight.getBlock( x - size, y, z );
		}
		if ( y < 0 )
		{
			return neighbourDown.getBlock( x, y + size, z );
		}
		if ( y >= size )
		{
			return neighbourUp.getBlock( x, y - size, z );
		}
		if ( z < 0 )
		{
			return neighbourBack.getBlock( x, y, z + size );
		} 
		if ( z >= size )
		{
			return neighbourForward.getBlock( x, y, z - size );
		}

		return blocks[ x, y, z ];
	}


	void Start()
	{
		center = transform.position + Vector3.one * size / 2f;
		meshFilter = GetComponent< MeshFilter >();
		meshCollider = GetComponent< MeshCollider >();
	}


	void Update()
	{
		if ( blocks == null ) return;

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
		}
		
		if ( distanceToPlayerSqr > terrain.destroyChunkDistanceSqr )
		{
			activateNeighbours();

			terrain.deleteChunk( this );
		}
	}


	public void generateBlocks()
	{
//		Debug.Log( "Generating blocks for chunk at " + position + " on frame " + Time.frameCount );

		var blocksTemp = new Block.Type[ size, size, size ];

		for ( int x = 0; x < size; ++x )
		{
//			var positionX = (float)(position.x * size + x) / 100f;

			for ( int y = 0; y < size; ++y )
			{
//				var positionY = (float)(position.y * size + y) / 100f;

				for ( int z = 0; z < size; ++z )
				{
//					var positionZ = (float)(position.z * size + z) / 100f;

//					blocksTemp[ x, y, z ] = SimplexNoise.Noise.Generate( positionX, positionY, positionZ ) < 0f ? Block.Type.dirt : Block.Type.air;
//					blocksTemp[ x, y, z ] = ( 0 < x ) && ( x < 16 ) && ( 0 < y ) && ( y < 16 ) && ( 0 < z ) && ( z < 16 ) ? Block.Type.dirt : Block.Type.none;

					blocksTemp[ x, y, z ] = (
					    ( ( x == 0 ) && ( y == 0 ) && ( z == 0 ) ) ||
					    ( ( x == 1 ) && ( y == 0 ) && ( z == 0 ) ) ||
					    ( ( x == 0 ) && ( y == 1 ) && ( z == 0 ) ) ||
					    ( ( x == 0 ) && ( y == 2 ) && ( z == 0 ) ) ||
					    ( ( x == 0 ) && ( y == 0 ) && ( z == 1 ) ) ||
					    ( ( x == 0 ) && ( y == 0 ) && ( z == 2 ) ) ||
					    ( ( x == 0 ) && ( y == 0 ) && ( z == 3 ) )
					) ? Block.Type.dirt : Block.Type.none;
				}
			}
		}

		blocks = blocksTemp;
	}


	void disableMesh()
	{
		renderer.enabled = false;
	}


	IEnumerator generateMesh()
	{
//		Debug.Log( "Generating mesh for chunk at " + position + " on frame " + Time.frameCount );

		if ( hasMesh )
		{
			renderer.enabled = true;
			yield break;
		}
		
		if ( buildingMesh )
		{
			Debug.LogError( "generating mesh more than once" );
			yield break;
		}

		buildingMesh = true;

//		enqueueBackgroundTask( drawBlocks );
		drawBlocks();

		while ( !hasMesh ) yield return null;

		if ( triangles.Count == 0 )
		{
			renderer.enabled = false;
			meshFilter.mesh = null; 
		}
		else
		{
			var mesh = new Mesh();
			mesh.vertices = vertices.ToArray();
			mesh.triangles = triangles.ToArray();
			mesh.uv = uvs.ToArray();

			mesh.RecalculateBounds();
			mesh.RecalculateNormals();

			meshFilter.mesh = mesh;
			meshCollider.sharedMesh = mesh;

			renderer.enabled = true;
		}

		vertices.Clear();
		triangles.Clear();
		uvs.Clear();
	}


	void generateNeighbours()
	{
//		Debug.Log( "Generating neighbours for chunk at " + position + " on frame " + Time.frameCount );

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
		var up = new bool[size, size, size];
		var down = new bool[size, size, size];
		var forward = new bool[size, size, size];
		var back = new bool[size, size, size];

		for ( int x = 0; x < size; ++x )
		{
			for ( int y = 0; y < size; ++y )
			{
				for ( int z = 0; z < size; ++z )
				{
					if ( blocks[ x, y, z ] == 0 ) continue;

					Debug.Log( "Block at (" + x + "," + y + "," + z + ")" );

					if ( Block.isTransparent( getBlock( x + 1, y, z ) ) ) right[ x, y, z ] = true;
					if ( Block.isTransparent( getBlock( x - 1, y, z ) ) ) left[ x, y, z ] = true;
					if ( Block.isTransparent( getBlock( x, y + 1, z ) ) ) up[ x, y, z ] = true;
					if ( Block.isTransparent( getBlock( x, y - 1, z ) ) ) down[ x, y, z ] = true;
					if ( Block.isTransparent( getBlock( x, y, z + 1 ) ) ) forward[ x, y, z ] = true;
					if ( Block.isTransparent( getBlock( x, y, z - 1 ) ) ) back[ x, y, z ] = true;
				}
			}
		}

		drawFaces( right, Vector3.right, Vector3.forward, Vector3.up );
		drawFaces( left, Vector3.up, Vector3.forward, Vector3.down );
		drawFaces( up, Vector3.up, Vector3.right, Vector3.forward );
		drawFaces( down, Vector3.forward, Vector3.right, Vector3.back );
		drawFaces( forward, Vector3.forward + Vector3.right, Vector3.left, Vector3.up );
		drawFaces( back, Vector3.zero, Vector3.right, Vector3.up );

		buildingMesh = false;
		hasMesh = true;
	}


	void drawFaces( bool[,,] faces, Vector3 offset, Vector3 right, Vector3 up )
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

					while ( x + width < size && faces[ x + width, y, z ] )
					{
						++width;
//						faces[ x, y, z + width ] = false;
					}

					Debug.Log( "Face at (" + x + "," + y + "," + z + ") size (" + width + "," + height );

					drawFace( offset + new Vector3( x, y, z ), right * width, up * height );

					x += width;
				}
			}
		}
	}


	void drawFace( Vector3 origin, Vector3 right, Vector3 up )
	{
//		Debug.Log( "Drawing face " + origin + " up: " + up + " right: " + right );

		int index = vertices.Count;

		vertices.Add( origin );
		vertices.Add( origin + up );
		vertices.Add( origin + up + right );
		vertices.Add( origin + right );

		uvs.Add( Vector2.zero );
		uvs.Add( Vector2.up );
		uvs.Add( Vector2.up + Vector2.right );
		uvs.Add( Vector2.right );

		triangles.Add( index );
		triangles.Add( index + 1 );
		triangles.Add( index + 2 );

		triangles.Add( index + 2 );
		triangles.Add( index + 3 );
		triangles.Add( index );
	}
}
