#ifndef CUSTOM_TEXTURE_UTIL_INCLUDED
#define CUSTOM_TEXTURE_UTIL_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D_X_FLOAT(_CustomDepthTexture);
SAMPLER(sampler_CustomDepthTexture);

TEXTURE2D(_CustomViewNormalTexture);
sampler sampler_CustomViewNormalTexture;

TEXTURE2D(_CustomDepthNormalTexture);
sampler sampler_CustomDepthNormalTexture;

float DecodeFloatRG(float2 enc)
{
	float2 kDecodeDot = float2(1.0, 1 / 255.0);
	return dot(enc, kDecodeDot);
}

float3 DecodeViewNormalStereo(float4 enc4)
{
	float kScale = 1.7777;
	float3 nn = enc4.xyz * float3(2 * kScale, 2 * kScale, 0) + float3(-kScale, -kScale, 1);
	float g = 2.0 / dot(nn.xyz, nn.xyz);
	float3 n;
	n.xy = g * nn.xy;
	n.z = g - 1;
	return n;
} 

void DecodeDepthNormal(float4 enc, out float depth, out float3 normal)
{
	depth = DecodeFloatRG(enc.zw);
	normal = DecodeViewNormalStereo(enc);
}

float SampleCustomDepth(float2 uv)
{
	return SAMPLE_TEXTURE2D_X(_CustomDepthTexture, sampler_CustomDepthTexture, uv).x;
}

float LoadCustomDepth(uint2 uv)
{
	return LOAD_TEXTURE2D_X(_CustomDepthTexture, uv).x;
}

float3 SampleCustomViewNormal(float2 uv)
{
	return SAMPLE_TEXTURE2D(_CustomViewNormalTexture, sampler_CustomViewNormalTexture, uv).xyz;
}

float3 LoadCustomViewNormal(uint2 uv)
{
	return LOAD_TEXTURE2D(_CustomViewNormalTexture, uv).xyz;
}

void SampleCustomDepthNormal(float2 uv, out float depth, out float3 normalVS)
{
	float4 enc = SAMPLE_TEXTURE2D(_CustomDepthNormalTexture, sampler_CustomDepthNormalTexture, uv);
	DecodeDepthNormal(enc, depth, normalVS);
}

void LoadCustomDepthNormal(uint2 uv, out float depth, out float3 normalVS)
{
	float4 enc = LOAD_TEXTURE2D(_CustomDepthNormalTexture, uv);
	DecodeDepthNormal(enc, depth, normalVS);
}

float SampleCustomDepthNormal_Depth(float2 uv)
{
	float4 enc = SAMPLE_TEXTURE2D(_CustomDepthNormalTexture, sampler_CustomDepthNormalTexture, uv);
	return DecodeFloatRG(enc.zw);
}

float LoadCustomDepthNormal_Depth(uint2 uv)
{
	float4 enc = LOAD_TEXTURE2D(_CustomDepthNormalTexture, uv);
	return DecodeFloatRG(enc.zw);
}

float3 SampleCustomDepthNormal_NormalVS(float2 uv)
{
	float4 enc = SAMPLE_TEXTURE2D(_CustomDepthNormalTexture, sampler_CustomDepthNormalTexture, uv);
	return DecodeViewNormalStereo(enc);
}

float3 LoadCustomDepthNormal_NormalVS(uint2 uv)
{
	float4 enc = LOAD_TEXTURE2D(_CustomDepthNormalTexture, uv);
	return DecodeViewNormalStereo(enc);
}

#endif