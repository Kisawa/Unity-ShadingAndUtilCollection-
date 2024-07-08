using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ScreenDrawer
{
    public class ScreenDrawerFeature : ScriptableRendererFeature
    {
        public static ScreenDrawerFeature Self;

        List<AssetBase> beforePassData = new List<AssetBase>();
        List<AssetBase> betweenPassData = new List<AssetBase>();
        List<AssetBase> afterPassData = new List<AssetBase>();

        SrceenDrawerPass afterPass;
        SrceenDrawerPass betweenPass;
        SrceenDrawerPass beforePass;

        public class SrceenDrawerPass : ScriptableRenderPass
        {
            static readonly string RenderTag = "Screen Drawer";

            List<AssetBase> data;

            public SrceenDrawerPass(List<AssetBase> data)
            {
                this.data = data;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                if (data == null)
                    return;
                for (int i = 0; i < data.Count; i++)
                {
                    AssetBase asset = data[i];
                    if (asset == null)
                        continue;
                    asset.UpdateFrame();
                }
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (data == null)
                    return;
                CommandBuffer cmd = CommandBufferPool.Get(RenderTag);
                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

                for (int i = 0; i < data.Count; i++)
                {
                    AssetBase asset = data[i];
                    if (asset == null)
                        continue;
                    DrawRenderer(cmd, asset);
                }
                cmd.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix, renderingData.cameraData.camera.projectionMatrix);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            void DrawRenderer(CommandBuffer cmd, AssetBase asset)
            {
                if (asset.material == null || asset.mesh == null)
                    return;
                Matrix4x4 matrix;
                switch (asset.anchor)
                {
                    case AnchorType.Top:
                        matrix = Matrix4x4.TRS(new Vector3(asset.tilingOffset.Val.z, asset.tilingOffset.Val.w + 1), Quaternion.identity, new Vector3(asset.tilingOffset.Val.x, asset.tilingOffset.Val.y));
                        break;
                    case AnchorType.Botton:
                        matrix = Matrix4x4.TRS(new Vector3(asset.tilingOffset.Val.z, asset.tilingOffset.Val.w - 1), Quaternion.identity, new Vector3(asset.tilingOffset.Val.x, asset.tilingOffset.Val.y));
                        break;
                    case AnchorType.Left:
                        matrix = Matrix4x4.TRS(new Vector3(asset.tilingOffset.Val.z - 1, asset.tilingOffset.Val.w), Quaternion.identity, new Vector3(asset.tilingOffset.Val.x, asset.tilingOffset.Val.y));
                        break;
                    case AnchorType.Right:
                        matrix = Matrix4x4.TRS(new Vector3(asset.tilingOffset.Val.z + 1, asset.tilingOffset.Val.w), Quaternion.identity, new Vector3(asset.tilingOffset.Val.x, asset.tilingOffset.Val.y));
                        break;
                    case AnchorType.TopLeft:
                        matrix = Matrix4x4.TRS(new Vector3(asset.tilingOffset.Val.z - 1, asset.tilingOffset.Val.w + 1), Quaternion.identity, new Vector3(asset.tilingOffset.Val.x, asset.tilingOffset.Val.y));
                        break;
                    case AnchorType.TopRight:
                        matrix = Matrix4x4.TRS(new Vector3(asset.tilingOffset.Val.z + 1, asset.tilingOffset.Val.w + 1), Quaternion.identity, new Vector3(asset.tilingOffset.Val.x, asset.tilingOffset.Val.y));
                        break;
                    case AnchorType.BottonLeft:
                        matrix = Matrix4x4.TRS(new Vector3(asset.tilingOffset.Val.z - 1, asset.tilingOffset.Val.w - 1), Quaternion.identity, new Vector3(asset.tilingOffset.Val.x, asset.tilingOffset.Val.y));
                        break;
                    case AnchorType.BottonRight:
                        matrix = Matrix4x4.TRS(new Vector3(asset.tilingOffset.Val.z + 1, asset.tilingOffset.Val.w - 1), Quaternion.identity, new Vector3(asset.tilingOffset.Val.x, asset.tilingOffset.Val.y));
                        break;
                    default:
                        matrix = Matrix4x4.TRS(new Vector3(asset.tilingOffset.Val.z, asset.tilingOffset.Val.w), Quaternion.identity, new Vector3(asset.tilingOffset.Val.x, asset.tilingOffset.Val.y));
                        break;
                }
                for (int i = 0; i < asset.Pass.Count; i++)
                {
                    int pass = asset.Pass[i];
                    if (pass >= asset.material.passCount)
                        continue;
                    cmd.DrawMesh(asset.mesh, matrix, asset.material, 0, pass);
                }
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.isPreviewCamera || renderingData.cameraData.isSceneViewCamera)
                return;
            if (beforePassData.Count > 0)
                renderer.EnqueuePass(beforePass);
            if (betweenPassData.Count > 0)
                renderer.EnqueuePass(betweenPass);
            if (afterPassData.Count > 0)
                renderer.EnqueuePass(afterPass);
        }

        public override void Create()
        {
            Self = this;
            name = "Screen Drawer Feature";
            beforePass = new SrceenDrawerPass(beforePassData);
            beforePass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            betweenPass = new SrceenDrawerPass(betweenPassData);
            betweenPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            afterPass = new SrceenDrawerPass(afterPassData);
            afterPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public void SetPriority(AssetBase asset, EventType type)
        {
            if (asset == null)
                return;
            switch (type)
            {
                case EventType.Before:
                    setPriority(beforePassData, asset);
                    break;
                case EventType.Between:
                    setPriority(betweenPassData, asset);
                    break;
                case EventType.After:
                    setPriority(afterPassData, asset);
                    break;
            }
        }

        void setPriority(List<AssetBase> data, AssetBase asset)
        {
            if (data == null || asset == null)
                return;
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i] == asset)
                {
                    data.RemoveAt(i);
                    data.Add(asset);
                    break;
                }
            }
        }

        public void Setup(AssetBase asset, EventType type)
        {
            if (asset == null)
                return;
            switch (type)
            {
                case EventType.Before:
                    if (!DataAny(beforePassData, x => x == asset))
                        beforePassData.Add(asset);
                    break;
                case EventType.Between:
                    if (!DataAny(betweenPassData, x => x == asset))
                        betweenPassData.Add(asset);
                    break;
                case EventType.After:
                    if (!DataAny(afterPassData, x => x == asset))
                        afterPassData.Add(asset);
                    break;
            }
        }

        public void Unload(AssetBase asset, EventType type)
        {
            if (asset == null)
                return;
            switch (type)
            {
                case EventType.Before:
                    {
                        int index = beforePassData.FindIndex(x => x == asset);
                        if (index > -1)
                            beforePassData.RemoveAt(index);
                    }
                    break;
                case EventType.Between:
                    {
                        int index = betweenPassData.FindIndex(x => x == asset);
                        if (index > -1)
                            betweenPassData.RemoveAt(index);
                    }
                    break;
                case EventType.After:
                    {
                        int index = afterPassData.FindIndex(x => x == asset);
                        if (index > -1)
                            afterPassData.RemoveAt(index);
                    }
                    break;
            }
        }

        public bool Contains(AssetBase asset, EventType type)
        {
            switch (type)
            {
                case EventType.Before:
                    return DataAny(beforePassData, x => x == asset);
                case EventType.Between:
                    return DataAny(betweenPassData, x => x == asset);
                case EventType.After:
                    return DataAny(afterPassData, x => x == asset);
            }
            return false;
        }

        bool DataAny(List<AssetBase> assets, Func<AssetBase, bool> conditions)
        {
            for (int i = 0; i < assets.Count; i++)
            {
                AssetBase asset = assets[i];
                if(conditions == null || conditions.Invoke(asset))
                    return true;
            }
            return false;
        }

        public enum EventType { After, Before, Between }
    }
}