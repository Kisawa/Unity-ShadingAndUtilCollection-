using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using ScreenDrawer;

namespace CustomTimeline
{
    [TrackColor(.75f, 0, .75f)]
    [TrackClipType(typeof(ScreenDrawerAsset))]
    [TrackBindingType(typeof(AssetBase))]
    public class ScreenDrawerTrack : TrackAsset
    {
        public AssetBase asset;

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            PlayableDirector director = graph.GetResolver() as PlayableDirector;
            AssetBase _asset = asset;
            _asset = director.GetGenericBinding(this) as AssetBase;
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(_asset)))
                asset = _asset;
#endif
            var playable = ScriptPlayable<ScreenDrawerMixerBehaviour>.Create(graph, inputCount);
            ScreenDrawerMixerBehaviour behaviour = playable.GetBehaviour();
            behaviour.asset = _asset;
            behaviour.clipAssets = new ScreenDrawerAsset[inputCount];
            int index = 0;
            foreach (var item in GetClips())
            {
                ScreenDrawerAsset asset = item.asset as ScreenDrawerAsset;
                asset.start = (float)item.start;
                asset.end = (float)item.end;
                behaviour.clipAssets[index++] = asset;
            }
            return playable;
        }
    }
}