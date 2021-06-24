using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraHudPass : ScriptableRenderPass
{
    RenderTargetHandle tempRt;

    Material materialToBlit;

    public RenderTargetIdentifier CameraColorRt;

    public CameraHudPass(string profilerTag, RenderPassEvent renderPassEvent, Material hudMaterial)
    {
        this.renderPassEvent = renderPassEvent;
        materialToBlit = hudMaterial;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        cmd.GetTemporaryRT(tempRt.id, cameraTextureDescriptor);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("Camera HUD Pass");
        cmd.Clear();

        cmd.Blit(CameraColorRt, tempRt.Identifier(), materialToBlit, 0);
        cmd.Blit(tempRt.Identifier(), CameraColorRt);

        context.ExecuteCommandBuffer(cmd);

        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(tempRt.id);
    }
}
