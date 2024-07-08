Shader "Instance/Grass"
{
    Properties
    {
        [Group(Vertex, Feature, _BILLBOARD_ON)] _Billboard("Billboard", Float) = 0.0
        [Group(Vertex)]_Scale("Scale", Vector) = (1, 1, 1, 0)

        [Group(Surface)]_Cutoff("Alpha Threshold", Range(0, 1)) = 0.5
        [Group(Surface)]_BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Group(Surface, Ramp, _128)]_BaseRamp("Base Ramp", 2D) = "white" {}
        [Group(Surface)]_BaseMap("Base Map", 2D) = "white" {}
        [Group(Surface)]_BaseMapTile("Base Map Tile", Vector) = (1, 1, 0, 0)

        [Group(Environment)]_BackgroundBlendPower("Background Blend Power", Range(.01, 10)) = 1
        [Group(Environment)]_ShadowColor("Shadow Color", Color) = (.5, .5, .5, 1)

        [HideInInspector]_PositionOffset("Position Offset", Vector) = (0, 0, 0, 0)
    }

    CustomEditor "GroupBasedMaterialEditor"

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
        #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
        #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
        #pragma multi_compile_fragment _ _SHADOWS_SOFT
        #pragma multi_compile_fragment _ _LIGHT_LAYERS
        #pragma multi_compile_fragment _ _LIGHT_COOKIES
        #pragma multi_compile_instancing
        #pragma shader_feature_local_vertex _BILLBOARD_ON
        #include "ShaderLibrary/InstancingUtil.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        struct Attributes
        {
            float3 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 uv : TEXCOORD0;
            half4 color : TEXCOORD1;
            half3 background : TEXCOORD2;
        };

        TEXTURE2D(_BaseRamp);
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
        float3 _Scale;
        half _Cutoff;
        half4 _BaseColor;
        float4 _BaseMap_ST;
        float2 _BaseMapTile;

        half3 _ShadowColor;
        float _BackgroundBlendPower;

        //InstancingData
        StructuredBuffer<InstancingColor> _InstancingBuffer;
        float3 _PositionOffset;
        CBUFFER_END

        float2 CalcTileUV(float2 uv, float val)
        {
            float _val = floor(val);
            float2 len = 1 / _BaseMapTile;
            float x = _val % _BaseMapTile.x;
            float y = floor(_val / _BaseMapTile.x);
            y = y % _BaseMapTile.y;
            float2 tile = float2(x * len.x, y * len.y);
            return tile + len * uv;
        }

        half3 VertexLighting(float3 positionWS)
        {
            float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
            Light mainLight = GetMainLight(shadowCoord);
            return lerp(_ShadowColor, 1, mainLight.shadowAttenuation);
        }

        Varyings vert(Attributes IN, uint instanceID : SV_InstanceID)
        {
            Varyings OUT;
            InstancingColor buffer = _InstancingBuffer[instanceID];
            float3 positionOS = IN.positionOS * buffer.rotateScale.w * _Scale;
            Transform(positionOS, buffer.rotateScale.xyz);
#if defined(_BILLBOARD_ON)
            float3 cameraTransformRightWS = UNITY_MATRIX_V[0].xyz;
            float3 cameraTransformUpWS = UNITY_MATRIX_V[1].xyz;
            float3 _positionOS = positionOS.x * cameraTransformRightWS;
            _positionOS += positionOS.y * cameraTransformUpWS;
            positionOS = _positionOS;
#endif
            float3 positionWS = positionOS + buffer.positionWS.xyz + _PositionOffset;
            OUT.positionCS = TransformWorldToHClip(positionWS);
            OUT.uv.xy = IN.uv;
            OUT.uv.z = buffer.positionWS.w;
            OUT.color = _BaseColor;
            OUT.color.rgb *= VertexLighting(positionWS);
            OUT.background = buffer.color;
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            half4 ramp = SAMPLE_TEXTURE2D(_BaseRamp, sampler_LinearClamp, float2(IN.uv.y, 0));
            float2 uv = TRANSFORM_TEX(CalcTileUV(IN.uv.xy, IN.uv.z), _BaseMap);
            half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
            clip(albedo.a - _Cutoff);
            half4 col = IN.color * ramp;
            col.rgb = lerp(IN.background, col.rgb, pow(saturate(IN.uv.y), _BackgroundBlendPower));
            return col;
        }
        ENDHLSL

        Pass
        {
            Name "UniversalForward"
            Tags{ "LightMode" = "UniversalForward" }
            Cull Off ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}
