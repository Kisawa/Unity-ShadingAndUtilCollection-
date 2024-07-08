using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using ScreenDrawer;

namespace CustomTimeline
{
    public class ScreenDrawerMixerBehaviour : PlayableBehaviour
    {
        static readonly int _AnimeFlip = Shader.PropertyToID("_AnimeFlip");
        static readonly int _BaseInvVP = Shader.PropertyToID("_BaseInvVP");
        static readonly int _BaseZBufferParams = Shader.PropertyToID("_BaseZBufferParams");
        static readonly int _VP = Shader.PropertyToID("_VP");
        static readonly int _DSRDepth = Shader.PropertyToID("_DSRDepth");
        static readonly string _DSR = "_DSR";

        public AssetBase asset;
        public ScreenDrawerAsset[] clipAssets { get; set; }

        ScreenDrawerFeature screenDrawerFeature => ScreenDrawerFeature.Self;

        Route<ScreenDrawerAsset.Setting> settingCache = new Route<ScreenDrawerAsset.Setting>(null);
        Route<float> factorCache = new Route<float>(0);
        Route<DrawEventType> drawEventType = new Route<DrawEventType>(DrawEventType.None);
        Camera cam;
        float originFOV;
        Vector3 originPosition;
        Vector3 originEularAngle;

        public override void OnGraphStart(Playable playable)
        {
            base.OnGraphStart(playable);
            SetActive(true);
            if (asset == null)
                return;
            asset.Enable();
            cam = Camera.main;
            if (cam != null && asset.material != null)
            {
                originFOV = cam.fieldOfView;
                originPosition = cam.transform.position;
                originEularAngle = cam.transform.eulerAngles;
                Matrix4x4 BaseVP = CalcVP(cam, originPosition, originEularAngle, originFOV);
                Matrix4x4 BaseInvVP = BaseVP.inverse;
                Vector4 BaseZBufferParams = CalcZBufferParam(cam);
                asset.material.SetMatrix(_BaseInvVP, BaseInvVP);
                asset.material.SetVector(_BaseZBufferParams, BaseZBufferParams);
            }
            if (screenDrawerFeature == null)
                return;
            if (screenDrawerFeature.Contains(asset, ScreenDrawerFeature.EventType.Before))
                drawEventType.origin = DrawEventType.Before;
            else if (screenDrawerFeature.Contains(asset, ScreenDrawerFeature.EventType.Between))
                drawEventType.origin = DrawEventType.Between;
            else if (screenDrawerFeature.Contains(asset, ScreenDrawerFeature.EventType.After))
                drawEventType.origin = DrawEventType.After;
            else
                drawEventType.origin = DrawEventType.None;
        }

        public override void OnGraphStop(Playable playable)
        {
            base.OnGraphStop(playable);
            SetActive(false);
            asset.Disable();
            if (asset.material != null)
                asset.material.DisableKeyword(_DSR);
            if (screenDrawerFeature == null)
                return;
            switch (drawEventType.Result)
            {
                case DrawEventType.After:
                    screenDrawerFeature.Unload(asset, ScreenDrawerFeature.EventType.After);
                    break;
                case DrawEventType.Between:
                    screenDrawerFeature.Unload(asset, ScreenDrawerFeature.EventType.Between);
                    break;
                case DrawEventType.Before:
                    screenDrawerFeature.Unload(asset, ScreenDrawerFeature.EventType.Before);
                    break;
            }
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            base.PrepareFrame(playable, info);
            ResetCache();
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            base.ProcessFrame(playable, info, playerData);
            SetFrameData(playable);
        }

        void SetFrameData(Playable playable)
        {
            if (asset == null)
                return;
            float time = (float)playable.GetTime();
            for (int i = 0; i < clipAssets.Length; i++)
            {
                ScreenDrawerAsset clipAsset = clipAssets[i];
                if (time <= clipAsset.start)
                    continue;
                float weight = playable.GetInputWeight(i);
                Playable _playable = playable.GetInput(i);
                if (time < clipAsset.end)
                    LerpSetting(CalcFactor(_playable), weight, clipAsset);
                else
                {
                    LerpSetting(1, 1, clipAsset);
                    if (i == clipAssets.Length - 1)
                        drawEventType.Result = DrawEventType.None;
                }
            }
            SetFrameData();
        }

