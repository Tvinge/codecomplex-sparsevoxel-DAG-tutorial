using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimplexNoise;

public static class GlobalNoise 
{
    public static float[,] GetNoise()
    {
        Noise.Seed = World.Instance.noiseSeed;
        //the number of points to generate in the 1st and 2nd dimension
        int width = World.Instance.chunkSize * World.Instance.worldSize;
        int height = World.Instance.chunkSize * World.Instance.worldSize;
        // the scale of the noise. the greater the scale, the denser the noise gets
        float scale = World.Instance.noiseScale;
        float[,] noise = Noise.Calc2D(width, height, scale); // returns to array containing 2d simplex noise

        return noise;

    }
    public static float GetNoisePoint(int x, int z)
    {
        float scale = World.Instance.noiseScale;
        float noise = Noise.CalcPixel2D(x, z, scale);

        return noise;
    }

    public static void SetSeed()
    {
        Noise.Seed = World.Instance.noiseSeed;
    }

    public static float GetGlobalNoiseValue(float globalX, float globalZ, float[,] globalNoiseMap)
    {
        //convert global coordinate to noise map coordinates
        int noiseMapX = (int)globalX % globalNoiseMap.GetLength(0);
        int noiseMapZ = (int)globalZ % globalNoiseMap.GetLength(1);

        //ensure that the coordinates are withing the bounds of the noise mpa
        if (noiseMapX >= 0 && noiseMapX < globalNoiseMap.GetLength(0) && noiseMapZ >= 0 && noiseMapZ < globalNoiseMap.GetLength(1))
        {
            return globalNoiseMap[noiseMapX, noiseMapZ];
        }
        else
        {
            return 0; //default value if out of bounds
        }
    }
}
