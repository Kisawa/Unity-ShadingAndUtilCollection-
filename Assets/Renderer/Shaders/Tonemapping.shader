Shader "Postprocessing/Tonemapping"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            float2 uv: TEXCOORD0;
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        Varyings vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv = input.uv;
            return output;
        }

        half3 Tonemap(half3 color)
        {
            half3 c0 = (1.36 * color + 0.047) * color;
            half3 c1 = (0.93 * color + 0.56) * color + 0.14;
            return saturate(c0 / c1);
        }

        half4 frag(Varyings input) : SV_Target
        {
            half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
            col.rgb = Tonemap(col.rgb);
            return col;
        }
        ENDHLSL

        Pass
        {
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}