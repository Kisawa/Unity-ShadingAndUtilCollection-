#ifndef SSPR_TOOLS_INCLUDED
#define SSPR_TOOLS_INCLUDED

TEXTURE2D(_SSPRMap);
sampler LinearClampSampler;

half4 SampleSSPR(float2 screenUV)
{
	half4 col = SAMPLE_TEXTURE2D(_SSPRMap, LinearClampSampler, screenUV);
	return col;
}

#endif