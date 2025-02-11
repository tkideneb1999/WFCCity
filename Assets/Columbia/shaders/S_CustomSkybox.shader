Shader "Custom/CustomSkybox"
{
    Properties
    {
        _Color1 ("Color 1", Color) = (1.0, 1.0, 1.0, 1.0)
        _Color1Pos ("Color 1 Position", Range(0,1)) = 1.0
        _Color2 ("Color 2", Color) = (0.0, 1.0, 0.0, 1.0)
        _Color2Pos ("Color 2 Position", Range(0,1)) = 0.5
        _Color3 ("Color 3", Color) = (1.0, 0.0, 0.0, 1.0)
        _Color3Pos ("Color 3 Position", Range(0,1)) = 1.0
        _SunSize("Sun Size", float) = 0.5
        _SunFalloff("Sun Falloff", float) = 0.1
        _SunBrightness ("Sun Brightness", float) = 1.0
        _UpVector("UpVector", Vector) = (0.0, 1.0, 0.0, 1.0)
        
    }
    SubShader
    {
        Tags { "RenderType"="Background" "Queue" = "Background"}
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            //#include "UnityCG.cginc"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            half Remap(half value, half minOld, half maxOld, half minNew, half maxNew)
            {
                return minNew + (value - minOld) * (maxNew - minNew) / (maxOld - minOld);
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color1;
                half _Color1Pos;
                half4 _Color2;
                half _Color2Pos;
                half4 _Color3;
                half _Color3Pos;
                half3 _UpVector;
                half _SunBrightness;
                half _SunFalloff;
                half _SunSize;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half gradient = dot(normalize(i.uv), _UpVector) * 0.5 + 0.5;
                half color12Lerp = saturate(Remap(gradient, _Color1Pos, _Color2Pos, 0, 1));
                half color23Lerp = saturate(Remap(gradient, _Color2Pos, _Color3Pos, 0, 1));
                half4 col = lerp(_Color1, lerp(_Color2, _Color3, color23Lerp), color12Lerp);

                Light mainLight = GetMainLight();
                half sunDF = dot(normalize(mainLight.direction), normalize(i.uv));
                half sun = Remap(saturate(sunDF + _SunSize), 0, 1 + _SunSize, 0, 1);
                half sunFalloff = saturate(1- pow(cos(PI * sun/2.0), _SunFalloff));

                col = lerp(col, half4(mainLight.color * _SunBrightness,1) , sunFalloff);
                return col;
            }
            ENDHLSL
        }
    }
}
