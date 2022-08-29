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
    public Light torch;

    private void Start()
    {
        Screen.lockCursor = !Application.isEditor;
        Cursor.visible = false;
        chunk = terrain.getChunkAtCoordiate(transform.position);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            RaycastHit hit;

            if (Physics.Raycast(transform.position, transform.forward, out hit, 5f, 1 << 9))
            {
                Position3 blockPositionAdd = hit.point + hit.normal / 2f;
                if (!playerCollideWithBlockAt(blockPositionAdd))
                {
                    Chunk chunkToCreate = terrain.getChunkAtCoordiate(blockPositionAdd);
                    chunkToCreate.setBlock(blockPositionAdd - chunkToCreate.position * terrain.chunkSize, Block.Type.rock);
                }
            }
        }

        if (Input.GetMouseButtonDown(1)) // Right mouse button
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, 5f, 1 << 9))
            {
                Position3 blockPositionDel = hit.point - hit.normal / 2f;
                Chunk chunkToDelete = terrain.getChunkAtCoordiate(blockPositionDel);
                chunkToDelete.setBlock(blockPositionDel - chunkToDelete.position * terrain.chunkSize, Block.Type.none);
            }
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            torch.enabled = !torch.enabled;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Transform newTorch = Instantiate(torch.transform) as Transform;
            newTorch.GetComponent<Light>().enabled = true;
            newTorch.position = torch.transform.position;
            newTorch.parent = transform.parent.parent;
        }

        Vector3 velocity = characterController.velocity;
        velocity.x = velocity.z = 0f;
        velocity += Input.GetAxis("Horizontal") * transform.parent.right * speed;
        velocity += Input.GetAxis("Vertical") * transform.parent.forward * speed;
        velocity += Time.deltaTime * gravity * Vector3.down;
        if (Input.GetKeyDown(jumpKey)/* && characterController.isGrounded*/ ) velocity += Vector3.up * jumpForce;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
        if (chunk.hasCollisionMesh) characterController.Move(velocity * Time.deltaTime);
        updateChunkMeshes();
    }

    private void updateChunkMeshes()
    {
        var oldChunk = chunk;
        chunk = terrain.getChunkAtCoordiate(transform.position);
        if (chunk.position != oldChunk.position)
        {
            if (chunk.position.x != oldChunk.position.x)
            {
                //				Debug.Log( "New x position" );
                terrain.chunksNeedingMesh.enqueueTasks(oldChunk.getAllNeighboursInYZPlane());
                terrain.chunksNeedingMesh.enqueueTasks(chunk.getAllNeighboursInYZPlane());
            }
            if (chunk.position.y != oldChunk.position.y)
            {
                //				Debug.Log( "New y position" );
                terrain.chunksNeedingMesh.enqueueTasks(oldChunk.getAllNeighboursInXZPlane());
                terrain.chunksNeedingMesh.enqueueTasks(chunk.getAllNeighboursInXZPlane());
            }
            if (chunk.position.z != oldChunk.position.z)
            {
                //				Debug.Log( "New z position" );
                terrain.chunksNeedingMesh.enqueueTasks(oldChunk.getAllNeighboursInXYPlane());
                terrain.chunksNeedingMesh.enqueueTasks(chunk.getAllNeighboursInXYPlane());
            }
            terrain.chunksNeedingMesh.enqueueTask(oldChunk);
            terrain.chunksNeedingMesh.enqueueTask(chunk);
        }
    }

    private bool playerCollideWithBlockAt(Vector3 blockPosition)
    {
        Vector3 distance = transform.parent.position - (blockPosition + Vector3.one / 2f);
        return (Mathf.Abs(distance.x) < 0.9f && Mathf.Abs(distance.y) < 1.45f && Mathf.Abs(distance.z) < 0.9f);
    }
}
