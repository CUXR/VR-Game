Shader "VRGame/TutorialFigmaOverlay"
{
    Properties
    {
        _MainTex ("Figma Export", 2D) = "white" {}
        _Opacity ("Opacity", Range(0, 1)) = 1
        _BlurPixels ("Blur Pixels", Range(0, 8)) = 0
        _Solid ("Solid Black", Range(0, 1)) = 0
        _UseTextureAlpha ("Use Source Alpha", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay+1" "RenderType"="Transparent" }
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            CBUFFER_START(UnityPerMaterial)
                float _Opacity;
                float _BlurPixels;
                float _Solid;
                float _UseTextureAlpha;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float Ink(float2 uv)
            {
                half3 rgb = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
                // Figma's PNG export is black artwork composited over #1E1E1E.
                // Recover the source alpha while keeping its exact glyph edges.
                float encodedRed;
                #if defined(UNITY_COLORSPACE_GAMMA)
                    encodedRed = rgb.r;
                #else
                    encodedRed = rgb.r <= 0.0031308
                        ? rgb.r * 12.92
                        : 1.055 * pow(abs(rgb.r), 1.0 / 2.4) - 0.055;
                #endif
                return saturate((30.0 / 255.0 - encodedRed - 2.0 / 255.0)
                    / (28.0 / 255.0));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float alpha = Ink(input.uv);
                if (_BlurPixels > 0.01)
                {
                    float2 d = _MainTex_TexelSize.xy * _BlurPixels;
                    alpha = (alpha * 4.0
                        + Ink(input.uv + float2(d.x, 0)) * 2.0
                        + Ink(input.uv - float2(d.x, 0)) * 2.0
                        + Ink(input.uv + float2(0, d.y)) * 2.0
                        + Ink(input.uv - float2(0, d.y)) * 2.0
                        + Ink(input.uv + d) + Ink(input.uv - d)
                        + Ink(input.uv + float2(d.x, -d.y))
                        + Ink(input.uv + float2(-d.x, d.y))) / 16.0;
                }
                alpha = lerp(alpha,
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a,
                    _UseTextureAlpha);
                alpha = lerp(alpha, 1.0, _Solid);
                return half4(0, 0, 0, alpha * _Opacity);
            }
            ENDHLSL
        }
    }
}
