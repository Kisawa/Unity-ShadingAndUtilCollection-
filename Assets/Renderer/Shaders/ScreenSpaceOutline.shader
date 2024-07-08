Shader "Postprocessing/ScreenSpaceOutline"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _OutlineColorTint("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineSaturation("Outline Saturation", Float) = 0
        _OutlineContrast("Outline Contrast", Float) = 0
        _OutlineScale("Outline Scale", Float) = 0
        _RobertsCrossMultiplier("Roberts Cross Multiplier", Float) = 0
        _DepthThreshold("Depth Threshold", Float) = 0
        _NormalThreshold("Normal Threshold", Float) = 0
        _SteepAngleThreshold("Steep Angle Threshold", Float) = 0
        _SteepAngleMultiplier("Steep Angle Multiplier", Float) = 0
    }

        SubShader
        {
            HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ShaderLibrary/MathTools.hlsl"
            #include "ShaderLibrary/ColorTools.hlsl"
            #include "ShaderLibrary/CustomTextureUtil.hlsl"
            #pragma multi_compile_fog
            #pragma multi_compile _ _DEBUGOUTLINE

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv: TEXCOORD0;
                float3 positionVS : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_TexelSize;
            half4 _OutlineColorTint;
            half _OutlineSaturation;
            half _OutlineContrast;
            float _OutlineScale;
            float _RobertsCrossMultiplier;
            float _DepthThreshold;
            float _NormalThreshold;
            float _SteepAngleThreshold;
            float _SteepAngleMultiplier;
            CBUFFER_END

            void GetCorssSampleUVs(float2 uv, float2 texelSize, float offsetMultiplier, out float2 topRightUV, out float2 topLeftUV, out float2 bottomRightUV, out float2 bottomLeftUV)
            {
                topRightUV = uv + texelSize * offsetMultiplier;
                topLeftUV = uv + float2(-texelSize.x, texelSize.y) * offsetMultiplier;
                bottomRightUV = uv + float2(texelSize.x, -texelSize.y) * offsetMultiplier;
                bottomLeftUV = uv - texelSize * offsetMultiplier;
            }

            float CalcSteepAngleMask(float3 normal, float3 positionVS, float steepAngleThreshold, float steepAngleMultiplier)
            {
                float NdotV = 1 - dot(normal, remap(0, 1, 1, -1, positionVS));
                float mask = smoothstep(_SteepAngleThreshold, 2, NdotV) * steepAngleMultiplier + 1;
                return mask;
            }

            float CalcDepthOutline(float robertsCrossMultiplier, float depthThreshold, float steepAngleMask, float originDepth, float topRightDepth, float topLeftDepth, float bottomRightDepth, float bottomLeftDepth)
            {
                float DIF0 = pow2(topRightDepth - bottomLeftDepth);
                float DIF1 = pow2(topLeftDepth - bottomRightDepth);
                float res = sqrt(DIF0 + DIF1) * robertsCrossMultiplier;
                res = step(originDepth * depthThreshold * steepAngleMask, res);
                return res;
            }

            float CalcNormalOutline(float normalThreshold, float3 topRightNormal, float3 topLeftNormal, float3 bottomRightNormal, float3 bottomLeftNormal)
            {
                float3 sub0 = topRightNormal - bottomLeftNormal;
                float DIF0 = dot(sub0, sub0);
                float3 sub1 = topLeftNormal - bottomRightNormal;
                float DIF1 = dot(sub1, sub1);
                float res = sqrt(DIF0 + DIF1);
                res = step(normalThreshold, res);
                return res;
            }

            void OutlineMixFog(inout half3 outline, float depth01)
            {
                float clipZ_0Far = depth01 * _ProjectionParams.z - _ProjectionParams.y;
                float fogFactor = ComputeFogFactorZ0ToFar(clipZ_0Far);
                outline.rgb = MixFog(outline.rgb, fogFactor);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS);
                output.positionCS = vertexInput.positionCS;
                output.positionVS = vertexInput.positionVS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 topRightUV, topLeftUV, bottomRightUV, bottomLeftUV;
                GetCorssSampleUVs(uv, _MainTex_TexelSize.xy, _OutlineScale, topRightUV, topLeftUV, bottomRightUV, bottomLeftUV);

                float originDepth, topRightDepth, topLeftDepth, bottomRightDepth, bottomLeftDepth;
                float3 originNormal, topRightNormal, topLeftNormal, bottomRightNormal, bottomLeftNormal;
                SampleCustomDepthNormal(uv, originDepth, originNormal);
                SampleCustomDepthNormal(topRightUV, topRightDepth, topRightNormal);
                SampleCustomDepthNormal(topLeftUV, topLeftDepth, topLeftNormal);
                SampleCustomDepthNormal(bottomRightUV, bottomRightDepth, bottomRightNormal);
                SampleCustomDepthNormal(bottomLeftUV, bottomLeftDepth, bottomLeftNormal);

                float steepAngleMask = CalcSteepAngleMask(originNormal, input.positionVS, _SteepAngleThreshold, _SteepAngleMultiplier);
                float depthOutline = CalcDepthOutline(_RobertsCrossMultiplier, _DepthThreshold, steepAngleMask, originDepth, topRightDepth, topLeftDepth, bottomRightDepth, bottomLeftDepth);
                float normalOutline = CalcNormalOutline(_NormalThreshold, topRightNormal, topLeftNormal, bottomRightNormal, bottomLeftNormal);
                float outline = max(depthOutline, normalOutline);

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 outlineCol = _OutlineColorTint;
                outlineCol.rgb = Saturation(col.rgb * outlineCol.rgb, _OutlineSaturation);
                outlineCol.rgb = Contrast(outlineCol.rgb, _OutlineContrast);
    #if _DEBUGOUTLINE
                col = lerp(1, outlineCol, outline);
    #else
                OutlineMixFog(outlineCol.rgb, originDepth);
                col = lerp(col, outlineCol, outline);
    #endif
                return col;
            }
            ENDHLSL

            Cull Off ZWrite Off ZTest Always

            Pass
            {
                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                ENDHLSL
            }
        }
}