#ifndef TUTORIAL_GLASS_BACKDROP_INCLUDED
#define TUTORIAL_GLASS_BACKDROP_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
TEXTURE2D_X(_TutorialGlassBlurMedium);
TEXTURE2D_X(_TutorialGlassBlurBroad);
// x = ready for this camera, y/z = standard deviation in full-resolution pixels.
float4 _TutorialGlassBlurState;

half3 TutorialGlassBackdrop(float2 uv, float sigmaPixels)
{
    if (_TutorialGlassBlurState.x < .5)
        return SampleSceneColor(uv); // No shared blur: use URP's ordinary opaque snapshot.

    float mediumSigma = _TutorialGlassBlurState.y;
    float broadSigma = _TutorialGlassBlurState.z;
    half3 medium = SAMPLE_TEXTURE2D_X(_TutorialGlassBlurMedium, sampler_LinearClamp, uv).rgb;
    if (sigmaPixels < mediumSigma)
        return lerp(SampleSceneColor(uv), medium, saturate(sigmaPixels / mediumSigma));
    half3 broad = SAMPLE_TEXTURE2D_X(_TutorialGlassBlurBroad, sampler_LinearClamp, uv).rgb;
    float amount = saturate((sigmaPixels * sigmaPixels - mediumSigma * mediumSigma)
        / max(broadSigma * broadSigma - mediumSigma * mediumSigma, 1));
    return lerp(medium, broad, amount);
}
#endif
