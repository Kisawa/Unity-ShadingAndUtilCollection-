using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlurFeature : ScriptableRendererFeature
{
    public RenderPassEvent Event = RenderPassEvent.AfterRenderingPostProcessing;
    public RuntimeVal<BlurType> blurType;
    public BlurSetting blurSetting = new BlurSetting();

    DualKawaseBlurPass dualKawaseBlurPass;
    GaussianBlurPass gaussianBlurPass;

    public override void Create()
    {
        dualKawaseBlurPass = new DualKawaseBlurPass(blurSetting, Event);
        gaussianBlurPass = new GaussianBlurPass(blurSetting, Event);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isSceneViewCamera || renderingData.cameraData.isPreviewCamera)
            return;
        switch (blurType.Val)
        {
            case BlurType.DualKawase:
                renderer.EnqueuePass(dualKawaseBlurPass);
                break;
            case BlurType.Gaussian:
                renderer.EnqueuePass(gaussianBlurPass);
                break;
        }
    }

    [Serializable]
    public class BlurSetting
    {
        public DownSample downSample;
        [Range(1, 5)]
        public int Iterations = 3;
        [Range(0, 5)]
        public float BlurSpread = 1;
    }

    public enum BlurType
    {
        None,
        DualKawase,
        Gaussian
    }
}