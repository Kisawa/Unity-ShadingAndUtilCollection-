#ifndef TILE_SAMPLE_TOOLS_INCLUDED
#define TILE_SAMPLE_TOOLS_INCLUDED

uint3 Rand3DPCG16(int3 p)
{
	uint3 v = uint3(p);
	v = v * 1664525u + 1013904223u;
	v.x += v.y * v.z;
	v.y += v.z * v.x;
	v.z += v.x * v.y;
	v.x += v.y * v.z;
	v.y += v.z * v.x;
	v.z += v.x * v.y;
	return v >> 16u;
}

half4 SampleTileMap(float2 uv, TEXTURE2D_PARAM(map, sampler_map), TEXTURE2D_PARAM(lut, sampler_lut), float jitter)
{
	struct
	{
		float2 hash2(int2 seed) { return Rand3DPCG16(int3(seed, 0x8a)) / (float)0x10000; }
		float4 erf(float4 x) 
		{
			x = abs(x);
			float4 p = (1 + (0.278393 + (0.230389 + (0.000972 + 0.078108 * x) * x) * x) * x);
			p *= p;
			p *= p;
			return 1 - 1 / p;
		}
	} util;

	const float sqrt3 = sqrt(3);
	const float2x2 T = float2x2(float2(1, 0), float2(0.5, sqrt3 * 0.5));
	const float2x2 T_1 = float2x2(float2(1, 0), float2(-1 / sqrt3, 2 / sqrt3));

	const float inv_scale = 1 / (sqrt3 + jitter);

	float2 Tuv = mul(uv, T);
	int2 iTuv = floor(Tuv);
	float2 fTuv = Tuv - iTuv;

	bool side = Tuv.x * Tuv.y > 0;
	bool upper = fTuv.x - fTuv.y < 0;

	int2	v1 = iTuv;
	int2 v2 = iTuv + 1;
	int2 v3 = iTuv + int2(!upper, upper);

	float2	uv1 = (uv - floor(mul(v1, T_1)) + jitter * util.hash2(v1)) * inv_scale;
	float2 uv2 = (uv - floor(mul(v2, T_1)) + jitter * util.hash2(v2)) * inv_scale;
	float2 uv3 = (uv - floor(mul(v3, T_1)) + jitter * util.hash2(v3)) * inv_scale;

	float4 g1 = SAMPLE_TEXTURE2D(map, sampler_map, uv1);
	float4 g2 = SAMPLE_TEXTURE2D(map, sampler_map, uv2);
	float4 g3 = SAMPLE_TEXTURE2D(map, sampler_map, uv3);

	float w3 = abs((fTuv.x - fTuv.y));
	float w1 = min((1 - fTuv.x), (1 - fTuv.y));
	float w2 = min((fTuv.x), (fTuv.y));

	float4 g = (w1 * g1 + w2 * g2 + w3 * g3 - 0.5)
		/ sqrt(w1 * w1 + w2 * w2 + w3 * w3) + 0.5;

	g = saturate(g);

	return float4(
		SAMPLE_TEXTURE2D(lut, sampler_lut, g.xx).x,
		SAMPLE_TEXTURE2D(lut, sampler_lut, g.yy).y,
		SAMPLE_TEXTURE2D(lut, sampler_lut, g.zz).z,
		SAMPLE_TEXTURE2D(lut, sampler_lut, g.ww).w
	);
}

#endif