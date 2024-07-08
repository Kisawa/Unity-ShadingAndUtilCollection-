Shader "Postprocessing/GTAO"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
		HLSLINCLUDE
		#pragma shader_feature_local_fragment _ _VIEW_ORIGIN _VIEW_AO
		#pragma shader_feature_local_fragment _ _USE_CAMERADEPTH_CUSTOMVIEWNORMAL _USE_CUSTOMDEPTH_CUSTOMVIEWNORMAL
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
		#include "ShaderLibrary/CustomTextureUtil.hlsl"

        struct appdata
        {
            float3 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        struct v2f
        {
			float4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        v2f vert (appdata v)
        {
            v2f o;
            o.pos = TransformObjectToHClip(v.vertex);
            o.uv = v.texcoord;
            return o;
        }

		TEXTURE2D(_MainTex);
		sampler sampler_MainTex;

		float4 _TexelSize;
		float2 _UVToView;
		float _ProjScale;
		int _SampleDirectionCount;
		float _SampleRadius;
		int _SampleStep;
		float _AOPower;
		float _AOThickness;
		float _AOCompactness;

		void SampleDepthNormal(float2 uv, out float depth01, out float3 viewNormal)
		{
			float4 depthNormal = SAMPLE_TEXTURE2D(_CustomDepthNormalTexture, sampler_CustomDepthNormalTexture, uv);
			DecodeDepthNormal(depthNormal, depth01, viewNormal);
		}

		float SampleDepth01(float2 uv)
		{
			return DecodeFloatRG(SAMPLE_TEXTURE2D(_CustomDepthNormalTexture, sampler_CustomDepthNormalTexture, uv).zw);
		}
		
		inline float3 CalcViewPos(float2 uv, float depth01)
		{
			float2 ndc = uv * 2.0 - 1.0;
			float3 viewDir = float3(ndc * _UVToView, 1);
			return viewDir * depth01 * _ProjectionParams.z;
		}

		inline float3 GetViewPos(float2 uv)
		{
			return CalcViewPos(uv, SampleDepth01(uv));
		}

		inline float IntegrateArc_UniformWeight(float2 h)
		{
			float2 Arc = 1 - cos(h);
			return Arc.x + Arc.y;
		}

		inline float IntegrateArc_CosWeight(float2 h, float n)
		{
			float2 Arc = -cos(2 * h - n) + cos(n) + 2 * h * sin(n);
			return 0.25 * (Arc.x + Arc.y);
		}

		inline float GetRandom(float2 uv)
		{
			return frac(sin(dot(uv, float2(12.9898, 78.233) * 3.0)) * 43758.5453);
		}

		inline float GTAO_Noise(float2 position)
		{
			return frac(52.9829189 * frac(dot(position, float2( 0.06711056, 0.00583715))));
		}

		inline half3 MultiBounce(half AO, half3 Albedo)
		{
			half3 A = 2 * Albedo - 0.33;
			half3 B = -4.8 * Albedo + 0.64;
			half3 C = 2.75 * Albedo + 0.69;
			return max(AO, ((AO * A + B) * AO + C) * AO);
		}

		half4 frag_AO (v2f i) : SV_Target
        {
			float2 uv = i.uv;
			float3 viewNormal, BentNormal;
			float depth01, Occlusion;
			SampleDepthNormal(i.uv, depth01, viewNormal);
			float3 viewPos = CalcViewPos(i.uv, depth01);
			viewNormal.z *= -1;
			float3 viewDir = -normalize(viewPos);
			
			float stepRadius = _SampleRadius * _ProjScale / (viewPos.z * (float)_SampleStep * _AOCompactness);
			half jitter = GetRandom(uv) * stepRadius;
			float thetaAngle = PI / (float)_SampleDirectionCount;
			float startAngle = GTAO_Noise(uv * _TexelSize.zw);
			UNITY_LOOP
			for (int ii = 0; ii < _SampleDirectionCount; ii++)
			{
				float angle = startAngle + ii * thetaAngle;
				float3 sliceDir = float3(float2(cos(angle), sin(angle)), 0);
				float2 slideDir_TexelSize = sliceDir.xy * _TexelSize.xy;
				float2 h = -1;

				UNITY_LOOP
				for (int j = 0; j < _SampleStep; j++)
				{
					float2 uvOffset = slideDir_TexelSize * max(stepRadius * j + jitter, j + 1);
					float4 uvSlice = uv.xyxy + float4(uvOffset, -uvOffset);

					float3 dir0 = GetViewPos(uvSlice.xy) - viewPos;
					float3 dir1 = GetViewPos(uvSlice.zw) - viewPos;

					float2 squareLen = float2(dot(dir0, dir0), dot(dir1, dir1));
					float2 invLength = rsqrt(squareLen);

					float2 falloff = saturate(squareLen / (_SampleRadius * _SampleRadius));

					float2 dotView = float2(dot(dir0, viewDir), dot(dir1, viewDir)) * invLength;
					h = (dotView > h) ? lerp(dotView, h, falloff) : lerp(dotView, h, _AOThickness);
				}

				float3 planeNormal = cross(sliceDir, viewDir);
				float3 tangent = cross(viewDir, planeNormal);
				float3 projectedNormal = viewNormal - planeNormal * dot(viewNormal, planeNormal);
				float projLength = length(projectedNormal);

				float cos_n = dot(normalize(projectedNormal), viewDir);
				float n = -sign(dot(projectedNormal, tangent)) * acos(cos_n);
				h = acos(h);
				h.x = n + max(-h.x - n, -HALF_PI);
				h.y = n + min(h.y - n, HALF_PI);
				//float bentAngle = (h.x + h.y) * 0.5;

				//BentNormal += viewDir * cos(bentAngle) - tangent * sin(bentAngle);
				Occlusion += projLength * IntegrateArc_CosWeight(h, n);
			}

			//BentNormal = normalize(normalize(BentNormal) - viewDir * 0.5);
			Occlusion = pow(saturate(Occlusion / (float)_SampleDirectionCount), _AOPower);
			return half4(depth01, Occlusion, 0, 0);
        }

		float _BlurSpread;
		float _BlurThreshold;
		
		float blurAO(float2 uv, float depth01)
		{
			float occlusion, allWeight;
			float depth = depth01 * _ProjectionParams.z;
			for(int x = -1; x <= 1; x++)
			{
				for(int y = -1; y <= 1; y++)
				{
					float2 _uv = uv + _TexelSize.xy * float2(x, y) * _BlurSpread;
					float4 _data = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, _uv);
					float _occlusion = _data.y;
					float _depth = _data.x * _ProjectionParams.z;
					float _weight = 1 - smoothstep(0, _BlurThreshold, abs(depth - _depth));
					allWeight += _weight;
					occlusion += _occlusion * _weight;
				}
			}
			occlusion /= allWeight;
			return occlusion;
		}

		half4 frag_blur (v2f i) : SV_Target
		{
			float4 data = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
			data.y = blurAO(i.uv, data.x);
			return data;
		}

		TEXTURE2D(_AOMap);
		sampler sampler_AOMap;
		float _MultiBounce;

		half4 frag (v2f i) : SV_Target
		{
			half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
			float4 data = SAMPLE_TEXTURE2D(_AOMap, sampler_AOMap, i.uv);
			float3 ao = lerp(data.yyy, MultiBounce(data.y, col.rgb), _MultiBounce);
#if _VIEW_ORIGIN
			return col;
#elif _VIEW_AO
			return half4(ao, 1);
#else
			col.rgb *= ao;
			return col;
#endif
		}
		ENDHLSL

        Cull Off ZWrite Off ZTest Always

        Pass
        {
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_AO
			ENDHLSL
        }

		Pass
        {
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_blur
			ENDHLSL
        }

		Pass
        {
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			ENDHLSL
        }
    }
}