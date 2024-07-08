#ifndef COLOR_TOOLS_INCLUDED
#define COLOR_TOOLS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

half3 RotateHue(half3 color, float Offset)
{
    float3 hsv = RgbToHsv(color);
    float hue = hsv.x + Offset;
    hsv.x = RotateHue(hue, 0, 1);
    return HsvToRgb(hsv);
}

half3 Saturation(half3 color, half saturation)
{
    half lum = Luminance(color);
    return lum.xxx + saturation.xxx * (color - lum.xxx);
}

half3 Contrast(half3 color, half contrast)
{
    float midpoint = pow(0.5, 2.2);
    return (color - midpoint) * contrast + midpoint;
}

half3 Desaturation(half3 color)
{
    half3 grayXfer = half3(.3, .59, .11);
    half grayf = dot(color, grayXfer);
    return grayf;
}

#endif