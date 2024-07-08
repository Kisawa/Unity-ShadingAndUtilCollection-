#ifndef DEFAULT_LIGHTING_1999_INCLUDED
#define DEFAULT_LIGHTING_1999_INCLUDED

half4 SimpleLighting(Input_Data inputData, Surface_Data surfaceData)
{
    half3 diffuse = 1;
    half shadow = 1;
    uint meshRenderingLayers = GetMeshRenderingLayer();

    Light mainLight = GetMainLight(inputData.shadowCoord);
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
    {
        diffuse = mainLight.color;
#if defined(_RECEIVE_SHADOW)
        shadow *= smoothstep(inputData.shadowStep, inputData.shadowThreshold, mainLight.shadowAttenuation);
#endif
    }

#if defined(_RECEIVE_SHADOW_ADDITIONAL_LIGHTS) && defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(pixelLightCount)
    Light light = GetAdditionalLight(lightIndex, inputData.positionWS, 0);
    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    {
        half realtimeShadow = light.shadowAttenuation;
        half fade = saturate(light.distanceAttenuation * Luminance(light.color));
        shadow *= lerp(1, smoothstep(inputData.shadowStep, inputData.shadowThreshold, realtimeShadow), fade);
    }
    LIGHT_LOOP_END
#endif

    half3 col = surfaceData.albedo + surfaceData.emission * diffuse;
    shadow = lerp(1, shadow, lerp(1, surfaceData.shadowNoise, inputData.shadowNoiseStrength));
    col = lerp(surfaceData.shadowColor.rgb, col, lerp(1, shadow, surfaceData.shadowColor.a));
    return half4(col, surfaceData.alpha);
}

#endif