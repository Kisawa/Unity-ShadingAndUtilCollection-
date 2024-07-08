using CustomVolumeComponent;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TonemappingFeature : ScriptableRendererFeature
{
    class TonemappingPass : ScriptableRenderPass
    {
        static int _TempBufferId = Shader.PropertyToID("_TempBuffer");

        Material mat;
        RenderTargetIdentifier tempBuffer;
        int tempBufferId = -1;

        public TonemappingPass()
        {
            Shader shader = Shader.Find("Postprocessing/Tonemapping");
            if (shader == null)
            {
                Debug.LogError("Tonemapping Feature: shader not found.");
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
            if (mat == null || tempBufferId == -1)
                return;
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, new ProfilingSampler("Tonemapping")))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                RenderTargetIdentifier cameraColorTarget = renderingData.cameraData.renderer.cameraColorTarget;
                Blit(cmd, cameraColorTarget, tempBuffer, mat);
                Blit(cmd, tempBuffer, cameraColorTarget);
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
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isSceneViewCamera || renderingData.cameraData.isPreviewCamera)
            return;
        var stack = VolumeManager.instance.stack;
        Tonemapping tonemapping = stack.GetComponent<Tonemapping>();
        if (tonemapping.IsActive())
            return;
        renderer.EnqueuePass(pass);
    }

    TonemappingPass pass;

    public override void Create()
    {
        name = "Tonemapping";
        pass = new TonemappingPass();
        pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }
}