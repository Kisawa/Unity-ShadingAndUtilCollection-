Shader "Project/SpotVolumeLight"
{
    Properties
    {
        [PerRendererData] _LightIndex("Light Index", Int) = 0
        [PerRendererData][HDR]_ColorTint("Color Tint", Color) = (1, 1, 1, 1)
        [PerRendererData][HDR]_Color("Color", Color) = (1, 1, 1, 1)
        [PerRendererData]_ShapeSoftness("Shape Softness", Range(0, 1)) = 1
        [PerRendererData]_ScatterStrength("Scatter Strength", Range(0, 5)) = 3
        [PerRendererData]_CookieSmoothness("Cookie Smoothness", Range(0, 1)) = .5
        [PerRendererData]_IlluminantRadius("Illuminant Radius", Range(0, .05)) = 0
        [PerRendererData]_FadeOutDistance("FadeOut Distance", Float) = 10
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "ShaderLibrary/TransformTools.hlsl"
        #include "ShaderLibrary/MathTools.hlsl"

        struct Attributes
        {
            float3 positionOS : POSITION;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionOS : TEXCOORD1;
            float3 viewOS : TEXCOORD2;
        };

        CBUFFER_START(UnityPerMaterial)
        half4 _ColorTint;
        half4 _Color;
        half _ShapeSoftness;
        half _ScatterStrength;
        half _CookieSmoothness;
        float _IlluminantRadius;
        float _FadeOutDistance;
        int _LightIndex;
        CBUFFER_END

        half4 coneIntersect(half3 ro, half3 rd, half3 pa, half3 pb, half ra, half rb)
        {
            half3  ba = pb - pa;
            half3  oa = ro - pa;
            half3  ob = ro - pb;
            half m0 = dot(ba, ba);
            half m1 = dot(oa, ba);
            half m2 = dot(rd, ba);
            half m3 = dot(rd, oa);
            half m5 = dot(oa, oa);
            half m9 = dot(ob, ba);
            // caps
            if (m1 < 0.0)
            {
                if (dot2(oa * m2 - rd * m1) < (ra * ra * m2 * m2)) // delayed division
                    return half4(-m1 / m2, -ba * rsqrt(m0));
            }
            else if (m9 > 0.0)
            {
                float t = -m9 / m2;                     // NOT delayed division
                if (dot2(ob + rd * t) < (rb * rb))
                    return half4(t, ba * rsqrt(m0));
            }

            // body
            half rr = ra - rb;
            half hy = m0 + rr * rr;
            half k2 = m0 * m0 - m2 * m2 * hy;
            half k1 = m0 * m0 * m3 - m1 * m2 * hy + m0 * ra * (rr * m2 * 1.0);
            half k0 = m0 * m0 * m5 - m1 * m1 * hy + m0 * ra * (rr * m1 * 2.0 - m0 * ra);
            half h = k1 * k1 - k2 * k0;

            half t = (-k1 - sqrt(h)) / k2;
            half y = m1 + t * m2;
            if (h < 0) return -1; //no intersection
            
            if (y<0 || y>m0) return -1; //no intersection
            return half4(t, normalize(m0 * (m0 * (oa + t * rd) + rr * ba * ra) - ba * hy * y));
        }

        float InScatter(float3 start, float3 rd, float3 lightPos, float d)
        {
            float3 q = start - lightPos;
            float b = dot(rd, q);
            float c = dot(q, q);
            float iv = 1.0f / sqrt(c - b * b);
            float l = iv * (atan((d + b) * iv) - atan(b * iv));
            return l;
        }

        Varyings vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.positionOS = input.positionOS;
            output.viewOS = input.positionOS - TransformWorldToObject(_WorldSpaceCameraPos);
            return output;
        }

        #define STEP 64
        half4 frag(Varyings input) : SV_Target
        {
            const float3 POINT0 = float3(0, 0, .999);
            const float3 POINT1 = float3(0, 0, .001);
            const float RADIUS = .49;
            float3 origin = input.positionOS;
            float3 dir = normalize(input.viewOS);
            float2 screenUV = input.positionCS.xy / _ScaledScreenParams.xy;
            float sceneDepth = SampleSceneDepth(screenUV);

            half shape = 0;
            half3 cookieCol = 0;
            float radiusSpan = RADIUS - _IlluminantRadius;
            [loop]
            for (int i = 0; i < STEP; i++)
            {
                float radius = _IlluminantRadius + radiusSpan * i / STEP;
                float4 cone = coneIntersect(origin, dir, POINT0, POINT1, radius, _IlluminantRadius);
                if (cone.x > 0)
                {
                    float3 positionOS = origin + cone.x * dir;
                    float4 positionCS = TransformObjectToHClip(positionOS);
                    float depth = positionCS.z / positionCS.w;
                    if (depth > sceneDepth)
                    {
                        half shadow = 1;
                        half3 cookieColor = 1;
#if defined(_ADDITIONAL_LIGHT_SHADOWS)
                        float3 positionWS = TransformObjectToWorld(positionOS);
                        shadow = AdditionalLightRealtimeShadow(_LightIndex, positionWS, 0);
#if defined(_LIGHT_COOKIES)
                        cookieColor = SampleAdditionalLightCookie(_LightIndex, positionWS);
#endif
#endif
                        cookieCol += cookieColor;
                        half fadeout = smoothstep(0, _FadeOutDistance, positionCS.w);
                        half3 lightDirection = half3(0, 0, 1);
                        half cosThet = -dot(lightDirection, cone.yzw);
                        shape += cos(acos(cosThet) - HALF_PI) * fadeout * shadow;
                    }
                }
            }
            shape = smoothstep(0, _ShapeSoftness, shape / STEP);
            const float3 LIGHT_OFFSET = float3(0, 0, .1);
            half scatter = max(0, InScatter(origin + POINT1, -dir, LIGHT_OFFSET, _ScatterStrength));
            cookieCol = lerp(cookieCol / STEP, 1, saturate(scatter) * _CookieSmoothness);
            return half4(_Color.rgb * _ColorTint.rgb * scatter * cookieCol, _Color.a * _ColorTint.a * shape);
        }
        ENDHLSL

        Pass
        {
            Cull Off ZWrite Off ZTest Off Blend SrcAlpha One
            HLSLPROGRAM
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}