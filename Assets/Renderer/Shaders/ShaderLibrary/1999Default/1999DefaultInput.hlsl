#ifndef DEFAULT_INPUT_1999_INCLUDED
#define DEFAULT_INPUT_1999_INCLUDED

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);

TEXTURE2D(_ShadowNoiseMap);
SAMPLER(sampler_ShadowNoiseMap);

TEXTURE2D(_ShadowCasterClipMap);
SAMPLER(sampler_ShadowCasterClipMap);

CBUFFER_START(UnityPerMaterial)
half4 _BaseColor;
float4 _BaseMap_ST;
float2 _BaseAnime;
half _Cutoff;

half3 _EmissionColor;
float4 _EmissionMap_ST;
float2 _EmissionAnime;

half4 _ShadowColor;
half _ShadowStep;
half _ShadowThreshold;
float4 _ShadowNoiseMap_ST;
float _ShadowNoiseSharpness;
half _ShadowNoiseStep;
half _ShadowNoiseThreshold;
half _ShadowNoiseStrength;

float4 _ShadowCasterClipMap_ST;
half _ShadowCasterCutoff;
CBUFFER_END

struct Attributes_Forward
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    half3 normalOS : NORMAL;
};

struct Varyings_Forward
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    half3 normalWS : TEXCOORD2;
};

struct Surface_Data
{
    half3 albedo;
    half alpha;
    half3 emission;
    half4 shadowColor;
    half shadowNoise;
};

struct Input_Data
{
    float3 positionWS;
    half3 normalWS;
    float4 shadowCoord;
    half shadowStep;
    half shadowThreshold;
    half shadowNoiseStrength;
};

#endif