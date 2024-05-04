using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Security.Cryptography;


public class SparseVoxelDAG : MonoBehaviour
{
    public byte[,,] voxelGrid;
    public int gridDepth = 4; // define the depth of your octree. LOD/resolution. 4 - tree can split up to 4 times. determines maximum size of the voxel 4 - 2^4 in each dimenson if the tree is fully expanded
    public VoxelNode rootNode; // Where the root of the octree will be stored

    //stores cache of subtrees, key is representation of the tree and value is the voxelnode that represents it
    private Dictionary<string, VoxelNode> subtreeCache = new Dictionary<string, VoxelNode>();
    public static SparseVoxelDAG Instance { get; private set; }

    private void Awake()
    {
        if(Instance == null) { Instance = this; } else { Destroy(gameObject); }
        
    }
    public VoxelNode InitializeVoxels()
    {
        int gridSize = 1 << gridDepth; // calculate the size of the grid
        voxelGrid = new byte[gridSize, gridSize, gridSize];

        // fill the voxel grid with test data
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    //simple pattern: fill every other voxel
                    voxelGrid[x, y, z] = (byte)((x + y + z) % 2);
                }
            }
        }

        //Assume the worlds origin is also the root nodes position and the grid size is its size
        Vector3 rootPosition = new Vector3(0, 0, 0);
        int rootSize = gridSize; //Ensure this is a power of two

        //Build the DAG from the voxel grid with position and size 
        rootNode = BuildDAG(voxelGrid, gridDepth, rootPosition, rootSize);
        return rootNode;
    }

    VoxelNode BuildDAG(byte[,,] grid, int depth, Vector3 rootPosition, int rootSize)
    {
        //Generate a key for the current grid to check for duplicates
        string key = GenerateSubtreeKey(grid);

        // Check if an identical subtree is already in the cache
        if (subtreeCache.TryGetValue(key, out VoxelNode existingNode))
        {
            return existingNode; // Reuse the existing node
        }

        if (depth == 0 || IsHomogeneous(grid))
        {
            var node = new VoxelNode(rootPosition, rootSize, Color.blue) { IsLeaf = true, Value = grid[0, 0, 0] };
            subtreeCache[key] = node;
            return node;
        }
        else
        {
            VoxelNode node = new VoxelNode(rootPosition, rootSize, Color.blue) {  IsLeaf = false };
            int newDepth = depth - 1;
            int size = 1 << newDepth;
            for (int i = 0; i < 8; i++)
            {
                byte[,,] octant = GetOctant(grid, i, size);
                node.Children[i] = BuildDAG(octant, newDepth, rootPosition, rootSize);
            }

            subtreeCache[key] = node; // Add the new subtree to the cache
            return node;
        }
    }
    
    void VisualizeDAG(VoxelNode node, Vector3 position, float size)
    {
        if (node == null) return;

        //Visualization for the node 
        Gizmos.color = node.IsLeaf ? Color.green : Color.blue;
        Gizmos.DrawWireCube(position, Vector3.one * size);

        if (!node.IsLeaf)
        {
            //Recrusively visualize children
            float childSize = size / 2f;
            for (int i = 0; i < node.Children.Length; i++)
            {
                Vector3 childPosition = CalculateChildPosition(position, childSize, i);
                VisualizeDAG(node.Children[i], childPosition, childSize);
            }

        }
    }

    private void OnDrawGizmos()
    {
        if (rootNode != null)
        {
            VisualizeDAG(rootNode, Vector3.zero, gridDepth);
        }
    }

    public Vector3 CalculateChildPosition(Vector3 parentPosition, float childSize, int octantIndex)
    {
        float offsetX = (octantIndex & 1) * childSize;
        float offsetY = (octantIndex & 2) * childSize / 2;
        float offsetZ = (octantIndex & 4) * childSize / 4;
        return new Vector3(parentPosition.x + offsetX, parentPosition.y + offsetY, parentPosition.z + offsetZ);
    }

    string GenerateSubtreeKey(byte[,,] grid) { 
    //if the grid is homogeneous, return a simple key (( all values are the same)
    if (IsHomogeneous(grid))
        {
            return $"Homogeneous_{grid[0, 0, 0]}";
        }

        //if the grid is not homogeneous, create a more complex key
        // this example uses a StringBuilder to efficiently build a string but you may want
        // to consider a more sophisticated hashing function for larger or more complex grids

        StringBuilder keyBuilder = new StringBuilder();
        int gridSize = grid.GetLength(0); //assuming the grid is cubic 

        for (int z = 0; z < gridSize; z++)
        {
            for(int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    keyBuilder.Append(grid[x, y, z].ToString());
                }
            }
        }

        // use a hasing function to create a hash of the string 
        string key = keyBuilder.ToString();
        using ( var hashAlgorithm = SHA256.Create())
        {
            //convert a new StringBuilder to collecct the bytes and create a string
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(key));

            //Create a new StringBUilder to collect the bytes and create a string
            StringBuilder sBuilder = new StringBuilder();

            //loop through each byte of the hashed data and format each one as a hexadecimal string
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            //Return the hexadecimal string
            return sBuilder.ToString();
        }

    }

    //checks if all elements in 3D array of bytes (grid) are the same
    bool IsHomogeneous(byte[,,] grid)
    {
        byte firstValue = grid[0, 0, 0];
        foreach (byte value in grid)
        {
            if (value != firstValue)
            {
                return false;
            }
        }
        return true;
    }

    byte[,,] GetOctant(byte[,,] grid, int octantIndex, int size)
    {
        int xStart = (octantIndex & 1) * size;
        int yStart = (octantIndex & 2) * size / 2;
        int zStart = (octantIndex & 4) * size / 4;

        byte[,,] octant = new byte[size, size, size];
        
        for (int z = 0; z < size; z++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    octant[x, y, z] = grid[xStart + x, yStart + y, zStart + z];
                }
            }
        }
        return octant;
    }



}
