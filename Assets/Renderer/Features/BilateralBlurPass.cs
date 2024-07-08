using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BilateralBlurPass : ScriptableRenderPass, IPass
{
    public static int _Spread = Shader.PropertyToID("_Spread");
    public static int _ColorSigma = Shader.PropertyToID("_ColorSigma");
    public static int _SpaceSigma = Shader.PropertyToID("_SpaceSigma");
    static readonly int _TempBufferId = Shader.PropertyToID("_TempBuffer");
    
    Setting setting;
    Material mat;
    RenderTargetIdentifier tempBuffer;
    int tempBufferId = -1;

    public BilateralBlurPass()
    {
        CreateMaterial();
    }

    public BilateralBlurPass(Setting setting)
    {
        this.setting = setting;
        CreateMaterial();
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

        tempBufferId = _TempBufferId;
        cmd.GetTemporaryRT(tempBufferId, blitTargetDescriptor);
        tempBuffer = new RenderTargetIdentifier(tempBufferId);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (mat == null)
            return;
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, new ProfilingSampler("Bilateral Blur")))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            InjectMaterial(setting);
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
            cmd.ReleaseTemporaryRT(tempBufferId);
    }

    public void InjectMaterial(PassSettingBase setting)
    {
        if (setting == null)
            return;
        setting.InjectMaterial(mat);
    }

    void CreateMaterial()
    {
        Shader shader = Shader.Find("Postprocessing/BilateralBlur");
        if (shader == null)
        {
            Debug.LogError("BilateralBlurPass: shader not found.");
            return;
        }
        mat = CoreUtils.CreateEngineMaterial(shader);
    }

    [System.Serializable]
    public class Setting : PassSettingBase
    {
        [Range(0, .1f)]
        public float Spread = .001f;
        [Range(.01f, 1)]
        public float ColorSigma = .1f;
        [Range(1, 20)]
        public float SpaceSigma = 10;

        public override void InjectMaterial(Material mat)
        {
            if (mat == null)
                return;
            mat.SetFloat(_Spread, Spread);
            mat.SetFloat(_ColorSigma, ColorSigma);
            mat.SetFloat(_SpaceSigma, SpaceSigma);
        }
    }

    [System.Serializable]
    public class SettingVolume : PassSettingBase
    {
        [Space(10)]
        [Header("Bilateral Blur")]
        public ClampedFloatParameter Spread = new ClampedFloatParameter(.001f, 0, .1f);
        public ClampedFloatParameter ColorSigma = new ClampedFloatParameter(.1f, .01f, 1);
        public ClampedFloatParameter SpaceSigma = new ClampedFloatParameter(10, 1, 20);

        public override void InjectMaterial(Material mat)
        {
            if (mat == null)
                return;
            mat.SetFloat(_Spread, Spread.value);
            mat.SetFloat(_ColorSigma, ColorSigma.value);
            mat.SetFloat(_SpaceSigma, SpaceSigma.value);
        }
    }
}