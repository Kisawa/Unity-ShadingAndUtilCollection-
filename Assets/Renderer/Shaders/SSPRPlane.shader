Shader "Project/SSPRPlane"
{
    Properties
    {
        [Group(Surface)]_ColorTint("Color Tint", Color) = (1, 1, 1, 1)
        [Group(Surface)]_MainTex("Texture", 2D) = "white" {}
        [Group(Surface, Feature, _NONE, _GRAY_R, _GRAY_G, _GRAY_B, _GRAY_A)]_GrayType("Gray Type", Float) = 0.0
        [Group(Surface)]_GrayStep("Gray Step", Range(0, 1)) = 0
        [Group(Surface)]_GrayThreshold("Gray Threshold", Range(0, 1)) = 1

        [Group(SSPR)]_SSPRColorTint("Color Tint", Color) = (1, 1, 1, 1)
        [Group(SSPR)]_Fade("Fade", Range(0, 1)) = 1
        [Group(SSPR)]_Offset("Offset", Vector) = (0, 0, 0, 0)
        [Group(SSPR)]_NoiseMap("Noise Map", 2D) = "bump" {}
        [Group(SSPR)]_NoiseStrength("Noise Strength", Range(0, .1)) = .01
        [Group(SSPR)]_NoiseStep("Noise Step", Range(0, 1)) = 0
        [Group(SSPR)]_NoiseThreshold("Noise Threshold", Range(0, 1)) = 1
    }

    CustomEditor "GroupBasedMaterialEditor"

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "ShaderLibrary/CommonPass.hlsl"
        #include "ShaderLibrary/SSPRTools.hlsl"
        #pragma shader_feature_local_fragment _ _GRAY_R _GRAY_G _GRAY_B _GRAY_A

        struct Attributes
        {
            float3 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        TEXTURE2D(_NoiseMap);
        SAMPLER(sampler_NoiseMap);

        CBUFFER_START(UnityPerMaterial)
        half4 _ColorTint;
        float4 _MainTex_ST;
        half _GrayStep;
        half _GrayThreshold;

        half3 _SSPRColorTint;
        half _Fade;
        float2 _Offset;
        float4 _NoiseMap_ST;
        float _NoiseStrength;
        float _NoiseStep;
        float _NoiseThreshold;
        float _NoiseFadeStep;
        float _NoiseFadeThreshold;
        CBUFFER_END

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS);
            OUT.positionCS = vertexInput.positionCS;
            OUT.uv = IN.uv;
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            float2 baseUV = TRANSFORM_TEX(IN.uv, _MainTex);
            half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, baseUV);
#if defined(_GRAY_R)
            col = smoothstep(_GrayStep, _GrayThreshold, col.r) * _ColorTint;
#elif defined(_GRAY_G)
            col = smoothstep(_GrayStep, _GrayThreshold, col.g) * _ColorTint;
#elif defined(_GRAY_B)
            col = smoothstep(_GrayStep, _GrayThreshold, col.b) * _ColorTint;
#elif defined(_GRAY_A)
            col = smoothstep(_GrayStep, _GrayThreshold, col.a) * _ColorTint;
#else
            col = col * _ColorTint;
#endif
            float2 noiseUV = TRANSFORM_TEX(IN.uv, _NoiseMap);
            float2 noise = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noiseUV).xy;
            noise = smoothstep(_NoiseStep, _NoiseThreshold, noise) - .5;
            float2 screenUV = IN.positionCS.xy / _ScaledScreenParams.xy + _NoiseStrength * noise + _Offset;
            half4 reflect = SampleSSPR(screenUV);
            col.rgb = lerp(col.rgb, reflect.rgb * _SSPRColorTint, _Fade * reflect.a);
            return col;
        }
        ENDHLSL

        Pass
        {
            Name "AfterSSPR"
            Tags { "LightMode" = "AfterSSPR" }
            Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}