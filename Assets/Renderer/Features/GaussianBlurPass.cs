using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

public class GaussianBlurPass : ScriptableRenderPass
{
    static readonly string RenderTag = "Gaussian Blur";
    static readonly int _BlurSpread = Shader.PropertyToID("_BlurSpread");
    static readonly int _TempRT0 = Shader.PropertyToID("_TempRT0");
    static readonly int _TempRT1 = Shader.PropertyToID("_TempRT1");

    BlurFeature.BlurSetting setting;
    Material mat;
    int temp_id0 = -1;
    int temp_id1 = -1;
    RenderTargetIdentifier temp0;
    RenderTargetIdentifier temp1;

    public GaussianBlurPass(BlurFeature.BlurSetting setting, RenderPassEvent @event)
    {
        renderPassEvent = @event;
        this.setting = setting;
        Shader shader = Shader.Find("Postprocessing/GaussianBlur");
        if (shader == null)
        {
            Debug.LogError("GaussianBlurPass: shader not found.");
            return;
        }
        mat = CoreUtils.CreateEngineMaterial(shader);
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        if (mat == null)
        {
            temp_id0 = -1;
            temp_id1 = -1;
            return;
        }
        RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        blitTargetDescriptor.depthBufferBits = 0;
        blitTargetDescriptor.msaaSamples = 1;
        switch (setting.downSample)
        {
            case DownSample.x2:
                blitTargetDescriptor.width /= 2;
                blitTargetDescriptor.height /= 2;
                break;
            case DownSample.x4:
                blitTargetDescriptor.width /= 4;
                blitTargetDescriptor.height /= 4;
                break;
        }
        temp_id0 = _TempRT0;
        cmd.GetTemporaryRT(temp_id0, blitTargetDescriptor);
        temp0 = new RenderTargetIdentifier(temp_id0);
        temp_id1 = _TempRT1;
        cmd.GetTemporaryRT(temp_id1, blitTargetDescriptor);
        temp1 = new RenderTargetIdentifier(temp_id1);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (mat == null)
            return;
        CommandBuffer cmd = CommandBufferPool.Get(RenderTag);

        RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTarget;
        mat.SetFloat(_BlurSpread, setting.BlurSpread);
        Blit(cmd, source, temp0, mat, 0);
        Blit(cmd, temp0, temp1, mat, 1);
        for (int i = 2; i <= setting.Iterations; i++)
        {
            mat.SetFloat(_BlurSpread, setting.BlurSpread * i);
            Blit(cmd, temp1, temp0, mat, 0);
            Blit(cmd, temp0, temp1, mat, 1);
        }
        Blit(cmd, temp1, source);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        if(temp_id0 != -1)
            cmd.ReleaseTemporaryRT(temp_id0);
        if (temp_id1 != -1)
            cmd.ReleaseTemporaryRT(temp_id1);
    }
}

public enum DownSample { none, x2, x4 };