Shader "Postprocessing/SunShaft"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        HLSLINCLUDE
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
        #pragma multi_compile_fragment _ _SHADOWS_SOFT
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "ShaderLibrary/TransformTools.hlsl"

        struct Attributes
        {
            float3 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv: TEXCOORD0;
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        TEXTURE2D(_BaseTex);
        SAMPLER(sampler_BaseTex);

        TEXTURE2D_X_FLOAT(_BlueNoise);
        SAMPLER(sampler_BlueNoise);

        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_TexelSize;
        half4 _ColorTint;
        float _MieG;
        float _ExtictionFactor;
        float _ShadowStrength;
        float4 _BlueNoise_TexelSize;

        float4x4 _InverseVP;
        CBUFFER_END

        #define random(seed) sin(seed * 641.5467987313875 + 1.943856175)

        float ComputePhaseMie(float theta, float g)
        {
            float g2 = g * g;
            return (1.0 - g2) / pow(1.0 + g2 - 2.0 * g * saturate(theta), 1.5) / (4.0 * PI);
        }

        float ExtingctionFunc(float stepLength, float extictionFactor, inout float extinction)
        {
            extinction += extictionFactor * stepLength;
            return exp(-extinction);
        }

        #define COUNT 32
        float2 RayMarching(float3 start, float3 dir, float len, float noise)
        {
            float stepLen = len / COUNT;

            float3 pos = start + dir * stepLen * noise;
            float2 atten = 0;
            float extinction = 0;
            [unroll]
            for (int i = 0; i < COUNT; i++, pos += dir * stepLen)
            {
                float4 shadowCoord = TransformWorldToShadowCoord(pos);
                half shadow = step(.9, MainLightRealtimeShadow(shadowCoord));
                atten.y += shadow;
                shadow *= ExtingctionFunc(stepLen, _ExtictionFactor, extinction);
                atten.x += shadow;
            }
            return atten / COUNT;
        }

        Varyings vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv = input.uv;
            return output;
        }

        half4 frag_light(Varyings input) : SV_Target
        {
            float depth = SampleSceneDepth(input.uv);
            float3 positionWS = CalcPositionWS(input.uv, depth, _InverseVP);
            float3 camPositionWS = GetCameraPositionWS();
            float3 dir = normalize(positionWS - camPositionWS);
            float len = distance(positionWS, camPositionWS);
            float2 noiseUV = input.uv * _MainTex_TexelSize.zw * _BlueNoise_TexelSize.xy;
            float noise = SAMPLE_TEXTURE2D_LOD(_BlueNoise, sampler_BlueNoise, noiseUV, 0).a;
            float phaseTheta = dot(dir, _MainLightPosition.xyz);
            float phaseMie = ComputePhaseMie(phaseTheta, _MieG);

            float2 res = RayMarching(camPositionWS, dir, len, noise);
            res.x *= phaseMie;
            half4 col = half4(res, 0, 0);
            return col;
        }

        half4 frag_blend(Varyings input) : SV_Target
        {
            half4 col = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, input.uv);
            half2 light = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rg;
            float fade = smoothstep(0, .1, _MainLightPosition.y);
            col.rgb += light.x * _ColorTint.rgb * _ColorTint.a * fade;
            col.rgb *= lerp(1, light.y, _ShadowStrength * fade);
            return col;
        }
        ENDHLSL

        Pass
        {
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_light
            ENDHLSL
        }

        Pass
        {
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_blend
            ENDHLSL
        }
    }
}