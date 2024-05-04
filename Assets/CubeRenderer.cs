using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGPU : MonoBehaviour
{
    public Material material;
    public Mesh mesh;

    //a GraphicsBuffer object for storing indirect drawing arguments It's used for GPU instancing with indrect draw calls
    GraphicsBuffer commandBuf;

    // an array of IndirectDrawIndexedArgs that stores data for each indrect draw call, like the number of vertices and instances to draw
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData; 

    // number of commands in  the commandBuf. this is typucally the number of different draw calls you plan to make.
    const int commandCount = 2;

    //dimensions of the grid for mesh
    public int gridWidth = 1000;
    public int gridHeight = 1000;

    //distance between each instance in the grid
    public float spacing = 1.0f;

    //calculated value representing the total number of instances (meshes) to be drawn. The product of gridWidth and gridHeight
    int totalInstances;


    void Start()
    {
        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];
        totalInstances = gridWidth * gridHeight;
    }

    void Update()
    {
        //shader parameteres
        //Sets the grid width height and spacing in the material shader using SetInt and SetFloat
        //used for positioning instances in the grid
        material.SetInt("_GridWidth", gridWidth);
        material.SetInt("_GridHeight", gridHeight);
        material.SetFloat("_Spacing", spacing);

        //Render Parameters
        //Creates RenderParams object with the material and sets the world bounds for frostum culllin
        RenderParams rp = new RenderParams(material);
        rp.worldBounds = new Bounds(Vector3.zero, 1000 * Vector3.one); // use tigther bounds for better FOV culling

        //Material Property Block
        //Initializes a MaterialPropertBlock to optimze material property changes without creating new materials
        rp.matProps = new MaterialPropertyBlock();

        //Instance Transformation
        //Sets a transformation matrix in the material propery block for positioning instances
        rp.matProps.SetMatrix("_ObjectToWorld", Matrix4x4.Translate(new Vector3(-4.5f, 0, 0)));

        //Command Data Setup
        //Configures cammandData for two draw calls, setting the nuymber of indices per instance and the number of instances to draw
        commandData[0].indexCountPerInstance = mesh.GetIndexCount(0);
        commandData[0].instanceCount = (uint)totalInstances;
        commandData[1].indexCountPerInstance = mesh.GetIndexCount(0);
        commandData[1].instanceCount = (uint)totalInstances;

        //CommandBuffer Update
        //Updates the commandBuf with commandData
        commandBuf.SetData(commandData);

        //Rendering
        //Calls Graphics.RenderMeshIndirect to draw the meshes using the indirect draw arguments in commandBuf
        Graphics.RenderMeshIndirect(rp, mesh, commandBuf, commandCount);
    }

    private void OnDestroy()
    {
        //realeses the memory asociated with commandBuf
        commandBuf?.Release(); // ? - null-conditional operator, ensuring that release is called only if commandBuf is not null
        //important for resource managemenet, preventing memory leaks by ensuring
        //that the graphics buffer is proprtly disposed of when the script or its GameObject is destroye

        commandBuf = null;
    }
}
