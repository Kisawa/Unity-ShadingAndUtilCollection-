Shader "Postprocessing/GaussianBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSpread("Blur Spread", Float) = 1
    }
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float3 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv[5]: TEXCOORD0;
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_TexelSize;
        float _BlurSpread;
        CBUFFER_END

        Varyings Vertex_X(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv[0] = input.uv;
            output.uv[1] = input.uv + _MainTex_TexelSize.xy * float2(_BlurSpread, 0);
            output.uv[2] = input.uv - _MainTex_TexelSize.xy * float2(_BlurSpread, 0);
            output.uv[3] = input.uv + _MainTex_TexelSize.xy * float2(_BlurSpread * 2, 0);
            output.uv[4] = input.uv - _MainTex_TexelSize.xy * float2(_BlurSpread * 2, 0);
            return output;
        }

        Varyings Vertex_Y(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv[0] = input.uv;
            output.uv[1] = input.uv + _MainTex_TexelSize.xy * float2(0, _BlurSpread);
            output.uv[2] = input.uv - _MainTex_TexelSize.xy * float2(0, _BlurSpread);
            output.uv[3] = input.uv + _MainTex_TexelSize.xy * float2(0, _BlurSpread * 2);
            output.uv[4] = input.uv - _MainTex_TexelSize.xy * float2(0, _BlurSpread * 2);
            return output;
        }

        float3 GaussianBlur(float2 uv[5])
        {
            const float weight[3] = { 0.4026, 0.2442, 0.0545 };
            half3 result = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv[0]).xyz * weight[0];
            [unroll]
            for (int j = 1; j < 3; j++)
            {
                result += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv[2 * j]).xyz * weight[j];
                result += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv[2 * j - 1]).xyz * weight[j];
            }
            return result;
        }

        half4 Fragment(Varyings input) : SV_Target
        {
            half3 blurResult = GaussianBlur(input.uv);
            return half4(blurResult, 1);
        }
        ENDHLSL

        Cull Off ZWrite Off ZTest Always
        Pass
        {
            Name "GaussianBlur_X"
            HLSLPROGRAM
            #pragma vertex Vertex_X
            #pragma fragment Fragment
            ENDHLSL
        }

        Pass
        {
            Name "GaussianBlur_Y"
            HLSLPROGRAM
            #pragma vertex Vertex_Y
            #pragma fragment Fragment
            ENDHLSL
        }
    }
}