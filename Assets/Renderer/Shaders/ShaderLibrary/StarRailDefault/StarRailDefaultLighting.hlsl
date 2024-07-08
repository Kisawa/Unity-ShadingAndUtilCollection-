#ifndef DEFAULT_LIGHTING_STARRAIL_INCLUDED
#define DEFAULT_LIGHTING_STARRAIL_INCLUDED

half3 CalcRim(half NoL, Input_Data inputData, Surface_Data surfaceData)
{
    half3 normalVS = TransformWorldToViewDir(inputData.normalWS);
    float2 offset = normalVS.xy * surfaceData.rimFactor.x * .01 * NoL / inputData.eyeDepth;
    float depth = SampleCustomDepth(inputData.screenUV + offset);
    depth = LinearEyeDepth(depth, _ZBufferParams);
    half rim = saturate(depth - inputData.eyeDepth - surfaceData.rimFactor.y);
    return rim * surfaceData.rimColor;
}

half3 CalcLight(Light light, Input_Data inputData, Surface_Data surfaceData, inout half3 specCol)
{
    half NoL = dot(inputData.normalWS, light.direction);
    half remappedNoL = NoL * .5 + .5;
    half shade = smoothstep(surfaceData.shadeFactor.x, surfaceData.shadeFactor.y, remappedNoL) * surfaceData.ao;
#if !defined(_SPECULARHIGHLIGHTS_OFF)
    half3 halfWS = normalize(inputData.viewWS + light.direction);
    half NoH = dot(inputData.normalWS, halfWS);
    half blinnPhong = pow(saturate(NoH), surfaceData.specularFactor.x);
    half nonMetalSpecular = step(1.04 - blinnPhong, surfaceData.specularFactor.w) * surfaceData.specularFactor.y;
    half3 metalSpecular = blinnPhong * surfaceData.specularFactor.w * surfaceData.specularFactor.z * surfaceData.albedo;
    specCol += lerp(nonMetalSpecular, metalSpecular, surfaceData.metal) * surfaceData.specular * light.color;
#endif
#if defined(_RIM_ON)
    specCol += CalcRim(remappedNoL, inputData, surfaceData) * light.color;
#endif
    return lerp(surfaceData.shadowColor, 1, shade) * light.color;
}

half3 CalcSDFDiffuse(Light light, Input_Data inputData, Surface_Data surfaceData)
{
    half3 forward = inputData.headForwardWS;
    half3 right = inputData.headRightWS;
    half3 up = cross(forward, right);
    half3 lightProj = normalize(light.direction - dot(light.direction, up) * up);
    float2 sdfUV = float2(ssign(dot(lightProj, right)), 1) * inputData.baseUV * float2(-1, 1);

    half sdf = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, sdfUV).a;
    half threshold = 1 - (dot(lightProj, forward) * .5 + .5);
    half shade = smoothstep(threshold - surfaceData.shadeFactor.w, threshold + surfaceData.shadeFactor.w, sdf) * surfaceData.ao;
    return lerp(surfaceData.shadowColor, 1, shade) * light.color;
}

void MixStockingsLight(Input_Data inputData, Surface_Data surfaceData, inout half3 col)
{
    half NoV = dot(inputData.normalWS, inputData.viewWS);
    half factor = pow(saturate(NoV), surfaceData.stockingsFactor.x);
    factor = saturate((factor - surfaceData.stockingsFactor.y * .5) / (1 - surfaceData.stockingsFactor.y));
    half3 translucentColor = SAMPLE_TEXTURE2D(_StockingsRamp, sampler_StockingsRamp, float2(factor * surfaceData.stockingsFactor.w, 0));
    col = lerp(col, translucentColor, surfaceData.stockingsFactor.z * factor);
}

half4 SimpleLighting(Input_Data inputData, Surface_Data surfaceData)
{
    half ao = lerp(1, surfaceData.ao, surfaceData.aoStrength);
    half3 SH = surfaceData.SH * ao;
    half3 diffuse = 1, specCol = 0;
#if defined(_LIGHTING_ON)
    uint meshRenderingLayers = GetMeshRenderingLayer();
    Light mainLight = GetMainLight(inputData.shadowCoord);
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
    {
        mainLight.color = lerp(Desaturation(mainLight.color), mainLight.color, _MainLightColorUsage);
#if defined(_MASK_MAP_ON)
        diffuse = CalcSDFDiffuse(mainLight, inputData, surfaceData);
#else
        diffuse = CalcLight(mainLight, inputData, surfaceData, specCol);
#endif
    }
#endif

    half3 col = surfaceData.albedo * (diffuse + SH) + specCol + surfaceData.emission;
#if _STOCKINGS_ON
    MixStockingsLight(inputData, surfaceData, col);
#endif
    return half4(col, surfaceData.alpha);
}

#endif