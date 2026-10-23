Shader "Hidden/VRGame/TutorialGlowComposite"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend One One
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex GlowVertex
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            TEXTURE2D_X(_HintGlowTight);
            TEXTURE2D_X(_HintGlowWide);
            TEXTURE2D_X(_HintGlowMask);
            Varyings GlowVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 tight = SAMPLE_TEXTURE2D_X(_HintGlowTight, sampler_LinearClamp, input.texcoord).rgb;
                half3 wide = SAMPLE_TEXTURE2D_X(_HintGlowWide, sampler_LinearClamp, input.texcoord).rgb;
                half interior = saturate(SAMPLE_TEXTURE2D_X(_HintGlowMask, sampler_LinearClamp, input.texcoord).a);
                // Leave the source's interior unchanged; scatter light around it.
                return half4((tight * .48h + wide * .46h) * half3(.80h, .91h, 1.0h) * (1 - interior), 0);
            }
            ENDHLSL
        }
    }
}
