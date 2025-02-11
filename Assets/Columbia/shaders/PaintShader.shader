Shader "Custom/PaintShader"
{
    Properties
    {
        [NoScaleOffset][MainTexture] _PaintColorMap ("Paint Base Map (RGB)", 2D) = "white" {}
        [MainColor] _BaseColor ("Bottom Color", Color) = (1,1,1,1)
        _TopColor ("Top Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _MaxHeight ("Max Height", float) = 10
        _Tiling ("Tiling", float) = 1

        [Space(20)]
        [Toggle(_NORMALMAP)] _NormalMapToggle ("Use Normal Map", float) = 0
        [NoScaleOffset] _PaintNormalMap("Paint Normal Map", 2D) = "Bump" {}
        _NormalMapStrength ("Normal Map Strength", float) = 1

        [Space(20)]
        [NoScaleOffset] _PaintDataMap ("Paint Data Map", 2D) = "white" {}

        [Space(20)]
        [Toggle(_SPECULAR_SETUP)] _MetallicSpecToggle("Worflow, Specular (if on), Metallic (if off)", float) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.95
        _Metallic("Metallic", Range(0, 1)) = 0
        _SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 0.5)
    }
    SubShader
    {
        Tags {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float _Tiling;
        float4 _BaseColor;
        float4 _TopColor;
        float _MaxHeight;
        float _NormalMapStrength;

        float _Smoothness;
        float _Metallic;
        float4 _SpecColor;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags {
                "LightMode"="UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
                float3 positionWS : TEXCOORD2;
                float4 normalWS : NORMAL;
                float4 tangentWS : TANGENT;
                float4 bitangentWS : TEXCOORD3;

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord : TEXCOORD4;
                #endif
            };

            TEXTURE2D(_PaintColorMap);    SAMPLER(sampler_PaintColorMap);
            TEXTURE2D(_PaintNormalMap);   SAMPLER(sampler_PaintNormalMap);
            TEXTURE2D(_PaintDataMap);     SAMPLER(sampler_PaintDataMap);

            void InitializeSurfaceData(Varyings IN, out SurfaceData surfaceData)
            {
                surfaceData = (SurfaceData)0;
                surfaceData.alpha = 1.0f;

                half3 scaledPosition = IN.positionWS * _Tiling.xxx;
                half3 albedoXY = SAMPLE_TEXTURE2D(_PaintColorMap, sampler_PaintColorMap, scaledPosition.xy).xyz;
                half3 albedoXZ = SAMPLE_TEXTURE2D(_PaintColorMap, sampler_PaintColorMap, scaledPosition.xz).xyz;
                half3 albedoYZ = SAMPLE_TEXTURE2D(_PaintColorMap, sampler_PaintColorMap, scaledPosition.yz).xyz;
                half3 color = lerp(_BaseColor.xyz, _TopColor.xyz, IN.positionWS.y / _MaxHeight);
                surfaceData.albedo = lerp(lerp(albedoXY, albedoXZ, abs(IN.normalWS.y)), albedoYZ, abs(IN.normalWS.x)) * color;

                half3 normalXY = SampleNormal(scaledPosition.xy, TEXTURE2D_ARGS(_PaintNormalMap, sampler_PaintNormalMap), abs(IN.normalWS.z) * _NormalMapStrength);
                half3 normalXZ = SampleNormal(scaledPosition.xz, TEXTURE2D_ARGS(_PaintNormalMap, sampler_PaintNormalMap), abs(IN.normalWS.y) * _NormalMapStrength);
                half3 normalYZ = SampleNormal(scaledPosition.yz, TEXTURE2D_ARGS(_PaintNormalMap, sampler_PaintNormalMap), abs(IN.normalWS.x) * _NormalMapStrength);
                surfaceData.normalTS = normalXY + normalXZ + normalYZ;
                surfaceData.emission = 0.0h;
                surfaceData.occlusion = 1.0h;
                #if _SPECULAR_SETUP
                    surfaceData.metallic = 1.0h;
                    surfaceData.specular = specColor.rgb;
                #else
                    surfaceData.metallic = _Metallic;
                    surfaceData.specular = half3(0.0h, 0.0h, 0.0h);
                #endif
                surfaceData.smoothness = _Smoothness;
            }

            void InitializeInputData(Varyings IN, half3 normalTS, out InputData inputData)
            {
                inputData = (InputData)0;

                inputData.positionWS = IN.positionWS;
                half3 viewDirWS = half3(IN.normalWS.w, IN.tangentWS.w, IN.bitangentWS.w);
                #ifdef _NORMALMAP
                    inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(IN.tangentWS.xyz, IN.bitangentWS.xyz, IN.normalWS.xyz));
                #else
                    inputData.normalWS = IN.normalWS.xyz;
                #endif

                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);

                viewDirWS = SafeNormalize(viewDirWS);
                inputData.viewDirectionWS = viewDirWS;

                //#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                //    inputData.shadowCoord = IN.shadowCoord;
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                #else
                    inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif

                inputData.bakedGI = SAMPLE_GI(IN.lightmapUV, IN.vertexSH, inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;

                half3 viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);

                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                    OUT.normalWS = half4(normalInputs.normalWS, viewDirWS.x);
                    OUT.tangentWS = half4(normalInputs.tangentWS, viewDirWS.y);
                    OUT.bitangentWS = half4(normalInputs.bitangentWS, viewDirWS.z);

                OUTPUT_LIGHTMAP_UV(IN.lightmapUV, unity_LightmapST, OUT.lightmapUV);
                OUTPUT_SH(OUT.normalWS.xyz, OUT.vertexSH);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    OUT.shadowCoord = GetShadowCoord(positionInputs);
                #endif
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                SurfaceData surfaceData;
                InitializeSurfaceData(IN, surfaceData);

                InputData inputData;
                InitializeInputData(IN, surfaceData.normalTS, inputData);
                

                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                return color;
            }
            ENDHLSL
        }

        Pass
        {
            name "ShadowCaster"
            Tags {"LightMode"="ShadowCaster"}

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW


            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
            };

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes IN)
            {
                Varyings OUT;
                OUT.uv = IN.uv;
                OUT.positionCS = GetShadowPositionHClip(IN);
                return OUT;
            }

            half4 ShadowPassFragment(Varyings IN) : SV_TARGET
            {
                return 0;
            }

            ENDHLSL
        }
        Pass
        {
            name "DepthOnly"
            Tags {"LightMode"="DepthOnly"}

            ColorMask 0
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // GPU Instancing
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            struct Attributes
            {
                float4 position     : POSITION;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            
                output.uv = input.uv;
                output.positionCS = TransformObjectToHClip(input.position.xyz);
                return output;
            }
            
            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }
            Pass{
                Name "DepthNormals"
                Tags { "LightMode" = "DepthNormals" }

                ZWrite On
                ZTest LEqual

                HLSLPROGRAM
                #pragma vertex DepthNormalsVertex
                #pragma fragment DepthNormalsFragment

                // Material Keywords
                #pragma shader_feature_local _NORMALMAP
                #pragma shader_feature_local_fragment _ALPHATEST_ON
                #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

                // GPU Instancing
                #pragma multi_compile_instancing

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
                

                struct Attributes
                {
                    float4 positionOS     : POSITION;
                    float4 tangentOS      : TANGENT;
                    float2 texcoord     : TEXCOORD0;
                    float3 normal       : NORMAL;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };
                
                struct Varyings
                {
                    float4 positionCS   : SV_POSITION;
                    float2 uv           : TEXCOORD1;
                    float3 normalWS                 : TEXCOORD2;
                
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                    UNITY_VERTEX_OUTPUT_STEREO
                };
                
                Varyings DepthNormalsVertex(Attributes input)
                {
                    Varyings output = (Varyings)0;
                    UNITY_SETUP_INSTANCE_ID(input);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                    output.uv = input.texcoord;
                    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                
                    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal, input.tangentOS);
                    output.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);
                
                    return output;
                }
                
                half4 DepthNormalsFragment(Varyings input) : SV_TARGET
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                    #if defined(_GBUFFER_NORMALS_OCT)
                    float3 normalWS = normalize(input.normalWS);
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);           // values between [-1, +1], must use fp32 on some platforms.
                    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0,  1]
                    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);      // values between [ 0,  1]
                    return half4(packedNormalWS, 0.0);
                    #else
                    float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                    return half4(normalWS, 0.0);
                    #endif
                }

                ENDHLSL
            }
    }
}
