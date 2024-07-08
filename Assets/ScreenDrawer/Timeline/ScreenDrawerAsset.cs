using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Playables;
using ScreenDrawer;

namespace CustomTimeline
{
    [Serializable]
    public class ScreenDrawerAsset : PlayableAsset
    {
        public AnimationCurve curve = new AnimationCurve(new Keyframe(1, 1), new Keyframe(0, 0));
        public Setting setting = new Setting();

        public float start;
        public float end;

        public override double duration => 1;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<SingleScreenDrawerBehaviour>.Create(graph);
            return playable;
        }

        [Serializable]
        public class Setting
        {
            public ScreenDrawerFeature.EventType EventType;
            public Vector4 TilingOffset = new Vector4(1, 1, 0, 0);
            public bool DSR;
            [Range(0, 100)]
            public float DSRDepth = 10;
            [Range(-1, 1)]
            public float DSRFOVFactor = 1;
            public Vector3 DSRPositionFactor = Vector3.one;
            public Vector3 DSREularAngleFactor = Vector3.one;
        }
    }
}