        void SetFrameData()
        {
            if (asset == null)
                return;
            asset.tilingOffset.Val = Vector4.LerpUnclamped(asset.tilingOffset.OriginVal, settingCache.Result.TilingOffset, factorCache.Result);
            if (settingCache.Result.DSR && cam != null && asset.material != null)
            {
                asset.material.EnableKeyword(_DSR);
                asset.material.SetFloat(_DSRDepth, settingCache.Result.DSRDepth);
                float fov = Mathf.LerpUnclamped(originFOV, cam.fieldOfView, settingCache.Result.DSRFOVFactor);
                Vector3 position = LerpVector3(originPosition, cam.transform.position, settingCache.Result.DSRPositionFactor);
                Vector3 eularAngle = LerpVector3(originEularAngle, cam.transform.eulerAngles, settingCache.Result.DSREularAngleFactor);
                Matrix4x4 VP = CalcVP(cam, position, eularAngle, fov);
                asset.material.SetMatrix(_VP, VP);
            }
            else
                asset.material.DisableKeyword(_DSR);
            if (screenDrawerFeature == null)
                return;
            switch (drawEventType.origin)
            {
                case DrawEventType.None:
                    switch (drawEventType.Result)
                    {
                        case DrawEventType.After:
                            screenDrawerFeature.Setup(asset, ScreenDrawerFeature.EventType.After);
                            drawEventType.origin = DrawEventType.After;
                            break;
                        case DrawEventType.Before:
                            screenDrawerFeature.Setup(asset, ScreenDrawerFeature.EventType.Before);
                            drawEventType.origin = DrawEventType.Before;
                            break;
                        case DrawEventType.Between:
                            screenDrawerFeature.Setup(asset, ScreenDrawerFeature.EventType.Between);
                            drawEventType.origin = DrawEventType.Between;
                            break;
                    }
                    break;
                case DrawEventType.After:
                    switch (drawEventType.Result)
                    {
                        case DrawEventType.None:
                            screenDrawerFeature.Unload(asset, ScreenDrawerFeature.EventType.After);
                            drawEventType.origin = DrawEventType.None;
                            break;
                        case DrawEventType.Before:
                            screenDrawerFeature.Unload(asset, ScreenDrawerFeature.EventType.After);
                            screenDrawerFeature.Unload(asset, ScreenDrawerFeature.EventType.Between);
                            screenDrawerFeature.Setup(asset, ScreenDrawerFeature.EventType.Before);
                            drawEventType.origin = DrawEventType.Before;
                            break;
                        case DrawEventType.Between:
                            screenDrawerFeature.Unload(asset, ScreenDrawerFeature.EventType.After);
                            screenDrawerFeature.Unload(asset, ScreenDrawerFeature.EventType.Before);
                            screenDrawerFeature.Setup(asset, ScreenDrawerFeature.EventType.Between);
                            drawEventType.origin = DrawEventType.Between;
                            break;
                        case DrawEventType.After:
                            screenDrawerFeature.SetPriority(asset, ScreenDrawerFeature.EventType.After);
                            break;
                    }
                    break;
                case DrawEventType.Before:
                    switch (drawEventType.Result)
                    {
                        case DrawEventType.None:
                            screenDrawerFeature.Unload(asset, ScreenDrawerFeature.EventType.Before);
                            drawEventType.origin = DrawEventType.None;
                            break;
                        case DrawEventType.After:
                            screenDrawerFeature.Unload(asset, ScreenDrawerFeature.EventType.Before);
                            screenDrawerFeature.Unload(asset, ScreenDrawerFeature.EventType.Between);
                            screenDrawerFeature.Setup(asset, ScreenDrawerFeature.EventType.After);
                            drawEventType.origin = DrawEventType.After;
                            break;
                        case DrawEventType.Between:
                            screenDrawerFeature.Unload(asset, ScreenDrawerFeature.EventType.After);
                            screenDrawerFeature.Unload(asset, ScreenDrawerFeature.EventType.Before);
                            screenDrawerFeature.Setup(asset, ScreenDrawerFeature.EventType.Between);
                            drawEventType.origin = DrawEventType.Between;
                            break;
                        case DrawEventType.Before:
                            screenDrawerFeature.SetPriority(asset, ScreenDrawerFeature.EventType.Between);
                            break;
                    }
                    break;
                case DrawEventType.Between:
                    switch (drawEventType.Result)
                    {
                        case DrawEventType.None:
                            screenDrawerFeature.Unload(asset, ScreenDrawerFeature.EventType.Between);
                            drawEventType.origin = DrawEventType.None;
                            break;
                        case DrawEventType.After:
                            screenDrawerFeature.Unload(asset, ScreenDrawerFeature.EventType.Before);
                            screenDrawerFeature.Unload(asset, ScreenDrawerFeature.EventType.Between);
                            screenDrawerFeature.Setup(asset, ScreenDrawerFeature.EventType.After);
                            drawEventType.origin = DrawEventType.After;
                            break;
                        case DrawEventType.Before:
                            screenDrawerFeature.Unload(asset, ScreenDrawerFeature.EventType.After);
                            screenDrawerFeature.Unload(asset, ScreenDrawerFeature.EventType.Between);
                            screenDrawerFeature.Setup(asset, ScreenDrawerFeature.EventType.Before);
                            drawEventType.origin = DrawEventType.Before;
                            break;
                        case DrawEventType.Between:
                            screenDrawerFeature.SetPriority(asset, ScreenDrawerFeature.EventType.Between);
                            break;
                    }
                    break;
            }
        }

