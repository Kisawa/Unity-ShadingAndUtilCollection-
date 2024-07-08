Shader "Project/SpineDefault" {
	Properties{
		[Toggle(_BILLBOARD)]_Billboard("Billboard", Float) = 1.0
		_Cutoff("Shadow alpha cutoff", Range(0,1)) = 0.1
		[NoScaleOffset] _MainTex("Main Texture", 2D) = "black" {}
		[Toggle(_STRAIGHT_ALPHA_INPUT)] _StraightAlphaInput("Straight Alpha Texture", Int) = 0
		[HideInInspector] _StencilRef("Stencil Reference", Float) = 1.0
		[HideInInspector][Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Stencil Comparison", Float) = 8 // Set to Always as default

			// Outline properties are drawn via custom editor.
			[HideInInspector] _OutlineWidth("Outline Width", Range(0,8)) = 3.0
			[HideInInspector] _OutlineColor("Outline Color", Color) = (1,1,0,1)
			[HideInInspector] _OutlineReferenceTexWidth("Reference Texture Width", Int) = 1024
			[HideInInspector] _ThresholdEnd("Outline Threshold", Range(0,1)) = 0.25
			[HideInInspector] _OutlineSmoothness("Outline Smoothness", Range(0,1)) = 1.0
			[HideInInspector][MaterialToggle(_USE8NEIGHBOURHOOD_ON)] _Use8Neighbourhood("Sample 8 Neighbours", Float) = 1
			[HideInInspector] _OutlineOpaqueAlpha("Opaque Alpha", Range(0,1)) = 1.0
			[HideInInspector] _OutlineMipLevel("Outline Mip Level", Range(0,3)) = 0

			[Header(Dpeth Trick)]
			[HDR]_DepthRimColor("Depth Rim Color", Color) = (.5, .5, .5, 1)
			_DepthRimOffset("Depth Rim Offset", Range(0, .1)) = 0
			_DepthRimSmooth("Depth Rim Smooth", Range(0, 1)) = 1
			_DepthDiffuseColor("Depth Diffuse Color", Color) = (0, 0, 0, 1)
			_DepthDiffuseOffset("Depth Diffuse Offset", Range(0, .1)) = 0
			_DepthDiffuseSmooth("Depth Diffuse Smooth", Range(0, 1)) = 1
	}

		SubShader{
			Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }

			Fog { Mode Off }
			Cull Off
			ZWrite Off
			Blend One OneMinusSrcAlpha, One One
			Lighting Off

			Stencil {
				Ref[_StencilRef]
				Comp[_StencilComp]
				Pass Keep
			}

			Pass {
				Name "Normal"

				CGPROGRAM
				#pragma shader_feature _ _BILLBOARD
				#pragma shader_feature _ _STRAIGHT_ALPHA_INPUT
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"
				#include "Lighting.cginc"
				#include "Runtime/spine-unity/Shaders/CGIncludes/Spine-Common.cginc"
				sampler2D _MainTex;
				sampler2D _CustomDepthTexture;
				
				half3 _DepthRimColor;
				float _DepthRimOffset;
				float _DepthRimSmooth;
				half3 _DepthDiffuseColor;
				float _DepthDiffuseOffset;
				float _DepthDiffuseSmooth;

				struct VertexInput {
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
					float4 vertexColor : COLOR;
				};

				struct VertexOutput {
					float4 pos : SV_POSITION;
					float2 uv : TEXCOORD0;
					float4 vertexColor : COLOR;
					float4 positionSS : TEXCOORD1;
					float3 positionWS : TEXCOORD2;
				};

				float3 ViewerBillboard(float3 positionOS)
				{
					float3 viewer = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1));
					float3 right = normalize(cross(float3(0, 1, 0), viewer));
					float3 up = normalize(cross(right, viewer));
					return right * positionOS.x - up * positionOS.y + viewer * positionOS.z;
				}
	
				#define SMOOTH_COUNT 16
				half3 CalcDepthRim(float2 screenUV, float3 positionWS, out float shade)
				{
					float depth = Linear01Depth(tex2D(_CustomDepthTexture, screenUV).r);
					float3 lightDirWS = UnityWorldSpaceLightDir(positionWS);
					float2 lightDirSS = normalize(UnityWorldSpaceViewDir(normalize(lightDirWS)).xy);

					float rim = 0;
					for (int i = 0; i < SMOOTH_COUNT; i++)
					{
						float offset = _DepthRimOffset / SMOOTH_COUNT * i;
						float offsetDepth = Linear01Depth(tex2D(_CustomDepthTexture, screenUV + lightDirSS * offset).r);
						rim += step(.1, abs(depth - offsetDepth));

						float invOffset = _DepthDiffuseOffset / SMOOTH_COUNT * i;
						float invOffsetDepth = Linear01Depth(tex2D(_CustomDepthTexture, screenUV - lightDirSS * invOffset).r);
						shade += step(.1, abs(depth - invOffsetDepth));
					}
					rim /= SMOOTH_COUNT;
					rim = smoothstep(.5 - _DepthRimSmooth * .5, .5 + _DepthRimSmooth, rim);
					shade /= SMOOTH_COUNT;
					shade = 1 - smoothstep(.5 - _DepthDiffuseSmooth * .5, .5 + _DepthDiffuseSmooth, shade);
					half3 rimCol = _DepthRimColor * rim * _LightColor0;
					
					return rimCol;
				}

				VertexOutput vert(VertexInput v) {
					VertexOutput o;
#if _BILLBOARD
					float3 positionOS = ViewerBillboard(v.vertex);
#else
					float3 positionOS = v.vertex;
#endif
					o.pos = UnityObjectToClipPos(positionOS);
					o.uv = v.uv;
					o.vertexColor = PMAGammaToTargetSpace(v.vertexColor);
					o.positionSS = ComputeScreenPos(o.pos);
					o.positionWS = mul(unity_ObjectToWorld, v.vertex);
					return o;
				}

				float4 frag(VertexOutput i) : SV_Target
				{
					float4 texColor = tex2D(_MainTex, i.uv);

					#if defined(_STRAIGHT_ALPHA_INPUT)
					texColor.rgb *= texColor.a;
					#endif

					float2 screenUV = i.positionSS.xy / i.positionSS.w;
					half shade = 1;
					half3 depthRim = CalcDepthRim(screenUV, i.positionWS, shade);
					texColor.rgb = texColor.rgb * _LightColor0 * lerp(_DepthDiffuseColor, 1, shade) + depthRim * texColor.a;

					return (texColor * i.vertexColor);
				}
				ENDCG
			}

			Pass {
				Name "Caster"
				Tags { "LightMode" = "ShadowCaster" }
				Offset 1, 1
				ZWrite On
				ZTest LEqual

				Fog { Mode Off }
				Cull Off
				Lighting Off

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_shadowcaster
				#pragma fragmentoption ARB_precision_hint_fastest
				#include "UnityCG.cginc"
				sampler2D _MainTex;
				fixed _Cutoff;

				struct VertexOutput {
					V2F_SHADOW_CASTER;
					float4 uvAndAlpha : TEXCOORD1;
				};

				VertexOutput vert(appdata_base v, float4 vertexColor : COLOR) {
					VertexOutput o;
					o.uvAndAlpha = v.texcoord;
					o.uvAndAlpha.a = vertexColor.a;
					TRANSFER_SHADOW_CASTER(o)
					return o;
				}

				float4 frag(VertexOutput i) : SV_Target {
					fixed4 texcol = tex2D(_MainTex, i.uvAndAlpha.xy);
					clip(texcol.a* i.uvAndAlpha.a - _Cutoff);
					SHADOW_CASTER_FRAGMENT(i)
				}
				ENDCG
			}

			Pass
			{
				Name "CustomDepth"
				Tags{ "LightMode" = "CustomDepth" }
				ZWrite On ZTest LEqual ColorMask 0 Cull Off
				HLSLPROGRAM
				#include "Assets/Renderer/Shaders/ShaderLibrary/CommonPass.hlsl"
				#pragma vertex vert
				#pragma fragment frag

				TEXTURE2D(_MainTex);
				SAMPLER(sampler_MainTex);

				CBUFFER_START(UnityPerMaterial)
				half _Cutoff;
				CBUFFER_END

				float3 ViewerBillboard(float3 positionOS)
				{
					float3 viewer = TransformWorldToObject(_WorldSpaceCameraPos);
					float3 right = normalize(cross(float3(0, 1, 0), viewer));
					float3 up = normalize(cross(right, viewer));
					return right * positionOS.x - up * positionOS.y + viewer * positionOS.z;
				}

				Varyings_UV vert(Attributes_UV input)
				{
					Varyings_UV output;
					float3 positionOS = ViewerBillboard(input.positionOS);
					VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS);
					output.positionCS = vertexInput.positionCS;
					output.uv = input.uv;
					return output;
				}

				void frag(Varyings_UV input)
				{
					half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
					clip(col.a - _Cutoff);
				}
				ENDHLSL
			}
		}
			CustomEditor "SpineShaderWithOutlineGUI"
}
