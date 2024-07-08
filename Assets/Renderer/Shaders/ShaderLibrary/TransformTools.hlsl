#ifndef TRANSFORM_TOOLS_INCLUDED
#define TRANSFORM_TOOLS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

inline float3 optimizedMul(float4x4 mat, float3 vec)
{
	return (mat._m00_m10_m20_m30 * vec.x + (mat._m01_m11_m21_m31 * vec.y + (mat._m02_m12_m22_m32 * vec.z + mat._m03_m13_m23_m33))).xyz;
}

inline float GetScreenAspect()
{
	return _ScreenParams.x / _ScreenParams.y;
}

float GetCameraFOV()
{
	//https://answers.unity.com/questions/770838/how-can-i-extract-the-fov-information-from-the-pro.html
	float t = unity_CameraProjection._m11;
	float Rad2Deg = 180 / 3.1415;
	float fov = atan(1.0f / t) * 2.0 * Rad2Deg;
	return fov;
}

float2 CalcSequenceUV(float2 uv, float2 size, float frame, bool invV = true)
{
	float x = frame / size.x;
	float y = floor(x) / size.y;
	float2 offset = fmod(float2(x, y), 1.);
	float2 tiling = 1. / size;
	offset.y = invV ? 1 - offset.y - tiling.y : offset.y;
	return uv * tiling + offset;
}

//url: https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample/blob/master/NiloZOffset.hlsl
float4 PositionCSWithZOffset(float4 originalPositionCS, float viewSpaceZOffsetAmount)
{
	if (unity_OrthoParams.w == 0)
	{
		//Perspective camera case
		float2 ProjM_ZRow_ZW = UNITY_MATRIX_P[2].zw;
		float modifiedPositionVS_Z = -originalPositionCS.w + -viewSpaceZOffsetAmount; // push imaginary vertex
		float modifiedPositionCS_Z = modifiedPositionVS_Z * ProjM_ZRow_ZW[0] + ProjM_ZRow_ZW[1];
		originalPositionCS.z = modifiedPositionCS_Z * originalPositionCS.w / (-modifiedPositionVS_Z); // overwrite positionCS.z
		return originalPositionCS;
	}
	else
	{
		//Orthographic camera case
		originalPositionCS.z += -viewSpaceZOffsetAmount / _ProjectionParams.z; // push imaginary vertex and overwrite positionCS.z
		return originalPositionCS;
	}
}

//url https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample/blob/master/NiloOutlineUtil.hlsl
float GetOutlineCameraFovAndDistanceFixMultiplier(float positionVS_Z)
{
	float cameraMulFix;
	if (unity_OrthoParams.w == 0)
	{
		// Perspective camera case
		cameraMulFix = saturate(abs(positionVS_Z)) * GetCameraFOV();
	}
	else
	{
		// Orthographic camera case
		cameraMulFix = saturate(abs(unity_OrthoParams.y)) * 50;
	}
	return cameraMulFix;
}

float4 PerspectiveCorrect(float4 positionCS, float3 anchorWS, half usage)
{
	float centerPosVS_z = TransformWorldToView(anchorWS).z;
	float2 newPosCS_xy = positionCS.xy;
	newPosCS_xy *= abs(positionCS.w);
	newPosCS_xy *= rcp(abs(centerPosVS_z));
	positionCS.xy = lerp(positionCS.xy, newPosCS_xy, usage);
	return positionCS;
}

//return positionVS;
float3 Billboard(float3 positionOS)
{
	float3 pivotVS = TransformWorldToView(TransformObjectToWorld(0));
	float2 scaleXY_WS = float2(
		length(float3(GetObjectToWorldMatrix()[0].x, GetObjectToWorldMatrix()[1].x, GetObjectToWorldMatrix()[2].x)), // scale x axis
		length(float3(GetObjectToWorldMatrix()[0].y, GetObjectToWorldMatrix()[1].y, GetObjectToWorldMatrix()[2].y)) // scale y axis
		);
	return pivotVS + float3(positionOS.xy * scaleXY_WS, 0);
}

