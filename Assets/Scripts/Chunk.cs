using UnityEngine;
using System.Collections.Generic;


[RequireComponent( typeof( MeshFilter ) )]
[RequireComponent( typeof( MeshRenderer ) )]
[RequireComponent( typeof( MeshCollider ) )]
public class Chunk : MonoBehaviour
{
	public Terrain terrain;
	[System.NonSerializedAttribute]
	public int size;
	public Vector3 center;
	public Position3 chunkPosition;
	MeshFilter meshFilter;
	MeshCollider meshCollider;
	byte[,,] blocks;


	public byte getBlock( Position3 position )
	{
		if ( (position.x < 0) || (position.y < 0) || (position.z < 0) || (position.x >= size) || (position.y >= size) || (position.z >= size) ) return terrain.getBlock( position + chunkPosition );

		return blocks[ position.x, position.y, position.z ];
	}


	public byte getBlock( int x, int y, int z )
	{
		return getBlock( new Position3( x, y, z ) );

	}


	void Start()
	{

		center = transform.position + Vector3.one * size;
		chunkPosition = new Position3( transform.position );
		blocks = new byte[ size, size, size ];
		meshFilter = GetComponent< MeshFilter >();
		meshCollider = GetComponent< MeshCollider >();

		generateBlocks();
		generateMesh();

		terrain.chunks.Add( chunkPosition, this );
	}


	void generateBlocks()
	{
		for ( int x = 0; x < size; ++x )
		{
			for ( int y = 0; y < size; ++y )
			{
				for ( int z = 0; z < size; ++z )
				{
					blocks[ x, y, z ] = (byte)(Mathf.Sin( (float)(chunkPosition.x + x + chunkPosition.z + z) / 20f ) * 5f > (chunkPosition.y + y) ? 1 : 0); //(byte)Random.Range( 0, 2 );
				}
			}
		}
	}


	void generateMesh()
	{
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
			Mesh mesh = new Mesh();
			mesh.vertices = vertices.ToArray();
			mesh.triangles = triangles.ToArray();
			mesh.uv = uvs.ToArray();

			mesh.RecalculateBounds();
			mesh.RecalculateNormals();

			meshFilter.mesh = mesh;
			meshCollider.sharedMesh = mesh;

			renderer.enabled = true;
		}
	}


	void drawBlocks( List< Vector3 > vertices,
	                 List< int > triangles,
	                 List< Vector2 > uvs
	)
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


	void drawFace( Vector3 origin, Vector3 up, Vector3 right,
	               List< Vector3 > vertices,
	               List< int > triangles,
	               List< Vector2 > uvs )
	{
		int index = vertices.Count;

		// Add vertices
		vertices.Add( origin );
		vertices.Add( origin + up );
		vertices.Add( origin + up + right );
		vertices.Add( origin + right );

		uvs.Add( Vector2.zero );
		uvs.Add( Vector2.up );
		uvs.Add( Vector2.up + Vector2.right );
		uvs.Add( Vector2.right );

		// Add faces
		triangles.Add( index );
		triangles.Add( index + 1 );
		triangles.Add( index + 2 );

		triangles.Add( index + 2 );
		triangles.Add( index + 3 );
		triangles.Add( index );
	}
}
