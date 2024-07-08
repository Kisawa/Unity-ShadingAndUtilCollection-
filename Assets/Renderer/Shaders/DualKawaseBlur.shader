Shader "Postprocessing/DualKawaseBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSpread("Blur Spread", Range(0, 5)) = .5
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

        struct Varyings_DownSample
        {
            float4 positionCS : SV_POSITION;
            float2 uv: TEXCOORD0;
            float4 uv01 : TEXCOORD1;
            float4 uv23 : TEXCOORD2;
        };

        struct Varyings_UpSample
        {
            float4 positionCS : SV_POSITION;
            float4 uv01 : TEXCOORD0;
            float4 uv23 : TEXCOORD1;
            float4 uv45 : TEXCOORD2;
            float4 uv67 : TEXCOORD3;
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_TexelSize;
        float _BlurSpread;
        CBUFFER_END

        Varyings_DownSample DownSampleVertex(Attributes input)
        {
            Varyings_DownSample output;
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv = input.uv;
            output.uv01.xy = input.uv - _MainTex_TexelSize.xy * float2(_BlurSpread, _BlurSpread);
            output.uv01.zw = input.uv + _MainTex_TexelSize.xy * float2(_BlurSpread, _BlurSpread);
            output.uv23.xy = input.uv - _MainTex_TexelSize.xy * float2(_BlurSpread, -_BlurSpread);
            output.uv23.zw = input.uv + _MainTex_TexelSize.xy * float2(_BlurSpread, -_BlurSpread);
            return output;
        }

        half4 DownSampleBlurFragment(Varyings_DownSample input) : SV_Target
        {
            half4 sum = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * 4;
            sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv01.xy);
            sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv01.zw);
            sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv23.xy);
            sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv23.zw);
            return sum * 0.125;
        }

        Varyings_UpSample UpSampleVertex(Attributes input)
        {
            Varyings_UpSample output;
            output.positionCS = TransformObjectToHClip(input.positionOS);

            output.uv01.xy = input.uv + _MainTex_TexelSize.xy * float2(-_BlurSpread * 2, 0);
            output.uv01.zw = input.uv + _MainTex_TexelSize.xy * float2(-_BlurSpread, _BlurSpread);
            output.uv23.xy = input.uv + _MainTex_TexelSize.xy * float2(0, _BlurSpread * 2);
            output.uv23.zw = input.uv + _MainTex_TexelSize.xy * _BlurSpread;
            output.uv45.xy = input.uv + _MainTex_TexelSize.xy * float2(_BlurSpread * 2, 0);
            output.uv45.zw = input.uv + _MainTex_TexelSize.xy * float2(_BlurSpread, -_BlurSpread);
            output.uv67.xy = input.uv + _MainTex_TexelSize.xy * float2(0, -_BlurSpread * 2);
            output.uv67.zw = input.uv - _MainTex_TexelSize.xy * _BlurSpread;
            return output;
        }

        half4 UpSampleBlurFragment(Varyings_UpSample input) : SV_Target
        {
            half4 sum = 0;
            sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv01.xy);
            sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv01.zw) * 2;
            sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv23.xy);
            sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv23.zw) * 2;
            sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv45.xy);
            sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv45.zw) * 2;
            sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv67.xy);
            sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv67.zw) * 2;
            return sum * 0.0833;
        }
        ENDHLSL

        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex DownSampleVertex
            #pragma fragment DownSampleBlurFragment
            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex UpSampleVertex
            #pragma fragment UpSampleBlurFragment
            ENDHLSL
        }
    }
}