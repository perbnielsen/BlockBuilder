using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour, IPriorityTask
{
    public Terrain terrain;
    public int size;
    public Vector3 center;
    public Position3 position;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;

    private byte[] blocks;

    private Chunk _neighbourRight;
    private Chunk _neighbourLeft;
    private Chunk _neighbourUp;
    private Chunk _neighbourDown;
    private Chunk _neighbourForward;
    private Chunk _neighbourBack;

    private bool hasNeighbourRight;
    private bool hasNeighbourLeft;
    private bool hasNeighbourUp;
    private bool hasNeighbourDown;
    private bool hasNeighbourForward;
    private bool hasNeighbourBack;

    private Chunk NeighbourRight
    {
        get { return _neighbourRight; }
        set
        {
            _neighbourRight = value;
            hasNeighbourRight = (value != null);
        }
    }

    private Chunk NeighbourLeft
    {
        get { return _neighbourLeft; }
        set
        {
            _neighbourLeft = value;
            hasNeighbourLeft = (value != null);
        }
    }

    private Chunk NeighbourUp
    {
        get { return _neighbourUp; }
        set
        {
            _neighbourUp = value;
            hasNeighbourUp = (value != null);
        }
    }

    private Chunk NeighbourDown
    {
        get { return _neighbourDown; }
        set
        {
            _neighbourDown = value;
            hasNeighbourDown = (value != null);
        }
    }

    private Chunk NeighbourForward
    {
        get { return _neighbourForward; }
        set
        {
            _neighbourForward = value;
            hasNeighbourForward = (value != null);
        }
    }

    private Chunk NeighbourBack
    {
        get { return _neighbourBack; }
        set
        {
            _neighbourBack = value;
            hasNeighbourBack = (value != null);
        }
    }

    [Flags]
    private enum State : byte
    {
        Initialised = 0x0,
        HasBlocks = 0x1,
        GeneratingAllNeighbours = 0x2,
        HasMesh = 0x4,
        HasCollisionMesh = 0x8,
        IsActive = 0x10
    }

    private State state;

    public float GetPriority()
    {
        Vector3 fromPlayerToChunk = center - terrain.player.transform.position;

        return (Vector3.Dot(fromPlayerToChunk.normalized, terrain.player.transform.forward.normalized) + 1.25f) / fromPlayerToChunk.sqrMagnitude;
    }

    private bool HasAllNeighbours => hasNeighbourRight && hasNeighbourLeft && hasNeighbourUp && hasNeighbourDown && hasNeighbourForward && hasNeighbourBack;

    public bool HasBlocks => (state & State.HasBlocks) == State.HasBlocks;
    public bool HasMesh => (state & State.HasMesh) == State.HasMesh;
    public bool HasCollisionMesh => (state & State.HasCollisionMesh) == State.HasCollisionMesh;
    public bool IsActive => (state & State.IsActive) == State.IsActive;

    [ContextMenu("Show state")]
    public void ShowState()
    {
        Debug.Log(state);
    }

    #region Serialization

    [ContextMenu("Save to disk")]
    public void SaveChunkToDisk()
    {
        terrain.fileTasks.EnqueueTask(SaveChunkToDiskTask);
    }

    private void SaveChunkToDiskTask()
    {
        using var file = File.Create("Chunks/" + position + ".chunk");
        using var binaryWriter = new BinaryWriter(file);
        binaryWriter.Write(GZip.compress(blocks));
    }

    [ContextMenu("Load from disk")]
    // Note: Only for use from Unity!
    public void LoadFromDisk()
    {
        //		state = State.GeneratingBlocks;

        enabled = true;

        terrain.fileTasks.EnqueueTask(LoadChunkFromDiskTask);
    }

    private void LoadChunkFromDiskTask()
    {
        //		state = State.GeneratingBlocks;

        try
        {
            using var fs = File.OpenRead("Chunks/" + position + ".chunk");
            using var binaryReader = new BinaryReader(fs);
            blocks = GZip.decompress(binaryReader.ReadBytes(size * size * size), size * size * size);

            //			state = State.HasBlocks;
        }
        catch (FileNotFoundException)
        {
            Debug.LogWarning("Chunk at " + position + " tried to load data from disk but the file was not found");
        }
        catch (DirectoryNotFoundException)
        {
            Debug.LogWarning("Chunk at " + position + " tried to load data from disk but the directory was not found");
        }
    }

    #endregion

    public Block.Type GetBlock(Position3 blockPosition)
    {
        return GetBlock(blockPosition.x, blockPosition.y, blockPosition.z);
    }

    public Block.Type GetBlock(int x, int y, int z)
    {
        if (x < 0) return hasNeighbourLeft ? NeighbourLeft.GetBlock(x + size, y, z) : Block.Type.none;
        if (x >= size) return hasNeighbourRight ? NeighbourRight.GetBlock(x - size, y, z) : Block.Type.none;

        if (y < 0) return hasNeighbourDown ? NeighbourDown.GetBlock(x, y + size, z) : Block.Type.none;
        if (y >= size) return hasNeighbourUp ? NeighbourUp.GetBlock(x, y - size, z) : Block.Type.none;

        if (z < 0) return hasNeighbourBack ? NeighbourBack.GetBlock(x, y, z + size) : Block.Type.none;
        if (z >= size) return hasNeighbourForward ? NeighbourForward.GetBlock(x, y, z - size) : Block.Type.none;

        if (blocks == null) return Block.Type.none;

        return (Block.Type)blocks[x + size * (y + size * z)];
    }

    public void SetBlock(Position3 blockPosition, Block.Type blockType)
    {
        if (blocks == null) return;

        // Set block 
        blocks[blockPosition.x + size * (blockPosition.y + size * blockPosition.z)] = (byte)blockType;

        terrain.chunksNeedingMesh.EnqueueTask(this);
        terrain.chunksNeedingCollisionMesh.EnqueueTask(this);

        // Update any neighbour chunk that might be affected
        if (blockPosition.x == 0 && hasNeighbourLeft) NeighbourLeft.NeighbourBlocksHaveChanged();
        if (blockPosition.y == 0 && hasNeighbourDown) NeighbourDown.NeighbourBlocksHaveChanged();
        if (blockPosition.z == 0 && hasNeighbourBack) NeighbourBack.NeighbourBlocksHaveChanged();

        if (blockPosition.x == size - 1 && hasNeighbourRight) NeighbourRight.NeighbourBlocksHaveChanged();
        if (blockPosition.y == size - 1 && hasNeighbourUp) NeighbourUp.NeighbourBlocksHaveChanged();
        if (blockPosition.z == size - 1 && hasNeighbourForward) NeighbourForward.NeighbourBlocksHaveChanged();

        SaveChunkToDisk();
    }

    public void PopulateBlocks()
    {
        if (File.Exists("Chunks/" + position + ".chunk"))
        {
            LoadChunkFromDiskTask();
        }
        else
        {
            GenerateBlocks();
        }

        terrain.runOnMainThread.Add(() =>
        {
            terrain.inactiveChunks.Add(this);
            InformNeighboursOfBlockGeneration();
        });

        state |= State.HasBlocks;
    }

    public void GenerateBlocks()
    {
        const float scale = 100f;

        blocks = new byte[size * size * size];

        int i = 0;

        for (int z = 0; z < size; ++z)
        {
            var positionZ = (float)(position.z * size + z) / scale;

            for (int y = 0; y < size; ++y)
            {
                var positionY = (float)(position.y * size + y) / scale;

                for (int x = 0; x < size; ++x)
                {
                    var positionX = (float)(position.x * size + x) / scale;

                    blocks[i++] = (5f * positionY) < Noise.Generate(positionX, positionY, positionZ) ? (byte)Block.Type.rock : (byte)Block.Type.none;
                }
            }
        }
    }

    [ContextMenu("Check if still inactive")]
    public bool CheckIfStillInactive()
    {
        var distanceToPlayerSqr = (terrain.player.transform.position - center).sqrMagnitude;

        if (distanceToPlayerSqr > terrain.DestroyChunkDistanceSqr)
        {
            //			Debug.Log( "Chunk at " + position + " is being destroyed" );
            terrain.DeleteChunk(this);

            return false;
        }

        if (distanceToPlayerSqr < terrain.DisplayChunkDistanceSqr)
        {
            //			Debug.Log( "Chunk at " + position + " is turning active" );

            state |= State.IsActive;

            terrain.activeChunks.Add(this);

            if (!HasAllNeighbours)
            {
                GenerateNeighbours();
            }
            else
            {
                if (HasMesh)
                {
                    terrain.runOnMainThread.Add(() => GetComponent<Renderer>().enabled = true);
                }
                else
                {
                    terrain.chunksNeedingMesh.EnqueueTask(this);
                }

                if (HasCollisionMesh)
                {
                    terrain.runOnMainThread.Add(() => meshCollider.enabled = true);
                }
                else
                {
                    terrain.chunksNeedingCollisionMesh.EnqueueTask(this);
                }
            }

            return false;
        }

        return true;
    }

    [ContextMenu("Check if still active")]
    public bool CheckIfStillActive()
    {
        var distanceToPlayerSqr = (terrain.player.transform.position - center).sqrMagnitude;

        if (distanceToPlayerSqr > terrain.DisableChunkDistanceSqr)
        {
            state &= ~State.IsActive;

            terrain.runOnMainThread.Add(() =>
            {
                if (!this)
                {
                    return;
                }
                GetComponent<Renderer>().enabled = false;
                meshCollider.enabled = false;
            });

            terrain.chunksNeedingBlocks.DequeueTask(this);
            terrain.chunksNeedingCollisionMesh.DequeueTask(this);
            terrain.chunksNeedingMesh.DequeueTask(this);

            terrain.inactiveChunks.Add(this);

            return false;
        }

        return true;
    }

    public void SetMesh(Vector3[] vertices, int[] triangles, Vector2[] uvs)
    {
        if (!this)
        {
            return;
        }

        //		DestroyMesh();
        state |= State.HasMesh;

        if (triangles.Length == 0)
        {
            GetComponent<Renderer>().enabled = false;

            return;
        }

        if (meshFilter.mesh == null) meshFilter.mesh = new Mesh();

        var mesh = meshFilter.mesh;
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        //		mesh.Optimize();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<Renderer>().enabled = true;
    }

    public void SetCollisionMesh(Vector3[] vertices, int[] triangles)
    {
        if (meshCollider == null) return;

        DestroyCollisionMesh();

        state |= State.HasCollisionMesh;

        if (triangles.Length == 0)
        {
            meshCollider.enabled = false;

            return;
        }

        var mesh = new Mesh();
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshCollider.sharedMesh = mesh;
        meshCollider.enabled = true;
    }

    public void OnDestroy()
    {
        //		Debug.Log( "Destroying chunk at " + position );

        terrain.chunksNeedingBlocks.DequeueTask(this);
        terrain.chunksNeedingMesh.DequeueTask(this);
        terrain.chunksNeedingCollisionMesh.DequeueTask(this);

        if (NeighbourRight != null) NeighbourRight.NeighbourLeft = null;
        if (NeighbourLeft != null) NeighbourLeft.NeighbourRight = null;
        if (NeighbourUp != null) NeighbourUp.NeighbourDown = null;
        if (NeighbourDown != null) NeighbourDown.NeighbourUp = null;
        if (NeighbourForward != null) NeighbourForward.NeighbourBack = null;
        if (NeighbourBack != null) NeighbourBack.NeighbourForward = null;

        if (!terrain.isQuitting)
        {
            DestroyMesh();
            DestroyCollisionMesh();
        }
    }

    void DestroyMesh()
    {
        state &= ~State.HasMesh;

        GetComponent<Renderer>().enabled = false;

        Destroy(meshFilter.mesh);
    }

    void DestroyCollisionMesh()
    {
        state &= ~State.HasCollisionMesh;

        meshCollider.enabled = false;

        Destroy(meshCollider.sharedMesh);
    }

    public void NeighbourBlocksHaveChanged()
    {
        if (HasAllNeighbours && IsActive)
        {
            terrain.chunksNeedingMesh.EnqueueTask(this);
            terrain.chunksNeedingCollisionMesh.EnqueueTask(this);
        }
    }

    public void InformNeighboursOfBlockGeneration()
    {
        if (NeighbourRight = terrain.GetChunk(position + Position3.Right, createIfNonExistent: false))
        {
            NeighbourRight.NeighbourLeft = this;
            NeighbourRight.NeighbourBlocksHaveChanged();
        }

        if (NeighbourLeft = terrain.GetChunk(position + Position3.Left, createIfNonExistent: false))
        {
            NeighbourLeft.NeighbourRight = this;
            NeighbourLeft.NeighbourBlocksHaveChanged();
        }

        if (NeighbourUp = terrain.GetChunk(position + Position3.Up, createIfNonExistent: false))
        {
            NeighbourUp.NeighbourDown = this;
            NeighbourUp.NeighbourBlocksHaveChanged();
        }

        if (NeighbourDown = terrain.GetChunk(position + Position3.Down, createIfNonExistent: false))
        {
            NeighbourDown.NeighbourUp = this;
            NeighbourDown.NeighbourBlocksHaveChanged();
        }

        if (NeighbourForward = terrain.GetChunk(position + Position3.Forward, createIfNonExistent: false))
        {
            NeighbourForward.NeighbourBack = this;
            NeighbourForward.NeighbourBlocksHaveChanged();
        }

        if (NeighbourBack = terrain.GetChunk(position + Position3.Back, createIfNonExistent: false))
        {
            NeighbourBack.NeighbourForward = this;
            NeighbourBack.NeighbourBlocksHaveChanged();
        }
    }

    [ContextMenu("Generate neighbours")]
    public void GenerateNeighbours()
    {
        state |= State.GeneratingAllNeighbours;

        if (NeighbourRight == null) terrain.GetChunk(position + Position3.Right).NeighbourLeft = this;
        if (NeighbourLeft == null) terrain.GetChunk(position + Position3.Left).NeighbourRight = this;
        if (NeighbourUp == null) terrain.GetChunk(position + Position3.Up).NeighbourDown = this;
        if (NeighbourDown == null) terrain.GetChunk(position + Position3.Down).NeighbourUp = this;
        if (NeighbourForward == null) terrain.GetChunk(position + Position3.Forward).NeighbourBack = this;
        if (NeighbourBack == null) terrain.GetChunk(position + Position3.Back).NeighbourForward = this;
    }

    [ContextMenu("Build mesh")]
    public void BuildMesh()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var uvs = new List<Vector2>();

        var positionRelativeToPlayer = terrain.player.chunk.position - position;
        GenerateMesh(positionRelativeToPlayer, ref vertices, ref triangles, ref uvs);

        var vertexArray = vertices.ToArray();
        var triangleArray = triangles.ToArray();
        var uvArray = uvs.ToArray();

        //		if ( triangles.Count > 0 ) 
        terrain.runOnMainThread.Add(() => SetMesh(vertexArray, triangleArray, uvArray));
    }

    [ContextMenu("Build collision mesh")]
    public void BuildCollisionMesh()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var uvs = new List<Vector2>();

        GenerateMesh(Position3.Zero, ref vertices, ref triangles, ref uvs);

        var vertexArray = vertices.ToArray();
        var triangleArray = triangles.ToArray();

        //		if ( triangles.Count > 0 ) 
        terrain.runOnePerFrameOnMainThread.Add(() => SetCollisionMesh(vertexArray, triangleArray));
    }

    public void GenerateMesh(Position3 positionRelativeToPlayer, ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector2> uvs)
    {
        var right = new bool[size, size, size];
        var left = new bool[size, size, size];
        var top = new bool[size, size, size];
        var bottom = new bool[size, size, size];
        var front = new bool[size, size, size];
        var back = new bool[size, size, size];

        for (int x = 0; x < size; ++x)
        {
            for (int y = 0; y < size; ++y)
            {
                for (int z = 0; z < size; ++z)
                {
                    if (blocks[x + size * (y + size * z)] == 0) continue;

                    if (positionRelativeToPlayer.x >= 0f && Block.IsTransparent(GetBlock(x + 1, y, z))) right[z, y, size - 1 - x] = true;
                    if (positionRelativeToPlayer.x <= 0f && Block.IsTransparent(GetBlock(x - 1, y, z))) left[size - 1 - z, y, x] = true;
                    if (positionRelativeToPlayer.y >= 0f && Block.IsTransparent(GetBlock(x, y + 1, z))) top[x, z, size - 1 - y] = true;
                    if (positionRelativeToPlayer.y <= 0f && Block.IsTransparent(GetBlock(x, y - 1, z))) bottom[x, size - 1 - z, y] = true;
                    if (positionRelativeToPlayer.z >= 0f && Block.IsTransparent(GetBlock(x, y, z + 1))) front[size - 1 - x, y, size - 1 - z] = true;
                    if (positionRelativeToPlayer.z <= 0f && Block.IsTransparent(GetBlock(x, y, z - 1))) back[x, y, z] = true;
                }
            }
        }

        if (positionRelativeToPlayer.x >= 0f) DrawFaces(ref right, Vector3.right * size, Vector3.forward, Vector3.up, Vector3.left, ref vertices, ref triangles, ref uvs);
        if (positionRelativeToPlayer.x <= 0f) DrawFaces(ref left, Vector3.forward * size, Vector3.back, Vector3.up, Vector3.right, ref vertices, ref triangles, ref uvs);
        if (positionRelativeToPlayer.y >= 0f) DrawFaces(ref top, Vector3.up * size, Vector3.right, Vector3.forward, Vector3.down, ref vertices, ref triangles, ref uvs);
        if (positionRelativeToPlayer.y <= 0f) DrawFaces(ref bottom, Vector3.forward * size, Vector3.right, Vector3.back, Vector3.up, ref vertices, ref triangles, ref uvs);
        if (positionRelativeToPlayer.z >= 0f) DrawFaces(ref front, Vector3.forward * size + Vector3.right * size, Vector3.left, Vector3.up, Vector3.back, ref vertices, ref triangles, ref uvs);
        if (positionRelativeToPlayer.z <= 0f) DrawFaces(ref back, Vector3.zero, Vector3.right, Vector3.up, Vector3.forward, ref vertices, ref triangles, ref uvs);
    }

    private void DrawFaces(ref bool[,,] faces, Vector3 offset, Vector3 right, Vector3 up, Vector3 forward, ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector2> uvs)
    {
        int width;
        int height;

        for (int z = 0; z < size; ++z)
        {
            for (int y = 0; y < size; ++y)
            {
                for (int x = 0; x < size;)
                {
                    if (!faces[x, y, z])
                    {
                        ++x;
                        continue;
                    }

                    width = 1;
                    height = 1;

                    // Expand face in x
                    while (x + width < size && faces[x + width, y, z])
                    {
                        //faces[ x, y, z + width ] = false;
                        ++width;
                    }

                    // Expand face in y
                    while (FaceCanBeExtended(ref faces, x, y + height, z, width))
                    {
                        for (int i = 0; i < width; i++) faces[x + i, y + height, z] = false;
                        ++height;
                    }

                    DrawFace(offset + x * right + y * up + z * forward, right * width, up * height, ref vertices, ref triangles, ref uvs);

                    x += width;
                }
            }
        }
    }

    private bool FaceCanBeExtended(ref bool[,,] faces, int x, int y, int z, int width)
    {
        if (y >= size) return false;

        for (int i = x; i < x + width; ++i) if (!faces[i, y, z]) return false;

        return true;
    }

    static private void DrawFace(Vector3 origin, Vector3 right, Vector3 up, ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector2> uvs)
    {
        int index = vertices.Count;

        vertices.Add(origin);
        vertices.Add(origin + up);
        vertices.Add(origin + (up + right));
        vertices.Add(origin + right);

        if (uvs != null)
        {
            var uvUp = Vector2.up * up.magnitude;
            var uvRight = Vector2.right * right.magnitude;

            uvs.Add(Vector2.zero);
            uvs.Add(uvUp);
            uvs.Add(uvUp + uvRight);
            uvs.Add(uvRight);
        }

        triangles.Add(index);
        triangles.Add(index + 1);
        triangles.Add(index + 2);

        triangles.Add(index + 2);
        triangles.Add(index + 3);
        triangles.Add(index);
    }

    public List<Chunk> GetAllNeighboursInX()
    {
        var neighbours = new List<Chunk>();

        for (var currentChunkRight = this; currentChunkRight.NeighbourRight != null; currentChunkRight = currentChunkRight.NeighbourRight)
        {
            neighbours.Add(currentChunkRight);
        }

        for (var currentChunkLeft = this; currentChunkLeft.NeighbourLeft != null; currentChunkLeft = currentChunkLeft.NeighbourLeft)
        {
            neighbours.Add(currentChunkLeft);
        }

        return neighbours;
    }

    public List<Chunk> GetAllNeighboursInY()
    {
        var neighbours = new List<Chunk>();

        for (var currentChunkUp = this; currentChunkUp.NeighbourUp != null; currentChunkUp = currentChunkUp.NeighbourUp)
        {
            neighbours.Add(currentChunkUp);
        }

        for (var currentChunkDown = this; currentChunkDown.NeighbourDown != null; currentChunkDown = currentChunkDown.NeighbourDown)
        {
            neighbours.Add(currentChunkDown);
        }

        return neighbours;
    }

    public List<Chunk> GetAllNeighboursInZ()
    {
        var neighbours = new List<Chunk>();

        for (var currentChunkForward = this; currentChunkForward.NeighbourForward != null; currentChunkForward = currentChunkForward.NeighbourForward)
        {
            neighbours.Add(currentChunkForward);
        }

        for (var currentChunkBack = this; currentChunkBack.NeighbourBack != null; currentChunkBack = currentChunkBack.NeighbourBack)
        {
            neighbours.Add(currentChunkBack);
        }

        return neighbours;
    }

    public List<Chunk> GetAllNeighboursInXYPlane()
    {
        var neighboursInXY = new List<Chunk>();

        var neighboursInX = GetAllNeighboursInX();

        foreach (var neighbour in neighboursInX)
        {
            neighboursInXY.AddRange(neighbour.GetAllNeighboursInY());
        }

        neighboursInXY.AddRange(neighboursInX);

        return neighboursInXY;
    }

    public List<Chunk> GetAllNeighboursInXZPlane()
    {
        var neighboursInXZ = new List<Chunk>();

        var neighboursInX = GetAllNeighboursInX();

        foreach (var neighbour in neighboursInX)
        {
            neighboursInXZ.AddRange(neighbour.GetAllNeighboursInZ());
        }

        neighboursInXZ.AddRange(neighboursInX);

        return neighboursInXZ;
    }

    public List<Chunk> GetAllNeighboursInYZPlane()
    {
        var neighboursInYZ = new List<Chunk>();

        var neighboursInY = GetAllNeighboursInY();

        foreach (var neighbour in neighboursInY)
        {
            neighboursInYZ.AddRange(neighbour.GetAllNeighboursInZ());
        }

        neighboursInYZ.AddRange(neighboursInY);

        return neighboursInYZ;
    }
}
