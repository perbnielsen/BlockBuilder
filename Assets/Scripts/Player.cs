using UnityEngine;


public class Player : MonoBehaviour
{
	public Terrain terrain;
	public KeyCode jumpKey;
	public float jumpForce;
	public float speed;
	public float gravity = 20f;
	public float maxSpeed = 20f;
	public CharacterController characterController;
	public Chunk chunk;


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

				if ( !playerCollideWithBlockAt( blockPositionAdd ) )
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

		Vector3 velocity = characterController.velocity;

		velocity.x = velocity.z = 0f;

		velocity += Input.GetAxis( "Horizontal" ) * transform.parent.right * speed;
		velocity += Input.GetAxis( "Vertical" ) * transform.parent.forward * speed;

		velocity += Time.deltaTime * gravity * Vector3.down;

		if ( Input.GetKeyDown( jumpKey ) && characterController.isGrounded ) velocity += Vector3.up * jumpForce;

		velocity = Vector3.ClampMagnitude( velocity, maxSpeed );

		characterController.Move( velocity * Time.deltaTime );

		this.chunk = terrain.getChunkAtCoordiate( transform.position );
	}


	bool playerCollideWithBlockAt( Vector3 blockPosition )
	{
		Vector3 distance = transform.parent.position - (blockPosition + Vector3.one / 2f);

		return (Mathf.Abs( distance.x ) < 0.9f && Mathf.Abs( distance.y ) < 1.45f && Mathf.Abs( distance.z ) < 0.9f);
	}
}
