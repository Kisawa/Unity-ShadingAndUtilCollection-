Shader "Unlit/CommonEffect"
{
    Properties
    {
        [Group(Surface, Feature, _MAIN_POLAR)] _MainPolarOn("Main Polar On", Float) = 0
        [Group(Surface)]_VertexColorFactor("Vertex Color Factor", Range(0, 1)) = 1
        [Group(Surface)][HDR]_ColorTint("Color Tint", Color) = (1, 1, 1, 1)
        [Group(Surface)]_MainTex ("Texture", 2D) = "white" {}
        [Group(Surface)]_MainRotate("Main Rotate", Range(-1, 1)) = 0
        [Group(Surface)]_MainAnime("Main Anime", Vector) = (0, 0, 0, 0)
        [Group(Surface)]_MainDisturbFactor("Disturb Factor", Vector) = (.1, .1, 0, 0)
        [Group(Surface, Feature, _NONE, _MAIN_R, _MAIN_G, _MAIN_B, _MAIN_A, _FINAL_ALPHA)] _MainChannel("Main Channel", Float) = 0
        [Group(Surface, Ramp)]_MainRamp("Main Ramp", 2D) = "white"{}
        
        [Group(Disturb, Feature, _NONE, _DISTURB_R, _DISTURB_G, _DISTURB_B, _DISTURB_A)] _DisturbChannel("Disturb Channel", Float) = 0
        [Group(Disturb)]_DisturbMap("Disturb Map", 2D) = "black" {}
        [Group(Disturb, Feature, _DISTURB_POLAR)] _DisturbPolarOn("Disturb Polar On", Float) = 0
        [Group(Disturb)]_DisturbRotate("Disturb Rotate", Range(-1, 1)) = 0
        [Group(Disturb)]_DisturbAnime("Disturb Anime", Vector) = (0, 0, 0, 0)
        [Group(Disturb)]_DisturbStep("Disturb Step", Range(0, 1)) = 0
        [Group(Disturb)]_DisturbThreshold("Disturb Threshold", Range(0, 1)) = 1
        [Group(Disturb)]_DisturbStrength("Disturb Strength", Range(0, 1)) = .5

        [Group(Mask, Feature, _NONE, _MASK_R, _MASK_G, _MASK_B, _MASK_A)] _MaskChannel("Mask Channel", Float) = 0
        [Group(Mask)]_MaskMap("Mask Map", 2D) = "white" {}
        [Group(Mask, Feature, _MASK_POLAR)] _MaskPolarOn("Mask Polar On", Float) = 0
        [Group(Mask)]_MaskRotate("Mask Rotate", Range(-1, 1)) = 0
        [Group(Mask)]_MaskAnime("Mask Anime", Vector) = (0, 0, 0, 0)
        [Group(Mask)]_MaskDisturbFactor("Disturb Factor", Vector) = (0, 0, 0, 0)
        [Group(Mask)]_MaskStep("Mask Step", Range(0, 1)) = 0
        [Group(Mask)]_MaskThreshold("Mask Threshold", Range(0, 1)) = 1

        [Group(Mask Extra, Feature, _NONE, _MASK_EXTRA_R, _MASK_EXTRA_G, _MASK_EXTRA_B, _MASK_EXTRA_A)] _MaskExtraChannel("Mask Channel", Float) = 0
        [Group(Mask Extra)]_MaskExtraMap("Mask Map", 2D) = "white" {}
        [Group(Mask Extra, Feature, _MASK_EXTRA_POLAR)] _MaskExtraPolarOn("Mask Polar On", Float) = 0
        [Group(Mask Extra)]_MaskExtraRotate("Mask Rotate", Range(-1, 1)) = 0
        [Group(Mask Extra)]_MaskExtraAnime("Mask Anime", Vector) = (0, 0, 0, 0)
        [Group(Mask Extra)]_MaskExtraDisturbFactor("Disturb Factor", Vector) = (0, 0, 0, 0)
        [Group(Mask Extra)]_MaskExtraStep("Mask Step", Range(0, 1)) = 0
        [Group(Mask Extra)]_MaskExtraThreshold("Mask Threshold", Range(0, 1)) = 1

        [Group(Dissolve, Feature, _NONE, _DISSOLVE_R, _DISSOLVE_G, _DISSOLVE_B, _DISSOLVE_A)] _DissolveChannel("Dissolve Channel", Float) = 0
        [Group(Dissolve)]_DissolveMap("Dissolve Map", 2D) = "white" {}
        [Group(Dissolve, Feature, _DISSOLVE_POLAR)] _DissolvePolarOn("Dissolve Polar On", Float) = 0
        [Group(Dissolve)]_DissolveRotate("Dissolve Rotate", Range(-1, 1)) = 0
        [Group(Dissolve)]_DissolveAnime("Dissolve Anime", Vector) = (0, 0, 0, 0)
        [Group(Dissolve)]_DissolveDisturbFactor("Disturb Factor", Vector) = (0, 0, 0, 0)
        [Group(Dissolve)]_DissolveMixBackgroundUsage("Dissolve Mix Background Usage", Range(0, 1)) = 0
        [Group(Dissolve)][HDR]_DissolveEdgeColor("Dissolve Edge Color", Color) = (1, 1, 1, 0)
        [Group(Dissolve)]_DissolveSmooth("Dissolve Smooth", Range(0, 1)) = 0
        [Group(Dissolve)]_DissolveEdgeSmooth("Dissolve Edge Smooth", Range(0, 1)) = 1

        [Group(Math Mask, Feature, _NONE, _Caustic, _Perlin)]_MathMaskType("Math Mask Type", Float) = 0
        [Group(Math Mask, Feature, _NONE, _REFER_POSITION_WS)]_MathReferType("Math Refer Type", Float) = 0
        [Group(Math Mask)]_MathMaskFactor("Math Mask Factor", Vector) = (1, 1, 1, 0)
        [Group(Math Mask)]_MathMaskAnime("Math Mask Anime", Vector) = (0, 0, 0, 0)
        [Group(Math Mask)]_MathMaskStep("Mask Step", Range(0, 1)) = 0
        [Group(Math Mask)]_MathMaskThreshold("Mask Threshold", Range(0, 1)) = 1
        [Group(Math Mask)]_MathMaskDisturbFactor("Disturb Factor", Vector) = (0, 0, 0, 0)
        [Group(Math Mask)]_MathMaskStrength("Mask Strength", Range(0, 1)) = 1

        [Group(Vertex)]_VertexMultiplySize("Multiply Size", Vector) = (1, 1, 1, 0)
        [Group(Vertex)]_MultiplySizeVertexColorFactor("Multiply Size Vertex Color Factor", Vector) = (1, 1, 1, 1)

        [Group(Environment)]_FogFadeFactor("Fog Fade Factor", Range(0, 1)) = 1
        [Group(Environment)]_NearFadeStart("Near Fade Start", Float) = 0
        [Group(Environment)]_NearFadeEnd("Near Fade End", Float) = 0

        [Group(Setting)]_ZOffset("ZOffset", Range(-3, 3)) = 0

        [Queue]_Queue("Queue", Float) = 3
        [QueueOffset]_QueueOffset("Queue Offset", Float) = 0
        [Cull]_Cull("Cul", Float) = 0
        [ZTest]_ZTest("ZTest", Float) = 4
        [ZWrite]_ZWrite("ZWrite", Float) = 0
        [SrcBlend]_SrcBlend("SrcBlend", Float) = 5
        [DstBlend]_DstBlend("DstBlend", Float) = 10
        [BlendOp]_BlendOp("BlendOp", Float) = 0
    }

    CustomEditor "GroupBasedMaterialEditor"

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }

        HLSLINCLUDE
        #include "ShaderLibrary/TransformTools.hlsl"
        #include "ShaderLibrary/NoiseTools.hlsl"
        #include "ShaderLibrary/MathTools.hlsl"

        struct Attributes
        {
            float3 positionOS : POSITION;
            float2 uv : TEXCOORD0;
            float3 normalOS : NORMAL;
            half4 color : COLOR;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            half4 color : TEXCOORD1;
            float4 positionWS_fogFactor : TEXCOORD2;
            float3 normalWS : TEXCOORD3;
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_MainRamp);
        SAMPLER(sampler_LinearClamp);
        TEXTURE2D(_DisturbMap);
        SAMPLER(sampler_DisturbMap);
        TEXTURE2D(_MaskMap);
        SAMPLER(sampler_MaskMap);
        TEXTURE2D(_MaskExtraMap);
        SAMPLER(sampler_MaskExtraMap);
        TEXTURE2D(_DissolveMap);
        SAMPLER(sampler_DissolveMap);

        CBUFFER_START(UnityPerMaterial)
        float _ZOffset;
        float _CurvedStrength;

        half _VertexColorFactor;
        half4 _ColorTint;
        float4 _MainTex_ST;
        float _MainRotate;
        float2 _MainAnime;
        float2 _MainDisturbFactor;

        float4 _DisturbMap_ST;
        float _DisturbRotate;
        float2 _DisturbAnime;
        half _DisturbStep;
        half _DisturbThreshold;
        float _DisturbStrength;

        float4 _MaskMap_ST;
        float _MaskRotate;
        float2 _MaskAnime;
        half _MaskStep;
        half _MaskThreshold;
        float2 _MaskDisturbFactor;

        float4 _MaskExtraMap_ST;
        float _MaskExtraRotate;
        float2 _MaskExtraAnime;
        half _MaskExtraStep;
        half _MaskExtraThreshold;
        float2 _MaskExtraDisturbFactor;

        float4 _DissolveMap_ST;
        float _DissolveRotate;
        float2 _DissolveAnime;
        float2 _DissolveDisturbFactor;
        half _DissolveMixBackgroundUsage;
        half4 _DissolveEdgeColor;
        half _DissolveSmooth;
        half _DissolveEdgeSmooth;
        
        float4 _MathMaskFactor;
        float4 _MathMaskAnime;
        float _MathMaskStep;
        float _MathMaskThreshold;
        float3 _MathMaskDisturbFactor;
        float _MathMaskStrength;

        float3 _VertexMultiplySize;
        float4 _MultiplySizeVertexColorFactor;

        float _FogFadeFactor;
        float _NearFadeStart;
        float _NearFadeEnd;
        CBUFFER_END

        float2 Rotate2D(float2 val, float angle)
        {
            val = val * 2. - 1.;
            angle *= PI;
            float s = sin(angle);
            float c = cos(angle);
            return float2(val.x * c - val.y * s, val.x * s + val.y * c) * .5 + .5;
        }

        float2 PolarCoordinates(float2 val, float RadialScale, float LengthScale)
        {
            float2 delta = val - .5;
            float radius = length(delta) * 2. * RadialScale;
            float angle = atan2(delta.x, delta.y) * 1.0 / TWO_PI * LengthScale;
            return float2(radius, angle);
        }

        float2 TransformMainUV(float2 uv)
        {
            float2 val = Rotate2D(uv, _MainRotate);
            float2 anime = _MainAnime * _Time.y;
#if _MAIN_POLAR
            val = PolarCoordinates(val, _MainTex_ST.x, _MainTex_ST.y) + _MainTex_ST.zw + anime;
#else
            val = val * _MainTex_ST.xy + _MainTex_ST.zw + anime;
#endif
            return val;
        }

        float SampleDisturbStrength(float2 uv)
        {
            float2 val = Rotate2D(uv, _DisturbRotate);
            float2 anime = _DisturbAnime * _Time.x;
#if _DISTURB_POLAR
            val = PolarCoordinates(val, _DisturbMap_ST.x, _DisturbMap_ST.y) + _DisturbMap_ST.zw + anime;
#else
            val = val * _DisturbMap_ST.xy + _DisturbMap_ST.zw + anime;
#endif
            half4 disturbVal = SAMPLE_TEXTURE2D(_DisturbMap, sampler_DisturbMap, val);
            float disturb = 0;
#if _DISTURB_R
            disturb = disturbVal.r;
            disturb = smoothstep(_DisturbStep, _DisturbThreshold, disturb) - .5;
#elif _DISTURB_G
            disturb = disturbVal.g;
            disturb = smoothstep(_DisturbStep, _DisturbThreshold, disturb) - .5;
#elif _DISTURB_B
            disturb = disturbVal.b;
            disturb = smoothstep(_DisturbStep, _DisturbThreshold, disturb) - .5;
#elif _DISTURB_A
            disturb = disturbVal.a;
            disturb = smoothstep(_DisturbStep, _DisturbThreshold, disturb) - .5;
#endif
            return disturb * _DisturbStrength;
        }

        float2 TransformMaskUV(float2 uv)
        {
            float2 val = Rotate2D(uv, _MaskRotate);
            float2 anime = _MaskAnime * _Time.y;
#if _MASK_POLAR
            val = PolarCoordinates(val, _MaskMap_ST.x, _MaskMap_ST.y) + _MaskMap_ST.zw + anime;
#else
            val = val * _MaskMap_ST.xy + _MaskMap_ST.zw + anime;
#endif
            return val;
        }

        float2 TransformMaskExtraUV(float2 uv)
        {
            float2 val = Rotate2D(uv, _MaskExtraRotate);
            float2 anime = _MaskExtraAnime * _Time.y;
#if _MASK_EXTRA_POLAR
            val = PolarCoordinates(val, _MaskExtraMap_ST.x, _MaskExtraMap_ST.y) + _MaskExtraMap_ST.zw + anime;
#else
            val = val * _MaskExtraMap_ST.xy + _MaskExtraMap_ST.zw + anime;
#endif
            return val;
        }

        float2 TransformDissolveUV(float2 uv)
        {
            float2 val = Rotate2D(uv, _DissolveRotate);
            float2 anime = _DissolveAnime * _Time.x;
#if _DISSOLVE_POLAR
            val = PolarCoordinates(val, _DissolveMap_ST.x, _DissolveMap_ST.y) + _DissolveMap_ST.zw + anime;
#else
            val = val * _DissolveMap_ST.xy + _DissolveMap_ST.zw + anime;
#endif
            return val;
        }

        half4 SampleMainColor(float2 uv)
        {
            float2 mainUV = TransformMainUV(uv);
            half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mainUV);
