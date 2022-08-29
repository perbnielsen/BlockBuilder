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
    public readonly List<Action> runOnMainThread = new List<Action>();
    public readonly List<Action> runOnePerFrameOnMainThread = new List<Action>();

    public bool isQuitting;
    public Player player;
    public int seed;
    public int chunkSize;
    public Chunk chunkPrefab;

    public float _displayChunkDistance;
    private float _displayChunkDistanceSqr;
    private float _disableChunkDistanceSqr;
    private float _destroyChunkDistanceSqr;

    public readonly TaskQueue chunkTasks = new TaskQueue();
    public readonly TaskQueue fileTasks = new TaskQueue();

    readonly Dictionary<Position3, Chunk> chunks = new Dictionary<Position3, Chunk>();

    public readonly List<Chunk> activeChunks = new List<Chunk>();
    public readonly List<Chunk> inactiveChunks = new List<Chunk>();

    public readonly PrioryTaskQueue<Chunk> chunksNeedingBlocks = new PrioryTaskQueue<Chunk>("blocks", chunk => chunk.populateBlocks(), 1);
    public readonly PrioryTaskQueue<Chunk> chunksNeedingMesh = new PrioryTaskQueue<Chunk>("mesh", chunk => chunk.buildMesh(), 1);
    public readonly PrioryTaskQueue<Chunk> chunksNeedingCollisionMesh = new PrioryTaskQueue<Chunk>("collision mesh", chunk => chunk.buildCollisionMesh(), 1);

    public float displayChunkDistance
    {
        get { return _displayChunkDistance; }
        set
        {
            _displayChunkDistance = value;
            _displayChunkDistanceSqr = Mathf.Pow(_displayChunkDistance, 2);
            _disableChunkDistanceSqr = Mathf.Pow(_displayChunkDistance + chunkSize * 1, 2.0f);
            _destroyChunkDistanceSqr = Mathf.Pow(_displayChunkDistance + chunkSize * 2, 2.0f);

            player.GetComponent<Camera>().farClipPlane = value + chunkSize;
        }
    }

    public float displayChunkDistanceSqr { get { return _displayChunkDistanceSqr; } }
    public float disableChunkDistanceSqr { get { return _disableChunkDistanceSqr; } }
    public float destroyChunkDistanceSqr { get { return _destroyChunkDistanceSqr; } }

    public Chunk getChunkAtCoordiate(Vector3 coordinate)
    {
        return getChunk(coordinate / chunkSize);
    }

    private Chunk createChunk(Position3 position)
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

        chunksNeedingBlocks.enqueueTask(chunk);

        return chunk;
    }

    public Chunk getChunk(Position3 position, bool createIfNonexistent = true)
    {
        // Note: Returns the chunk at position (in chunks). If the chunk does not exist,
        // and createIfNonexistent is true, it will be created and returned.
        if (!chunks.ContainsKey(position))
        {
            return createIfNonexistent ? createChunk(position) : null;
        }

        return chunks[position];
    }

    public void deleteChunk(Chunk chunk)
    {
        chunks.Remove(chunk.position);

        Destroy(chunk.gameObject);
    }

    private void Start()
    {
        if (!Directory.Exists("Chunks")) Directory.CreateDirectory("Chunks");

        displayChunkDistance = _displayChunkDistance;
    }

    [ContextMenu("Print queue sizes")]
    private void printQueueSizes()
    {
        Debug.Log("chunksNeedingBlocks: " + chunksNeedingBlocks.Count);
        Debug.Log("chunksNeedingMesh: " + chunksNeedingMesh.Count);
        Debug.Log("chunksNeedingCollisionMesh: " + chunksNeedingCollisionMesh.Count);
    }

    private void Update()
    {
        handleInput();

        //		Profiler.BeginSample( "1" );
        chunksNeedingBlocks.reprioritise();
        chunksNeedingMesh.reprioritise();
        chunksNeedingCollisionMesh.reprioritise();
        //		Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("2");
        runMainThreadTask();
        runOnePerFrameOnMainThreadTask();
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("3");
        activeChunks.RemoveAll(chunk => !chunk.checkIfStillActive());
        inactiveChunks.RemoveAll(chunk => !chunk.checkIfStillInactive());
        UnityEngine.Profiling.Profiler.EndSample();
    }

    private void handleInput()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            displayChunkDistance = Mathf.Max(chunkSize, displayChunkDistance - 8);
            Debug.Log("displayChunkDistance: " + displayChunkDistance);
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            displayChunkDistance += 8;
            Debug.Log("displayChunkDistance: " + displayChunkDistance);
        }
    }

    private void runMainThreadTask()
    {
        Action task = null;

        lock (runOnMainThread)
        {
            while (runOnMainThread.Count > 0)
            {
                task = runOnMainThread[0];
                runOnMainThread.RemoveAt(0);

                if (task != null) task();
            }
        }
    }

    private void runOnePerFrameOnMainThreadTask()
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

        if (task != null) task();
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
        chunkTasks.stop();
        fileTasks.stop();

        chunksNeedingBlocks.stop();
        chunksNeedingMesh.stop();
        chunksNeedingCollisionMesh.stop();
    }
}
