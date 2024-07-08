#ifndef MATH_TOOLS_INCLUDED
#define MATH_TOOLS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

inline real ssign(real value)
{
    return step(0, value) * 2 - 1;
}

inline real pow2(real value)
{
    return value * value;
}

inline real dot2(real2 value)
{
    return dot(value, value);
}

inline real dot2(real3 value)
{
    return dot(value, value);
}

real aaStep(real compValue, real gradient)
{
    real halfChange = fwidth(gradient) / 2;
    //base the range of the inverse lerp on the change over one pixel
    real lowerEdge = compValue - halfChange;
    real upperEdge = compValue + halfChange;
    //do the inverse interpolation
    real stepped = (gradient - lowerEdge) / (upperEdge - lowerEdge);
    stepped = saturate(stepped);
    return stepped;
}

inline real invLerp(real from, real to, real value)
{
    return (value - from) / (to - from);
}

inline real2 invLerp(real2 from, real2 to, real2 value)
{
    return (value - from) / (to - from);
}

inline real3 invLerp(real3 from, real3 to, real3 value)
{
    return (value - from) / (to - from);
}

inline real4 invLerp(real4 from, real4 to, real4 value)
{
    return (value - from) / (to - from);
}

inline real remap(real origFrom, real origTo, real targetFrom, real targetTo, real value)
{
    real rel = invLerp(origFrom, origTo, value);
    return lerp(targetFrom, targetTo, rel);
}

inline real2 remap(real2 origFrom, real2 origTo, real2 targetFrom, real2 targetTo, real2 value)
{
    real2 rel = invLerp(origFrom, origTo, value);
    return lerp(targetFrom, targetTo, rel);
}

inline real3 remap(real3 origFrom, real3 origTo, real3 targetFrom, real3 targetTo, real3 value)
{
    real3 rel = invLerp(origFrom, origTo, value);
    return lerp(targetFrom, targetTo, rel);
}

inline real4 remap(real4 origFrom, real4 origTo, real4 targetFrom, real4 targetTo, real4 value)
{
    real4 rel = invLerp(origFrom, origTo, value);
    return lerp(targetFrom, targetTo, rel);
}

inline real2 toPolar(real2 cartesian)
{
    real distance = length(cartesian);
    real angle = atan2(cartesian.y, cartesian.x);
    return real2(angle / TWO_PI, distance);
}

inline real2 toCartesian(real2 polar)
{
    real2 cartesian;
    sincos(polar.x * TWO_PI, cartesian.y, cartesian.x);
    return cartesian * polar.y;
}

inline real2 rotate2D(real2 res, real angle)
{
    real s, c;
    sincos(angle, s, c);
    return real2(res.x * c - res.y * s, res.x * s + res.y * c);
}

inline real2 pixelUV(real2 uv, real tiling, real aspect = 1)
{
    real2 _tiling = real2(tiling * aspect, tiling);
    real2 _uv = floor(uv * _tiling) + .5;
    return _uv / _tiling;
}

real isLeftOfLine(real2 pos, real2 linePoint1, real2 linePoint2)
{
    real2 lineDirection = linePoint2 - linePoint1;
    real2 lineNormal = real2(-lineDirection.y, lineDirection.x);
    real2 toPos = pos - linePoint1;

    real side = dot(toPos, lineNormal);
    side = step(0, side);
    return side;
}

float Dither16(float alpha, float2 ScreenPosition)
{
    float2 uv = ScreenPosition * _ScreenParams.xy;
    float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    return alpha - DITHER_THRESHOLDS[index];
}

#endif