#if _MAIN_R
            color = SAMPLE_TEXTURE2D(_MainRamp, sampler_LinearClamp, float2(color.r, 0));
#elif _MAIN_G
            color = SAMPLE_TEXTURE2D(_MainRamp, sampler_LinearClamp, float2(color.g, 0));
#elif _MAIN_B
            color = SAMPLE_TEXTURE2D(_MainRamp, sampler_LinearClamp, float2(color.b, 0));
#elif _MAIN_A
            color = SAMPLE_TEXTURE2D(_MainRamp, sampler_LinearClamp, float2(color.a, 0));
#endif
            return color;
        }

        void ApplyMask(float2 uv, inout half4 col)
        {
            half mask = 1;
            float2 maskUV = TransformMaskUV(uv);
            half4 maskVal = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, maskUV);
#if _MASK_R
            mask = maskVal.r;
#elif _MASK_G
            mask = maskVal.g;
#elif _MASK_B
            mask = maskVal.b;
#elif _MASK_A
            mask = maskVal.a;
#endif
            mask = smoothstep(_MaskStep, _MaskThreshold, mask);

            half maskExtra = 1;
            float2 maskExtraUV = TransformMaskExtraUV(uv);
            half4 maskExtraVal = SAMPLE_TEXTURE2D(_MaskExtraMap, sampler_MaskExtraMap, maskExtraUV);
