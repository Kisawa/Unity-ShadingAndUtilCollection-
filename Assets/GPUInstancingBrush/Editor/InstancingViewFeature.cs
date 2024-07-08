using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GPUInstancingUtil
{
    public class InstancingViewFeature : ScriptableRendererFeature
    {
        static int _PositionOffset = Shader.PropertyToID("_PositionOffset");

        public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

        class InstancingViewPass : ScriptableRenderPass
        {
            static readonly string RenderTag = "InstancingView";

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (!CheckData())
                    return;
                CommandBuffer cmd = CommandBufferPool.Get(RenderTag);
                GPUInstancingBrush.Self.ViewMat.SetVector(_PositionOffset, Vector3.zero);
                cmd.DrawMeshInstancedIndirect(GPUInstancingBrush.Self.ViewMesh, 0, GPUInstancingBrush.Self.ViewMat, 0, GPUInstancingBrush.Self.argsBuffer);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        InstancingViewPass instancingViewPass;

        public override void Create()
        {
            instancingViewPass = new InstancingViewPass();
            instancingViewPass.renderPassEvent = Event;
            name = "InstancingView";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!CheckData() || renderingData.cameraData.isPreviewCamera)
                return;
            renderer.EnqueuePass(instancingViewPass);
        }

        public static bool CheckData()
        {
            if (GPUInstancingBrush.Self == null || GPUInstancingBrush.Self.Data == null || GPUInstancingBrush.Self.ViewMesh == null || GPUInstancingBrush.Self.ViewMat == null || GPUInstancingBrush.Self.instancingBuffer == null || GPUInstancingBrush.Self.argsBuffer == null)
                return false;
            return true;
        }
    }
}