Shader "VRGame/TutorialControllerDisplay"
{
    Properties
    {
        _UIFade ("UI fade", Range(0,1)) = 1
        _BaseColor ("Color and opacity", Color) = (0.62, 0.68, 0.72, 0.8)
        _BlurPixels ("Frosted blur radius in screen pixels", Range(0, 80)) = 36
        _EmissionIntensity ("White emission brightness", Range(0, 8)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" "RenderType"="Transparent" }
        Cull Back
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalVS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            CBUFFER_START(UnityPerMaterial)
                float _UIFade;
                half4 _BaseColor;
                float _BlurPixels;
                float _EmissionIntensity;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalVS = TransformWorldToViewDir(TransformObjectToWorldNormal(input.normalOS), true);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half lighting = 0.38h + 0.62h * saturate(dot(normalize(input.normalVS),
                    normalize(float3(-0.35, 0.65, 1.0))));
                half3 color = _BaseColor.rgb * lighting;
                if (_BlurPixels > 0.01)
                {
                    float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                    float2 radius = _BlurPixels / _ScaledScreenParams.xy;
                    half3 blurred = SampleSceneColor(uv) * 4.0h;
                    [unroll] for (int ring = 1; ring <= 2; ring++)
                    {
                        float2 offsetUV = radius * (ring * 0.5);
                        half weight = ring == 1 ? 1.0h : 0.5h;
                        blurred += SampleSceneColor(uv + float2(offsetUV.x, 0)) * 2.0h * weight;
                        blurred += SampleSceneColor(uv - float2(offsetUV.x, 0)) * 2.0h * weight;
                        blurred += SampleSceneColor(uv + float2(0, offsetUV.y)) * 2.0h * weight;
                        blurred += SampleSceneColor(uv - float2(0, offsetUV.y)) * 2.0h * weight;
                        blurred += SampleSceneColor(uv + offsetUV) * weight;
                        blurred += SampleSceneColor(uv - offsetUV) * weight;
                        blurred += SampleSceneColor(uv + offsetUV * float2(1,-1)) * weight;
                        blurred += SampleSceneColor(uv + offsetUV * float2(-1,1)) * weight;
                    }
                    color = lerp(color, blurred / 22.0h, 0.75h);
                }
                color += _BaseColor.rgb * _EmissionIntensity;
                return half4(color, _BaseColor.a * _UIFade);
            }
            ENDHLSL
        }
    }
}
