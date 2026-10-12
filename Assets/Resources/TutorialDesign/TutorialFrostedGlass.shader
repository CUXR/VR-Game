Shader "VRGame/TutorialFrostedGlass"
{
    Properties
    {
        _UIFade ("UI fade", Range(0,1)) = 1
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
            #include "TutorialGlassBackdrop.hlsl"

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

            CBUFFER_START(UnityPerMaterial)
                float _UIFade;
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
                // Match softness to the current projected Figma geometry.
                // Estimate its size in screen pixels without dividing by UV
                // derivatives of the screen (which are unstable when tilted).
                float pixelsPerDesignX = rcp(max(length(float2(ddx(input.uv.x), ddy(input.uv.x))) * _PanelWidth, 1e-5));
                float pixelsPerDesignY = rcp(max(length(float2(ddx(input.uv.y), ddy(input.uv.y))) * _PanelHeight, 1e-5));
                float sigmaPixels = _BlurRadius * min(pixelsPerDesignX, pixelsPerDesignY) / 3.0;
                half3 scene = TutorialGlassBackdrop(uv, sigmaPixels);

                // Figma's two inset white shadows: +/-18.952, +/-26.398,
                // each with 67.688 px softness and 20% white opacity.
                float insetA = RoundedBoxDistance(shapePoint - float2(-18.952, -26.398), halfSize, radius);
                float insetB = RoundedBoxDistance(shapePoint - float2(18.952, 26.398), halfSize, radius);
                float glow = 0.2 * (exp(-max(-insetA, 0) / 67.688)
                    + exp(-max(-insetB, 0) / 67.688));
                // Composite the 90% tinted surface over the BLURRED backdrop.
                // Blending that result at 90% over the original scene again
                // would restore crisp edges and defeat the frosted material.
                half3 tinted = saturate(scene + input.color.rgb * _TintStrength + glow);
                half3 color = lerp(scene, tinted, 0.9h * input.color.a);
                return half4(color, shapeAlpha * _UIFade);
            }
            ENDHLSL
        }
    }
}
