using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimplexNoise;
using Unity.Collections;
using Unity.Jobs;
public class Chunk : MonoBehaviour
{
    private Voxel[,,] voxels;
    private int chunkSize = 16;
    private Color gizmoColor;

    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    private void Start()
    {
        /*
        // Initialize Mesh Components
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshCollider = gameObject.AddComponent<MeshCollider>();

        // Call this to generate the chunk mesh
        GenerateMesh();*/
    }


    void OnDrawGizmos()
    {
        if (voxels != null)
        {
            Gizmos.color = gizmoColor;
            //Gizmos.DrawCube(transform.position + new Vector3(chunkSize / 2, chunkSize / 2, chunkSize / 2), new Vector3(chunkSize, chunkSize, chunkSize));
        }
    }
    
    private void GenerateVoxelData(Vector3 chunkWordPosition)
    {
        int totalVoxels = chunkSize * chunkSize * chunkSize;

        //allocates block of memory to hold the voxel data (uses allocator tempjob - good for short amount of time jobs, needs to be deallocated soon after the job
        NativeArray<Voxel> voxelsData = new NativeArray<Voxel>(totalVoxels, Allocator.TempJob);

        //created a job structure that will be used to determine the type of each voxel, filled with neccesary data 
        VoxelTypeDeterminationJob voxelJob = new VoxelTypeDeterminationJob
        {
            voxels = voxelsData,
            chunkSize = this.chunkSize,
            maxHeight = World.Instance.maxHeight,
            noiseScale = World.Instance.noiseScale,
            chunkWorldPosition = chunkWordPosition // pass the chunk's world position
        };

        //schedules the job for execution
        JobHandle jobHandle = voxelJob.Schedule();

        //waits for the job to complete, neccesary  vefore accesing the results of the job to avoid race(?) conditions or acessing incomplete data
        jobHandle.Complete();

        //Use voxelsData to initialize voxels array 
        InitializeVoxels(voxelsData);

        //freees allocated memore for the voxelsData native array. 
        //It's crucial to dispose of any allocated NativeArray after use to prevent memory leaks.
        voxelsData.Dispose();

       
    }


    // WORKING ON ALREADY CREATED STUFF
    private Voxel.VoxelType DetermineVoxelType(float x, float y, float z)
    {
        //makes cube
        //float noiseValue = Noise.CalcPixel3D((int)x, (int)y, (int)z, 0.1f);

        //now we use the new GetNoisePoint() function
        float noiseValue = GlobalNoise.GetNoisePoint((int)x, (int)z);

        //normalize noise value to [0, 1]
        float normalizedNoiseValue = (noiseValue + 1) / 2;

        // calculate maxheight
        float maxHeight = normalizedNoiseValue * World.Instance.maxHeight;

        if (y <= maxHeight)
            return Voxel.VoxelType.Grass;
        else
            return Voxel.VoxelType.Air;
    }

