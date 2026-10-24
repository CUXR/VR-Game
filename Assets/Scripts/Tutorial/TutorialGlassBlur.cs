using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using RenderGraphUtils = UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils;
using UnityEngine.Rendering.Universal;

/// <summary>
/// A shared, per-eye backdrop for tutorial glass. Acquiring a lease schedules a
/// URP pass for the existing camera; no renderer assets or XR rig are modified.
/// </summary>
internal static class TutorialGlassBlur
{
    private static readonly Dictionary<Camera, CameraBlur> Cameras = new Dictionary<Camera, CameraBlur>();
    private static readonly int StateId = Shader.PropertyToID("_TutorialGlassBlurState");

    internal static IDisposable Acquire(Camera camera)
    {
        if (!Cameras.TryGetValue(camera, out CameraBlur blur))
        {
            blur = new CameraBlur();
            Cameras.Add(camera, blur);
            if (Cameras.Count == 1)
                RenderPipelineManager.beginCameraRendering += BeginCamera;
        }
        blur.users++;
        return new Lease(camera);
    }

    private static void BeginCamera(ScriptableRenderContext context, Camera camera)
    {
        // Never reuse another camera's texture (including Scene view and XR eyes).
        Shader.SetGlobalVector(StateId, Vector4.zero);
        if (!Cameras.TryGetValue(camera, out CameraBlur blur) || blur.pass == null)
            return;
        ScriptableRenderer renderer = camera.TryGetComponent(out UniversalAdditionalCameraData data)
            ? data.scriptableRenderer : UniversalRenderPipeline.asset?.GetRenderer(-1);
        // URP consumes the public pass queue later in this camera's render, and
        // clears it when that camera finishes. ConfigureInput requests the
        // resolved opaque texture only while a tutorial lease exists.
        renderer?.EnqueuePass(blur.pass);
    }

    private sealed class Lease : IDisposable
    {
        private Camera camera;
        internal Lease(Camera camera) { this.camera = camera; }
        public void Dispose()
        {
            if (ReferenceEquals(camera, null)) return;
            if (Cameras.TryGetValue(camera, out CameraBlur blur) && --blur.users == 0)
            {
                blur.Dispose();
                Cameras.Remove(camera);
                if (Cameras.Count == 0)
                {
                    RenderPipelineManager.beginCameraRendering -= BeginCamera;
                    Shader.SetGlobalVector(StateId, Vector4.zero);
                }
            }
            camera = null;
        }
    }

    private sealed class CameraBlur : IDisposable
    {
        internal int users;
        internal readonly GaussianPass pass;
        private readonly Material material;
        internal CameraBlur()
        {
            Shader shader = Resources.Load<Shader>("TutorialDesign/TutorialGaussianBlur");
            if (shader == null)
            {
                Debug.LogError("Tutorial glass: Gaussian blur shader is missing.");
                return;
            }
            material = CoreUtils.CreateEngineMaterial(shader);
            pass = new GaussianPass(material);
        }
        public void Dispose() { CoreUtils.Destroy(material); }
    }

    private sealed class GaussianPass : ScriptableRenderPass
    {
        private static readonly int MediumId = Shader.PropertyToID("_TutorialGlassBlurMedium");
        private static readonly int BroadId = Shader.PropertyToID("_TutorialGlassBlurBroad");
        private static readonly int StepId = Shader.PropertyToID("_BlurStep");
        private readonly Material material;
        // Separate property blocks are essential: RenderGraph executes later,
        // so changing one shared block would overwrite preceding pass settings.
        private readonly MaterialPropertyBlock[] blocks = new MaterialPropertyBlock[12];