#if _MASK_EXTRA_R
            maskExtra = maskExtraVal.r;
#elif _MASK_EXTRA_G
            maskExtra = maskExtraVal.g;
#elif _MASK_EXTRA_B
            maskExtra = maskExtraVal.b;
#elif _MASK_EXTRA_A
            maskExtra = maskExtraVal.a;
#endif
            maskExtra = smoothstep(_MaskExtraStep, _MaskExtraThreshold, maskExtra);

            col.a *= mask * maskExtra;
        }

        void ApplyDissolve(float2 uv, inout half4 col, float strength)
        {
            float2 dissolveUV = TransformDissolveUV(uv);
            half4 dissolveVal = SAMPLE_TEXTURE2D(_DissolveMap, sampler_DissolveMap, dissolveUV);
            float dissolve = 1;
#if _DISSOLVE_R
            dissolve = dissolveVal.r;
#elif _DISSOLVE_G
            dissolve = dissolveVal.g;
#elif _DISSOLVE_B
            dissolve = dissolveVal.b;
#elif _DISSOLVE_A
            dissolve = dissolveVal.a;
#endif
            float threshold0 = strength - lerp(_DissolveSmooth * .5, 0., clamp(strength - .5, 0., 1.) * 2.);
            float threshold1 = strength + lerp(_DissolveSmooth * .5, 0., clamp(.5 - strength, 0., 1.) * 2.);
            dissolve = smoothstep(threshold0, threshold1, dissolve);
            half3 edgeCol = lerp(_DissolveEdgeColor.rgb, _DissolveEdgeColor.rgb * col.rgb, _DissolveMixBackgroundUsage);
            col.rgb = lerp(edgeCol, col.rgb, dissolve);
            float alphaDissolve = smoothstep(0., _DissolveEdgeSmooth, dissolve);
            col.a *= lerp(alphaDissolve, 1., _DissolveEdgeColor.a);
        }

        void MixMathMask(float2 uv, float3 positionWS, float2 disturb, inout half4 col)
        {
            uv += disturb * _MathMaskDisturbFactor.xy;
            float3 refer = float3(uv.x, 0, uv.y);
#if _REFER_POSITION_WS
            refer = positionWS + float3(disturb.x, 0, disturb.y) * _MathMaskDisturbFactor;
#endif
            float noise = 1;
            float3 noiseFactor = refer / _MathMaskFactor.xyz + _MathMaskAnime.xyz * _Time.y;
#if _Caustic
            noise = GenshipCaustic(noiseFactor);
#elif _Perlin
            noise = PerlinNoise(noiseFactor);
#endif
            noise = smoothstep(_MathMaskStep, _MathMaskThreshold, noise);
            col.a *= lerp(1, noise, _MathMaskStrength);
        }

        void MixEnvironment(float4 positionWS_fogFactor, inout half4 col)
        {
            float nearToFarZ = max(positionWS_fogFactor.w - _ProjectionParams.y, 0);
#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
            float fogCoord = ComputeFogFactorZ0ToFar(nearToFarZ);
            if (IsFogEnabled())
            {
                float fogIntensity = ComputeFogIntensity(fogCoord);
                fogIntensity = lerp(1, fogIntensity, _FogFadeFactor);
                col.a *= fogIntensity;
            }
#endif
            float nearFadeRangeDistance = _NearFadeEnd - _NearFadeStart;
            float nearFadeIntensity = 1 - saturate(nearToFarZ / -nearFadeRangeDistance + _NearFadeEnd / nearFadeRangeDistance);
            col.a *= nearFadeIntensity;
        }

        Varyings vert(Attributes input)
        {
            Varyings output;
            float3 vertexColorFactor = lerp(1, input.color.xyz, _MultiplySizeVertexColorFactor.xyz);
            float3 positionOS = input.positionOS * lerp(1, _VertexMultiplySize, vertexColorFactor);
            float3 positionWS = TransformObjectToWorld(positionOS);
            output.positionCS = PositionCSWithZOffset(TransformWorldToHClip(positionWS), _ZOffset);
            output.uv = input.uv;
            output.color = input.color;
            output.color.rgb = lerp(1, input.color.rgb, _VertexColorFactor);
            output.positionWS_fogFactor = float4(positionWS, output.positionCS.w);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            return output;
        }

        half4 frag(Varyings input) : SV_Target
        {
            float2 uv = input.uv;
            float2 disturb = SampleDisturbStrength(uv);
            half4 col = SampleMainColor(uv + disturb * _MainDisturbFactor);
            ApplyMask(uv + disturb * _MaskDisturbFactor, col);
            col.rgb = col.rgb * input.color.rgb;
            col = col * _ColorTint;
#if _DISSOLVE_R || _DISSOLVE_G || _DISSOLVE_B || _DISSOLVE_A
            ApplyDissolve(uv + disturb * _DissolveDisturbFactor, col, 1. - input.color.a);
#else
            col.a *= input.color.a;
#endif 
            MixMathMask(uv, input.positionWS_fogFactor.xyz, disturb, col);
#if _FINAL_ALPHA
            col = SAMPLE_TEXTURE2D(_MainRamp, sampler_LinearClamp, float2(col.a, 0));
#endif
            MixEnvironment(input.positionWS_fogFactor, col);
            return col;
        }
        ENDHLSL

        Pass
        {
            Tags{ "LightMode" = "UniversalForward" }
            Cull [_Cull] ZTest [_ZTest] ZWrite [_ZWrite] Blend [_SrcBlend] [_DstBlend] BlendOp [_BlendOp]
            HLSLPROGRAM
            #pragma multi_compile_fog
            #pragma shader_feature_local_fragment _ _MAIN_R _MAIN_G _MAIN_B _MAIN_A _FINAL_ALPHA
            #pragma shader_feature_local_fragment _MAIN_POLAR
            #pragma shader_feature_local_fragment _ _DISTURB_R _DISTURB_G _DISTURB_B _DISTURB_A
            #pragma shader_feature_local_fragment _DISTURB_POLAR
            #pragma shader_feature_local_fragment _ _MASK_EXTRA_R _MASK_EXTRA_G _MASK_EXTRA_B _MASK_EXTRA_A
            #pragma shader_feature_local_fragment _MASK_EXTRA_POLAR
            #pragma shader_feature_local_fragment _ _MASK_R _MASK_G _MASK_B _MASK_A
            #pragma shader_feature_local_fragment _MASK_POLAR
            #pragma shader_feature_local_fragment _ _DISSOLVE_R _DISSOLVE_G _DISSOLVE_B _DISSOLVE_A
            #pragma shader_feature_local_fragment _DISSOLVE_POLAR
            #pragma shader_feature_local_fragment _ _Caustic _Perlin
            #pragma shader_feature_local_fragment _ _REFER_POSITION_WS
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}