        void LerpSetting(float factor, float weight, ScreenDrawerAsset clipAsset)
        {
            switch (clipAsset.setting.EventType)
            {
                case ScreenDrawerFeature.EventType.After:
                    drawEventType.Result = DrawEventType.After;
                    break;
                case ScreenDrawerFeature.EventType.Between:
                    drawEventType.Result = DrawEventType.Between;
                    break;
                case ScreenDrawerFeature.EventType.Before:
                    drawEventType.Result = DrawEventType.Before;
                    break;
            }
            if (factorCache.origin == 1)
                factorCache.Result = 0;
            if (factorCache.origin == 0 || factorCache.origin == 1)
            {
                settingCache.Result.TilingOffset = Vector4.zero;
                settingCache.Result.DSR = false;
                settingCache.Result.DSRDepth = 0;
                settingCache.Result.DSRFOVFactor = 0;
                settingCache.Result.DSRPositionFactor = Vector3.zero;
                settingCache.Result.DSREularAngleFactor = Vector3.zero;
            }
            float _weight = !factorCache.NoMixer ? weight : 1;
            factorCache.origin = clipAsset.curve.Evaluate(factor);
            Vector4 tilingOffset = clipAsset.setting.TilingOffset;
            settingCache.origin.TilingOffset = tilingOffset;
            settingCache.origin.DSR = clipAsset.setting.DSR;
            settingCache.origin.DSRDepth = clipAsset.setting.DSRDepth;
            settingCache.origin.DSRFOVFactor = clipAsset.setting.DSRFOVFactor;
            Vector3 DSRPositionFactor = clipAsset.setting.DSRPositionFactor;
            settingCache.origin.DSRPositionFactor = DSRPositionFactor;
            settingCache.origin.DSREularAngleFactor = clipAsset.setting.DSREularAngleFactor;
            if (_weight == 1)
                ConformCacheRoute();
            else
            {
                factorCache.Result += factorCache.origin * _weight;
                settingCache.Result.TilingOffset += settingCache.origin.TilingOffset * _weight;
                if (_weight > .5f)
                    settingCache.Result.DSR = settingCache.origin.DSR;
                settingCache.Result.DSRDepth += settingCache.origin.DSRDepth * _weight;
                settingCache.Result.DSRFOVFactor += settingCache.origin.DSRFOVFactor * _weight;
                settingCache.Result.DSRPositionFactor += settingCache.origin.DSRPositionFactor * _weight;
                settingCache.Result.DSREularAngleFactor += settingCache.origin.DSREularAngleFactor * _weight;
            }
        }

