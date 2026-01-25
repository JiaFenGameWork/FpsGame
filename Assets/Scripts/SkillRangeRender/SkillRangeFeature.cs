using System;
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
        public float CameraHeight = 200f;
        public LayerMask layerMask = -1;
        public LayerMask layerMaskdepth = -1;
        public int texSize = 1024;
        public float aspect =200f;
        public float near = 0.1f;
        public float far = 1000f;
        public float top = 200f;
    }
    
    SkillRangePass pass;

    public SkillRangeSettings settings = new SkillRangeSettings();
    
    public override void Create()
    {
        pass = new SkillRangePass(settings);
        pass.renderPassEvent = settings.renderPassEvent;

    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(pass);
    }

    class SkillRangePass : ScriptableRenderPass
    {
        SkillRangeSettings settings;
        Transform _mainCamPosition;
        RTHandle cameraColorTarget;
        
        public SkillRangePass(SkillRangeSettings settings )
        {
            this.settings = settings;
        }
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {

        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            _mainCamPosition = Camera.main.transform;
            CommandBuffer cmd = CommandBufferPool.Get("SkillRange");
            var SkillId = Shader.PropertyToID("SkillMask");
            
            // TODO: 你的渲染逻辑
            RenderTargetIdentifier SkillMask = new RenderTargetIdentifier(SkillId);
            cmd.GetTemporaryRT(SkillId, settings.texSize, settings.texSize, 16, FilterMode.Point);
            Matrix4x4 V = CaculateTopCameraMatrix(_mainCamPosition.position,settings.CameraHeight);
            Matrix4x4 P = CaculateOrientationProjectionMatrix(settings.aspect,settings.near,settings.far,settings.top);
            cmd.SetViewProjectionMatrices(V, P);
            context.ExecuteCommandBuffer(cmd);

             Vector4 topCamPos = _mainCamPosition.position + new Vector3(0, settings.CameraHeight, 0);
             cmd.SetGlobalVector("_TopCameraPos", topCamPos);
             cmd.SetGlobalFloat("_CameraHeight", _mainCamPosition.position.y);
             // (aspect, top, near, far) - 用于重建世界坐标
             cmd.SetGlobalVector("_TopCameraParams", new Vector4(settings.aspect, settings.top, settings.near, settings.far));
             
            //cmd.SetProjectionMatrix(CaculateOrientationProjectionMatrix(settings.aspect,settings.near,settings.far,settings.top));
           cmd.SetRenderTarget(SkillMask);
           cmd.ClearRenderTarget(true, true, Color.clear);
             context.ExecuteCommandBuffer(cmd);
             cmd.Clear();
                        
                      // 1. 使用默认设置创建 DrawingSettings
            var sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = CreateDrawingSettings(new ShaderTagId("UniversalForward"), ref renderingData, sortingCriteria);
            drawingSettings.overrideMaterial = settings.material;
            drawingSettings.overrideMaterialPassIndex = 1;
            // 2. 补充常见的 Shader Pass Tag，确保能画出绝大多数物体
            drawingSettings.SetShaderPassName(1, new ShaderTagId("UniversalForwardOnly"));
            drawingSettings.SetShaderPassName(2, new ShaderTagId("SRPDefaultUnlit"));
            
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all, settings.layerMask);
            context.DrawRenderers(renderingData.cullResults,ref drawingSettings,ref filterSettings);
            
            // 设置 SkillMask 全局纹理
            cmd.SetGlobalTexture("_SkillMask", SkillId);
            
            // 恢复主相机矩阵
            cmd.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix,renderingData.cameraData.camera.projectionMatrix);
                int tempCameraColorId = Shader.PropertyToID("TempCameraColor");
            cmd.SetGlobalTexture("_BlitTexture",cameraColorTarget);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            RenderTextureDescriptor cameraColorDesc = renderingData.cameraData.cameraTargetDescriptor;

            cmd.GetTemporaryRT(tempCameraColorId,cameraColorDesc);
            cmd.Blit(cameraColorTarget,tempCameraColorId,settings.material,0);

            cmd.Blit(tempCameraColorId,cameraColorTarget);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            cmd.ReleaseTemporaryRT(tempCameraColorId);
            cmd.ReleaseTemporaryRT(SkillId);
            CommandBufferPool.Release(cmd);
        }
        public Matrix4x4 CaculateTopCameraMatrix(Vector3 position,float height)
        {
            Vector3 pos = position+new Vector3(0,height,0);
            // 相机看向 -Z 方向，所以如果想向下看（世界-Y），Z轴应该指向世界+Y
            Vector3 forward = Vector3.up;
            Vector3 up = Vector3.forward;      // 相机的上方向是世界+Z
            Vector3 right = Vector3.right;
            Matrix4x4 V = Matrix4x4.identity;
            V.SetRow(0,new Vector4(right.x,right.y,right.z,-Vector3.Dot(right,pos)));
            V.SetRow(1,new Vector4(up.x,up.y,up.z,-Vector3.Dot(up,pos)));
            V.SetRow(2,new Vector4(forward.x,forward.y,forward.z,-Vector3.Dot(forward,pos)));
            V.SetRow(3,new Vector4(0,0,0,1));
            return V;
        }
        public Matrix4x4 CaculateOrientationProjectionMatrix(float aspect,float near,float far,float top)
        {
            Matrix4x4 P = Matrix4x4.zero;
    P.m00 = 1f / aspect;
    P.m11 = 1f / top;
    // D3D 风格：z 从 [near, far] 映射到 [0, 1]
    P.m22 = -1f / (far - near);
    P.m23 = -near / (far - near);
    P.m33 = 1;
            return P;
        }
    }
}
