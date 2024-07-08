Shader "Project/StarRailDefault"
{
    Properties
    {
        [Group(BackFace Outline)] _BackFaceOutlineZOffset("BackFace Outline ZOffset", Range(-.1, .1)) = 0
        [Group(BackFace Outline, Feature, _NONE, _BACKFACE_OUTLINE_TANGENT, _BACKFACE_OUTLINE_COLOR)]_BackFaceOutlineDirectionChannel("BackFace Outline Direction Channel", Float) = 0.0
        [Group(BackFace Outline)]_BackFaceOutlineColor("BackFace Outline Color", Color) = (0, 0, 0, 1)
        [Group(BackFace Outline)]_BackFaceOutlineWidth("BackFace Outline Width", Range(0, .1)) = .001
        [Group(BackFace Outline)]_BackFaceOutlineFixMultiplier("BackFace Outline Fix Multiplier", Range(0, .1)) = .03

        [Group(Surface)]_ZOffset("ZOffset", Range(-.1, .1)) = 0
        [Group(Surface)] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Group(Surface)] _BackColor("Back Color", Color) = (.75, .75, .75, 1)
        [Group(Surface)]_BaseMap("Texture", 2D) = "white" {}
        [Group(Surface, Feature, _ALPHATEST_ON)]_AlphaClip("Alpha Clipping", Float) = 0.0
        [Group(Surface)]_Cutoff("Cutoff", Range(0, 1)) = 0.5

        [GroupFeature(Emission, _EMISSION)]_EmissionOn("Emission On", Float) = 0.0
        [Group(Emission, Feature, _EMISSION_PREMULTIPLY)]_PremultiplyAlbedo("Premultiply Albedo", Float) = 0.0
        [Group(Emission)][HDR]_EmissionColor("Color", Color) = (0, 0, 0)
        [Group(Emission)]_EmissionMap("Emission", 2D) = "white" {}

        [GroupFeature(Lighting, _LIGHTING_ON)]_LightOn("Lighting On", Float) = 0.0
        [Group(Lighting)]_SHHardness("SH Hardness", Range(0, 1)) = 1
        [Group(Lighting)]_GIStrength("GI Strength", Range(0, 1)) = 1
        [Group(Lighting)]_GIMixBaseColorUsage("GI Mix Base Color Usage", Range(0, 1)) = .5
        [Group(Lighting)]_MainLightColorUsage("Main Light Color Usage", Range(0, 1)) = 1

        [Group(Shadow)]_ReceiveShadowBias("Receive Shadow Bias", Float) = 0
        [Group(Shadow)]_ShadowColor("Shadow Color", Color) = (0, 0, 0, 1)
        [Group(Shadow)]_ShadeStep("Shade Step", Range(-1, 1)) = .5
        [Group(Shadow)]_ShadeSoftness("Shade Softness", Range(0, 1)) = 0

        [Group(Specular, Feature, _SPECULARHIGHLIGHTS_OFF)]_DisableSpecularHighlights("Disable Specular Highlights", Float) = 0.0
        [Group(Specular)][HDR]_SpecularColor("Specular", Color) = (1, 1, 1, 1)
        [Group(Specular)]_SpecularExpon("Specular Expon", Range(.1, 64)) = 1
        [Group(Specular)]_SpecularNonMetalKs("Non Metal Ks", Range(0, 2)) = .04
        [Group(Specular)]_SpecularMetalKs("Metal Ks", Range(0, 2)) = 1

        [GroupFeature(Rim, _RIM_ON)]_Rim("Rim", Float) = 0.0
        [Group(Rim)][HDR]_RimColor("Rim Color", Color) = (.3, .3, .3, 1)
        [Group(Rim)]_RimWidth("Rim Width", Range(0, 1)) = .1
        [Group(Rim)]_RimThreshold("Rim Threshold", Range(0, 1)) = .1

        [Group(Map, TextureFeature, _LIGHT_MAP_ON)][NoOffsetScale]_LightMap("Light Map (r-AO, g-ShadeThreshold, a-rampY&metal)", 2D) = "white" {}
        [Group(Map, TextureFeature, _MASK_MAP_ON)][NoOffsetScale]_MaskMap("Mask Map (a-Face SDF)", 2D) = "white" {}
        [Group(Map)]_AOStrength("AO Strength", Range(0, 1)) = .5
        [Group(Map)]_MetalThreshold("Metal Threshold", Range(0, 1)) = .52

        [GroupFeature(Stockings, _STOCKINGS_ON)]_StockingsMap("Stockings Map (r-Mask, g-Thickness, b-Detail)", 2D) = "black" {}
        [Group(Stockings, Ramp)]_StockingsRamp("Stockings Ramp", 2D) = "black" {}
        [Group(Stockings)]_StockingsPower("Stockings Power", Range(.1, 10)) = 1
        [Group(Stockings)]_StockingsHardness("Stockings Hardness", Range(-1, 1)) = 0
        [Group(Stockings)]_StockingsUsage("Stockings Usage", Range(0, 1)) = 1

        [PerRendererData]_HeadForwardDirection("Head Forward Direction", Vector) = (0, 0, 1, 1)
        [PerRendererData]_HeadRightDirection("Head Right Direction", Vector) = (1, 0, 0, 1)

        [Queue]_Queue("Queue", Float) = 1
        [QueueOffset]_QueueOffset("Queue Offset", Float) = 0
        [Cull]_Cull("Cul", Float) = 2
        [ZTest]_ZTest("ZTest", Float) = 4
        [ZWrite]_ZWrite("ZWrite", Float) = 1
        [SrcBlend]_SrcBlend("SrcBlend", Float) = 1
        [DstBlend]_DstBlend("DstBlend", Float) = 0
        [BlendOp]_BlendOp("BlendOp", Float) = 0

        [HideInInspector]_MainTex("Main Tex", 2D) = "white"{}
    }

    CustomEditor "GroupBasedMaterialEditor"

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "ShaderLibrary/CommonPass.hlsl"
        #include "ShaderLibrary/MathTools.hlsl"
        #include "ShaderLibrary/TransformTools.hlsl"
        #include "ShaderLibrary/ColorTools.hlsl"
        #include "ShaderLibrary/CustomTextureUtil.hlsl"
        #include "ShaderLibrary/StarRailDefault/StarRailDefaultUtil.hlsl"
        ENDHLSL

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull[_Cull] ZTest[_ZTest] ZWrite[_ZWrite] BlendOp[_BlendOp] Blend[_SrcBlend][_DstBlend], One One
            HLSLPROGRAM
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _EMISSION_PREMULTIPLY
            #pragma shader_feature_local_fragment _LIGHTING_ON
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _RIM_ON
            #pragma shader_feature_local_fragment _LIGHT_MAP_ON
            #pragma shader_feature_local_fragment _MASK_MAP_ON
            #pragma shader_feature_local_fragment _STOCKINGS_ON
            #pragma vertex ForwardVertex
            #pragma fragment ForwardFragment
            ENDHLSL
        }

        Pass
        {
            Name "BackFaceOutline"
            Tags{ "LightMode" = "BackFaceOutline" }
            Cull Front ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma shader_feature_local_vertex _ _BACKFACE_OUTLINE_TANGENT _BACKFACE_OUTLINE_COLOR
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{ "LightMode" = "ShadowCaster" }
            Cull[_Cull] ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma vertex ClipShadowCasterVertex
            #pragma fragment ClipNullFragment
            ENDHLSL
        }

        Pass
        {
            Name "CustomDepth"
            Tags{ "LightMode" = "CustomDepth" }
            Cull[_Cull] ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _RIM_ON
            #pragma vertex ClipVertex
            #pragma fragment ClipDepthFragment
            ENDHLSL
        }
    }
}