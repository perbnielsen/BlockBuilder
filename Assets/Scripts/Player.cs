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

    public void Start()
    {
        Cursor.lockState = Application.isEditor ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = false;
        chunk = terrain.GetChunkAtCoordiate(transform.position);
    }

    public void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 5f, 1 << 9))
            {
                Position3 blockPositionAdd = hit.point + hit.normal / 2f;
                if (!PlayerCollideWithBlockAt(blockPositionAdd))
                {
                    Chunk chunkToCreate = terrain.GetChunkAtCoordiate(blockPositionAdd);
                    chunkToCreate.SetBlock(blockPositionAdd - chunkToCreate.position * terrain.chunkSize, Block.Type.rock);
                }
            }
        }

        if (Input.GetMouseButtonDown(1)) // Right mouse button
        {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 5f, 1 << 9))
            {
                Position3 blockPositionDel = hit.point - hit.normal / 2f;
                Chunk chunkToDelete = terrain.GetChunkAtCoordiate(blockPositionDel);
                chunkToDelete.SetBlock(blockPositionDel - chunkToDelete.position * terrain.chunkSize, Block.Type.none);
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
        if (chunk.HasCollisionMesh) characterController.Move(velocity * Time.deltaTime);
        UpdateChunkMeshes();
    }

    private void UpdateChunkMeshes()
    {
        var oldChunk = chunk;
        chunk = terrain.GetChunkAtCoordiate(transform.position);
        if (chunk.position != oldChunk.position)
        {
            if (chunk.position.x != oldChunk.position.x)
            {
                //				Debug.Log( "New x position" );
                terrain.chunksNeedingMesh.EnqueueTasks(oldChunk.GetAllNeighboursInYZPlane());
                terrain.chunksNeedingMesh.EnqueueTasks(chunk.GetAllNeighboursInYZPlane());
            }
            if (chunk.position.y != oldChunk.position.y)
            {
                //				Debug.Log( "New y position" );
                terrain.chunksNeedingMesh.EnqueueTasks(oldChunk.GetAllNeighboursInXZPlane());
                terrain.chunksNeedingMesh.EnqueueTasks(chunk.GetAllNeighboursInXZPlane());
            }
            if (chunk.position.z != oldChunk.position.z)
            {
                //				Debug.Log( "New z position" );
                terrain.chunksNeedingMesh.EnqueueTasks(oldChunk.GetAllNeighboursInXYPlane());
                terrain.chunksNeedingMesh.EnqueueTasks(chunk.GetAllNeighboursInXYPlane());
            }
            terrain.chunksNeedingMesh.EnqueueTask(oldChunk);
            terrain.chunksNeedingMesh.EnqueueTask(chunk);
        }
    }

    private bool PlayerCollideWithBlockAt(Vector3 blockPosition)
    {
        Vector3 distance = transform.parent.position - (blockPosition + Vector3.one / 2f);
        return Mathf.Abs(distance.x) < 0.9f && Mathf.Abs(distance.y) < 1.45f && Mathf.Abs(distance.z) < 0.9f;
    }
}
