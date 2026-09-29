Shader "VRGame/TutorialFrostedGlass"
{
    Properties
    {
        _MainTex ("Figma Glass Tint", 2D) = "white" {}
        _Color ("Figma Glass Tint", Color) = (0.243137, 0.337255, 0.392157, 1)
        _BlurRadius ("Figma Backdrop Blur", Float) = 338.438
        _TintStrength ("Glass Tint Strength", Range(0, 1)) = 0.82
        _PanelWidth ("Panel Width", Float) = 2100
        _PanelHeight ("Panel Height", Float) = 900
        _CornerRadius ("Corner Radius", Float) = 140
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            float4 _CameraOpaqueTexture_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _BlurRadius;
                float _TintStrength;
                float _PanelWidth;
                float _PanelHeight;
                float _CornerRadius;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            float RoundedBoxDistance(float2 samplePosition, float2 halfSize, float radius)
            {
                float2 q = abs(samplePosition) - (halfSize - radius);
                return length(max(q, 0)) + min(max(q.x, q.y), 0) - radius;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 halfSize = float2(_PanelWidth, _PanelHeight) * 0.5;
                float radius = min(_CornerRadius, min(halfSize.x, halfSize.y));
                float2 shapePoint = (input.uv - 0.5) * float2(_PanelWidth, _PanelHeight);
                float signedDistance = RoundedBoxDistance(shapePoint, halfSize, radius);
                float edgeWidth = max(fwidth(signedDistance), 0.5);
                float shapeAlpha = 1 - smoothstep(-edgeWidth, edgeWidth, signedDistance);
                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                // Convert the 338.438 design-pixel blur to screen UV at the
                // current headset projection, including the curved edges.
                float2 designPixelUV = float2(
                    abs(ddx(uv.x)) / max(abs(ddx(input.uv.x)) * _PanelWidth, 1e-5),
                    abs(ddy(uv.y)) / max(abs(ddy(input.uv.y)) * _PanelHeight, 1e-5));
                float2 stepUV = min(_BlurRadius * designPixelUV, 0.16);
                half3 scene = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv).rgb;
                [unroll] for (int ring = 1; ring <= 3; ring++)
                {
                    float2 offsetUV = stepUV * (ring / 3.0);
                    scene += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(offsetUV.x, 0)).rgb;
                    scene += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv - float2(offsetUV.x, 0)).rgb;
                    scene += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(0, offsetUV.y)).rgb;
                    scene += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv - float2(0, offsetUV.y)).rgb;
                    scene += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + offsetUV * 0.707107).rgb;
                    scene += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv - offsetUV * 0.707107).rgb;
                    scene += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + offsetUV * float2(0.707107, -0.707107)).rgb;
                    scene += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + offsetUV * float2(-0.707107, 0.707107)).rgb;
                }
                scene *= 1.0 / 25.0;

                // Figma's two inset white shadows: +/-18.952, +/-26.398,
                // each with 67.688 px softness and 20% white opacity.
                float insetA = RoundedBoxDistance(shapePoint - float2(-18.952, -26.398), halfSize, radius);
                float insetB = RoundedBoxDistance(shapePoint - float2(18.952, 26.398), halfSize, radius);
                float glow = 0.2 * (exp(-max(-insetA, 0) / 67.688)
                    + exp(-max(-insetB, 0) / 67.688));
                half alpha = 0.9 * shapeAlpha * input.color.a;
                // Plus-lighter mixes the tinted glass additively with its
                // blurred backdrop before the 90% surface opacity is applied.
                half3 color = saturate(scene + input.color.rgb * _TintStrength + glow);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
