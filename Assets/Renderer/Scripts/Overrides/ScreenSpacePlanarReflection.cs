using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace CustomVolumeComponent
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Custom/Screen Space Planar Reflection", typeof(UniversalRenderPipeline))]
    public class ScreenSpacePlanarReflection : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter Resolution = new ClampedFloatParameter(1, .1f, 1);
        public FloatParameter HorizontalPlaneHeightWS = new FloatParameter(.05f);
        public ClampedFloatParameter Fade = new ClampedFloatParameter(.15f, 0, 1);
        public ClampedFloatParameter FadeHorizontalThreshold = new ClampedFloatParameter(5, 0, 10);
        public ClampedFloatParameter HeightStretch = new ClampedFloatParameter(0, 0, 3);
        public ClampedFloatParameter HeightStretchPower = new ClampedFloatParameter(1.5f, .1f, 3);
        public BoolParameter EnableHDR = new BoolParameter(true);
        public BoolParameter FillHoles = new BoolParameter(true);

        public bool IsActive() => Resolution.overrideState;

        public bool IsTileCompatible() => true;
    }
}