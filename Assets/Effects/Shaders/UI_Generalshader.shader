// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "UI/Generalshader"
{
	Properties
	{
		[Toggle(_DisableFguiClip)]_DisableFguiClip("Disable Fgui Clip", Float) = 0.0
		[Toggle(_ManualCorrectGamma)] _ManualCorrectGamma("Manual Correct Gamma", Float) = 0
		[ASEBegin][Enum(UnityEngine.Rendering.BlendMode)]_Src("Src", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)]_Dst("Dst", Float) = 10
		[Enum(UnityEngine.Rendering.CullMode)]_cull("cull", Float) = 0
		[Enum(On,0,Off,1)]_ZwriteMode("ZwriteMode", Float) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode("ZTest", Float) = 4

		[Space(10)]
		[Toggle(_PARTICLE_CUSTOM_DATA_ON)]_ParticleCustomData_On("Particle Custom Data On", Float) = 0.0

		[Header(Surface)]
		[Gamma]_Exponsure("Exponsure", Range(1, 5)) = 1
		_MainTex("MainTex", 2D) = "white" {}
		[HDR]_Color0("Color 0", Color) = (1,1,1,1)
		_MainTexSpeedXYTimez("MainTexSpeedXYTimez", Vector) = (0,0,0,0)
		[KeywordEnum(None,R,G,B,A)]_IsGray("Is Gray)", Float) = 0
		_GrayStep("Gray Step", Range(0, 1)) = 0
		_GrayThreshold("Gray Threshold", Range(0, 1)) = 1
		_GrayFrom("Gray From", Color) = (1, 1, 1, 1)
		_GrayTo("Gray To", Color) = (0, 0, 0, 1)

		[Header(Disturbance)]
		[Toggle(_OPENDISTURBANCE_ON)]_OpenDisturbance("Open Disturbance", Float) = 0
		_DisturbanceTex("DisturbanceTex", 2D) = "white" {}
		_DistubanceMaskTex("DistubanceMaskTex", 2D) = "white" {}
		_DisturbanceTexSpeedXYTimezFlaotW("DisturbanceTexSpeedXYTimezFlaotW", Vector) = (0,0,0,0)

		[Header(Dissolve)]
		[Toggle(_OPENDISSOLVE_ON)]_OpenDissolve("Open Dissolve", Float) = 0
		_dissolveTex("dissolveTex", 2D) = "white" {}
		_DisslveTexSpeedXYTimezFlaotW("DisslveTexSpeedXYTimezFlaotW", Vector) = (0,0,0,0)

		[Header(Mask)]
		[KeywordEnum(None,R,G,B,A)] _OpenMask("OpenMask", Float) = 0
		_MaskTex("MaskTex", 2D) = "white" {}
		_MaskTex_Speed_Time("MaskTex_Speed_Time", Vector) = (0,0,0,0)

		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector]_StencilComp("Stencil Comparison", Float) = 8
		[HideInInspector]_Stencil("Stencil ID", Float) = 0
		[HideInInspector]_StencilOp("Stencil Operation", Float) = 0
		[HideInInspector]_StencilWriteMask("Stencil Write Mask", Float) = 255
		[HideInInspector]_StencilReadMask("Stencil Read Mask", Float) = 255
		[HideInInspector]_ClipBox("Clip Box", Vector) = (-2, -2, 0, 0)
		[HideInInspector]_ClipSoftness("Clip Softness", Vector) = (0, 0, 0, 0)
	}

	SubShader
	{
		LOD 0
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
		
		Cull [_cull]
		AlphaToMask Off
		HLSLINCLUDE
		#pragma target 3.0
		ENDHLSL

		Stencil
		{
			Ref[_Stencil]
			Comp[_StencilComp]
			Pass[_StencilOp]
			ReadMask[_StencilReadMask]
			WriteMask[_StencilWriteMask]
		}

		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForward" }
			
			Blend [_Src] [_Dst], One One
			ZWrite [_ZwriteMode]
			ZTest [_ZTestMode]
			Offset 0,0
			ColorMask RGBA
			
			HLSLPROGRAM
			#pragma multi_compile_instancing
			#define ASE_SRP_VERSION 999999

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_FRAG_COLOR
			#pragma shader_feature_local _ISGRAY_NONE _ISGRAY_R _ISGRAY_G _ISGRAY_B _ISGRAY_A
			#pragma shader_feature_local_fragment _ManualCorrectGamma
			#pragma shader_feature_local _OPENDISTURBANCE_ON
			#pragma shader_feature_local _OPENDISSOLVE_ON
			#pragma shader_feature_local _OPENMASK_NONE _OPENMASK_R _OPENMASK_G _OPENMASK_B _OPENMASK_A
			#pragma multi_compile _ _DisableFguiClip
			#pragma multi_compile NOT_CLIPPED CLIPPED SOFT_CLIPPED ALPHA_MASK
			#pragma shader_feature_local _PARTICLE_CUSTOM_DATA_ON
			
			struct VertexInput
			{
				float4 vertex : POSITION;
				half4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
#if _PARTICLE_CUSTOM_DATA_ON
				float4 custom2 : TEXCOORD1;
#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				float4 ase_texcoord4 : TEXCOORD1;
#if _PARTICLE_CUSTOM_DATA_ON
				float4 custom2 : TEXCOORD2;
#endif
				half4 ase_color : COLOR;

#if !_DisableFguiClip
#if CLIPPED || SOFT_CLIPPED
				float2 fguiClipPos : TEXCOORD3;
#endif
#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			half4 _FresnelColor;
			float4 _MainTex_ST;
			half _GrayStep;
			half _GrayThreshold;
			half4 _GrayFrom;
			half4 _GrayTo;
			float4 _DisturbanceTexSpeedXYTimezFlaotW;
			float4 _DisturbanceTex_ST;
			float4 _DistubanceMaskTex_ST;
			half4 _Color0;
			float4 _DisslveTexSpeedXYTimezFlaotW;
			float4 _dissolveTex_ST;
			float4 _MaskTex_ST;
			float3 _Vector0;
			float3 _MainTexSpeedXYTimez;
			float3 _MaskTex_Speed_Time;

			half _Exponsure;
			float4 _ClipBox;
			float4 _ClipSoftness;
			CBUFFER_END
			sampler2D _MainTex;
			sampler2D _DisturbanceTex;
			sampler2D _DistubanceMaskTex;
			sampler2D _dissolveTex;
			sampler2D _MaskTex;

			inline half3 LinearToGammaSpace(half3 linRGB)
			{
				linRGB = max(linRGB, half3(0.h, 0.h, 0.h));
				return max(1.055h * pow(linRGB, 0.416666667h) - 0.055h, 0.h);
			}
						
			VertexOutput VertexFunction ( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				
				o.ase_texcoord4 = v.ase_texcoord;
#if _PARTICLE_CUSTOM_DATA_ON
				o.custom2 = v.custom2;
#endif
				o.ase_color = v.ase_color;
				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float4 positionCS = TransformWorldToHClip( positionWS );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif
				o.clipPos = positionCS;

#if !_DisableFguiClip
#if CLIPPED || SOFT_CLIPPED
				o.fguiClipPos = positionWS.xy * _ClipBox.zw + _ClipBox.xy;
#endif
#endif
				return o;
			}

			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}

			half4 frag ( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				float mulTime15 = _TimeParameters.x * _MainTexSpeedXYTimez.z;
				float2 appendResult13 = (float2(_MainTexSpeedXYTimez.x , _MainTexSpeedXYTimez.y));
				float2 uv_MainTex = IN.ase_texcoord4.xy * _MainTex_ST.xy + _MainTex_ST.zw;
#if _PARTICLE_CUSTOM_DATA_ON
				uv_MainTex += IN.custom2.xy;
#endif
				float2 panner4 = ( mulTime15 * appendResult13 + uv_MainTex);

#ifdef _OPENDISTURBANCE_ON
				float mulTime20 = _TimeParameters.x * _DisturbanceTexSpeedXYTimezFlaotW.z;
				float2 appendResult19 = (float2(_DisturbanceTexSpeedXYTimezFlaotW.x , _DisturbanceTexSpeedXYTimezFlaotW.y));
				float2 uv_DisturbanceTex = IN.ase_texcoord4.xy * _DisturbanceTex_ST.xy + _DisturbanceTex_ST.zw;
				float2 panner21 = ( mulTime20 * appendResult19 + uv_DisturbanceTex);
				float2 uv_DistubanceMaskTex = IN.ase_texcoord4.xy * _DistubanceMaskTex_ST.xy + _DistubanceMaskTex_ST.zw;
				half2 disturbance = (tex2D(_DisturbanceTex, panner21) * _DisturbanceTexSpeedXYTimezFlaotW.w * tex2D(_DistubanceMaskTex, uv_DistubanceMaskTex)).rg;
#else
				half2 disturbance = 0;
#endif

				half4 tex2DNode2 = tex2D( _MainTex, panner4 + disturbance);
#if !defined(_ISGRAY_NONE)
#if defined(_ISGRAY_R)
				half mainGray = smoothstep(_GrayStep, _GrayThreshold, tex2DNode2.r);
				tex2DNode2 = lerp(_GrayFrom, _GrayTo, mainGray);
#elif defined(_ISGRAY_G)
				half mainGray = smoothstep(_GrayStep, _GrayThreshold, tex2DNode2.g);
				tex2DNode2 = lerp(_GrayFrom, _GrayTo, mainGray);
#elif defined(_ISGRAY_B)
				half mainGray = smoothstep(_GrayStep, _GrayThreshold, tex2DNode2.b);
				tex2DNode2 = lerp(_GrayFrom, _GrayTo, mainGray);
#elif defined(_ISGRAY_A)
				half mainGray = smoothstep(_GrayStep, _GrayThreshold, tex2DNode2.a);
				tex2DNode2 = lerp(_GrayFrom, _GrayTo, mainGray);
#endif
#endif

				float4 texCoord62 = IN.ase_texcoord4;
				texCoord62.xy = IN.ase_texcoord4.xy * float2(0, 0) + float2(0, 0);

#ifdef _OPENDISSOLVE_ON
				float mulTime59 = _TimeParameters.x * _DisslveTexSpeedXYTimezFlaotW.z;
				float2 appendResult58 = (float2(_DisslveTexSpeedXYTimezFlaotW.x , _DisslveTexSpeedXYTimezFlaotW.y));
				float2 uv_dissolveTex = IN.ase_texcoord4.xy * _dissolveTex_ST.xy + _dissolveTex_ST.zw;
				float2 panner57 = ( mulTime59 * appendResult58 + uv_dissolveTex);
				half dissolve = saturate((tex2D(_dissolveTex, panner57).r + 1.0 + (texCoord62.z * -2.0)));
#else
				half dissolve = 1;
#endif

#if defined(_OPENMASK_NONE)
				half mask = 1;
#else
				float mulTime78 = _TimeParameters.x * _MaskTex_Speed_Time.z;
				float2 appendResult76 = (float2(_MaskTex_Speed_Time.x , _MaskTex_Speed_Time.y));
				float2 uv_MaskTex = IN.ase_texcoord4.xy * _MaskTex_ST.xy + _MaskTex_ST.zw;
				float2 panner72 = ( mulTime78 * appendResult76 + uv_MaskTex);
				half4 maskVal = tex2D(_MaskTex, panner72);
#if defined(_OPENMASK_R)
				half mask = maskVal.r;
#elif defined(_OPENMASK_G)
				half mask = maskVal.g;
#elif defined(_OPENMASK_B)
				half mask = maskVal.b;
#elif defined(_OPENMASK_A)
				half mask = maskVal.a;
#else
				half mask = 1;
#endif
#endif
				half temp_output_30_0 = ( IN.ase_color.a * dissolve * tex2DNode2.a *  mask);
				
				float3 Color = ( ( ( tex2DNode2 * IN.ase_color * _Color0 * texCoord62.w ) ) ).rgb;
				half Alpha = temp_output_30_0;
				half AlphaClipThreshold = 0.5;
				half AlphaClipThresholdShadow = 0.5;

				#ifdef _ALPHATEST_ON
					clip( Alpha - AlphaClipThreshold );
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif

				#ifdef ASE_FOG
					Color = MixFog( Color, IN.fogFactor );
				#endif
				
				Color *= _Exponsure;
#if _ManualCorrectGamma
				Color = LinearToGammaSpace(Color);
#endif

#if !_DisableFguiClip
#if CLIPPED
				float2 factor = abs(IN.fguiClipPos);
				//Alpha *= step(max(factor.x, factor.y), 1);
				clip(step(max(factor.x, factor.y), 1) - .01);
#endif

#if SOFT_CLIPPED
				float2 factor = float2(0, 0);
				if (IN.fguiClipPos.x < 0)
					factor.x = (1.0 - abs(IN.fguiClipPos.x)) * _ClipSoftness.x;
				else
					factor.x = (1.0 - IN.fguiClipPos.x) * _ClipSoftness.z;
				if (IN.fguiClipPos.y < 0)
					factor.y = (1.0 - abs(IN.fguiClipPos.y)) * _ClipSoftness.w;
				else
					factor.y = (1.0 - IN.fguiClipPos.y) * _ClipSoftness.y;
				Alpha *= clamp(min(factor.x, factor.y), 0.0, 1.0);
#endif
#endif

				return half4( Color, Alpha );
			}

			ENDHLSL
		}
	}

	CustomEditor "UnityEditor.ShaderGraph.PBRMasterGUI"
	Fallback "Hidden/InternalErrorShader"
}
/*ASEBEGIN
Version=18707
410;181;1920;982;674.6602;193.1831;1.354779;True;False
Node;AmplifyShaderEditor.CommentaryNode;41;-2618.456,46.4826;Inherit;False;1470.522;607.4742;Comment;8;23;16;21;18;20;19;24;71;Disturbance;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;39;-2205.885,630.0132;Inherit;False;2117.995;567.1876;Comment;12;61;60;59;58;57;37;34;33;31;32;38;62;Dissolve;1,1,1,1;0;0
Node;AmplifyShaderEditor.Vector4Node;24;-2568.456,354.0835;Inherit;False;Property;_DisturbanceTexSpeedXYTimezFlaotW;DisturbanceTexSpeedXYTimezFlaotW;9;0;Create;True;0;0;False;0;False;0,0,0,0;-1,1,0.1,0.085;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;60;-2017.196,882.7489;Inherit;False;Property;_DisslveTexSpeedXYTimezFlaotW;DisslveTexSpeedXYTimezFlaotW;11;0;Create;True;0;0;False;0;False;0,0,0,0;-1,0,0.5,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleTimeNode;20;-2113.275,361.7408;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;19;-2088.275,252.7408;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;18;-2342.695,96.48262;Inherit;False;0;16;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;40;-1998.227,-361.3073;Inherit;False;835.5981;400.0113;Comment;5;12;15;13;3;4;MainTexPanner;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;45;-1463.863,1215.17;Inherit;False;1398.862;532.1719;Comment;6;44;72;73;75;76;78;MaskTex;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleTimeNode;59;-1635.169,986.0686;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;61;-1864.589,720.8104;Inherit;False;0;31;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector3Node;12;-1948.227,-175.2959;Inherit;False;Property;_MainTexSpeedXYTimez;MainTexSpeedXYTimez;6;0;Create;True;0;0;False;0;False;0,0,0;-1,0,0.08;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.PannerNode;21;-1885.924,136.901;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;58;-1663.628,861.5938;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;15;-1621.227,-72.29602;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;13;-1596.227,-181.2959;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector3Node;75;-1255.348,1472.855;Inherit;False;Property;_MaskTex_Speed_Time;MaskTex_Speed_Time;13;0;Create;True;0;0;False;0;False;0,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TextureCoordinatesNode;3;-1659.629,-311.3073;Inherit;False;0;2;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;57;-1437.816,718.2289;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;37;-1152.49,1060.529;Inherit;False;Constant;_Float3;Float 3;7;0;Create;True;0;0;False;0;False;-2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;71;-1655.31,400.4953;Inherit;True;Property;_DistubanceMaskTex;DistubanceMaskTex;8;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;16;-1647.291,110.9157;Inherit;True;Property;_DisturbanceTex;DisturbanceTex;7;0;Create;True;0;0;False;0;False;-1;None;b71002ddfb01b23449030e544ae4450b;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;62;-1414.68,907.9885;Inherit;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;-1324.282,217.2064;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;34;-974.7673,1004.95;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;4;-1367.629,-256.3073;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;33;-1130.659,889.7852;Inherit;False;Constant;_Float1;Float 1;7;0;Create;True;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;73;-1092.212,1330.344;Inherit;False;0;44;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;31;-1178.522,687.1264;Inherit;True;Property;_dissolveTex;dissolveTex;10;0;Create;True;0;0;False;0;False;-1;None;3c2220205bf33b74e91fb46cd5858af1;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;76;-1017.348,1454.855;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;78;-996.8694,1553.957;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;32;-794.4901,756.5878;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;25;-889.9489,20.35116;Inherit;False;2;2;0;FLOAT2;0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.PannerNode;72;-744.9357,1364.927;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;42;-723.3613,-87.45652;Inherit;False;646.5601;708.7084;Comment;4;2;7;10;6;MainTex;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;92;-855.4902,-655.6816;Inherit;False;892.4407;554.0494;Fresnel;7;99;98;97;96;95;94;93;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SamplerNode;2;-673.3613,-37.45652;Inherit;True;Property;_MainTex;MainTex;4;0;Create;True;0;0;False;0;False;-1;None;62bc3d422ee9b8d469683c24487a2200;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;44;-438.3179,1316.361;Inherit;True;Property;_MaskTex;MaskTex;12;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;38;-570.7882,775.5605;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;7;-536.3613,152.5435;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;10;-566.4849,409.2519;Inherit;False;Property;_Color0;Color 0;5;1;[HDR];Create;True;0;0;False;0;False;1,1,1,1;2,2,2,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;94;-327.7767,-344.632;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;368.6721,382.1268;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;65;165.5435,-615.3348;Inherit;False;Property;_Dst;Dst;1;1;[Enum];Create;True;1;Option1;0;1;UnityEngine.Rendering.BlendMode;True;0;False;1;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;125;781.6727,191.0925;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;64;165.6716,-687.0715;Inherit;False;Property;_Src;Src;0;1;[Enum];Create;True;1;Option1;0;1;UnityEngine.Rendering.BlendMode;True;0;False;1;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;99;-634.5198,-589.4988;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;98;-572.7767,-313.6321;Inherit;False;Property;_FresnelColor;FresnelColor;16;1;[HDR];Create;True;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;97;-390.2357,-552.0445;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;95;-159.5747,-605.6816;Inherit;False;Constant;_Float0;Float 0;15;0;Create;True;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;100;299.1779,14.31774;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;79;164.5249,-541.7922;Inherit;False;Property;_cull;cull;2;1;[Enum];Create;True;1;Option1;0;1;UnityEngine.Rendering.CullMode;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;93;-805.4903,-569.4537;Inherit;False;Property;_Vector0;Vector 0;15;0;Create;True;0;0;False;0;False;0,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.StaticSwitch;96;-120.0495,-333.9926;Inherit;False;Property;_Fresnel;Fresnel;14;0;Create;True;0;0;False;0;False;0;0;0;True;;Toggle;2;0;Fresnel;Create;True;True;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;6;-200.5771,22.81151;Inherit;False;4;4;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;102;169.3526,-464.8229;Inherit;False;Property;_ZwriteMode;ZwriteMode;3;1;[Enum];Create;True;2;On;0;Off;1;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;124;1355.351,202.9762;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;True;0;False;-1;True;0;False;-1;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;120;1355.351,202.9762;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;True;0;False;-1;True;0;False;-1;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;True;0;False;-1;True;True;True;True;True;0;False;-1;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;0;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;122;1355.351,202.9762;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;True;0;False;-1;True;0;False;-1;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;False;False;True;0;False;-1;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;123;1355.351,202.9762;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;True;0;False;-1;True;0;False;-1;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;False;False;False;False;0;False;-1;False;False;False;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;121;1297.85,140.1339;Float;False;True;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;3;UI/Generalshader;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;8;False;False;False;False;False;False;False;False;True;0;False;-1;True;2;False;79;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;2;0;True;1;5;True;64;10;True;65;0;1;False;-1;10;False;-1;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;-1;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;2;False;102;True;3;False;-1;True;False;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;0;Hidden/InternalErrorShader;0;0;Standard;22;Surface;1;  Blend;0;Two Sided;0;Cast Shadows;0;  Use Shadow Threshold;0;Receive Shadows;1;GPU Instancing;1;LOD CrossFade;0;Built-in Fog;0;DOTS Instancing;0;Meta Pass;0;Extra Pre Pass;0;Tessellation;0;  Phong;0;  Strength;0.5,False,-1;  Type;0;  Tess;16,False,-1;  Min;10,False,-1;  Max;25,False,-1;  Edge Length;16,False,-1;  Max Displacement;25,False,-1;Vertex Position,InvertActionOnDeselection;1;0;5;False;True;False;True;False;False;;False;0
WireConnection;20;0;24;3
WireConnection;19;0;24;1
WireConnection;19;1;24;2
WireConnection;59;0;60;3
WireConnection;21;0;18;0
WireConnection;21;2;19;0
WireConnection;21;1;20;0
WireConnection;58;0;60;1
WireConnection;58;1;60;2
WireConnection;15;0;12;3
WireConnection;13;0;12;1
WireConnection;13;1;12;2
WireConnection;57;0;61;0
WireConnection;57;2;58;0
WireConnection;57;1;59;0
WireConnection;16;1;21;0
WireConnection;23;0;16;0
WireConnection;23;1;24;4
WireConnection;23;2;71;0
WireConnection;34;0;62;3
WireConnection;34;1;37;0
WireConnection;4;0;3;0
WireConnection;4;2;13;0
WireConnection;4;1;15;0
WireConnection;31;1;57;0
WireConnection;76;0;75;1
WireConnection;76;1;75;2
WireConnection;78;0;75;3
WireConnection;32;0;31;1
WireConnection;32;1;33;0
WireConnection;32;2;34;0
WireConnection;25;0;4;0
WireConnection;25;1;23;0
WireConnection;72;0;73;0
WireConnection;72;2;76;0
WireConnection;72;1;78;0
WireConnection;2;1;25;0
WireConnection;44;1;72;0
WireConnection;38;0;32;0
WireConnection;94;0;97;0
WireConnection;94;1;98;0
WireConnection;30;0;7;4
WireConnection;30;1;38;0
WireConnection;30;2;2;4
WireConnection;30;3;44;0
WireConnection;125;0;100;0
WireConnection;125;1;30;0
WireConnection;99;1;93;1
WireConnection;99;2;93;2
WireConnection;99;3;93;3
WireConnection;97;0;99;0
WireConnection;100;0;96;0
WireConnection;100;1;6;0
WireConnection;96;1;94;0
WireConnection;96;0;95;0
WireConnection;6;0;2;0
WireConnection;6;1;7;0
WireConnection;6;2;10;0
WireConnection;6;3;62;4
WireConnection;121;2;125;0
WireConnection;121;3;30;0
ASEEND*/
//CHKSM=6B2E3C36E50900C75BC82CA1FD9D1D06C511FC62