        internal GaussianPass(Material material)
        {
            this.material = material;
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            ConfigureInput(ScriptableRenderPassInput.Color);
            for (int i = 0; i < blocks.Length; i++) blocks[i] = new MaterialPropertyBlock();
        }

        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            UniversalResourceData resources = frameData.Get<UniversalResourceData>();
            TextureHandle source = resources.cameraOpaqueTexture;
            if (!source.IsValid()) return;
            UniversalCameraData camera = frameData.Get<UniversalCameraData>();
            int fullWidth = camera.cameraTargetDescriptor.width;
            int fullHeight = camera.cameraTargetDescriptor.height;
            TextureHandle medium = default;
            float variance = 0f;
            float mediumSigma = 0f;

            // Four continuous Gaussian levels: 1/4, 1/8, 1/16, 1/32.
            // Each 9-tap separable Gaussian uses five bilinear reads. Large
            // softness comes from the pyramid, never widely spaced ring taps.
            for (int level = 0; level < 4; level++)
            {
                int divisor = 4 << level;
                int width = Mathf.Max(1, (fullWidth + divisor - 1) / divisor);
                int height = Mathf.Max(1, (fullHeight + divisor - 1) / divisor);
                TextureDesc desc = graph.GetTextureDesc(source);
                desc.width = width;
                desc.height = height;
                desc.msaaSamples = MSAASamples.None;
                desc.clearBuffer = false;
                desc.filterMode = FilterMode.Bilinear;
                desc.wrapMode = TextureWrapMode.Clamp;
                desc.useMipMap = false;
                desc.autoGenerateMips = false;
                desc.name = "Tutorial glass downsample " + divisor;
                TextureHandle downsample = graph.CreateTexture(desc);
                desc.name = "Tutorial glass horizontal " + divisor;
                TextureHandle horizontal = graph.CreateTexture(desc);
                desc.name = "Tutorial glass Gaussian " + divisor;
                TextureHandle vertical = graph.CreateTexture(desc);

                int index = level * 3;
                Blit(graph, source, downsample, blocks[index], new Vector4(.25f / width, .25f / height, 0, 0), 0, desc.name + " downsample");
                Blit(graph, downsample, horizontal, blocks[index + 1], new Vector4(1f / width, 0, 0, 0), 1, desc.name + " horizontal");
                using (var builder = Blit(graph, horizontal, vertical, blocks[index + 2], new Vector4(0, 1f / height, 0, 0), 1, desc.name + " vertical", true))
                {
                    if (level == 1) builder.SetGlobalTextureAfterPass(vertical, MediumId);
                    if (level == 3) builder.SetGlobalTextureAfterPass(vertical, BroadId);
                }
                variance += 2.854054f * divisor * divisor; // variance of the normalized 9-tap kernel
                if (level == 1)
                {
                    medium = vertical;
                    mediumSigma = Mathf.Sqrt(variance);
                }
                source = vertical;
            }

            using (var builder = graph.AddRasterRenderPass<PublishData>("Tutorial glass ready", out var publish))
            {
                builder.UseTexture(medium, AccessFlags.Read);
                builder.UseTexture(source, AccessFlags.Read);
                builder.AllowGlobalStateModification(true);
                builder.AllowPassCulling(false);
                publish.state = new Vector4(1, mediumSigma, Mathf.Sqrt(variance), 0);
                builder.SetRenderFunc(static (PublishData data, RasterGraphContext context) =>
                    context.cmd.SetGlobalVector(StateId, data.state));
            }
        }

        private IBaseRenderGraphBuilder Blit(RenderGraph graph, TextureHandle source, TextureHandle destination,
            MaterialPropertyBlock block, Vector4 step, int shaderPass, string name, bool returnBuilder = false)
        {
            block.SetVector(StepId, step);
            var parameters = new RenderGraphUtils.BlitMaterialParameters(source, destination,
                Vector2.one, Vector2.zero, material, shaderPass, block,
                RenderGraphUtils.FullScreenGeometryType.ProceduralTriangle);
            return graph.AddBlitPass(parameters, name, returnBuilder);
        }

        private sealed class PublishData { internal Vector4 state; }
    }
}
