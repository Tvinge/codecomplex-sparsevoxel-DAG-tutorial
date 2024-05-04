using System.Collections;
using Unity.Jobs;
using UnityEngine;
using SimplexNoise;
using Unity.Collections;

/// <summary>
/// Struct implementing the IJob interface, which allows it to run computations on a separate thread for better performanc.
/// </summary>
public struct VoxelTypeDeterminationJob : IJob
{
    public NativeArray<Voxel> voxels; //stores the voxel data for the entire chunk
    public int chunkSize;
    public float maxHeight;
    public float noiseScale;
    public Vector3 chunkWorldPosition;

    public void Execute()
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int index = x * chunkSize * chunkSize + y * chunkSize + z;
                    Vector3 worldPos = chunkWorldPosition + new Vector3(x, y, z);

                    //Calculate noise
                    float noiseValue = Noise.CalcPixel2D((int)worldPos.x, (int)worldPos.z, noiseScale);
                    float normalizedNoiseValue = (noiseValue + 1) / 2;
                    float calculatedHeight = normalizedNoiseValue * maxHeight;

                    //Determine voxel type 
                    Voxel.VoxelType type = (y <= calculatedHeight) ? Voxel.VoxelType.Grass : Voxel.VoxelType.Air;

                    //Calculate the position for the  voxel 
                    Vector3 voxelPosition = new Vector3(x, y, z); //Asuming local position in chunk

                    //Set voxel data
                    voxels[index] = new Voxel(voxelPosition, type, type != Voxel.VoxelType.Air);

                }
            }
        }
    }
}
