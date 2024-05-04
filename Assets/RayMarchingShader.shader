Shader "Unlit/RayMarchingShader"
{//https://medium.com/@adamy1558/building-a-high-performance-voxel-engine-in-unity-a-step-by-step-guide-part-8-ray-marching-590257b5984d
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _AmbientColor ("Ambient Color", Color) = (1, 1, 1, 1)
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.1
        _LightAzimuth ("Light Azimuth", Range(0, 360)) = 45
        _LightElevation ("Light Elevation", Range(0, 360)) = 45
        _FOV ("Field of View", Range(0, 20)) = .9
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
             CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
      
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _AmbientColor;
            float _AmbientStrength;
            float4 _CamPos;
            float4x4 _CamRot;

            float _FOV;
      
            float _LightAzimuth;
            float _LightElevation;
            float3 _LightDir = normalize(float3(-1, -1, -1)); // Light coming from the top left


            //Input structure for the vertex shader 
            struct appdata
            {
                //4 component vector    
                float4 vertex : POSITION;
                //represents uv coordinates of the vertex - two component vector (u, v)
                float2 uv : TEXCOORD0;
            };

            //output texture from the vertex shader which gets passed to the fragment shader
            struct v2f
            {
                //passes along the uv coordinates form the vertex shaader to the fragment shader    
                float2 uv : TEXCOORD0;
                //represents the position of the vertex in clip spacce after transformation
                //SV_POSITION is a system semantic whichcc tells the rasterizer stage of the GPU where the vertex is on the screen
                float4 vertex : SV_POSITION;
            };
            
            ///vertex shader function. it takes and appdata structure as input and returns a v2f structure
            v2f vert (appdata v)
            {
                v2f o;
                //converts the vertex position form object space to clip space. Clip space is a homogeneous space where rendering takes place before prespective division;
                //after the division, you get normalized device coordinates NDC. the UV coordinates are passed though uncghanged from the input to the output structure
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            void calculateLightDir()    
            {
                float azimuthRad = _LightAzimuth * 3.14159 / 180.0;
                float elevationRad = _LightElevation * 3.14159 / 180.0;
                _LightDir.x = cos(azimuthRad) * cos(elevationRad);
                _LightDir.y = sin(elevationRad);
                _LightDir.z = sin(azimuthRad) * cos(elevationRad);
                _LightDir = normalize(_LightDir);
            }

            float3 calculateNormal(float3 pos) {
                float eps = 0.01; // Small value for finite difference
                float3 epsVec = float3(eps, 0.0, 0.0);
                float dx = length(max(abs(pos + epsVec) - 0.5, 0.0)) - length(max(abs(pos - epsVec) - 0.5, 0.0));
                epsVec = float3(0.0, eps, 0.0);
                float dy = length(max(abs(pos + epsVec) - 0.5, 0.0)) - length(max(abs(pos - epsVec) - 0.5, 0.0));
                epsVec = float3(0.0, 0.0, eps);
                float dz = length(max(abs(pos + epsVec) - 0.5, 0.0)) - length(max(abs(pos - epsVec) - 0.5, 0.0));
                return normalize(float3(dx, dy, dz));
            }

            float4 frag (v2f i) : SV_Target {
                calculateLightDir();
                float3 camPos = _CamPos.xyz;
                float aspectRatio = _ScreenParams.y / _ScreenParams.x; // Height / Width
                float2 ndc = float2((i.uv.x * 2.0 - 1.0) * _FOV, (i.uv.y * 2.0 - 1.0) * _FOV * aspectRatio);
                float3 rayDir = mul((float3x3)_CamRot, normalize(float3(ndc, 1.0)));

                // Ray marching parameters
                float maxDistance = 100.0;//maximum distance the will travel
                float minDist = 0.01; // Minimum distance to consider a hit
                int maxSteps = 500; // Maximum steps to march. controls performance
                float cubeSize = 1.0; //Size of each cube
                float gridSpacing = 3.0; // Distance between the centers of ajacent cubes 

                // March the ray
                float distTravelled = 0.0;
                for (int j = 0; j < maxSteps; j++) {//"marches" the ray forward in steps, up to a maxSteps. each iteration advences ray further into the scene
                    float3 currentPos = camPos + distTravelled * rayDir;
                    //Adjust for grid pattern
                    float3 gridPos = fmod(currentPos, cubeSize + gridSpacing) - 0.5 * (cubeSize + gridSpacing);

                    float distToCube = length(max(abs(currentPos) - 0.5, 0.0)); // Distance to the surface of the cube
                    if (distToCube < minDist) {
                        // Hit detected
                        float3 normal = calculateNormal(currentPos);
                        float diffuse = max(0.0, dot(normal, _LightDir));

                        // Use the ambient color and strength properties
                        float3 ambient = _AmbientColor.rgb * _AmbientStrength;

                        // Add the ambient color to the diffuse lighting
                        float3 finalColor = diffuse + ambient;

                        return float4(finalColor, 1); // Use final color
                    }
                    distTravelled += distToCube;
                    if (distTravelled > maxDistance) break; // Exit if too far
                }
                // No hit
                return float4(0, 0, 0, 1); // Black color for background
            }



            ENDCG
        }
    }
}
/*            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }*/