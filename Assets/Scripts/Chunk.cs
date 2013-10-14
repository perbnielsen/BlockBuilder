using UnityEngine;
using System.Collections.Generic;


[RequireComponent( typeof( MeshFilter ) )]
[RequireComponent( typeof( MeshRenderer ) )]
[RequireComponent( typeof( MeshCollider ) )]
public class Chunk : MonoBehaviour
{
	public int size;
	MeshRenderer meshRenderer;
	MeshCollider meshCollider;
	byte[,,] blocks = new byte[ size, size, size];


	void Start()
	{
		meshRenderer = GetComponent< MeshRenderer >();
		meshCollider = GetComponent< MeshCollider >();


		for ( int x = 0; x < size; ++x )
		{
			for ( int y = 0; y < size; ++y )
			{
				for ( int z = 0; z < size; ++z )
				{
					blocks[ x, y, z ] = (byte)Random.Range( 0, 1 );
				}
			}
		}

		List< Vector3 > triangles = new List<Vector3>();


		Mesh mesh = new Mesh();


	}


	void drawBlocks( byte[,,] blocks,
	                 List< Vector3 > vertices,
	                 List< int > triangles,
	                 List< Vector2 > UVs )
	{
		for ( int x = 0; x < size; ++x )
		{
			for ( int y = 0; y < size; ++y )
			{
				for ( int z = 0; z < size; ++z )
				{
					drawFace( new Vector3( x, y, z ), Vector3.up, Vector3.right, vertices, triangles, UVs );
				}
			}
		}
	}


	void drawFace( Vector3 origin,
	               Vector3 up,
	               Vector3 right,
	               List< Vector3 > vertices,
	               List< int > triangles,
	               List< Vector2 > UVs )
	{
		int index = vertices.Count;

		vertices.Add( origin );
		vertices.Add( origin + up );
		vertices.Add( origin + up + right );
		vertices.Add( origin + right );

		triangles.Add( index );
		triangles.Add( index + 1 );
		triangles.Add( index + 2 );

		triangles.Add( index + 2 );
		triangles.Add( index + 3 );
		triangles.Add( index );
	}
}
