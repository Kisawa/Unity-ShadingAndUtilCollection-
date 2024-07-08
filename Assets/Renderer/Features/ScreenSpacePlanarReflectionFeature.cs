using CustomVolumeComponent;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenSpacePlanarReflectionFeature : ScriptableRendererFeature
{
    public RenderPassEvent Event = RenderPassEvent.BeforeRenderingTransparents;

    class ScreenSpacePlanarReflectionPass : ScriptableRenderPass
    {
        static readonly int _TempBufferId = Shader.PropertyToID("_TempBuffer");
        static readonly int _SSPRMapId = Shader.PropertyToID("_SSPRMap");
        static readonly int _SSPRHashBufferId = Shader.PropertyToID("_SSPRHashBuffer");
        static int _HorizontalPlaneHeightWS = Shader.PropertyToID("_HorizontalPlaneHeightWS");
        static int _Fade = Shader.PropertyToID("_Fade"); 
        static int _FadeHorizontalThreshold = Shader.PropertyToID("_FadeHorizontalThreshold");
        static int _HeightStretch = Shader.PropertyToID("_HeightStretch");
        static int _HeightStretchPower = Shader.PropertyToID("_HeightStretchPower");
        static int _VP = Shader.PropertyToID("_VP");
        static int _InverseVP = Shader.PropertyToID("_InverseVP");
        static int _RTSize = Shader.PropertyToID("_RTSize");

        static readonly int RW_HashBuffer = Shader.PropertyToID("RW_HashBuffer");
        static readonly int RW_SSPRMap = Shader.PropertyToID("RW_SSPRMap");
        static readonly int _CameraTexture = Shader.PropertyToID("_CameraTexture");
        static readonly int _DepthTexture = Shader.PropertyToID("_DepthTexture");

        public ScreenSpacePlanarReflection setting { get; set; }
        RenderTargetIdentifier TempBuffer;
        int TempBufferId = -1;
        int SSPRMapId = -1;
        int SSPRHashBufferId = -1;

        ComputeShader cs;
        int kernel_CalcSSPR;
        int kernel_FillHoles;
        Vector2 RTSize;
        Vector3Int dispatchThreadGroup;

        public ScreenSpacePlanarReflectionPass()
        {
            cs = Resources.Load<ComputeShader>("CalcSSPR");
            if (cs == null)
            {
                Debug.LogError("Screen Space Planar Reflection Feature: compute shader not found.");
                return;
            }
            kernel_CalcSSPR = cs.FindKernel("CalcSSPR");
            kernel_FillHoles = cs.FindKernel("FillHoles");
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (setting == null || cs == null)
            {
                TempBufferId = -1;
                SSPRMapId = -1;
                SSPRHashBufferId = -1;
                return;
            }
            RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            int width = Mathf.RoundToInt(blitTargetDescriptor.width * setting.Resolution.value);
            int height = Mathf.RoundToInt(blitTargetDescriptor.height * setting.Resolution.value);
            RTSize = new Vector2(width, height);
            dispatchThreadGroup = new Vector3Int(Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);

            bool enableHDR = setting.EnableHDR.value;
            if (blitTargetDescriptor.colorFormat == RenderTextureFormat.ARGB32)
                enableHDR = false;
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(width, height, enableHDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32, 0);
            TempBufferId = _TempBufferId;
            cmd.GetTemporaryRT(TempBufferId, descriptor);
            TempBuffer = new RenderTargetIdentifier(TempBufferId);

            descriptor.enableRandomWrite = true;
            SSPRMapId = _SSPRMapId;
            cmd.GetTemporaryRT(SSPRMapId, descriptor);

            descriptor.colorFormat = RenderTextureFormat.RFloat;
            SSPRHashBufferId = _SSPRHashBufferId;
            cmd.GetTemporaryRT(SSPRHashBufferId, descriptor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (setting == null || cs == null)
                return;
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, new ProfilingSampler("Screen Space Planar Reflection")))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                Matrix4x4 p = GL.GetGPUProjectionMatrix(renderingData.cameraData.GetProjectionMatrix(), false);
                Matrix4x4 v = renderingData.cameraData.GetViewMatrix();
                Matrix4x4 vp = p * v;

                Blit(cmd, renderingData.cameraData.renderer.cameraColorTarget, TempBuffer);
                RenderTargetIdentifier cameraTexture = TempBuffer;
                RenderTargetIdentifier depthTexture = renderingData.cameraData.renderer.cameraDepthTarget;
                
                cmd.SetComputeMatrixParam(cs, _VP, vp);
                cmd.SetComputeMatrixParam(cs, _InverseVP, Matrix4x4.Inverse(vp));
                cmd.SetComputeVectorParam(cs, _RTSize, RTSize);
                cmd.SetComputeFloatParam(cs, _HorizontalPlaneHeightWS, setting.HorizontalPlaneHeightWS.value);
                cmd.SetComputeFloatParam(cs, _Fade, setting.Fade.value);
                cmd.SetComputeFloatParam(cs, _FadeHorizontalThreshold, setting.FadeHorizontalThreshold.value);
                cmd.SetComputeFloatParam(cs, _HeightStretch, setting.HeightStretch.value);
                cmd.SetComputeFloatParam(cs, _HeightStretchPower, setting.HeightStretchPower.value);
                cmd.SetComputeTextureParam(cs, kernel_CalcSSPR, RW_SSPRMap, _SSPRMapId);
                cmd.SetComputeTextureParam(cs, kernel_CalcSSPR, RW_HashBuffer, _SSPRHashBufferId);
                cmd.SetComputeTextureParam(cs, kernel_CalcSSPR, _CameraTexture, cameraTexture);
                cmd.SetComputeTextureParam(cs, kernel_CalcSSPR, _DepthTexture, depthTexture);
                cmd.DispatchCompute(cs, kernel_CalcSSPR, dispatchThreadGroup.x, dispatchThreadGroup.y, dispatchThreadGroup.z);

                if (setting.FillHoles.value)
                {
                    cmd.SetComputeTextureParam(cs, kernel_FillHoles, RW_SSPRMap, _SSPRMapId);
                    cmd.SetComputeTextureParam(cs, kernel_FillHoles, RW_HashBuffer, _SSPRHashBufferId);
                    cmd.DispatchCompute(cs, kernel_FillHoles, dispatchThreadGroup.x, dispatchThreadGroup.y, dispatchThreadGroup.z);
                }
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (TempBufferId != -1)
                cmd.ReleaseTemporaryRT(TempBufferId);
            if (SSPRMapId != -1)
                cmd.ReleaseTemporaryRT(SSPRMapId);
            if (SSPRHashBufferId != -1)
                cmd.ReleaseTemporaryRT(SSPRHashBufferId);
        }
    }

    class AfterDrawerPass : ScriptableRenderPass
    {
        static readonly ShaderTagId ShaderTag = new ShaderTagId("AfterSSPR");

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            DrawingSettings drawingSettings = CreateDrawingSettings(ShaderTag, ref renderingData, SortingCriteria.CommonOpaque);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
        }
    }

    ScreenSpacePlanarReflectionPass pass;
    AfterDrawerPass afterPass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isPreviewCamera)
            return;
        var stack = VolumeManager.instance.stack;
        ScreenSpacePlanarReflection sspr = stack.GetComponent<ScreenSpacePlanarReflection>();
        if (!sspr.IsActive())
            return;
        pass.setting = sspr;
        renderer.EnqueuePass(pass);
        renderer.EnqueuePass(afterPass);
    }

    public override void Create()
    {
        name = "Screen Space Planar Reflection";
        pass = new ScreenSpacePlanarReflectionPass();
        pass.renderPassEvent = Event;
        afterPass = new AfterDrawerPass();
        afterPass.renderPassEvent = Event;
    }

    [System.Serializable]
    public class Setting
    {
        [Range(.1f, 1)]
        public float Resolution = 1;
        public float HorizontalPlaneHeightWS = .05f;
        [Range(0, 1)]
        public float Fade = .15f;
        [Range(0, 10)]
        public float FadeHorizontalThreshold = 5;
        [Range(0, 3)]
        public float HeightStretch = 0;
        [Range(.1f, 3)]
        public float HeightStretchPower = 1.5f;
        public bool EnableHDR = true;
        public bool FillHoles = true;
    }
}