using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelNode
{
    public bool IsLeaf; // last part of the tree, if not true its branches/internal nodes
    public byte Value; // for simplicity, assuming a binary voxel grid (0for empty, 1 for filled)
    public VoxelNode[] Children = new VoxelNode[8];
    public Vector3 Position; // the position of the voxel in world space
    public float Size; // the size of the voxel
    public Color NodeColor; // the color of the voxel

    //Constructor to initialize a VoxelNode with position, size, and color
    public VoxelNode(Vector3 position, float size, Color color)
    {
        Position = position;
        Size = size;
        IsLeaf = true;
        Value = 0; // Default to enpty
        NodeColor = color;  
    }



}
