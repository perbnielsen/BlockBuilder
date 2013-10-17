using UnityEngine;
using System.Collections.Generic;
using System.Collections;


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
	Chunk neighbourRight;
	Chunk neighbourLeft;
	Chunk neighbourUp;
	Chunk neighbourDown;
	Chunk neighbourForward;
	Chunk neighbourBack;


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


	IEnumerator Start()
	{
		center = transform.position + Vector3.one * size;
		meshFilter = GetComponent< MeshFilter >();
		meshCollider = GetComponent< MeshCollider >();

		while ( (terrain.player.position - center).sqrMagnitude < terrain.destroyChunkDistanceSqr )
		{
			if ( !hasMesh && (terrain.player.position - center).sqrMagnitude < terrain.displayChunkDistanceSqr )
			{
				generateMesh();
			}

			if ( hasMesh && (terrain.player.position - center).sqrMagnitude > terrain.displayChunkDistanceSqr + size )
			{
				disableMesh();
			}

			yield return null;
		}

//		Debug.Log( "Deleting chunk at " + position );

		terrain.chunks.Remove( position );

		Destroy( gameObject );
	}


	public void generateBlocks()
	{
//		Debug.Log( "Generating blocks for chunk at " + position + " on frame " + Time.frameCount );

		var blocksTemp = new Block.Type[ size, size, size ];

		for ( int x = 0; x < size; ++x )
		{
			for ( int y = 0; y < size; ++y )
			{
				for ( int z = 0; z < size; ++z )
				{
//					blocksTemp[ x, y, z ] = (UnityEngine.Random.value > 0.5f) ? Block.Type.dirt : Block.Type.air;
//					blocksTemp[ x, y, z ] = Mathf.Sin( (position.x * size + x + position.z * size + z) / 20f ) * 5f > (position.y * size + y) ? Block.Type.dirt : Block.Type.air;
					blocksTemp[ x, y, z ] = SimplexNoise.Noise.Generate( (float)(position.x * size + x) / 10000f, (float)(position.z * size + z) / 10000f ) > (float)(position.y * size + y) / 500f ? Block.Type.dirt : Block.Type.air;
				}
			}
		}

		blocks = blocksTemp;
	}


	void disableMesh()
	{
		renderer.enabled = false;

//		renderer.material.color = new Color( 12f / 256f, 154f / 256f, 92f / 256f, 13f / 256f );

		hasMesh = false;
	}


	void generateMesh( bool regenerate = false )
	{
		if ( hasMesh && !regenerate ) return;

//		Debug.Log( "Generating mesh for chunk at " + position + " on frame " + Time.frameCount );

		generateNeighbours();

		var vertices = new List<Vector3>();
		var triangles = new List<int>();
		var uvs = new List<Vector2>();

		drawBlocks( vertices, triangles, uvs );

		if ( triangles.Count == 0 )
		{
			renderer.enabled = false;
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
		
		hasMesh = true;
	}


	void generateNeighbours()
	{
		neighbourRight = terrain.getChunk( position + Position3.right );
		neighbourLeft = terrain.getChunk( position + Position3.left );
		neighbourUp = terrain.getChunk( position + Position3.up );
		neighbourDown = terrain.getChunk( position + Position3.down );
		neighbourForward = terrain.getChunk( position + Position3.forward );
		neighbourBack = terrain.getChunk( position + Position3.back );
	}


	void drawBlocks( List< Vector3 > vertices, List< int > triangles, List< Vector2 > uvs )
	{
		for ( int x = 0; x < size; ++x )
		{
			for ( int y = 0; y < size; ++y )
			{
				for ( int z = 0; z < size; ++z )
				{
					if ( blocks[ x, y, z ] == 0 ) continue;

					var origin = new Vector3( x, y, z );

					// Front
					if ( Block.isTransparent( getBlock( x, y, z - 1 ) ) ) drawFace( origin, Vector3.up, Vector3.right, vertices, triangles, uvs );

					// Left
					if ( Block.isTransparent( getBlock( x - 1, y, z ) ) ) drawFace( origin + Vector3.forward, Vector3.up, Vector3.back, vertices, triangles, uvs );

					// Back
					if ( Block.isTransparent( getBlock( x, y, z + 1 ) ) ) drawFace( origin + Vector3.forward + Vector3.right, Vector3.up, Vector3.left, vertices, triangles, uvs );

					// Right
					if ( Block.isTransparent( getBlock( x + 1, y, z ) ) ) drawFace( origin + Vector3.right, Vector3.up, Vector3.forward, vertices, triangles, uvs );

					// Top
					if ( Block.isTransparent( getBlock( x, y + 1, z ) ) ) drawFace( origin + Vector3.up, Vector3.forward, Vector3.right, vertices, triangles, uvs );

					// Bottom
					if ( Block.isTransparent( getBlock( x, y - 1, z ) ) ) drawFace( origin, Vector3.right, Vector3.forward, vertices, triangles, uvs );
				}
			}
		}
	}


	static void drawFace( Vector3 origin, Vector3 up, Vector3 right,
	                      ICollection<Vector3> vertices,
	                      ICollection<int> triangles,
	                      ICollection<Vector2> uvs )
	{
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
