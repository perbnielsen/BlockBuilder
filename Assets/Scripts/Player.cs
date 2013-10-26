﻿using UnityEngine;


public class Player : MonoBehaviour
{
	public Terrain terrain;


	void Start()
	{
		Screen.lockCursor = !Application.isEditor;
		Screen.showCursor = false;
	}


	void Update()
	{
		if ( Input.GetMouseButtonDown( 0 ) ) // Left mouse button
		{
			RaycastHit hit;
	
			if ( Physics.Raycast( transform.position, transform.forward, out hit, 5f, 1 << 9 ) )
			{
				Position3 blockPositionAdd = hit.point + hit.normal / 2f;

				if ( Physics.OverlapSphere( (Vector3)blockPositionAdd + ( Vector3.one / 2f ), 0.87f, 1 << 8 ).Length == 0 )
//				if ( !Physics.CheckSphere( (Vector3)blockPositionAdd + ( Vector3.one / 2f ), 0.7f, 1 << 8 ) )
				{
					Chunk chunk = terrain.getChunkAtCoordiate( blockPositionAdd );

					chunk.setBlock( blockPositionAdd - chunk.position * terrain.chunkSize, Block.Type.rock );
				}
			}
		}
	
		if ( Input.GetMouseButtonDown( 1 ) ) // Right mouse button
		{
			RaycastHit hit;

			if ( Physics.Raycast( transform.position, transform.forward, out hit, 5f, 1 << 9 ) )
			{
				Position3 blockPositionDel = hit.point - hit.normal / 2f;

				Chunk chunk = terrain.getChunkAtCoordiate( blockPositionDel );

				chunk.setBlock( blockPositionDel - chunk.position * terrain.chunkSize, Block.Type.none );
			}
		}
	}
}
