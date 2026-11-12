Shader "Tutorial/Flowing Climb Stroke"
{
    Properties
    {
        _BaseMap ("Figma stroke (RGBA)", 2D) = "white" {}
        [HDR] _Tint ("Tint", Color) = (1,1,1,1)
        _StrokeProgress ("Lifecycle", Range(0,1)) = 1
        _HDRIntensity ("Emission", Float) = 1
        _Impact ("Head impact", Float) = 0
        _InternalFlowSpeed ("Downward flow speed", Float) = 0.32
        _FlowStrength ("Flow strength", Range(0,1)) = 0.1
        _NearDistance ("Near distance (m)", Float) = 0.7
        _FarDistance ("Far distance (m)", Float) = 5
        _FarBrightness ("Far brightness", Range(0,1)) = 0.5
        [HideInInspector] _ViewerPosition ("Viewer", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "FlowingStroke"
            Tags { "LightMode"="UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint, _ViewerPosition;
                float _StrokeProgress, _HDRIntensity, _Impact;
                float _InternalFlowSpeed, _FlowStrength;
                float _NearDistance, _FarDistance, _FarBrightness;
            CBUFFER_END
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
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                return output;
            }
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f*f*(3.0-2.0*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),
                    lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
            }
            float Ease(float t)
            {
                t = saturate(t);
                return t*t*t*(t*(t*6.0-15.0)+10.0);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float p = saturate(_StrokeProgress);
                // Exact black endpoints; no looping remainder that can flash on retirement.
                if (p <= 0.0 || p >= 1.0) return 0;
                half4 source = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float down = 1.0 - input.uv.y;
                // Preserve every original layer. Lifecycle opacity is spatially uniform:
                // no UV-dependent clipping, reveal line, or drain line.
                float envelope = Ease(p / 0.18) * (1.0-Ease((p-0.3)/0.7));
                // uv.y + time translates constant-phase features DOWN the surface.
                float flowY = input.uv.y + _Time.y * _InternalFlowSpeed;
                float noise = Noise(float2(input.uv.x*3.0,flowY*3.5));
                float wave = sin(flowY*9.0 + input.uv.x*5.0)*0.5+0.5;
                float flow = 1.0 + _FlowStrength*(noise*0.8+wave*0.2-0.5)*2.0;
                float head = exp2(-pow((down-0.19)/0.16,2.0)*2.0);
                float3 viewer = _ViewerPosition.w > 0.5 ? _ViewerPosition.xyz : GetCameraPositionWS();
                float attenuation = lerp(1.0,_FarBrightness,smoothstep(_NearDistance,
                    max(_NearDistance+0.01,_FarDistance),distance(viewer,input.positionWS)));
                float alpha = saturate(source.a * _Tint.a * envelope);
                float energy = max(0.0,_HDRIntensity) * attenuation * (flow + head*_Impact);
                return half4(source.rgb * _Tint.rgb * energy * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
