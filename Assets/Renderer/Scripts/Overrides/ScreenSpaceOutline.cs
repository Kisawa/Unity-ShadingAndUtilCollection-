using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System;

namespace CustomVolumeComponent
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Custom/Outline", typeof(UniversalRenderPipeline))]
    public class ScreenSpaceOutline : VolumeComponent, IPostProcessComponent
    {
        public ScreenSpaceOutlineFeature.SettingVolume Setting = new ScreenSpaceOutlineFeature.SettingVolume();

        public bool IsActive() => Setting.OutlineScale.overrideState;

        public bool IsTileCompatible() => true;
    }
}