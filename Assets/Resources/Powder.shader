Shader "Unlit/Powder"
{
    Properties
    {
        [Header(Main)]
        [HDR]_Tint ("Tint", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _TextureOpacity ("Texture Opacity", Range(0,1)) = 1.0
        [HDR]_TopColor ("Top Color", Color) = (1,1,1,1)
        [Header(Foam)]
        [HDR]_FoamColor ("Foam Line Color", Color) = (1,1,1,1)
        _Line ("Foam Line Width", Range(0,0.1)) = 0.0    
        _LineSmooth ("Foam Line Smoothness", Range(0,0.1)) = 0.0    
        [Header(Rim)]
        [HDR]_RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0,10)) = 0.0
        [Header(Bump)]
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Bump Scale", Range(0, 2)) = 1.0
        _BumpIntensity ("Bump Intensity", Range(0, 10)) = 1.0
        [Header(Sine)]
        _Freq ("Frequency", Range(0,15)) = 8
        _Amplitude ("Amplitude", Range(0,0.5)) = 0.15
    }
    
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "DisableBatching" = "True" }
        
        // Single pass that handles both front and back faces
        Pass
        {
            ZWrite On  // Enable depth writing
            Cull Off   // Render both front and back faces
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;    
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 bumpUV : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                float4 vertex : SV_POSITION;
                float3 viewDir : COLOR;
                float3 normal : COLOR2;        
                float3 fillPosition : TEXCOORD3;
                float3 worldNormal : TEXCOORD4;
            };
            
            sampler2D _MainTex;
            float _TextureOpacity;
            float4 _MainTex_ST;
            sampler2D _BumpMap;
            float4 _BumpMap_ST;
            float _BumpScale;
            float _BumpIntensity;
            float3 _FillAmount;
            float _WobbleX, _WobbleZ;
            float _Height;
            float4 _TopColor, _RimColor, _FoamColor, _Tint;
            float _Line, _RimPower, _LineSmooth;
            float _Freq, _Amplitude;
            
            float3 Unity_RotateAboutAxis_Degrees(float3 In, float3 Axis, float Rotation)
            {
                Rotation = radians(Rotation);
                float s = sin(Rotation);
                float c = cos(Rotation);
                float one_minus_c = 1.0 - c;

                Axis = normalize(Axis);
                float3x3 rot_mat = 
                {   one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s, one_minus_c * Axis.z * Axis.x + Axis.y * s,
                    one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c, one_minus_c * Axis.y * Axis.z - Axis.x * s,
                    one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s, one_minus_c * Axis.z * Axis.z + c
                };
                float3 Out = mul(rot_mat,  In);
                return Out;
            }
            
            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.bumpUV = TRANSFORM_TEX(v.uv, _BumpMap);
                UNITY_TRANSFER_FOG(o,o.vertex);
                // get world position of the vertex - transform position
                float3 worldPos = mul (unity_ObjectToWorld, v.vertex.xyz);  
                float3 worldPosOffset = float3(worldPos.x, worldPos.y , worldPos.z) - _FillAmount;
                // rotate it around XY
                float3 worldPosX= Unity_RotateAboutAxis_Degrees(worldPosOffset, float3(0,0,1),90);
                // rotate around XZ
                float3 worldPosZ = Unity_RotateAboutAxis_Degrees(worldPosOffset, float3(1,0,0),90);
                // combine rotations with worldPos, based on sine wave from script
                float3 worldPosAdjusted = worldPos + (worldPosX * _WobbleX) + (worldPosZ * _WobbleZ); 
                // how high up the liquid is
                o.fillPosition =  worldPosAdjusted - _FillAmount;
                
                o.normal = v.normal;
                o.worldNormal  = mul ((float4x4)unity_ObjectToWorld, v.normal );
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }
            
            fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
            {          
                // Sample the normal map and transform to world space
                half3 tnormal = UnpackNormal(tex2D(_BumpMap, i.bumpUV));
                tnormal.xy *= _BumpScale;
                tnormal = normalize(tnormal);

                float3 worldNormal = mul( unity_ObjectToWorld, float4( i.normal, 0.0 ) ).xyz;
                
                // add movement based deform, using a sine wave
                float wobbleIntensity = abs(_WobbleX) + abs(_WobbleZ);            
                float wobble = sin((i.fillPosition.x * _Freq) + (i.fillPosition.z * _Freq) + (_Time.y)) * (_Amplitude * wobbleIntensity);    
                float normalInfluence = tnormal.xyz * _BumpIntensity * 0.001;
                float movingfillPosition = i.fillPosition.y + wobble + normalInfluence;

                // Calculate the liquid surface
                float cutoffTop = step(movingfillPosition, 0.5);
                
                // Check if we're outside the liquid area and discard if so
                if (cutoffTop < 0.01)
                    discard;
                
                // Calculate the liquid body
                fixed4 textureCol = tex2D(_MainTex, i.uv);
                fixed4 col = lerp(_Tint, textureCol * _Tint, _TextureOpacity);
                fixed4 topTextureCol = tex2D(_MainTex, tnormal.xz);
                fixed4 top_col = lerp(_TopColor, topTextureCol * _TopColor, _TextureOpacity);
                float result = cutoffTop;
                UNITY_APPLY_FOG(i.fogCoord, col);
                float4 resultColored = result * col;
                                
                // Different colors for front and back faces
                float4 finalResult;
                if (facing > 0) {
                    // Front face (the surface)
                    finalResult = resultColored;
                    finalResult.a = _Tint.a;
                } else {
                    // Back face (the top/inside)
                    finalResult = result * top_col;
                }
                return finalResult * _Tint.a;
            }
            ENDCG
        }
    }
}