#ifndef COMMON_PASS_INCLUDED
#define COMMON_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

float3 _LightDirection;
float3 _LightPosition;

struct Attributes_NULL
{
	float3 positionOS : POSITION;
};

struct Attributes_UV
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Attributes_Normal
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
};

struct Attributes_UV_Normal
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    float3 normalOS : NORMAL;
};

struct Attributes_All
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    half3 normalOS : NORMAL;
    half4 tangentOS : TANGENT;
    half4 color : COLOR0;
};

struct Varyings_NormalVS_Depth
{
    float4 positionCS : SV_POSITION;
    float4 normalVS_depth : TEXCOORD0;
};

struct Varyings_UV
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

struct Attributes_Meta
{
    float3 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv0          : TEXCOORD0;
    float2 uv1          : TEXCOORD1;
    float2 uv2          : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings_Meta
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
#ifdef EDITOR_VISUALIZATION
    float2 VizUV        : TEXCOORD1;
    float4 LightCoord   : TEXCOORD2;
#endif
};

float4 NullVertex(Attributes_NULL IN) : SV_Position
{
	return TransformObjectToHClip(IN.positionOS);
}

float4 ShadowCasterVertex(Attributes_Normal IN) : SV_Position
{
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
    return positionCS;
}

Varyings_NormalVS_Depth DepthViewNormalVertex(Attributes_Normal input)
{
    Varyings_NormalVS_Depth output;
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS);
    output.positionCS = vertexInput.positionCS;
    output.normalVS_depth.xyz = TransformWorldToViewDir(TransformObjectToWorldNormal(input.normalOS), true);
    output.normalVS_depth.w = -(vertexInput.positionVS.z * _ProjectionParams.w);
    return output;
}

Varyings_Meta Meta_Vertex(Attributes_Meta input)
{
    Varyings_Meta output;
    output.positionCS = UnityMetaVertexPosition(input.positionOS, input.uv1, input.uv2);
    output.uv = input.uv0;
#ifdef EDITOR_VISUALIZATION
    UnityEditorVizData(input.positionOS, input.uv0, input.uv1, input.uv2, output.VizUV, output.LightCoord);
#endif
    return output;
}

float4 ViewNormalFragment(Varyings_NormalVS_Depth input) : SV_Target
{
    float3 viewNormal = SafeNormalize(input.normalVS_depth.xyz);
    viewNormal = viewNormal * .5 + .5;
    return float4(viewNormal, 1);
}

float2 EncodeFloatRG(float v)
{
    float2 kEncodeMul = float2(1.0, 255.0);
    float kEncodeBit = 1.0 / 255.0;
    float2 enc = kEncodeMul * v;
    enc = frac(enc);
    enc.x -= enc.y * kEncodeBit;
    return enc;
}

float2 EncodeViewNormalStereo(float3 n)
{
    float kScale = 1.7777;
    float2 enc;
    enc = n.xy / (n.z + 1);
    enc /= kScale;
    enc = enc * 0.5 + 0.5;
    return enc;
}

float4 EncodeDepthNormal(float depth, float3 normal)
{
    float4 enc;
    enc.xy = EncodeViewNormalStereo(normal);
    enc.zw = EncodeFloatRG(depth);
    return enc;
}

float4 DepthViewNormalFragment(Varyings_NormalVS_Depth input) : SV_Target
{
    return EncodeDepthNormal(input.normalVS_depth.w, SafeNormalize(input.normalVS_depth.xyz));
}

void NullFragment() { }

#endif