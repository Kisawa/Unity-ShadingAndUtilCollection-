using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SpotVolumeLightHelper : ScriptableRendererFeature
{
    public static SpotVolumeLightHelper Self;

    List<Light> LightList = new List<Light>();
    List<int> LightIndexList = new List<int>();

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        for (int i = 0; i < LightList.Count; i++)
        {
            if (LightList[i] == null)
            {
                LightList.RemoveAt(i);
                LightIndexList.RemoveAt(i);
                i--;
            }
        }
        for (int i = 0; i < renderingData.lightData.visibleLights.Length; i++)
        {
            VisibleLight light = renderingData.lightData.visibleLights[i];
            if (!LightList.Contains(light.light))
            {
                LightList.Add(light.light);
                LightIndexList.Add(i);
            }
        }
    }

    public int GetLightIndex(Light light)
    {
        if (light == null)
            return -1;
        int i = LightList.IndexOf(light);
        if (i == -1)
            return 0;
        return LightIndexList[i];
    }

    public override void Create()
    {
        Self = this;
    }
}