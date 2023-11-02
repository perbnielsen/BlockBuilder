using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

/*
spawned()
	updateBlocks()
	if ( should be visible ) spawn all neighbours

neighbourBlocksHaveChanged()
	if ( all neighbours exist and should be visible ) generate visual mesh

playerMoved()
	regenerate visual mesh

updateBlocks()
	generateBlocks
	generate collision mesh
	send all existing neighbours neighbourBlocksHaveChanged
*/

public class Terrain : MonoBehaviour
{
    public readonly List<Action> runOnMainThread = new();
    public readonly List<Action> runOnePerFrameOnMainThread = new();

    public bool isQuitting;
    public Player player;
    public int seed;
    public int chunkSize;
    public Chunk chunkPrefab;

    public float _displayChunkDistance;
    public readonly TaskQueue chunkTasks = new();
    public readonly TaskQueue fileTasks = new();

    readonly Dictionary<Position3, Chunk> chunks = new();

    public readonly List<Chunk> activeChunks = new();
    public readonly List<Chunk> inactiveChunks = new();

    public readonly PrioryTaskQueue<Chunk> chunksNeedingBlocks = new("blocks", chunk => chunk.PopulateBlocks(), threadCount: 5);
    public readonly PrioryTaskQueue<Chunk> chunksNeedingMesh = new("mesh", chunk => chunk.BuildMesh(), threadCount: 5);
    public readonly PrioryTaskQueue<Chunk> chunksNeedingCollisionMesh = new("collision mesh", chunk => chunk.BuildCollisionMesh(), threadCount: 5);

    public float DisplayChunkDistance
    {
        get { return _displayChunkDistance; }
        set
        {
            _displayChunkDistance = value;
            DisplayChunkDistanceSqr = Mathf.Pow(_displayChunkDistance, 2);
            DisableChunkDistanceSqr = Mathf.Pow(_displayChunkDistance + chunkSize * 1, 2.0f);
            DestroyChunkDistanceSqr = Mathf.Pow(_displayChunkDistance + chunkSize * 2, 2.0f);

            player.GetComponent<Camera>().farClipPlane = value + chunkSize;
        }
    }

    public float DisplayChunkDistanceSqr { get; private set; }
    public float DisableChunkDistanceSqr { get; private set; }
    public float DestroyChunkDistanceSqr { get; private set; }

    public Chunk GetChunkAtCoordinate(Vector3 coordinate)
    {
        return GetChunk(coordinate / chunkSize);
    }

    private Chunk CreateChunk(Position3 position)
    {
        if (isQuitting) return null;

        //		Debug.Log( "Creating chunk at " + position );

        var chunk = (Chunk)Instantiate(chunkPrefab, position * chunkSize, Quaternion.identity);

        chunk.transform.parent = transform;
        chunk.terrain = this;
        chunk.size = chunkSize;
        chunk.position = position;
        chunk.center = chunk.transform.position + Vector3.one * chunkSize / 2f;
        chunk.meshFilter = chunk.GetComponent<MeshFilter>();
        chunk.meshCollider = chunk.GetComponent<MeshCollider>();

        chunks.Add(position, chunk);

        chunksNeedingBlocks.EnqueueTask(chunk);

        return chunk;
    }

    public Chunk GetChunk(Position3 position, bool createIfNonExistent = true)
    {
        // Note: Returns the chunk at position (in chunks). If the chunk does not exist,
        // and createIfNonExistent is true, it will be created and returned.
        if (!chunks.ContainsKey(position))
        {
            return createIfNonExistent ? CreateChunk(position) : null;
        }

        return chunks[position];
    }

    public void DeleteChunk(Chunk chunk)
    {
        chunks.Remove(chunk.position);

        Destroy(chunk.gameObject);
    }

    public void Start()
    {
        if (!Directory.Exists("Chunks")) Directory.CreateDirectory("Chunks");

        DisplayChunkDistance = _displayChunkDistance;
    }

    [ContextMenu("Print queue sizes")]
    public void PrintQueueSizes()
    {
        Debug.Log("chunksNeedingBlocks: " + chunksNeedingBlocks.Count);
        Debug.Log("chunksNeedingMesh: " + chunksNeedingMesh.Count);
        Debug.Log("chunksNeedingCollisionMesh: " + chunksNeedingCollisionMesh.Count);
    }

    public void Update()
    {
        HandleInput();

        //		Profiler.BeginSample( "1" );
        chunksNeedingBlocks.Prioritise();
        chunksNeedingMesh.Prioritise();
        chunksNeedingCollisionMesh.Prioritise();
        //		Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("2");
        RunMainThreadTask();
        RunOnePerFrameOnMainThreadTask();
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("3");
        activeChunks.RemoveAll(chunk => !chunk.CheckIfStillActive());
        inactiveChunks.RemoveAll(chunk => !chunk.CheckIfStillInactive());
        UnityEngine.Profiling.Profiler.EndSample();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DisplayChunkDistance = Mathf.Max(chunkSize, DisplayChunkDistance - 8);
            Debug.Log("displayChunkDistance: " + DisplayChunkDistance);
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            DisplayChunkDistance += 8;
            Debug.Log("displayChunkDistance: " + DisplayChunkDistance);
        }
    }

    private void RunMainThreadTask()
    {
        lock (runOnMainThread)
        {
            while (runOnMainThread.Count > 0)
            {
                Action task = runOnMainThread[0];
                runOnMainThread.RemoveAt(0);

                task?.Invoke();
            }
        }
    }

    private void RunOnePerFrameOnMainThreadTask()
    {
        Action task = null;

        lock (runOnePerFrameOnMainThread)
        {
            if (runOnePerFrameOnMainThread.Count > 0)
            {
                task = runOnePerFrameOnMainThread[0];
                runOnePerFrameOnMainThread.RemoveAt(0);
            }
        }

        task?.Invoke();
    }

    public void OnApplicationQuit()
    {
        isQuitting = true;
        chunkTasks.Stop();
        fileTasks.Stop();

        chunksNeedingBlocks.Stop();
        chunksNeedingMesh.Stop();
        chunksNeedingCollisionMesh.Stop();
    }
}
