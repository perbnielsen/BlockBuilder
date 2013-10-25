using UnityEngine;


public class Player : MonoBehaviour
{
	public Terrain terrain;
	public Transform block;


	void Update()
	{
		if ( Input.GetMouseButtonDown( 0 ) ) // Left mouse button
		{
			RaycastHit hit;
	
			if ( Physics.Raycast( transform.position, transform.forward, out hit, 10f ) )
			{
				Position3 blockPosition = hit.point + hit.normal / 2f;

				Chunk chunk = terrain.getChunkAtCoordiate( blockPosition );
	
//				Position3 blockPosition = new Position3( 
//					                          Mathf.FloorToInt( hit.point.x - chunk.position.x * terrain.chunkSize + hit.normal.x ),
//					                          Mathf.FloorToInt( hit.point.y - chunk.position.y * terrain.chunkSize + hit.normal.y ),
//					                          Mathf.FloorToInt( hit.point.z - chunk.position.z * terrain.chunkSize + hit.normal.z ) );
	
//				block.position = new Vector3( 
//					Mathf.Floor( hit.point.x - hit.normal.x / 2f ),
//					Mathf.Floor( hit.point.y - hit.normal.y / 2f ),
//					Mathf.Floor( hit.point.z - hit.normal.z / 2f ) );

//				block.position = new Vector3( 
//					Mathf.Floor( hit.point.x + hit.normal.x / 2f ),
//					Mathf.Floor( hit.point.y + hit.normal.y / 2f ),
//					Mathf.Floor( hit.point.z + hit.normal.z / 2f ) );
	

				block.position = blockPosition;

				block.gameObject.SetActive( true );

				chunk.setBlock( blockPosition - chunk.position * terrain.chunkSize, Block.Type.dirt );
			}
			else block.gameObject.SetActive( false );
		}
	
		if ( Input.GetMouseButtonDown( 1 ) ) // Right mouse button
		{
			Debug.Log( "Right mouse button pressed" );

			Position3 blockPosition = hit.point - hit.normal / 2f;
		}
	}


	void OnDrawGizmos()
	{
		Gizmos.DrawLine( transform.position, transform.position + transform.forward * 10f );
	}
}
