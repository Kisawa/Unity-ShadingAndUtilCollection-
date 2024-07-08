Shader "Project/1999Default"
{
    Properties
    {
        [Group(Surface)]_BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Group(Surface)]_BaseMap("Texture", 2D) = "white" {}
        [Group(Surface)]_BaseAnime("Base Anime", Vector) = (0, 0, 0, 0)
        [Group(Surface, Feature, _ALPHATEST_ON)]_AlphaClip("Alpha Clipping", Float) = 0.0
        [Group(Surface)]_Cutoff("Cutoff", Range(0.0, 1.0)) = 0.5

        [GroupFeature(Emission, _EMISSION)]_EmissionOn("Emission On", Float) = 0.0
        [Group(Emission, Feature, _EMISSION_PREMULTIPLY)]_PremultiplyAlbedo("Premultiply Albedo", Float) = 0.0
        [Group(Emission)][HDR]_EmissionColor("Color", Color) = (0,0,0)
        [Group(Emission)]_EmissionMap("Emission", 2D) = "white" {}
        [Group(Emission)]_EmissionAnime("Emission Anime", Vector) = (0, 0, 0, 0)

        [Group(Shadow, Feature, _RECEIVE_SHADOW)]_ReceiveMainLightShadow("Receive Main Light Shadow", Float) = 0.0
        [Group(Shadow, Feature, _RECEIVE_SHADOW_ADDITIONAL_LIGHTS)]_ReceiveAdditionalLightShadows("Receive Additional Light Shadows", Float) = 0.0
        [Group(Shadow)]_ShadowColor("Shadow Color", Color) = (0, 0, 0, 1)
        [Group(Shadow)]_ShadowStep("Shadow Step", Range(0, 1)) = 0
        [Group(Shadow)]_ShadowThreshold("Shadow Threshold", Range(0, 1)) = 1
        [Group(Shadow, TextureFeature, _SHADOW_NOISE_ON)]_ShadowNoiseMap("Shadow Noise Map", 2D) = "white" {}
        [Group(Shadow)]_ShadowNoiseSharpness("Shadow Noise Sharpness", Range(0, 1)) = .5
        [Group(Shadow)]_ShadowNoiseStep("Shadow Noise Step", Range(0, 1)) = 0
        [Group(Shadow)]_ShadowNoiseThreshold("Shadow Noise Threshold", Range(0, 1)) = 1
        [Group(Shadow)]_ShadowNoiseStrength("Shadow Noise Strength", Range(0, 1)) = 1

        [GroupFeature(Shadow Caster, _SHADOW_CASTER_ON)]_ShadowCaster("Shadow Caster", Float) = 0.0
        [Group(Shadow Caster)]_ShadowCasterClipMap("Clip Map", 2D) = "white" {}
        [Group(Shadow Caster)]_ShadowCasterCutoff("Cutoff", Range(0, 1)) = .5

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
        Tags { "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "ShaderLibrary/CommonPass.hlsl"
        #include "ShaderLibrary/TransformTools.hlsl"
        #include "ShaderLibrary/1999Default/1999DefaultUtil.hlsl"
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
            #pragma shader_feature_local_fragment _RECEIVE_SHADOW
            #pragma shader_feature_local_fragment _RECEIVE_SHADOW_ADDITIONAL_LIGHTS
            #pragma shader_feature_local_fragment _SHADOW_NOISE_ON
            #pragma vertex ForwardVertex
            #pragma fragment ForwardFragment
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{ "LightMode" = "ShadowCaster" }
            Cull[_Cull] ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma shader_feature_local_fragment _SHADOW_CASTER_ON
            #pragma vertex ShadowCasterVertex
            #pragma fragment ShadowCasterFragment
            ENDHLSL
        }
    }
}
