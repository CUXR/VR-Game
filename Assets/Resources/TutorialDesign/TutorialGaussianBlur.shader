Shader "Hidden/VRGame/TutorialGaussianBlur"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        float4 _BlurStep;
        half3 ReadBackdrop(float2 uv)
        {
            return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0).rgb;
        }
        half4 Downsample(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord;
            float2 stepUV = _BlurStep.xy;
            // Four bilinear footprints cover the downsampled pixel rather
            // than choosing a single source pixel (which would alias).
            half3 color = ReadBackdrop(uv + stepUV) + ReadBackdrop(uv - stepUV)
                + ReadBackdrop(uv + stepUV * float2(1,-1))
                + ReadBackdrop(uv + stepUV * float2(-1,1));
            return half4(color * .25h, 1);
        }
        half4 Gaussian(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord;
            float2 stepUV = _BlurStep.xy;
            // Normalized, contiguous 9-tap Gaussian. Linear filtering combines
            // adjacent tap pairs exactly, reducing nine reads to five.
            half3 color = ReadBackdrop(uv) * .2270270270h;
            color += (ReadBackdrop(uv + stepUV * 1.3846153846)
                + ReadBackdrop(uv - stepUV * 1.3846153846)) * .3162162162h;
            color += (ReadBackdrop(uv + stepUV * 3.2307692308)
                + ReadBackdrop(uv - stepUV * 3.2307692308)) * .0702702703h;
            return half4(color, 1);
        }
        ENDHLSL
        Pass
        {
            Name "Filtered downsample"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Downsample
            #pragma multi_compile_instancing
            ENDHLSL
        }
        Pass
        {
            Name "Separable Gaussian"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Gaussian
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
}
