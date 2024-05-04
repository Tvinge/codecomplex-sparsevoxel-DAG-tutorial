using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayMarchingControl : MonoBehaviour
{
    public Material rayMarchingMaterial;
    public RenderTexture renderTexture;

    void Start()
    {
        if ( rayMarchingMaterial == null)
        {
            Debug.Log("Rac marching material is not set.");
        }

        //Initialize the RenderTexture
        if ( renderTexture == null)
        {
            renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }
        
    }

    void Update()
    {
        //Pass the camera's position to the shader
        rayMarchingMaterial.SetVector("_CamPos", Camera.main.transform.position);

        // Convert the camer's rotation to matrx4x4 and pass it tot hte shader
        Matrix4x4 camRot = Matrix4x4.Rotate(Camera.main.transform.rotation);
        rayMarchingMaterial.SetMatrix("_CamRot", camRot);

    }

    //is called after all rendering, can be used to aplly post-processing effects
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (rayMarchingMaterial != null)
        {
            //draw the ray marchiung shader output tot the RenderTexture
            Graphics.Blit(null, renderTexture, rayMarchingMaterial);

            //optionally, display the RenderTexture on the screen
            Graphics.Blit(renderTexture, destination);
        }
    }
}
