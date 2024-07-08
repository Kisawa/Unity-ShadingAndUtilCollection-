#ifndef DEFAULT_UTIL_STARRAIL_INCLUDED
#define DEFAULT_UTIL_STARRAIL_INCLUDED

#include "StarRailDefaultInput.hlsl"
#include "StarRailDefaultLighting.hlsl"

inline void ApplyAlphaClip(half alpha)
{
#if defined(_ALPHATEST_ON)
    clip(alpha - _Cutoff);
#endif
}

half4 SampleSurface(float2 baseUV)
{
    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV);
}

half3 SampleEmission(float2 uv, half3 albedo)
{
    half3 emission = 0;
#ifdef _EMISSION
    uv = TRANSFORM_TEX(uv, _EmissionMap);
    half4 col = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv);
    emission = col.rgb * col.a * _EmissionColor;
#ifdef _EMISSION_PREMULTIPLY
    emission *= albedo;
#endif
#endif
    return emission;
}

void SampleLightMap(float2 baseUV, inout Surface_Data surfaceData)
{
#if defined(_LIGHT_MAP_ON)
    half4 lightMap = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, baseUV);
    surfaceData.ao = lightMap.r;
    surfaceData.shadeFactor.xy += 1 - lightMap.g;
    surfaceData.specularFactor.w = lightMap.b;
    surfaceData.metal = saturate((abs(lightMap.a - _MetalThreshold) - .1) / -.1);
#endif
}

void SampleMaskMap(float2 baseUV, inout Surface_Data surfaceData)
{
#if defined(_MASK_MAP_ON)
    half4 maskMap = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, baseUV);
    surfaceData.ao = lerp(1, maskMap.g, maskMap.r);
#endif
}

void SampleStockingsMap(float2 baseUV, inout Surface_Data surfaceData)
{
#if defined(_STOCKINGS_ON)
    half3 stockingsMap = SAMPLE_TEXTURE2D(_StockingsMap, sampler_StockingsMap, baseUV).rgb;
    surfaceData.stockingsFactor.w = lerp(1, stockingsMap.g, _StockingsUsage);
    float2 detailUV = TRANSFORM_TEX(baseUV, _StockingsMap);
    half stocking = SAMPLE_TEXTURE2D(_StockingsMap, sampler_StockingsMap, detailUV).b;
    surfaceData.stockingsFactor.z = stocking;
#endif
}

void InitializeSurfaceData(Varyings_Forward IN, Input_Data inputData, bool isFrontFace, out Surface_Data surfaceData)
{
    surfaceData = (Surface_Data)0;
    half4 baseCol = SampleSurface(inputData.baseUV);
    baseCol.a = _Queue == 1 ? 1 : baseCol.a;
    half4 colorTint = isFrontFace ? _BaseColor : _BackColor;
    surfaceData.albedo = baseCol.rgb * colorTint.rgb;
    surfaceData.alpha = baseCol.a * colorTint.a;
    surfaceData.emission = SampleEmission(IN.uv, surfaceData.albedo);
    surfaceData.SH = SampleSH(lerp(inputData.normalWS, 0, _SHHardness)) * _GIStrength * lerp(1, surfaceData.albedo, _GIMixBaseColorUsage);
    surfaceData.shadowColor = _ShadowColor;
    surfaceData.ao = 1;
    surfaceData.aoStrength = _AOStrength;
    surfaceData.shadeFactor = half4(_ShadeStep - _ShadeSoftness, _ShadeStep + _ShadeSoftness, _ShadeStep, _ShadeSoftness);
    surfaceData.specular = _SpecularColor;
    surfaceData.metal = 1;
    surfaceData.specularFactor = half4(_SpecularExpon, _SpecularNonMetalKs, _SpecularMetalKs, 0.32);
    SampleLightMap(inputData.baseUV, surfaceData);
    SampleMaskMap(inputData.baseUV, surfaceData);
    surfaceData.rimColor = _RimColor;
    surfaceData.rimFactor = half2(_RimWidth, _RimThreshold);
    surfaceData.stockingsFactor = half4(_StockingsPower, _StockingsHardness, 1, 1);
    SampleStockingsMap(inputData.baseUV, surfaceData);
}

