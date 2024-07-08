using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class InstancingDrawFeature : ScriptableRendererFeature
{
    static readonly string RenderTag = "InstancingDraw";
    static int _PositionOffset = Shader.PropertyToID("_PositionOffset");

    public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

    class InstancingDrawPass : ScriptableRenderPass
    {
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = DrawInstancing();
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    InstancingDrawPass instancingDrawPass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isPreviewCamera || InstancingController.List.Count == 0)
            return;
        renderer.EnqueuePass(instancingDrawPass);
    }

    public override void Create()
    {
        name = "GPU Instancing Draw";
        instancingDrawPass = new InstancingDrawPass();
        instancingDrawPass.renderPassEvent = Event;
        for (int i = 0; i < InstancingController.List.Count; i++)
            InstancingController.List[i].RefreshInstancingView();
    }

    public static CommandBuffer DrawInstancing(int pass = 0)
    {
        CommandBuffer cmd = CommandBufferPool.Get(RenderTag);
        for (int i = 0; i < InstancingController.List.Count; i++)
        {
            InstancingController controller = InstancingController.List[i];
            if (controller == null || !controller.IsAvailable())
                continue;
            controller.material.SetVector(_PositionOffset, controller.PositionOffset);
            cmd.DrawMeshInstancedIndirect(controller.Data.UseMesh, 0, controller.material, pass, controller.argsBuffer);
        }
        return cmd;
    }
}