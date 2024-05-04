using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public int worldSize = 5;
    public int chunkSize = 16;

    private Dictionary<Vector3, Chunk> chunks;


    public static World Instance { get; private set; }

    public Material VoxelMaterial;

    //stuff for world generation
    public int noiseSeed = 1234;
    public float maxHeight = 0.2f;
    public float noiseScale = 0.015f;
    public float[,] noiseArray;

    PlayerController playerController;
    Vector3 playerPosition;

    public int loadRadius = 5; // define how many chunks to load around the player
    public int unloadRadius = 7; // chunks outside this radius wiil be unloaded


    private Vector3Int lastPlayerChunkCoordinates;
    private int chunksMovedCount = 0;
    public int chunkUpdateThreshold = 2; // Updated every 2 chunks
    private bool JustStarted = true;

    //loading chunks stuff
    private Queue<Vector3> chunkLoadQueue = new Queue<Vector3>();
    private int chunksPerFrame = 4; // Number of chunks to load per frame
    private int loadInterval = 4; // Load chunks every 4 frames
    private int frameCounter = 0;

    //unloading chunks stuff
    private Queue<Vector3> chunkUnloadQueue = new Queue<Vector3>();
    private int unloadFrameCounter = 0;
    private int unloadInterval = 5;
    private int chunksPerFrameUnloading = 4;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // optional if you want it to persist through scenes
        }
        else
        {
            Destroy(gameObject);
        }

        noiseArray = GlobalNoise.GetNoise();
    }

    void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        chunks = new Dictionary<Vector3, Chunk>();
        lastPlayerChunkCoordinates = Vector3Int.zero;
        GlobalNoise.SetSeed();
        ChunkPoolManager.Instance.PopulateInitialPool();


    }

    private void Update()
    {
        playerPosition = playerController.getPlayerPosition();
        UpdateChunks(playerPosition);
        ProcessChunkLoadingQueue();
        ProcessChunkUnloadingQueue();
    }

    private void GenerateWorld()
    {
        for (int x = 0; x < worldSize; x++)
        {
            for (int y = 0; y < worldSize; y++)
            {
                for (int z = 0; z < worldSize; z++)
                {
                    Vector3 chunkPosition = new Vector3(x * chunkSize, y * chunkSize, z * chunkSize);
                    GameObject newChunkObject = new GameObject($"Chunk_{x}_{y}_{z}");
                    newChunkObject.transform.position = chunkPosition;
                    newChunkObject.transform.parent = this.transform;

                    Chunk newChunk = newChunkObject.AddComponent<Chunk>();
                    newChunk.Initialize(chunkSize);
                    chunks.Add(chunkPosition, newChunk);
                }
            }
        }
        //additional methods
    }


    void UpdateChunks(Vector3 playerPosition)
    {
        Vector3Int playerChunkCoordinates = new Vector3Int(
            Mathf.FloorToInt(playerPosition.x / chunkSize),
            Mathf.FloorToInt(playerPosition.y / chunkSize),
            Mathf.FloorToInt(playerPosition.z / chunkSize));

        // Check if player has moved to a new chunk
        if (!playerChunkCoordinates.Equals(lastPlayerChunkCoordinates))
        {
            if (chunksMovedCount >= chunkUpdateThreshold || JustStarted)
            {
                LoadChunksAround(playerChunkCoordinates);
                UnloadDistantChunks(playerChunkCoordinates);
                JustStarted = false;
                chunksMovedCount = 0;
            }

            lastPlayerChunkCoordinates = playerChunkCoordinates;
            chunksMovedCount++;
        }
    }


    //CHUNKS LOADING
    void LoadChunksAround(Vector3Int centerChunkCoordinates)
    {
        for (int x = -loadRadius; x <= loadRadius; x++)
        {
            for (int z = -loadRadius; z <= loadRadius; z++)
            {
                Vector3Int chunkCoordinates = new Vector3Int(centerChunkCoordinates.x + x, 0, centerChunkCoordinates.z + z);
                Vector3 chunkPosition = new Vector3(chunkCoordinates.x * chunkSize, 0, chunkCoordinates.z * chunkSize);
                if (!chunks.ContainsKey(chunkPosition))
                {
                    chunkLoadQueue.Enqueue(chunkPosition);
                }
            }
        }
    }
    
    void ProcessChunkLoadingQueue()
    {
        frameCounter++;
        if(frameCounter % loadInterval == 0)
        {
            for (int i = 0; i < chunksPerFrame && chunkLoadQueue.Count > 0; i++)
            {
                Vector3 chunkPosition = chunkLoadQueue.Dequeue();
                if (!chunks.ContainsKey(chunkPosition))
                {
                    Chunk chunkObject = ChunkPoolManager.Instance.GetChunk();
                    chunkObject.transform.position = chunkPosition;
                    chunkObject.transform.parent = this.transform; // Optional, for organizational purposes
                    chunkObject.Initialize(chunkSize); // Initialize the chunk with its size
                    chunks.Add(chunkPosition, chunkObject); // Add the chunk to the dictionary
                    chunkObject.gameObject.SetActive(true);
                }
            }
        }
    }

    //CHUNKS UNLOADING
    void UnloadDistantChunks(Vector3Int centerChunkCoordinates)
    {
        List<Vector3> chunksToUnload = new List<Vector3>();
        foreach (var chunk in chunks)
        {
            Vector3Int chunkCoord = new Vector3Int(
                Mathf.FloorToInt(chunk.Key.x / chunkSize),
                Mathf.FloorToInt(chunk.Key.y / chunkSize),
                Mathf.FloorToInt(chunk.Key.z / chunkSize));

            if (Vector3Int.Distance(chunkCoord, centerChunkCoordinates) > unloadRadius)
            {
                chunkUnloadQueue.Enqueue(chunk.Key);
            }
        }
    }

    void ProcessChunkUnloadingQueue()
    {
        // Check if there are chunks in the unload queu
        if (chunkUnloadQueue.Count > 0)
        {
            unloadFrameCounter++;
            if(unloadFrameCounter % unloadInterval == 0)
            {
                int chunksToProcess = Mathf.Min(chunksPerFrameUnloading, chunkUnloadQueue.Count);
                for (int i = 0; i < chunksToProcess; i++)
                {
                    Vector3 chunkPosition = chunkUnloadQueue.Dequeue();
                    Chunk chunkToUnload = GetChunkAt(chunkPosition);
                    if (chunkToUnload != null)
                    {
                        ChunkPoolManager.Instance.ReturnChunk(chunkToUnload);
                        chunks.Remove(chunkPosition); // remove the chunk from the active chunks dictionary
                    }
                }
            }
        }
    }






    public Chunk GetChunkAt(Vector3 globalPosition)
    {
        // Calculate the chunk's starting position based on the global position
        Vector3Int chunkCoordinates = new Vector3Int(
            Mathf.FloorToInt(globalPosition.x / chunkSize) * chunkSize,
            Mathf.FloorToInt(globalPosition.y / chunkSize) * chunkSize,
            Mathf.FloorToInt(globalPosition.z / chunkSize) * chunkSize
        );

        // Retrieve and return the chunk at the calculated position
        if (chunks.TryGetValue(chunkCoordinates, out Chunk chunk))
        {
            return chunk;
        }

        // Return null if no chunk exists at the position
        return null;
    }



}