void InitializeInputData(Varyings_Forward IN, out Input_Data inputData)
{
    inputData = (Input_Data)0;
    inputData.baseUV = TRANSFORM_TEX(IN.uv, _BaseMap);
    inputData.positionWS = IN.positionWS;
    inputData.normalWS = NormalizeNormalPerPixel(IN.normalWS);
    inputData.viewWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);
    float3 receiveShadowPositionWS = inputData.positionWS + _MainLightPosition.xyz * _ReceiveShadowBias;
    inputData.eyeDepth = LinearEyeDepth(IN.positionCS.z, _ZBufferParams);
    inputData.shadowCoord = TransformWorldToShadowCoord(receiveShadowPositionWS);
    inputData.headForwardWS = _HeadForwardDirection;
    inputData.headRightWS = _HeadRightDirection;
    inputData.screenUV = IN.positionCS.xy / _ScaledScreenParams.xy;
}

Varyings_Forward ForwardVertex(Attributes_Forward IN)
{
    Varyings_Forward OUT = (Varyings_Forward)0;
    VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS);
    VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS);
    OUT.positionCS = PositionCSWithZOffset(vertexInput.positionCS, _ZOffset);
    OUT.uv = IN.uv;
    OUT.positionWS = vertexInput.positionWS;
    OUT.normalWS = normalInput.normalWS;
    return OUT;
}

half4 ForwardFragment(Varyings_Forward IN, bool isFrontFace : SV_isFrontFace) : SV_Target
{
    Input_Data inputData;
    InitializeInputData(IN, inputData);

    Surface_Data surfaceData;
    InitializeSurfaceData(IN, inputData, isFrontFace, surfaceData);

    ApplyAlphaClip(surfaceData.alpha);

    half4 col = SimpleLighting(inputData, surfaceData);
    return col;
}

//Outline
Varyings_UV OutlineVertex(Attributes_All IN)
{
    Varyings_UV OUT;
    float3 positionWS = TransformObjectToWorld(IN.positionOS);                    
#if defined(_BACKFACE_OUTLINE_TANGENT)
    float4 directionOS = IN.tangentOS;
#elif defined(_BACKFACE_OUTLINE_COLOR)
    float4 directionOS = IN.color;
#else
    float4 directionOS = float4(IN.normalOS, 1);
#endif
    float3 normalWS = TransformObjectToWorldNormal(directionOS.xyz);

    float3 positionVS = TransformWorldToView(positionWS);
    float fade = GetOutlineCameraFovAndDistanceFixMultiplier(positionVS.z) * _BackFaceOutlineFixMultiplier;

    positionWS += normalWS * _BackFaceOutlineWidth * directionOS.w * fade;
    OUT.positionCS = PositionCSWithZOffset(TransformWorldToHClip(positionWS), _BackFaceOutlineZOffset);
    OUT.uv = IN.uv;
    return OUT;
}

half4 OutlineFragment(Varyings_UV IN) : SV_Target
{
    float2 baseUV = TRANSFORM_TEX(IN.uv, _BaseMap);
    half4 col = SampleSurface(baseUV);
    ApplyAlphaClip(col.a);
    half4 color = _BackFaceOutlineColor;
    color.rgb *= col.rgb;
    return color;
}

//ShadowCaster
Varyings_UV ClipShadowCasterVertex(Attributes_UV_Normal IN)
{
    Varyings_UV OUT;
    float3 positionWS = TransformObjectToWorld(IN.positionOS);
    float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif
    OUT.positionCS = positionCS;
    OUT.uv = IN.uv;
    return OUT;
}

void ClipNullFragment(Varyings_UV IN)
{
    float2 baseUV = TRANSFORM_TEX(IN.uv, _BaseMap);
    half4 col = SampleSurface(baseUV);
    ApplyAlphaClip(col.a);
}

//DepthOnly
Varyings_UV ClipVertex(Attributes_UV IN)
{
    Varyings_UV OUT;
    OUT.positionCS = TransformObjectToHClip(IN.positionOS);
    OUT.uv = IN.uv;
    return OUT;
}

//CustomDepth
void ClipDepthFragment(Varyings_UV IN)
{
#if !defined(_RIM_ON)
    discard;
#endif
    float2 baseUV = TRANSFORM_TEX(IN.uv, _BaseMap);
    half4 col = SampleSurface(baseUV);
    ApplyAlphaClip(col.a);
}

#endif