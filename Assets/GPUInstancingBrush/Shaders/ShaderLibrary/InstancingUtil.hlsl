#ifndef INSTANCING_UTIL_INCLUDED
#define INSTANCING_UTIL_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Instancing
{
	float4 positionWS;
	float4 rotateScale;
};

struct InstancingColor
{
	float4 positionWS;
	float4 rotateScale;
	float3 color;
};

float3 BillboardVert(float3 positionOS)
{
	float3 cameraTransformRightWS = UNITY_MATRIX_V[0].xyz;
	float3 cameraTransformUpWS = UNITY_MATRIX_V[1].xyz;
	float3 cameraTransformForwardWS = -UNITY_MATRIX_V[2].xyz;
	float3 _positionOS = positionOS.x * cameraTransformRightWS;
	_positionOS += positionOS.y * cameraTransformUpWS;
	return _positionOS;
}

void Transform(inout float3 positionOS, inout half3 normalOS, float3 rotate)
{
	float sx, cx, sy, cy, sz, cz;
	sincos(rotate.x, sx, cx);
	sincos(rotate.y, sy, cy);
	sincos(rotate.z, sz, cz);
	float4x4 mat_x = float4x4(1, 0, 0, 0,
		0, cx, -sx, 0,
		0, sx, cx, 0,
		0, 0, 0, 1);

	float4x4 mat_y = float4x4(cy, 0, sy, 0,
		0, 1, 0, 0,
		-sy, 0, cy, 0,
		0, 0, 0, 1);

	float4x4 mat_z = float4x4(cz, -sz, 0, 0,
		sz, cz, 0, 0,
		0, 0, 1, 0,
		0, 0, 0, 1);

	float4x4 mat = mul(mul(mat_y, mat_x), mat_z);
	positionOS = mul(mat, float4(positionOS, 1)).xyz;
	normalOS = normalize(mul((float3x3)mat, normalOS));
}

void Transform(inout float3 positionOS, float3 rotate)
{
	float sx, cx, sy, cy, sz, cz;
	sincos(rotate.x, sx, cx);
	sincos(rotate.y, sy, cy);
	sincos(rotate.z, sz, cz);
	float4x4 mat_x = float4x4(1, 0, 0, 0,
		0, cx, -sx, 0,
		0, sx, cx, 0,
		0, 0, 0, 1);

	float4x4 mat_y = float4x4(cy, 0, sy, 0,
		0, 1, 0, 0,
		-sy, 0, cy, 0,
		0, 0, 0, 1);

	float4x4 mat_z = float4x4(cz, -sz, 0, 0,
		sz, cz, 0, 0,
		0, 0, 1, 0,
		0, 0, 0, 1);

	float4x4 mat = mul(mul(mat_y, mat_x), mat_z);
	positionOS = mul(mat, float4(positionOS, 1)).xyz;
}

#endif