    //new method to iterate through the voxel data
    public void IterateVoxels()
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    ProcessVoxel(x, y, z);
                }
            }
        }
    }

    private void ProcessVoxel(int x, int y, int z)
    {
        //check if the voxels array is initialized and the indices are within bounds 
        if (voxels == null || x < 0 || x >= voxels.GetLength(0) || y < 0 || y >= voxels.GetLength(1) || z < 0 || z >= voxels.GetLength(2))
        {
            return; // skip processing if the array is not initialized or indices are out of bounds
        }

        Voxel voxel = voxels[x, y, z];
        if (voxel.isActive)
        {
            //check each face of the voxel for visibility
            bool[] facesVisible = new bool[6];

            //check visibility for each face
            facesVisible[0] = IsFaceVisible(x, y + 1, z); // top
            facesVisible[1] = IsFaceVisible(x, y - 1, z); // bot
            facesVisible[2] = IsFaceVisible(x - 1, y, z); // left
            facesVisible[3] = IsFaceVisible(x + 1, y, z); // right
            facesVisible[4] = IsFaceVisible(x, y, z + 1); // front
            facesVisible[5] = IsFaceVisible(x, y, z - 1); // back

            for (int i = 0; i < facesVisible.Length; i++)
            {
                if (facesVisible[i])
                    AddFaceData(x, y, z, i); // mehtod to add mesh data for the visible face
            }
        }
    }
    
    private bool IsFaceVisible(int x, int y, int z)
    {
        // Convert local chunk coordinates to global coordinates
        Vector3 globalPos = transform.position + new Vector3(x, y, z);

        // Check if the neighboring voxel is inactive or out of bounds in the current chunk
        // and also if it's inactive or out of bounds in the world (neighboring chunks)
        return IsVoxelHiddenInChunk(x, y, z) && IsVoxelHiddenInWorld(globalPos);
    }

    private bool IsVoxelHiddenInChunk(int x, int y, int z)
    {
        if (x < 0 || x >= chunkSize || y < 0 || y >= chunkSize || z < 0 || z >= chunkSize)
            return true; // Face is at the boundary of the chunk
        return !voxels[x, y, z].isActive;
    }

    private bool IsVoxelHiddenInWorld(Vector3 globalPos)
    {
        // Check if there is a chunk at the global position
        Chunk neighborChunk = World.Instance.GetChunkAt(globalPos);
        if (neighborChunk == null)
        {
            // No chunk at this position, so the voxel face should be hidden
            return true;
        }

        // Convert the global position to the local position within the neighboring chunk
        Vector3 localPos = neighborChunk.transform.InverseTransformPoint(globalPos);

        // If the voxel at this local position is inactive, the face should be visible (not hidden)
        return !neighborChunk.IsVoxelActiveAt(localPos);
    }

    public bool IsVoxelActiveAt(Vector3 localPosition)
    {
        // round the local position to get the nearest voxel index
        int x = Mathf.RoundToInt(localPosition.x);
        int y = Mathf.RoundToInt(localPosition.y);
        int z = Mathf.RoundToInt(localPosition.z);

        // check if the indices are within the bounds of the voxel array 
        if (x >= 0 && x < chunkSize && y >= 0 && y < chunkSize && z >= 0 && z < chunkSize)
        {
            // return the active state of the voxel at these indices 
            return voxels[x, y, z].isActive;
        }

        //if out of bounds, consider the voxel inactive
        return false;
    }

    public void ResetChunk()
    {
        // Clear voxel data
        voxels = new Voxel[chunkSize, chunkSize, chunkSize];

        // Clear mesh data
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            meshFilter.sharedMesh.Clear();
            vertices.Clear();
            triangles.Clear();
            uvs.Clear();
        }
    }






    //CREATING MESH VOXELS ETC
    private void GenerateMesh()
    {
        IterateVoxels();
        if (vertices.Count > 0)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();

            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;

            meshRenderer.material = World.Instance.VoxelMaterial;
        }
    }
    public void Initialize(int size)
    {
        this.chunkSize = size;
        voxels = new Voxel[size, size, size];

        GenerateVoxelData(transform.position);

        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) { meshFilter = gameObject.AddComponent<MeshFilter>(); }


        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) { meshRenderer = gameObject.AddComponent<MeshRenderer>(); }

        meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null) { meshCollider = gameObject.AddComponent<MeshCollider>(); }

        GenerateMesh(); // Call after ensuring all necessary components and data are set
    }

    private void InitializeVoxels(NativeArray<Voxel> voxelsData)
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int index = x * chunkSize + y * chunkSize + z * chunkSize;
                    Voxel voxel = voxelsData[index];

                    //use world coordinates for noise sampling
                    Vector3 worldPos = transform.position + new Vector3(x, y, z);

                    //Now the voxel type is already determined ny the job 
                    voxels[x, y, z] = new Voxel(worldPos, voxel.type, voxel.isActive);
                }
            }
        }
    }

    private void AddFaceData(int x, int y, int z, int faceIndex)
    {
        //based on faceIndex, determine vertices and triangles
        // add vertices and triangles for the visible face
        // calculate and add corresponding UVs

        if (faceIndex == 0) // Top Face
        {
            vertices.Add(new Vector3(x, y + 1, z));
            vertices.Add(new Vector3(x, y + 1, z + 1));
            vertices.Add(new Vector3(x + 1, y + 1, z + 1));
            vertices.Add(new Vector3(x + 1, y + 1, z));
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(0, 1));
        }

        if (faceIndex == 1) // Bottom Face
        {
            vertices.Add(new Vector3(x, y, z));
            vertices.Add(new Vector3(x + 1, y, z));
            vertices.Add(new Vector3(x + 1, y, z + 1));
            vertices.Add(new Vector3(x, y, z + 1));
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 0));
        }

        if (faceIndex == 2) // Left Face
        {
            vertices.Add(new Vector3(x, y, z));
            vertices.Add(new Vector3(x, y, z + 1));
            vertices.Add(new Vector3(x, y + 1, z + 1));
            vertices.Add(new Vector3(x, y + 1, z));
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(0, 1));
        }

        if (faceIndex == 3) // Right Face
        {
            vertices.Add(new Vector3(x + 1, y, z + 1));
            vertices.Add(new Vector3(x + 1, y, z));
            vertices.Add(new Vector3(x + 1, y + 1, z));
            vertices.Add(new Vector3(x + 1, y + 1, z + 1));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 0));
        }

        if (faceIndex == 4) // Front Face
        {
            vertices.Add(new Vector3(x, y, z + 1));
            vertices.Add(new Vector3(x + 1, y, z + 1));
            vertices.Add(new Vector3(x + 1, y + 1, z + 1));
            vertices.Add(new Vector3(x, y + 1, z + 1));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 1));
        }

        if (faceIndex == 5) // Back Face
        {
            vertices.Add(new Vector3(x + 1, y, z));
            vertices.Add(new Vector3(x, y, z));
            vertices.Add(new Vector3(x, y + 1, z));
            vertices.Add(new Vector3(x + 1, y + 1, z));
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 0));

        }
        AddTriangleIndices();
    }
    private void AddTriangleIndices()
    {
        int vertCount = vertices.Count;

        // First triangle
        triangles.Add(vertCount - 4);
        triangles.Add(vertCount - 3);
        triangles.Add(vertCount - 2);

        // Second triangle
        triangles.Add(vertCount - 4);
        triangles.Add(vertCount - 2);
        triangles.Add(vertCount - 1);
    }
}

