using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SkillRangeFeature : ScriptableRendererFeature
{
    [Serializable]
    public class SkillRangeSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material material;
    }
    SkillRangePass pass;
    public SkillRangeSettings settings = new SkillRangeSettings();
    public override void Create()
    {
        pass = new SkillRangePass(settings.material);
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderingData.cameraData.requiresDepthTexture = true;
        renderer.EnqueuePass(pass);
    }

    public class SkillRangePass :ScriptableRenderPass
    {
        public Material material;
        readonly int SkillRangeTexID = Shader.PropertyToID("_SkillRangeTex");
        RenderTargetIdentifier skillRangeTexTarget;
        public SkillRangePass(Material mat)
        {
            material = mat;
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
           CommandBuffer cmd =  CommandBufferPool.Get("SkillRange");
           RenderTextureDescriptor cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;
           cmd.GetTemporaryRT(SkillRangeTexID,cameraDescriptor);

           var cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
           cmd.Blit(cameraColorTarget, skillRangeTexTarget, material);
           cmd.Blit(skillRangeTexTarget, cameraColorTarget);
           cmd.ReleaseTemporaryRT(SkillRangeTexID);
           context.ExecuteCommandBuffer(cmd);
           CommandBufferPool.Release(cmd);
        }
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);
        }
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(SkillRangeTexID,cameraTextureDescriptor);
            skillRangeTexTarget = new RenderTargetIdentifier(SkillRangeTexID);
        }
    }
}