        void ResetCache()
        {
            factorCache.origin = 0;
            factorCache.Result = 0;
            drawEventType.Result = DrawEventType.None;
            settingCache.NoMixer = false;
            
            settingCache.origin.TilingOffset = Vector4.zero;
            settingCache.origin.DSR = false;
            settingCache.origin.DSRDepth = 0;
            settingCache.origin.DSRFOVFactor = 0;
            settingCache.origin.DSRPositionFactor = Vector3.zero;
            settingCache.origin.DSREularAngleFactor = Vector3.zero;

            settingCache.Result.TilingOffset = Vector4.zero;
            settingCache.Result.DSR = false;
            settingCache.Result.DSRDepth = 0;
            settingCache.Result.DSRFOVFactor = 0;
            settingCache.Result.DSRPositionFactor = Vector3.zero;
            settingCache.Result.DSREularAngleFactor = Vector3.zero;

            if (asset == null)
                return;
            asset.tilingOffset.Reset();
        }

        void ConformCacheRoute()
        {
            factorCache.Result = factorCache.origin;
            settingCache.Result.TilingOffset = settingCache.origin.TilingOffset;
            settingCache.Result.DSR = settingCache.origin.DSR;
            settingCache.Result.DSRDepth = settingCache.origin.DSRDepth;
            settingCache.Result.DSRFOVFactor = settingCache.origin.DSRFOVFactor;
            settingCache.Result.DSRPositionFactor = settingCache.origin.DSRPositionFactor;
            settingCache.Result.DSREularAngleFactor = settingCache.origin.DSREularAngleFactor;
        }

        float CalcFactor(Playable playable)
        {
            double pt = playable.GetTime();
            double pd = playable.GetDuration();
            return (float)(pt / pd);
        }

        void SetActive(bool res)
        {
            if (asset == null)
                return;
            asset.tilingOffset.SetActive(res);
        }

        static Vector3 LerpVector3(Vector3 from, Vector3 to, Vector3 factor)
        {
            Vector3 res = Vector3.zero;
            res.x = Mathf.LerpUnclamped(from.x, to.x, factor.x);
            res.y = Mathf.LerpUnclamped(from.y, to.y, factor.y);
            res.z = Mathf.LerpUnclamped(from.z, to.z, factor.z);
            return res;
        }

        static Matrix4x4 CalcVP(Camera cam, Vector3 position, Vector3 eularAngle, float FOV)
        {
            Matrix4x4 V = Matrix4x4.TRS(position, Quaternion.Euler(eularAngle), Vector3.one).inverse;
            Matrix4x4 P = GL.GetGPUProjectionMatrix(Matrix4x4.Perspective(FOV, cam.aspect, cam.nearClipPlane, cam.farClipPlane), false);
            return P * V;
        }

        static Vector4 CalcZBufferParam(Camera cam)
        {
            if (cam == null)
                return Vector4.zero;
            Vector4 zBufferParams = new Vector4(); ;
            float f = cam.farClipPlane / cam.nearClipPlane;
            if (SystemInfo.usesReversedZBuffer)
            {
                zBufferParams.x = -1 + f;
                zBufferParams.y = 1;
                zBufferParams.z = zBufferParams.x / cam.farClipPlane;
                zBufferParams.w = 1 / cam.farClipPlane;
            }
            else
            {
                zBufferParams.x = 1 - f;
                zBufferParams.y = f;
                zBufferParams.z = zBufferParams.x / cam.farClipPlane;
                zBufferParams.w = zBufferParams.y / cam.farClipPlane;
            }
            return zBufferParams;
        }

        class Route<T> where T : new()
        {
            public T origin;
            public T Result;
            public bool NoMixer;

            public Route(T origin)
            {
                if (origin == null)
                    origin = new T();
                this.origin = origin;
                Result = new T();
                NoMixer = false;
            }
        }

        enum DrawEventType { None, After, Before, Between }
    }

    public class SingleScreenDrawerBehaviour : PlayableBehaviour { }
}