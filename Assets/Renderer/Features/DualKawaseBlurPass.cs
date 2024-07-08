using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

public class DualKawaseBlurPass : ScriptableRenderPass
{
    static readonly string RenderTag = "Dual Kawase Blur";
    static readonly int _BlurSpread = Shader.PropertyToID("_BlurSpread");

    BlurFeature.BlurSetting setting;
    Material mat;

    RTCache cache;

    public DualKawaseBlurPass(BlurFeature.BlurSetting setting, RenderPassEvent @event)
    {
        renderPassEvent = @event;
        this.setting = setting;
        Shader shader = Shader.Find("Postprocessing/DualKawaseBlur");
        if (shader == null)
        {
            Debug.LogError("DualKawaseBlurPass: shader not found.");
            return;
        }
        mat = CoreUtils.CreateEngineMaterial(shader);
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        if (mat == null)
            return;
        RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        blitTargetDescriptor.depthBufferBits = 0;
        blitTargetDescriptor.msaaSamples = 1;
        RefreshCache();
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
        cache.Setup(blitTargetDescriptor, cmd);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (mat == null)
            return;
        CommandBuffer cmd = CommandBufferPool.Get(RenderTag);

        mat.SetFloat(_BlurSpread, setting.BlurSpread);

        RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTarget;
        RenderTargetIdentifier temp = source;
        for (int i = 0; i < cache.Count; i++)
        {
            RenderTargetIdentifier _temp = cache.down[i];
            Blit(cmd, temp, _temp, mat, 0);
            temp = _temp;
        }
        for (int i = cache.Count - 1; i >= 0; i--)
        {
            RenderTargetIdentifier _temp = cache.up[i];
            Blit(cmd, temp, _temp, mat, 1);
            temp = _temp;
        }
        Blit(cmd, temp, source);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        cache.Cleanup(cmd);
    }

    void RefreshCache()
    {
        if (cache.Count == setting.Iterations)
            return;
        cache = new RTCache(setting.Iterations);
    }

    public struct RTCache
    {
        public int Count { get; private set; }
        public RenderTargetIdentifier[] down { get; private set; }
        public RenderTargetIdentifier[] up { get; private set; }
        int[] down_ids;
        int[] up_ids;

        public RTCache(int count)
        {
            Count = count;
            down = new RenderTargetIdentifier[count];
            up = new RenderTargetIdentifier[count];
            down_ids = Enumerable.Repeat(-1, count).ToArray();
            up_ids = Enumerable.Repeat(-1, count).ToArray();
        }

        public void Setup(RenderTextureDescriptor descriptor, CommandBuffer cmd)
        {
            for (int i = 0; i < Count; i++)
            {
                int _down = Shader.PropertyToID($"DownRT{i}");
                int _up = Shader.PropertyToID($"UpRT{i}");
                cmd.GetTemporaryRT(_down, descriptor);
                cmd.GetTemporaryRT(_up, descriptor);
                down_ids[i] = _down;
                up_ids[i] = _up;
                down[i] = new RenderTargetIdentifier(_down);
                up[i] = new RenderTargetIdentifier(_up);
                descriptor.width /= 2;
                descriptor.height /= 2;
            }
        }

        public void Cleanup(CommandBuffer cmd)
        {
            for (int i = 0; i < Count; i++)
            {
                int _down = down_ids[i];
                if (_down != -1)
                    cmd.ReleaseTemporaryRT(_down);
                down_ids[i] = -1;
                int _up = up_ids[i];
                if (_up != -1)
                    cmd.ReleaseTemporaryRT(_up);
                up_ids[i] = -1;
            }
        }
    }
}