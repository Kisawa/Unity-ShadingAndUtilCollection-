using CustomVolumeComponent;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenSpaceOutlineFeature : ScriptableRendererFeature, IFrameCaptureFeatureHandle
{
    static int _OutlineColorTint = Shader.PropertyToID("_OutlineColorTint");
    static int _OutlineSaturation = Shader.PropertyToID("_OutlineSaturation");
    static int _OutlineContrast = Shader.PropertyToID("_OutlineContrast");
    static int _OutlineScale = Shader.PropertyToID("_OutlineScale");
    static int _RobertsCrossMultiplier = Shader.PropertyToID("_RobertsCrossMultiplier");
    static int _DepthThreshold = Shader.PropertyToID("_DepthThreshold");
    static int _NormalThreshold = Shader.PropertyToID("_NormalThreshold");
    static int _SteepAngleThreshold = Shader.PropertyToID("_SteepAngleThreshold");
    static int _SteepAngleMultiplier = Shader.PropertyToID("_SteepAngleMultiplier");

    public RenderPassEvent Event = RenderPassEvent.BeforeRenderingTransparents;

    class ScreenSpaceOutlinePass : ScriptableRenderPass
    {
        static int _TempBufferId = Shader.PropertyToID("_TempBuffer");

        public SettingVolume setting { get; set; }
        public bool Enable { get; set; }
        public bool jump { get; set; }

        Material mat;
        RenderTargetIdentifier tempBuffer;
        int tempBufferId = -1;

        public ScreenSpaceOutlinePass()
        {
            Shader shader = Shader.Find("Postprocessing/ScreenSpaceOutline");
            if (shader == null)
            {
                Debug.LogError("Screen Space Outline Feature: shader not found.");
                return;
            }
            mat = CoreUtils.CreateEngineMaterial(shader);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (mat == null)
            {
                tempBufferId = -1;
                return;
            }
            RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            blitTargetDescriptor.depthBufferBits = 0;
            blitTargetDescriptor.msaaSamples = 1;
            blitTargetDescriptor.colorFormat = renderingData.cameraData.isHdrEnabled ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
            tempBufferId = _TempBufferId;
            cmd.GetTemporaryRT(tempBufferId, blitTargetDescriptor);
            tempBuffer = new RenderTargetIdentifier(tempBufferId);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (jump)
            {
                jump = false;
                return;
            }
            if (mat == null || tempBufferId == -1)
                return;
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, new ProfilingSampler("Screen Space Outline")))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                RenderTargetIdentifier cameraColorTarget = renderingData.cameraData.renderer.cameraColorTarget;
                setting.InjectMaterial(mat);
                Blit(cmd, cameraColorTarget, tempBuffer);
                Blit(cmd, tempBuffer, cameraColorTarget, mat);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (tempBufferId != -1)
            {
                cmd.ReleaseTemporaryRT(tempBufferId);
                tempBufferId = -1;
            }
        }

        public void FrameCaptureBeforePostHandle(CommandBuffer cmd, RenderTargetHandle frameCaptureTexture)
        {
            if (mat == null || !Enable || tempBufferId == -1)
                return;
            setting.InjectMaterial(mat);
            Blit(cmd, frameCaptureTexture.Identifier(), tempBuffer);
            Blit(cmd, tempBuffer, frameCaptureTexture.Identifier(), mat);
        }
    }

    ScreenSpaceOutlinePass pass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isSceneViewCamera || renderingData.cameraData.isPreviewCamera)
            return;
        var stack = VolumeManager.instance.stack;
        ScreenSpaceOutline outline = stack.GetComponent<ScreenSpaceOutline>();
        pass.Enable = outline.IsActive();
        if (!pass.Enable)
            return;
        pass.setting = outline.Setting;
        renderer.EnqueuePass(pass);
    }

    public override void Create()
    {
        name = "Screen Space Outline";
        pass = new ScreenSpaceOutlinePass();
        pass.renderPassEvent = Event;
    }

    public void FrameCaptureAfterDrawHandle(CommandBuffer cmd, RenderTargetHandle customCameraTarget) { }

    public void FrameCaptureBeforePostHandle(CommandBuffer cmd, RenderTargetHandle frameCaptureTexture)
    {
        pass.FrameCaptureBeforePostHandle(cmd, frameCaptureTexture);
    }

    public void FrameCaptureAfterPostHandle(CommandBuffer cmd, RenderTargetHandle frameCaptureTexture) { }

    public void FrameCaptureWillPostHandle()
    {
        if (pass == null)
            return;
        pass.jump = true;
    }

    [System.Serializable]
    public class Setting : PassSettingBase
    {
        public bool Debug;
        public Color ColorTint = new Color(.7f, .7f, .7f, 1);
        [Range(0, 3)]
        public float Saturation = 3;
        [Range(0, 3)]
        public float Contrast = .85f;
        [Space(10)]
        [Range(0, 5)]
        public float OutlineScale = 1;
        [Range(0, 5)]
        public float RobertsCrossMultiplier = 5;
        [Range(0, 1)]
        public float DepthThreshold = .15f;
        [Range(0, 2)]
        public float NormalThreshold = .75f;
        [Range(0, 1)]
        public float SteepAngleThreshold = .5f;
        [Range(0, 20)]
        public float SteepAngleMultiplier = 20;

        public override void InjectMaterial(Material mat)
        {
            if (mat == null)
                return;
            mat.SetColor(_OutlineColorTint, ColorTint);
            mat.SetFloat(_OutlineSaturation, Saturation);
            mat.SetFloat(_OutlineContrast, Contrast);
            mat.SetFloat(_OutlineScale, OutlineScale);
            mat.SetFloat(_RobertsCrossMultiplier, RobertsCrossMultiplier);
            mat.SetFloat(_DepthThreshold, DepthThreshold);
            mat.SetFloat(_NormalThreshold, NormalThreshold);
            mat.SetFloat(_SteepAngleThreshold, SteepAngleThreshold);
            mat.SetFloat(_SteepAngleMultiplier, SteepAngleMultiplier);
            if (Debug)
                mat.EnableKeyword("_DEBUGOUTLINE");
            else
                mat.DisableKeyword("_DEBUGOUTLINE");
        }
    }

    [System.Serializable]
    public class SettingVolume : PassSettingBase
    {
        public ClampedFloatParameter OutlineScale = new ClampedFloatParameter(1, 0, 5);
        public ClampedFloatParameter RobertsCrossMultiplier = new ClampedFloatParameter(5, 0, 5);
        public ClampedFloatParameter DepthThreshold = new ClampedFloatParameter(.15f, 0, 1);
        public ClampedFloatParameter NormalThreshold = new ClampedFloatParameter(.75f, 0, 2);
        public ClampedFloatParameter SteepAngleThreshold = new ClampedFloatParameter(.5f, 0, 1);
        public ClampedFloatParameter SteepAngleMultiplier = new ClampedFloatParameter(20, 0, 20);
        [Space(10)]
        public BoolParameter Debug = new BoolParameter(false);
        public ColorParameter ColorTint = new ColorParameter(new Color(.7f, .7f, .7f, 1));
        public ClampedFloatParameter Saturation = new ClampedFloatParameter(3, 0, 3);
        public ClampedFloatParameter Contrast = new ClampedFloatParameter(.85f, 0, 3);

        public override void InjectMaterial(Material mat)
        {
            if (mat == null)
                return;
            mat.SetColor(_OutlineColorTint, ColorTint.value);
            mat.SetFloat(_OutlineSaturation, Saturation.value);
            mat.SetFloat(_OutlineContrast, Contrast.value);
            mat.SetFloat(_OutlineScale, OutlineScale.value);
            mat.SetFloat(_RobertsCrossMultiplier, RobertsCrossMultiplier.value);
            mat.SetFloat(_DepthThreshold, DepthThreshold.value);
            mat.SetFloat(_NormalThreshold, NormalThreshold.value);
            mat.SetFloat(_SteepAngleThreshold, SteepAngleThreshold.value);
            mat.SetFloat(_SteepAngleMultiplier, SteepAngleMultiplier.value);
            if (Debug.value)
                mat.EnableKeyword("_DEBUGOUTLINE");
            else
                mat.DisableKeyword("_DEBUGOUTLINE");
        }
    }
}