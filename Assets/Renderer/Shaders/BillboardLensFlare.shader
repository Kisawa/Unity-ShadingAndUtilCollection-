Shader "Project/BillboardLensFlare"
{
    Properties
    {
        [HDR]_BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _BaseMap("BaseMap", 2D) = "white" {}
        [Space(10)]
        _VisibilityTestRadius("Visibility Test Radius", range(0, 1)) = .05
        _DepthTestBias("Depth Test Bias", range(-1, 1)) = -.001
        [Space(10)]
        _StartFadeinDistanceWorldUnit("Start Fadein Distance World Unit", Float) = .05
        _EndFadeinDistanceWorldUnit("End Fadein Distance World Unit", Float) = .5
        [Space(10)]
        _FlickerSpeed("Flicker Speed", Range(0, 5)) = 1
        _FlickerSeed("Flicker Seed", Range(0, 1)) = 0
        _FlickerFadeinMin("Flicker Fadein Min", Range(0, 1)) = 0
        _FlickerSwell("Flicker Swell", Range(0, 3)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Unlit" "RenderType" = "Overlay" "Queue" = "Overlay" "DisableBatching" = "True" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #pragma multi_compile_instancing
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float3 positionOS : POSITION;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv: TEXCOORD0;
            half4 color : TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        SAMPLER(_CameraDepthTexture);
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
        half4 _BaseColor;
        float4 _BaseMap_ST;
        float _VisibilityTestRadius;
        float _DepthTestBias;
        float _StartFadeinDistanceWorldUnit;
        float _EndFadeinDistanceWorldUnit;
        half _FlickerSpeed;
        half _FlickerSeed;
        half _FlickerFadeinMin;
        half _FlickerSwell;
        CBUFFER_END

        inline half remap(half num, half inMin, half inMax, half outMin, half outMax)
        {
            return outMin + (num - inMin) * (outMax - outMin) / (inMax - inMin);
        }

        inline half2 remap(half num, half inMin, half inMax, half2 outMin, half2 outMax)
        {
            return outMin + (num - inMin) * (outMax - outMin) / (inMax - inMin);
        }

        #define COUNT 8
        Varyings vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            half flickerSin = sin((_Time.y + _FlickerSeed) * _FlickerSpeed);
            //Billboard
            float3 pivotVS = TransformWorldToView(TransformObjectToWorld(0));
            float2 scaleXY_WS = float2(
                length(float3(GetObjectToWorldMatrix()[0].x, GetObjectToWorldMatrix()[1].x, GetObjectToWorldMatrix()[2].x)),
                length(float3(GetObjectToWorldMatrix()[0].y, GetObjectToWorldMatrix()[1].y, GetObjectToWorldMatrix()[2].y))
                );
            scaleXY_WS = remap(flickerSin, -1, 1, scaleXY_WS, _FlickerSwell * scaleXY_WS);
            float3 positionVS = pivotVS + float3(input.positionOS.xy * scaleXY_WS, 0);
            output.positionCS = TransformWViewToHClip(positionVS);

            //ClipTest
            float visibilityTestPassedCount = 0;
            float singleLoopCount = COUNT * 2 + 1;
            float totalCount = singleLoopCount * singleLoopCount;
            float offset = _VisibilityTestRadius / singleLoopCount;
            for (int x = -COUNT; x <= COUNT; x++)
            {
                for (int y = -COUNT; y <= COUNT; y++)
                {
                    float3 posVS = pivotVS;
                    posVS.xy += float2(x, y) * offset;
                    float4 posCS = TransformWViewToHClip(posVS);
                    float4 screenPos = ComputeScreenPos(posCS);
                    float2 screenUV = screenPos.xy / screenPos.w;
                    if (screenUV.x > 1 || screenUV.x < 0 || screenUV.y > 1 || screenUV.y < 0)
                        continue;
                    float sceneDepth = tex2Dlod(_CameraDepthTexture, float4(screenUV, 0, 0)).x;
                    float sceneLinearEyeDepth = LinearEyeDepth(sceneDepth, _ZBufferParams);
                    float linearEyeDepth = posCS.w;
                    visibilityTestPassedCount += linearEyeDepth + _DepthTestBias < sceneLinearEyeDepth ? 1 : 0;
                }
            }
            float divider = 1. / totalCount;
            float visibilityResult01 = visibilityTestPassedCount * divider;
            visibilityResult01 *= smoothstep(_StartFadeinDistanceWorldUnit, _EndFadeinDistanceWorldUnit, -pivotVS.z);
            output.positionCS = visibilityResult01 < divider ? 0 : output.positionCS;
            //color
            output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
            half4 col = _BaseColor;
            col.a *= visibilityResult01;
            col.a *= remap(flickerSin, -1, 1, _FlickerFadeinMin, 1);
            output.color = col;
            return output;
        }

        half4 frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * input.color;
            return col;
        }
        ENDHLSL

        Pass
        {
            Cull Off ZWrite Off ZTest Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}