//return positionOS;
//Keep the Scale_X and Z the same
float3 ViewerBillboard(float3 positionOS)
{
	float3 viewer = TransformWorldToObject(_WorldSpaceCameraPos);
	float3 right = normalize(cross(float3(0, 1, 0), viewer));
	float3 up = normalize(cross(right, viewer));
	return right * positionOS.x + up * positionOS.y + viewer * positionOS.z;
}

float2 CalcScreenUV(float4 positionCS)
{
	float2 screenUV = positionCS.xy / _ScreenParams.xy;
#if defined(UNITY_SINGLE_PASS_STEREO)
	screenUV.x *= .5;
#endif
	return screenUV;
}

float3 CalcPositionWS(float2 screenUV, float NDCDepth, float4x4 _InverseVP)
{
	float4 clipPos;
	clipPos.xy = screenUV * 2 - 1;
	clipPos.z = NDCDepth;
	clipPos.w = 1;
	float4 pos = mul(_InverseVP, clipPos);
	float3 positionWS = pos.xyz / pos.w;
	return positionWS;
}

float3 CalcPositionVS(float2 screenUV, float eyeDepth, float4x4 _P)
{
	float4 positionCS = float4((screenUV * 2 - 1) * eyeDepth, 1, eyeDepth);
#if UNITY_UV_STARTS_AT_TOP
	positionCS.y = -positionCS.y;
#endif
	float3 positionVS = positionCS.xyw / _P._m00_m11_m32;
	return positionVS;
}

float3 CalcPositionWS(float2 screenUV, float eyeDepth, float4x4 _P, float4x4 _InverseV)
{
	float3 positionVS = CalcPositionVS(screenUV, eyeDepth, _P);
	return optimizedMul(_InverseV, positionVS);
}

/*
 float tanFov = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
Vector2 invFocalLen = new Vector2(tanFov * camera.aspect, tanFov);
material.SetVector(_UVToView, invFocalLen);
*/
float3 CalcPositionVS(float2 screenUV, float depth01, float2 _UVToView)
{
	float2 ndc = screenUV * 2.0 - 1.0;
	float3 viewDir = float3(ndc * _UVToView, 1);
	return viewDir * depth01 * _ProjectionParams.z;
}

//TriplanarMapping(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), positionWS, normalWS, _Sharpness, _BaseMap_ST)
half4 TriplanarMapping(TEXTURE2D_PARAM(map, sampler_map), float3 positionWS, float3 normalWS, float sharpness, float4 tilingOffset = float4(1, 1, 0, 0))
{
	float2 uv_front = positionWS.xy * tilingOffset.xy + tilingOffset.zw;
	float2 uv_side = positionWS.zy * tilingOffset.xy + tilingOffset.zw;
	float2 uv_top = positionWS.xz * tilingOffset.xy + tilingOffset.zw;

	half4 col_front = SAMPLE_TEXTURE2D(map, sampler_map, uv_front);
	half4 col_side = SAMPLE_TEXTURE2D(map, sampler_map, uv_side);
	half4 col_top = SAMPLE_TEXTURE2D(map, sampler_map, uv_top);

	float3 weights = pow(abs(normalWS), sharpness);
	weights = weights / (weights.x + weights.y + weights.z);
	return col_front * weights.z + col_side * weights.x + col_top * weights.y;
}

float3 NormalFromHeight(float In, float Strength, float3 positionWS, float3 normalWS)
{
	float3 worldDerivativeX = ddx(positionWS);
	float3 worldDerivativeY = ddy(positionWS);

	float3 crossX = cross(normalWS, worldDerivativeX);
	float3 crossY = cross(worldDerivativeY, normalWS);
	float d = dot(worldDerivativeX, crossY);
	float sgn = d < 0.0 ? (-1.0f) : 1.0f;
	float surface = sgn / max(0.000000000000001192093f, abs(d));

	float dHdx = ddx(In);
	float dHdy = ddy(In);
	float3 surfGrad = surface * (dHdx * crossY + dHdy * crossX);
	return SafeNormalize(normalWS - (Strength * surfGrad));
}
#endif