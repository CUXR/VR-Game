using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// Selective bloom: only registered hint emitters are blurred. The scene and
// controller glass never become bloom sources. Texture descriptors preserve XR eyes.
internal static class TutorialGlowBloom
{
    private sealed class Emitter
    {
        public MeshRenderer renderer;
        public Mesh mesh;
        public Material material;
    }
    private static readonly List<Emitter> emitters = new List<Emitter>();
    private static GlowPass pass;
    public static void Register(MeshRenderer renderer, Mesh mesh, Material material)
    {
        emitters.Add(new Emitter { renderer = renderer, mesh = mesh, material = material });
        if (pass == null)
        {
            pass = new GlowPass();
            RenderPipelineManager.beginCameraRendering += BeginCamera;
        }
    }
    public static void Unregister(MeshRenderer renderer)
    {
        emitters.RemoveAll(item => item.renderer == renderer);
        if (emitters.Count != 0 || pass == null) return;
        RenderPipelineManager.beginCameraRendering -= BeginCamera;
        pass.Dispose(); pass = null;
    }
    private static void BeginCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera != Camera.main || !emitters.Exists(item => item.renderer != null
            && item.renderer.enabled && item.renderer.gameObject.activeInHierarchy)) return;
        var renderer = camera.TryGetComponent<UniversalAdditionalCameraData>(out var data)
            ? data.scriptableRenderer : UniversalRenderPipeline.asset?.GetRenderer(-1);
        renderer?.EnqueuePass(pass);
    }
    private sealed class GlowPass : ScriptableRenderPass
    {
        private readonly Material gaussian;
        private readonly Material composite;
        private readonly MaterialPropertyBlock[] blocks = new MaterialPropertyBlock[9];
        private static readonly int Step = Shader.PropertyToID("_BlurStep");
        private static readonly int Tight = Shader.PropertyToID("_HintGlowTight");
        private static readonly int Wide = Shader.PropertyToID("_HintGlowWide");
        private static readonly int Mask = Shader.PropertyToID("_HintGlowMask");
        public GlowPass()
        {
            gaussian = CoreUtils.CreateEngineMaterial(Resources.Load<Shader>("TutorialDesign/TutorialGaussianBlur"));
            composite = CoreUtils.CreateEngineMaterial(Resources.Load<Shader>("TutorialDesign/TutorialGlowComposite"));
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            ConfigureInput(ScriptableRenderPassInput.Depth);
            for (int i = 0; i < blocks.Length; i++) blocks[i] = new MaterialPropertyBlock();
        }
        public void Dispose() { CoreUtils.Destroy(gaussian); CoreUtils.Destroy(composite); }
        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frame)
        {
            var resources = frame.Get<UniversalResourceData>();
            var camera = frame.Get<UniversalCameraData>();
            var desc = graph.GetTextureDesc(resources.activeColorTexture);
            int width = camera.cameraTargetDescriptor.width;
            int height = camera.cameraTargetDescriptor.height;
            desc.width = Mathf.Max(1, width / 2); desc.height = Mathf.Max(1, height / 2);
            desc.colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat;
            desc.msaaSamples = MSAASamples.None; desc.clearBuffer = true; desc.clearColor = Color.clear;
            desc.filterMode = FilterMode.Bilinear; desc.wrapMode = TextureWrapMode.Clamp;
            desc.name = "Hint glow visible emitters";
            var mask = graph.CreateTexture(desc);
            using (var builder = graph.AddRasterRenderPass<MaskData>("Hint glow depth-tested source", out var draw))
            {
                draw.items = new List<Emitter>();
                foreach (var item in emitters)
                    if (item.renderer != null && item.renderer.enabled && item.renderer.gameObject.activeInHierarchy
                        && (camera.camera.cullingMask & (1 << item.renderer.gameObject.layer)) != 0) draw.items.Add(item);
                builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                builder.SetGlobalTextureAfterPass(mask, Mask);
                if (resources.cameraDepthTexture.IsValid()) builder.UseTexture(resources.cameraDepthTexture);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (MaskData data, RasterGraphContext context) =>
                {
                    foreach (var item in data.items)
                        for (int submesh = 0; submesh < item.mesh.subMeshCount; submesh++)
                            context.cmd.DrawMesh(item.mesh, item.renderer.localToWorldMatrix, item.material, submesh, 0);
                });
            }
            TextureHandle source = mask, tight = default;
            for (int level = 0; level < 3; level++)
            {
                int divisor = 2 << level;
                desc.width = Mathf.Max(1, width / divisor); desc.height = Mathf.Max(1, height / divisor);
                desc.clearBuffer = false;
                desc.name = "Hint bloom downsample " + level; var down = graph.CreateTexture(desc);
                desc.name = "Hint bloom horizontal " + level; var horizontal = graph.CreateTexture(desc);
                desc.name = "Hint bloom vertical " + level; var vertical = graph.CreateTexture(desc);
                int index = level * 3;
                Blit(graph, source, down, blocks[index], new Vector4(.25f / desc.width, .25f / desc.height, 0, 0), 0);
                Blit(graph, down, horizontal, blocks[index + 1], new Vector4(1.6f / desc.width, 0, 0, 0), 1);
                using (var builder = Blit(graph, horizontal, vertical, blocks[index + 2], new Vector4(0, 1.6f / desc.height, 0, 0), 1, true))
                {
                    if (level == 0) builder.SetGlobalTextureAfterPass(vertical, Tight);
                    if (level == 2) builder.SetGlobalTextureAfterPass(vertical, Wide);
                }
                if (level == 0) tight = vertical;
                source = vertical;
            }
            using (var builder = graph.AddRasterRenderPass<CompositeData>("Hint bloom soft additive scatter", out var blend))
            {
                blend.material = composite;
                builder.UseTexture(tight); builder.UseTexture(source); builder.UseTexture(mask);
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (CompositeData data, RasterGraphContext context) =>
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3));
            }
        }
        private IBaseRenderGraphBuilder Blit(RenderGraph graph, TextureHandle source, TextureHandle destination,
            MaterialPropertyBlock block, Vector4 step, int shaderPass, bool returnBuilder = false)
        {
            block.SetVector(Step, step);
            return graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, destination,
                Vector2.one, Vector2.zero, gaussian, shaderPass, block,
                RenderGraphUtils.FullScreenGeometryType.ProceduralTriangle), "Hint Gaussian scatter", returnBuilder);
        }
        private sealed class MaskData { public List<Emitter> items; }
        private sealed class CompositeData { public Material material; }
    }
}
