Shader "VoxelShader"
{
    SubShader
    {
        Pass
        {
            //CGPROGRAM and ENDCG mark the beggining and end of the shade code wirtten in CG, a high level shade language
            CGPROGRAM 
            //tells the shader compiler that the vert function is the vertes shader
            #pragma vertex vert
            //indicates that the frag function is the fragment (or pixel) shader
            #pragma fragment frag
            //includes Unity's common shader include file, which contains usefull shader functions and macros
            #include "UnityCG.cginc"
            //defines a macro for indirect draw arguments used in GPU instancing
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            //inclyudes code necessary for indirect rendering
            #include "UnityIndirect.cginc"

            //uniform variables - parameters that can be set from a script and remain constant for each draw call. ther are used to determine 
            //the grid layaout and spcaing of the voxels and to transform the voxels in the world space
            uniform int _GridWidth;
            uniform int _GridHeight;
            uniform float _Spacing;
            uniform float4x4 _ObjectToWorld;

            //custom data structure used for passing data from the vertex shader to the fragment Shader
            struct v2f
            {
                //represents the postion of the vertex in screen space.  SV_POSITION semantic is used by the rasterizer to determine where the vertex apperas on the screen
                float4 pos : SV_POSITION;
                //this field is for the color of the vertex. COLOR0 seantinc indicates that this is the primary color output from from the vertex shader.
                //which will be interpolated across the surface of the primitive and used by the fragment shader
                float4 color : COLOR0;
            }; 

            //its the vertex shader in the shade corde. It processes each vertex of the mesh and performs the following tasks
            v2f vert(appdata_base v, uint svInstanceID : SV_InstanceID)
            {
                //Initializes indirect drawing arguments 
                InitIndirectDrawArgs(0);
                v2f o;

                //calculates command and instance IDs using GetCommandID and GetIndirectInstanceID
                uint cmdID = GetCommandID(0);
                uint instanceID = GetIndirectInstanceID(svInstanceID);

                //determiones the position of each instance in a gridf using the instace ID
                //grid width grid height and spacing
                uint x = instanceID % _GridWidth;
                uint z = instanceID / _GridWidth;
                
                float3 gridPosition = float3(x * _Spacing, 0, z * _Spacing);
                //transforms the vertex psotion form local to wrold space(wpos), than to screen space(o.pos)
                float4 wpos = mul(_ObjectToWorld, v.vertex + float4(gridPosition, 0));

                o.pos = mul(UNITY_MATRIX_VP, wpos);
                // Alternating green color based on instance ID
                // Checkerboard pattern
                bool isEven = ((x + z) % 2) == 0;
                o.color = isEven ? float4(0.0, 1.0, 0.0, 1.0) : float4(0.0, 0.5, 0.0, 1.0); // Adjust green tones as desired

                //returns the transforned vertex data v2f for further processing in the graphics pipeline
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}