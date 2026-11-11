using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class DatamoshRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material datamoshMaterial;
        [Range(1f, 128f)] public float blockSize = 16f; // Set to 1 for liquid-smooth smear
        [Range(0f, 1f)] public float noiseAmount = 0f;  // 0 = no noise
        public RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public Settings settings = new Settings();
    private DatamoshPass _pass;

    public override void Create()
    {
        _pass = new DatamoshPass(settings);
        _pass.renderPassEvent = settings.injectionPoint;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.datamoshMaterial == null || renderingData.cameraData.isPreviewCamera)
            return;

        // Requests _MotionVectorTexture from URP
        _pass.ConfigureInput(ScriptableRenderPassInput.Motion);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        _pass?.Dispose();
    }

    class DatamoshPass : ScriptableRenderPass
    {
        private readonly Settings _settings;
        private RTHandle _historyRT;
        private bool _hasHistory;

        private static readonly int HistoryTexID = Shader.PropertyToID("_HistoryTex");
        private static readonly int BlockSizeID = Shader.PropertyToID("_BlockSize");
        private static readonly int NoiseThresholdID = Shader.PropertyToID("_NoiseThreshold");

        public DatamoshPass(Settings settings)
        {
            _settings = settings;
        }

        private void EnsureHistoryBuffer(RenderTextureDescriptor desc)
        {
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            if (RenderingUtils.ReAllocateIfNeeded(ref _historyRT, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_DatamoshHistory"))
            {
                _hasHistory = false;
            }
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            if (resourceData.isActiveTargetBackBuffer)
                return;

            EnsureHistoryBuffer(cameraData.cameraTargetDescriptor);

            TextureHandle source = resourceData.activeColorTexture;
            TextureHandle historyHandle = renderGraph.ImportTexture(_historyRT);

            // Seed history texture on the first frame so it does not start black
            if (!_hasHistory)
            {
                renderGraph.AddBlitPass(source, historyHandle, Vector2.one, Vector2.zero, passName: "Datamosh Seed History");
                _hasHistory = true;
            }

            _settings.datamoshMaterial.SetTexture(HistoryTexID, _historyRT);
            _settings.datamoshMaterial.SetFloat(BlockSizeID, _settings.blockSize);
            _settings.datamoshMaterial.SetFloat("_NoiseAmount", _settings.noiseAmount);

            var tempDesc = renderGraph.GetTextureDesc(source);
            tempDesc.name = "_DatamoshTempRG";
            tempDesc.clearBuffer = false;
            TextureHandle temp = renderGraph.CreateTexture(tempDesc);

            // 1. Apply datamosh shader (reads source, _HistoryTex, and _MotionVectorTexture -> writes temp)
            var blitParams = new RenderGraphUtils.BlitMaterialParameters(source, temp, _settings.datamoshMaterial, 0);
            renderGraph.AddBlitPass(blitParams, passName: "Datamosh Effect");

            // 2. Write result back to both the active camera color and the persistent history buffer
            renderGraph.AddBlitPass(temp, source, Vector2.one, Vector2.zero, passName: "Datamosh Copy To Camera");
            renderGraph.AddBlitPass(temp, historyHandle, Vector2.one, Vector2.zero, passName: "Datamosh Update History");
        }

        public void Dispose()
        {
            _historyRT?.Release();
        }
    }
}
