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
					blocksTemp[ x, y, z ] = ( 0 < x ) && ( x < 16 ) && ( 0 < y ) && ( y < 16 ) && ( 0 < z ) && ( z < 16 ) ? Block.Type.dirt : Block.Type.none;
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


	struct Size
	{
		public byte w;
		public byte h;
	}


	void drawBlocks()
	{
		var right = new Size[size, size, size];
//		var left = new Size[size, size, size];
//		var up = new Size[size, size, size];
//		var down = new Size[size, size, size];
//		var forward = new Size[size, size, size];
//		var back = new Size[size, size, size];

		var faceSize = new Size { w = 1, h = 1 };

		for ( int x = 0; x < size; ++x )
		{
			for ( int y = 0; y < size; ++y )
			{
				for ( int z = 0; z < size; ++z )
				{
					if ( blocks[ x, y, z ] == 0 ) continue;
					
					if ( Block.isTransparent( getBlock( x + 1, y, z ) ) )
					{
						right[ x, y, z ] = faceSize;
						Debug.Log( "Set " + x + ", " + y + ", " + z + " to " + right[ x, y, z ].w + ", " + right[ x, y, z ].h );
					}
//					if ( Block.isTransparent( getBlock( x - 1, y, z ) ) ) left[ x, y, z ] = faceSize;
//					if ( Block.isTransparent( getBlock( x, y + 1, z ) ) ) up[ x, y, z ] = faceSize;
//					if ( Block.isTransparent( getBlock( x, y - 1, z ) ) ) down[ x, y, z ] = faceSize;
//					if ( Block.isTransparent( getBlock( x, y, z + 1 ) ) ) forward[ x, y, z ] = faceSize;
//					if ( Block.isTransparent( getBlock( x, y, z - 1 ) ) ) back[ x, y, z ] = faceSize;
				}
			}
		}

//		Debug.Log( "rights facing faces: " + right.Count );
//		printFaces( right );
	
//		right = optimizeFaces( right, Position3.up, Position3.forward, Position3.left );
//		left = optimizeFaces( left, Position3.up, Position3.forward, Position3.left );
//		up = optimizeFaces( up, Position3.forward, Position3.right, Position3.left );
//		down = optimizeFaces( down, Position3.right, Position3.forward, Position3.left );
//		forward = optimizeFaces( forward, Position3.up, Position3.right, Position3.left );
//		back = optimizeFaces( back, Position3.up, Position3.right, Position3.left ); 

//		Debug.Log( "rights facing faces optimized: " + right.Count );

//		printFaces( right );

		mergeFaces( ref right );


		drawFaces( right, Vector3.right, Vector3.up, Vector3.forward );
//		drawFaces( left, Vector3.forward, Vector3.up, Vector3.back );
//		drawFaces( up, Vector3.up, Vector3.forward, Vector3.right );
//		drawFaces( down, Vector3.zero, Vector3.right, Vector3.forward );
//		drawFaces( forward, Vector3.forward + Vector3.right, Vector3.up, Vector3.left );
//		drawFaces( back, Vector3.zero, Vector3.up, Vector3.right );

		buildingMesh = false;
		hasMesh = true;
	}


	void mergeFaces( ref Size[,,] faces )
	{
		Debug.Log( "MergeFaces" );

		growFace( ref faces, new Position3( 2, 2, 2 ) );

//		for ( int x = 0; x < size; ++x )
//		{
//			for ( int y = 0; y < size; ++y )
//			{
//				for ( int z = 0; z < size; ++z )
//				{
//					growFace( faces, new Position3( x, y, z ) );
//				}
//			}
//		}
	}


	void growFace( ref Size[,,] faces, Position3 facePosition )
	{
		var faceSize = faces[ facePosition.x, facePosition.y, facePosition.z ];

		Debug.Log( "Grow face at " + facePosition + " size: " + faceSize.w + ", " + faceSize.h );

		if ( faceSize.w == 0 || faceSize.h == 0 ) return;
//
//		var neighbourPosition = facePosition;
//
//		neighbourPosition.x = facePosition.x + faceSize.w;

//		while ( faces[ neighbourPosition.x, neighbourPosition.y, neighbourPosition.z ].h == faceSize.h )
//		{
//			faceSize.w += faces[ neighbourPosition.x, neighbourPosition.y, neighbourPosition.z ].w;
//			faces[ neighbourPosition.x, neighbourPosition.y, neighbourPosition.z ].h = 0;
//			faces[ neighbourPosition.x, neighbourPosition.y, neighbourPosition.z ].w = 0;
//
//			neighbourPosition.x = facePosition.x + faceSize.w;
//		}

		Debug.Log( "Result size: " + faceSize.w + ", " + faceSize.h );

		faces[ facePosition.x, facePosition.y, facePosition.z ] = faceSize;
	}


	Dictionary< Position3, Position2 > optimizeFaces( Dictionary< Position3, Position2 > faces, Position3 up, Position3 right, Position3 lastFace )
	{
		Dictionary< Position3, Position2 > best = faces;

		foreach ( Position3 currentFace in faces.Keys.ToList().OrderBy( key => key ) )
		{
			if ( currentFace.CompareTo( lastFace ) < 0 ) return best;

			Position2 faceSize = faces[ currentFace ]; 

			Position2 neighbourFaceSize;
			Position3 neighbourFacePosition = currentFace + right * faceSize.x;

			if ( faces.TryGetValue( neighbourFacePosition, out neighbourFaceSize ) && faceSize.y == neighbourFaceSize.y )
			{
				Dictionary< Position3, Position2 > facesMergedRight = new Dictionary< Position3, Position2 >( faces );

				facesMergedRight.Remove( neighbourFacePosition );

				Position2 newFaceSize = new Position2( faceSize.x + neighbourFaceSize.x, faceSize.y );
				facesMergedRight[ currentFace ] = newFaceSize;

				facesMergedRight = optimizeFaces( facesMergedRight, up, right, currentFace );

				if ( facesMergedRight.Count < best.Count )
				{
					best = facesMergedRight;
				}
			}

			neighbourFacePosition = currentFace + up * faceSize.y;

			if ( faces.TryGetValue( neighbourFacePosition, out neighbourFaceSize ) && faceSize.x == neighbourFaceSize.x )
			{
				Dictionary< Position3, Position2 > facesMergedUp = new Dictionary< Position3, Position2 >( faces );

				facesMergedUp.Remove( neighbourFacePosition );

				Position2 newFaceSize = new Position2( faceSize.x, faceSize.y + neighbourFaceSize.y );
				facesMergedUp[ currentFace ] = newFaceSize;

				facesMergedUp = optimizeFaces( facesMergedUp, up, right, currentFace );

//				Debug.Log( "merging face (" + origo + ", " + faceSize + ") with (" + neighbourFacePosition + ", " + neighbourFaceSize + " into (" + origo + newFaceSize + ")" );

				if ( facesMergedUp.Count < best.Count )
				{
					best = facesMergedUp;
				}
			}
		}

		return best;
	}


	void drawFaces( Size[,,] faces, Vector3 offset, Vector3 up, Vector3 right )
	{
		for ( int x = 0; x < size; ++x )
		{
			for ( int y = 0; y < size; ++y )
			{
				for ( int z = 0; z < size; ++z )
				{
					Size face = faces[ x, y, z ];

					if ( face.w == 0 || face.h == 0 ) continue;

					drawFace( offset + new Vector3( x, y, z ), up * face.h, right * face.w );
				}
			}
		}
	}


	void drawFace( Vector3 origin, Vector3 up, Vector3 right )
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
