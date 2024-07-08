Shader "Hidden/CommonPass"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "Queue" = "Geometry" "RenderType" = "Opaque" }

        HLSLINCLUDE
        #include "ShaderLibrary/CommonPass.hlsl"
        ENDHLSL

        Pass
        {
            Name "ShadowCaster"
            Tags{ "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex ShadowCasterVertex
            #pragma fragment NullFragment
            ENDHLSL
        }

        Pass
        {
            Name "CustomDepth"
            Tags{ "LightMode" = "CustomDepth" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex NullVertex
            #pragma fragment NullFragment
            ENDHLSL
        }

        Pass
        {
            Name "CustomViewNormal"
            Tags { "LightMode" = "CustomViewNormal" }
            ZWrite On ZTest LEqual Cull Back
            HLSLPROGRAM
            #pragma vertex DepthViewNormalVertex
            #pragma fragment ViewNormalFragment
            ENDHLSL
        }

        Pass
        {
            Name "CustomDepthNormal"
            Tags { "LightMode" = "CustomDepthNormal" }
            ZWrite On ZTest LEqual Cull Back
            HLSLPROGRAM
            #pragma vertex DepthViewNormalVertex
            #pragma fragment DepthViewNormalFragment
            ENDHLSL
        }
    }
}
//UniversalForward
//UniversalForwardOnly
//LightweightForward
//SRPDefaultUnlit