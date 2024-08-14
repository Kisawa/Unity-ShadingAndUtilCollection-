Shader "Unlit/TileDefault"
{
    Properties
    {
        _SeedMap("Base Map", 2D) = "white" {}
        [NoScaleOffset]_LutMap("Lut Map", 2D) = "white" {}
        _Jitter("Jitter", Range(0, 2)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent"}

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "ShaderLibrary/TileSampleTools.hlsl"

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

        TEXTURE2D(_SeedMap);
        SAMPLER(sampler_SeedMap);
        TEXTURE2D(_LutMap);
        SAMPLER(sampler_LinearClamp);

        CBUFFER_START(UnityPerMaterial)
        float4 _SeedMap_ST;
        float _Jitter;
        CBUFFER_END

        Varyings vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv = input.uv;
            return output;
        }

        half4 frag(Varyings input) : SV_Target
        {
            float2 uv = TRANSFORM_TEX(input.uv, _SeedMap);
            half4 col = SampleTileMap(uv, TEXTURE2D_ARGS(_SeedMap, sampler_SeedMap), TEXTURE2D_ARGS(_LutMap, sampler_LinearClamp), _Jitter);
            return col;
        }
        ENDHLSL

        Pass
        {
            Tags{ "LightMode" = "UniversalForward" }
            Cull Off ZTest LEqual ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}