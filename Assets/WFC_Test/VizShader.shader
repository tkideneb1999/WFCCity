Shader "Unlit/VizShader"
{
    Properties
    {
        _FrontColor("Front Color", Color) = (0.0, 0.9, 0.9)
        _BackColor("Back Color", Color) = (0.0, 0.9, 0.0)
    }
    SubShader
    {
        Tags { 
            "RenderType" = "Opaque" 
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _FrontColor;
            float4 _BackColor;
            CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct VertexInput
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct FragmentInput
            {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };

            FragmentInput vert (VertexInput v)
            {
                FragmentInput o;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = positionInputs.positionCS;
                o.normal = normalize(mul(unity_ObjectToWorld, float4(v.normal, 0)).xyz);
                return o;
            }

            half4 frag(FragmentInput i, half facing : VFACE) : SV_Target
            {
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                float3 normal = facing > 0 ? i.normal : -i.normal;
                float lambert = saturate(dot(lightDir, normal)) + 0.1f;
                float4 baseColor = facing > 0 ? _FrontColor : _BackColor;
                half4 col = half4(baseColor * lambert);
                return col;
            }
            ENDHLSL
        }
    }
}
