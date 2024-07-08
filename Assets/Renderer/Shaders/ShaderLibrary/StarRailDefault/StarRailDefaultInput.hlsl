#ifndef DEFAULT_INPUT_STARRAIL_INCLUDED
#define DEFAULT_INPUT_STARRAIL_INCLUDED

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);

TEXTURE2D(_LightMap);
SAMPLER(sampler_LightMap);

TEXTURE2D(_MaskMap);
SAMPLER(sampler_MaskMap);

TEXTURE2D(_StockingsMap);
SAMPLER(sampler_StockingsMap);

TEXTURE2D(_StockingsRamp);
SAMPLER(sampler_StockingsRamp);

CBUFFER_START(UnityPerMaterial)
float _BackFaceOutlineZOffset;
half4 _BackFaceOutlineColor;
float _BackFaceOutlineWidth;
float _BackFaceOutlineFixMultiplier;

float _ZOffset;
half4 _BaseColor;
half4 _BackColor;
float4 _BaseMap_ST;
half _Cutoff;

half3 _EmissionColor;
float4 _EmissionMap_ST;

half _SHHardness;
half _GIStrength;
half _GIMixBaseColorUsage;
half _MainLightColorUsage;

float _ReceiveShadowBias;
half3 _ShadowColor;
half _ShadeStep;
half _ShadeSoftness;

half3 _SpecularColor;
half _SpecularExpon;
half _SpecularNonMetalKs;
half _SpecularMetalKs;

half3 _RimColor;
half _RimWidth;
half _RimThreshold;

half _AOStrength;
half _MetalThreshold;

float4 _StockingsMap_ST;
half _StockingsPower;
half _StockingsHardness;
half _StockingsUsage;

half3 _HeadForwardDirection;
half3 _HeadRightDirection;

float _Queue;
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
    half3 SH;
    half3 shadowColor;
    half ao;
    half aoStrength;
    half4 shadeFactor;  //smoothstep.x, smoothstep.y, step, softness
    half3 specular;
    half metal;
    half4 specularFactor;   //expon, nonMetalKs, metalKs, threshold(lightMap.b)
    half3 rimColor;
    float2 rimFactor;    //width, threshold, fadeout
    half4 stockingsFactor;      //fresnel power, fresnel hardness, detail, thickness
};

struct Input_Data
{
    float2 baseUV;
    float3 positionWS;
    half3 normalWS;
    half3 viewWS;
    float4 shadowCoord;
    float eyeDepth;
    half3 headForwardWS;
    half3 headRightWS;
    float2 screenUV;
};

#endif