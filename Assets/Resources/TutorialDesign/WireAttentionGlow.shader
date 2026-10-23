Shader "VRGame/WireAttentionGlow"
{
    Properties
    {
        _GlowStrength ("White soft glow strength", Range(0, 1)) = 0
        _GlowExtents ("World bounds extents", Vector) = (0.1, 0.1, 0.1, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
        Pass
        {
            Blend One One
            ZWrite Off
            ZTest Always
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _GlowStrength;
                float4 _GlowExtents;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 radial : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float backDepth : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 center = TransformObjectToWorld(float3(0, 0, 0));
                float3 right = UNITY_MATRIX_I_V._m00_m10_m20;
                float3 up = UNITY_MATRIX_I_V._m01_m11_m21;
                float3 towardsEye = UNITY_MATRIX_I_V._m02_m12_m22;
                float3 extents = max(_GlowExtents.xyz, 0.005);
                float2 radius = float2(dot(abs(right), extents), dot(abs(up), extents)) * 1.65;
                // Place the soft source just ahead of its own bounds. Its depth is
                // independent of individual wire faces, eliminating self-occlusion shimmer.
                float3 position = center + towardsEye * (dot(abs(towardsEye), extents) + 0.01)
                    + right * (input.positionOS.x * radius.x) + up * (input.positionOS.y * radius.y);
                output.positionCS = TransformWorldToHClip(position);
                output.radial = input.positionOS.xy;
                output.backDepth = -TransformWorldToView(center).z + dot(abs(towardsEye), extents) + 0.02;
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.screenPos.xy / input.screenPos.w;
                float scene = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float surface = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                half visibility = smoothstep(-0.005, 0.025, scene - surface);
                float r2 = dot(input.radial, input.radial);
                half envelope = exp(-3.5 * r2) * (1 - smoothstep(0.72, 1.0, r2));
                half glow = envelope * _GlowStrength * 6 * visibility;
                // Scatter behind solid surfaces, without carving an artificial ring
                // around the bounds. Geometry only masks the composite, never emission.
                half interior = 1 - smoothstep(input.backDepth, input.backDepth + 0.02, scene);
                return half4(glow, glow, glow, interior * visibility);
            }
            ENDHLSL
        }
    }
}
