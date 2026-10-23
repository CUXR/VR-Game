Shader "VRGame/TutorialControllerDisplay"
{
    Properties
    {
        _UIFade ("UI fade", Range(0,1)) = 1
        _BaseColor ("Glass tint and density", Color) = (0.30, 0.425, 0.50, 0.50)
        _TintStrength ("Glass tint strength", Range(0, 1)) = 0.82
        _SurfaceBrightness ("Surface brightness", Range(0.5, 2)) = 1.35
        _EdgeMask ("Button boundary distance mask", 2D) = "white" {}
        _BoundaryLineStrength ("Boundary fine line opacity", Range(0, 1)) = 0.045
        _BoundaryGlowStrength ("Boundary soft glow strength", Range(0, 1)) = 0.56
        _BoundaryWidth ("Boundary width in mask texels", Range(0.5, 6)) = 3
        _BoundarySoftness ("Boundary soft spread", Range(1, 8)) = 4.5
        _ButtonSoftness ("Button soft spread", Range(1, 8)) = 2.25
        _RimStrength ("Silhouette white strength", Range(0, 1)) = 0.28
        _RimSoftStrength ("Silhouette soft wash", Range(0, 0.5)) = 0.30
        _RimPower ("Silhouette narrowness", Range(2, 12)) = 3.5
        _BlurPixels ("Frosted blur radius in screen pixels", Range(0, 80)) = 36
        _EmissionIntensity ("White emission brightness", Range(0, 8)) = 0
        _BreathOpacityMin ("Breathing minimum opacity", Range(0, 1)) = 1
        _BreathOpacityMax ("Breathing maximum opacity", Range(0, 1)) = 1
        _BreathPeriod ("Breathing period in seconds", Range(0.1, 10)) = 2.4
        [HideInInspector] _BreathTime ("Unscaled breathing time", Float) = 0
        _WhiteFrostedSurface ("White frosted surface", Range(0, 1)) = 0
        [HideInInspector] _HintAccentFade ("Hint swap accent fade", Range(0, 1)) = 1
        _UnderlayColor ("Grip frosted underlay tint", Color) = (0.30, 0.425, 0.50, 0.50)
        _UnderlayTintStrength ("Grip underlay tint strength", Range(0, 1)) = 0.82
        _UnderlayBrightness ("Grip underlay brightness", Range(0.5, 2)) = 1.35
        _ButtonBoundaryMultiplier ("Button boundary glow multiplier", Range(0, 1)) = 1
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
            #include "Assets/Resources/TutorialDesign/TutorialGlassBackdrop.hlsl"
            TEXTURE2D(_EdgeMask);
            SAMPLER(sampler_EdgeMask);
            float4 _EdgeMask_TexelSize;
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            CBUFFER_START(UnityPerMaterial)
                float _UIFade;
                half4 _BaseColor;
                float _TintStrength;
                float _SurfaceBrightness;
                float _BoundaryLineStrength;
                float _BoundaryGlowStrength;
                float _BoundaryWidth;
                float _BoundarySoftness;
                float _ButtonSoftness;
                float _RimStrength;
                float _RimSoftStrength;
                float _RimPower;
                float _BlurPixels;
                float _EmissionIntensity;
                float _BreathOpacityMin;
                float _BreathOpacityMax;
                float _BreathPeriod;
                float _BreathTime;
                float _WhiteFrostedSurface;
                float _HintAccentFade;
                half4 _UnderlayColor;
                float _UnderlayTintStrength;
                float _UnderlayBrightness;
                float _ButtonBoundaryMultiplier;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 normalWS = normalize(input.normalWS);
                float3 normalVS = TransformWorldToViewDir(normalWS, true);
                half facingLight = saturate(dot(normalVS,
                    normalize(float3(-0.35, 0.65, 1.0))));
                // Preserve the raised grip's curved surface instead of clipping
                // every face to flat white, which looks like a pasted glow card.
                half lighting = _EmissionIntensity > 0.01
                    ? 0.42h + 0.58h * facingLight
                    : 0.78h + 0.22h * facingLight;
                half3 color = _BaseColor.rgb * lighting * _SurfaceBrightness;
                half3 whiteCoat = color;
                half opacity = _BaseColor.a;
                float breath = 0.5 - 0.5 * cos(_BreathTime * 6.2831853 / max(0.1, _BreathPeriod));
                half breathingOpacity = lerp(_BreathOpacityMin, _BreathOpacityMax, breath);
                if (_BlurPixels > 0.01)
                {
                    float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                    half3 blurred = TutorialGlassBackdrop(uv, _BlurPixels * .5);
                    // Density colors the glass; backdrop replacement covers
                    // the silhouette. Alpha-blending a blurred layer over the
                    // unblurred scene would bring its hard contours back.
                    // Match the panel's plus-lighter glass tint, with gentle
                    // shading to retain the controller's three-dimensional form.
                    half3 tinted = saturate(blurred + color * _TintStrength);
                    // White buttons use a shaded white surface instead of additive
                    // tint saturation, keeping the whole frosted face readable.
                    // A permanent body-matched frosted base sits below the
                    // breathing white coat. Only the white coverage pulses.
                    half3 underlayTint = _UnderlayColor.rgb * lighting * _UnderlayBrightness;
                    half3 underlay = lerp(blurred, saturate(blurred + underlayTint * _UnderlayTintStrength), _UnderlayColor.a);
                    color = lerp(lerp(blurred, tinted, _BaseColor.a), underlay, _WhiteFrostedSurface);
                    opacity = 1;
                }
                // A fine edge over a wider, low-intensity surface wash. Both
                // follow the actual eye and stay attached to the curved shell.
                float3 viewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float grazing = saturate(1 - abs(dot(normalWS, viewDirection)));
                float fineRim = pow(grazing, _RimPower);
                // Control the fine silhouette and its wide wash independently:
                // the line can be fainter while the surrounding glass is brighter.
                half rim = fineRim * _RimStrength
                    + pow(grazing, max(1.0, _RimPower / 3.0)) * _RimSoftStrength * (1 - fineRim);
                half boundary = 0;
                if (_BoundaryLineStrength + _BoundaryGlowStrength > 0.001)
                {
                    // R is distance / 24 texels; G gives buttons priority over
                    // body creases. This follows UVs, including skinned presses.
                    // Distance-aware AA replaces mip averaging, which would
                    // erase the minimum of a thin unsigned distance field.
                    half2 mask = SAMPLE_TEXTURE2D_LOD(_EdgeMask, sampler_EdgeMask, input.uv, 0).rg;
                    float distanceToBoundary = mask.r * 24;
                    float footprint = max(length(ddx(input.uv) * _EdgeMask_TexelSize.zw),
                        length(ddy(input.uv) * _EdgeMask_TexelSize.zw));
                    float width = max(_BoundaryWidth, footprint * 0.85);
                    half core = 1 - smoothstep(0, width, distanceToBoundary);
                    // Stop before the distance mask's 24-texel range ends, so
                    // distant UV islands cannot become a uniform white patch.
                    // The authored mask uses 0.85 for buttons and 0.55 for the
                    // shell. Tighten only button halos, retaining the broad body wash.
                    half button = smoothstep(0.65h, 0.80h, mask.g);
                    float spread = lerp(_BoundarySoftness, _ButtonSoftness, button);
                    float softWidth = min(23.0, width * spread);
                    half soft = 1 - smoothstep(0, softWidth, distanceToBoundary);
                    boundary = (core * _BoundaryLineStrength + soft * _BoundaryGlowStrength) * mask.g;
                    boundary *= lerp(1.0, _ButtonBoundaryMultiplier, button);
                }
                // The grip underlay shares the body's soft white accents.
                // Composite the breathing coat afterwards, so it reveals these
                // accents as it fades without changing the base layer's strength.
                color += _BaseColor.rgb * _EmissionIntensity * (0.25h + 0.75h * facingLight);
                color = lerp(color, whiteCoat, _WhiteFrostedSurface * _BaseColor.a * breathingOpacity * _HintAccentFade);
                half accent = saturate(max(rim, boundary)) * lerp(1.0h, breathingOpacity * _HintAccentFade, _WhiteFrostedSurface);
                color = lerp(color, half3(1,1,1), accent);
                // Material-local breathing preserves the model's independent reveal fade.
                // Equal endpoints leave the controller body unchanged.
                return half4(color, opacity * lerp(breathingOpacity, 1.0h, _WhiteFrostedSurface) * _UIFade);
            }
            ENDHLSL
        }
    }
}
