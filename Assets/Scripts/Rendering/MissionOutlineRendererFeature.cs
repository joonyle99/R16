using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

/// <summary>
/// 특정 Rendering Layer 에 속한 스프라이트들을 하나의 실루엣으로 묶어, 그 외곽에만 단일 강조색 외곽선을 그리는 URP 2D Renderer Feature.
/// 미션 대상 적 표시에 사용한다. 런타임에서 적 SpriteRenderer 들의 renderingLayerMask 에 지정 비트를 켜고 끄는 것으로 on/off.
///
/// 동작: ① 대상 레이어만 마스크 RT 에 솔리드로 렌더 → ② 마스크를 두께만큼 팽창시켜 카메라 컬러에 외곽선 합성.
/// 여러 레이어 스프라이트(눈/몸통 등)가 마스크에서 합쳐지므로 내부 경계선 없이 한 덩어리로 외곽선이 나온다.
/// </summary>
public sealed class MissionOutlineRendererFeature : ScriptableRendererFeature
{
    [Tooltip("외곽선 대상으로 인식할 Rendering Layer. 적 SpriteRenderer 의 renderingLayerMask 와 일치해야 한다.")]
    [SerializeField] private RenderingLayerMask _targetLayer = 1 << 1;

    [SerializeField] private Color _outlineColor = new Color(1f, 0.9f, 0.2f, 1f);

    [Range(1, 6)]
    [SerializeField] private int _outlineThickness = 2;

    [SerializeField] private RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

    private MissionOutlinePass _pass;
    private Material _maskMaterial;
    private Material _compositeMaterial;

    public override void Create()
    {
        if (_maskMaterial == null)
        {
            var maskShader = Shader.Find("Hidden/MissionOutlineMask");
            if (maskShader != null) _maskMaterial = CoreUtils.CreateEngineMaterial(maskShader);
        }
        if (_compositeMaterial == null)
        {
            var compositeShader = Shader.Find("Hidden/MissionOutlineComposite");
            if (compositeShader != null) _compositeMaterial = CoreUtils.CreateEngineMaterial(compositeShader);
        }

        _pass = new MissionOutlinePass(_maskMaterial, _compositeMaterial)
        {
            renderPassEvent = _renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_maskMaterial == null || _compositeMaterial == null) return;

        var cameraType = renderingData.cameraData.cameraType;
        if (cameraType is CameraType.Preview or CameraType.Reflection) return;

        _pass.Setup((uint)_targetLayer, _outlineColor, _outlineThickness);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_maskMaterial);
        CoreUtils.Destroy(_compositeMaterial);
        _maskMaterial = null;
        _compositeMaterial = null;
    }

    // ============ Pass ============

    private sealed class MissionOutlinePass : ScriptableRenderPass
    {
        private static readonly List<ShaderTagId> ShaderTags = new()
        {
            new ShaderTagId("Universal2D"),
            new ShaderTagId("SRPDefaultUnlit"),
        };

        private static readonly int MaskId = Shader.PropertyToID("_MissionOutlineMask");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineThicknessId = Shader.PropertyToID("_OutlineThickness");
        private static readonly int OutlineTexelSizeId = Shader.PropertyToID("_OutlineTexelSize");

        private readonly Material _maskMaterial;
        private readonly Material _compositeMaterial;

        private uint _targetLayer;

        public MissionOutlinePass(Material maskMaterial, Material compositeMaterial)
        {
            _maskMaterial = maskMaterial;
            _compositeMaterial = compositeMaterial;
        }

        public void Setup(uint targetLayer, Color outlineColor, int thickness)
        {
            _targetLayer = targetLayer;
            _compositeMaterial.SetColor(OutlineColorId, outlineColor);
            _compositeMaterial.SetFloat(OutlineThicknessId, thickness);
        }

        private class MaskPassData
        {
            public RendererListHandle rendererList;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();

            if (resourceData.isActiveTargetBackBuffer) return;

            var cameraColor = resourceData.activeColorTexture;

            // --- 마스크 텍스처 ---
            var maskDesc = cameraData.cameraTargetDescriptor;
            maskDesc.depthBufferBits = 0;
            maskDesc.msaaSamples = 1;
            maskDesc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm;
            var maskTex = UniversalRenderer.CreateRenderGraphTexture(renderGraph, maskDesc, "_MissionOutlineMask", true);

            float w = Mathf.Max(1, maskDesc.width);
            float h = Mathf.Max(1, maskDesc.height);
            _compositeMaterial.SetVector(OutlineTexelSizeId, new Vector4(1f / w, 1f / h, w, h));

            // --- Pass 1: 대상 레이어를 마스크에 렌더 ---
            using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>("Mission Outline Mask", out var passData))
            {
                var sortFlags = cameraData.defaultOpaqueSortFlags;
                var drawSettings = RenderingUtils.CreateDrawingSettings(ShaderTags, renderingData, cameraData, lightData, sortFlags);
                drawSettings.overrideMaterial = _maskMaterial;
                drawSettings.overrideMaterialPassIndex = 0;

                var filterSettings = new FilteringSettings(RenderQueueRange.all)
                {
                    renderingLayerMask = _targetLayer,
                };

                var rlParams = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
                passData.rendererList = renderGraph.CreateRendererList(rlParams);

                builder.UseRendererList(passData.rendererList);
                builder.SetRenderAttachment(maskTex, 0);
                builder.AllowPassCulling(false);
                builder.SetGlobalTextureAfterPass(maskTex, MaskId);

                builder.SetRenderFunc((MaskPassData data, RasterGraphContext ctx) =>
                {
                    ctx.cmd.ClearRenderTarget(RTClearFlags.Color, Color.clear, 1f, 0);
                    ctx.cmd.DrawRendererList(data.rendererList);
                });
            }

            // --- Pass 2: 외곽선 합성 (cameraColor -> temp), 그리고 temp -> cameraColor 되돌리기 ---
            // temp 는 깊이/MSAA 없는 컬러 전용이어야 블릿 대상으로 안전 (카메라 디스크립터 그대로 쓰면 검은 화면 등 문제)
            var tempDesc = cameraData.cameraTargetDescriptor;
            tempDesc.depthBufferBits = 0;
            tempDesc.msaaSamples = 1;
            var tempColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, tempDesc, "_MissionOutlineTemp", false);

            var outlineParams = new RenderGraphUtils.BlitMaterialParameters(cameraColor, tempColor, _compositeMaterial, 0);
            renderGraph.AddBlitPass(outlineParams, "Mission Outline Composite");

            var copyParams = new RenderGraphUtils.BlitMaterialParameters(tempColor, cameraColor, _compositeMaterial, 1);
            renderGraph.AddBlitPass(copyParams, "Mission Outline Copy Back");
        }
    }
}
