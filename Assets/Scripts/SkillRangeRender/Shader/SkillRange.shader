Shader "Custom/SkillRange"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _HeightRange("Height Range", Range(0, 100)) = 100
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
         Pass
        {
            Name "SkillRange"
            Tags { "LightMode" = "SkillRender" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            float4 _TopCameraParams;  // (aspect, top, near, far)
            float3 _TopCameraPos;
            float _CameraHeight;
            float _HeightRange;
            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);
            TEXTURE2D(_SkillMask);
            SAMPLER(sampler_SkillMask);
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.uv;
                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, screenUV);
                // 1. 采样相机深度，重建世界坐标
                float cameraDepth = SampleSceneDepth(screenUV);  // 需要 DeclareDepthTexture.hlsl
                float2 posNDC = screenUV * 2.0 - 1.0;
                #if UNITY_UV_STARTS_AT_TOP
                posNDC.y = -posNDC.y;
                #endif
                float4 posCS = float4(posNDC, cameraDepth, 1.0);
                float4 worldPos4 = mul(UNITY_MATRIX_I_VP, posCS);
                float3 worldPos = worldPos4.xyz / worldPos4.w;
                float heightDiff = worldPos.y - _CameraHeight+18.0f;
                if(abs(heightDiff) > _HeightRange)
                {
                    return color;
                }
                // 2. 世界坐标 → 顶视图UV
                float2 topViewUV;
                topViewUV.x = (worldPos.x - _TopCameraPos.x) / (2.0 * _TopCameraParams.x) + 0.5;
                topViewUV.y = (worldPos.z - _TopCameraPos.z) / (2.0 * _TopCameraParams.y) + 0.5;
                
                // 3. 用顶视图UV采样 Mask
                float mask = SAMPLE_TEXTURE2D(_SkillMask, sampler_SkillMask, topViewUV).r;
                float3 skillcol = float3(1.0,0.2,0.2);
                float3 finalColor = lerp(color.rgb, skillcol, mask*0.5);
                return float4(finalColor,1.0);
            }
            ENDHLSL
        }
        Pass
        {
            Name "SkillRangeMask"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                return float4(1,1,1,1);
            }
            ENDHLSL
        }
    
    }
}
