using UnityEngine;


public class Player : MonoBehaviour
{
	public Terrain terrain;


	void Update()
	{
		if ( Input.GetMouseButtonDown( 0 ) ) // Left mouse button
		{
			RaycastHit hit;
	
			if ( Physics.Raycast( transform.position, transform.forward, out hit, 10f ) )
			{
				Position3 blockPositionAdd = hit.point + hit.normal / 2f;

				Chunk chunk = terrain.getChunkAtCoordiate( blockPositionAdd );
	
				chunk.setBlock( blockPositionAdd - chunk.position * terrain.chunkSize, Block.Type.dirt );
			}
		}
	
		if ( Input.GetMouseButtonDown( 1 ) ) // Right mouse button
		{
			RaycastHit hit;

			if ( Physics.Raycast( transform.position, transform.forward, out hit, 10f ) )
			{
				Position3 blockPositionDel = hit.point - hit.normal / 2f;

				Chunk chunk = terrain.getChunkAtCoordiate( blockPositionDel );

				chunk.setBlock( blockPositionDel - chunk.position * terrain.chunkSize, Block.Type.none );

			}
		}
	}


	void OnDrawGizmos()
	{
		Gizmos.DrawLine( transform.position, transform.position + transform.forward * 10f );
	}
}
