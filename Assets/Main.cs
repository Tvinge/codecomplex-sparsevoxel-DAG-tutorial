using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    VoxelNode voxels;
    Mesh voxelMesh;

    void Start()
    {
        voxels = SparseVoxelDAG.Instance.InitializeVoxels();
        voxelMesh = MeshGenerator.Instance.GenerateMesh(voxels, SparseVoxelDAG.Instance.gridDepth);

    }

    void Update()
    {
        
    }
}
