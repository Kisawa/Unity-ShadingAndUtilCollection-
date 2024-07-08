#ifndef DEFAULT_UTIL_1999_INCLUDED
#define DEFAULT_UTIL_1999_INCLUDED

#include "1999DefaultInput.hlsl"
#include "1999DefaultLighting.hlsl"

inline void ApplyAlphaClip(half alpha)
{
#if defined(_ALPHATEST_ON)
    clip(alpha - _Cutoff);
#endif
}

half3 SampleEmission(float2 uv)
{
    half3 emission = 0;
#ifdef _EMISSION
    emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor;
#endif
    return emission;
}

half SampleShadowNoise(float3 positionWS, float3 normalWS)
{
    half noise = 1;
#if defined(_SHADOW_NOISE_ON)
    noise = TriplanarMapping(TEXTURE2D_ARGS(_ShadowNoiseMap, sampler_ShadowNoiseMap), positionWS, normalWS, _ShadowNoiseSharpness, _ShadowNoiseMap_ST);
    noise = smoothstep(_ShadowNoiseStep, _ShadowNoiseThreshold, noise);
#endif
    return noise;
}

void InitializeSurfaceData(Varyings_Forward IN, Input_Data inputData, out Surface_Data surfaceData)
{
    surfaceData = (Surface_Data)0;
    float2 baseUV = TRANSFORM_TEX(IN.uv, _BaseMap) + _BaseAnime * _Time.y;
    half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV) * _BaseColor;
    surfaceData.albedo = col.rgb;
    surfaceData.alpha = col.a;
    float2 emissionUV = TRANSFORM_TEX(IN.uv, _EmissionMap) + _EmissionAnime * _Time.y;
    surfaceData.emission = SampleEmission(emissionUV);
#if defined(_EMISSION_PREMULTIPLY)
    surfaceData.emission *= surfaceData.albedo;
#endif
    surfaceData.shadowColor = _ShadowColor;
    surfaceData.shadowNoise = SampleShadowNoise(inputData.positionWS, inputData.normalWS);
}

void InitializeInputData(Varyings_Forward IN, out Input_Data inputData)
{
    inputData = (Input_Data)0;
    inputData.positionWS = IN.positionWS;
    inputData.normalWS = IN.normalWS;
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    inputData.shadowStep = _ShadowStep;
    inputData.shadowThreshold = _ShadowThreshold;
    inputData.shadowNoiseStrength = _ShadowNoiseStrength;
}

Varyings_Forward ForwardVertex(Attributes_Forward IN)
{
    Varyings_Forward OUT = (Varyings_Forward)0;
    VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS);
    VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS);
    OUT.positionCS = vertexInput.positionCS;
    OUT.uv = IN.uv;
    OUT.positionWS = vertexInput.positionWS;
    OUT.normalWS = normalInput.normalWS;
    return OUT;
}

half4 ForwardFragment(Varyings_Forward IN) : SV_Target
{
    Input_Data inputData;
    InitializeInputData(IN, inputData);

    Surface_Data surfaceData;
    InitializeSurfaceData(IN, inputData, surfaceData);

    ApplyAlphaClip(surfaceData.alpha);

    half4 col = SimpleLighting(inputData, surfaceData);
    return col;
}

//ShadowCaster
Varyings_UV ShadowCasterVertex(Attributes_UV_Normal IN)
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

void ShadowCasterFragment(Varyings_UV IN)
{
#if defined(_SHADOW_CASTER_ON)
    float2 uv = TRANSFORM_TEX(IN.uv, _ShadowCasterClipMap);
    half4 col = SAMPLE_TEXTURE2D(_ShadowCasterClipMap, sampler_ShadowCasterClipMap, uv);
    clip(col.a - _ShadowCasterCutoff);
#else
    discard;
#endif
}

#endif